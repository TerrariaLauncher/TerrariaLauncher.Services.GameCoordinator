using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator
{
    class InterceptorChannels
    {
        public InterceptorChannels()
        {
            var channelOptions = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            this.TerrariaClientRaw = Channel.CreateBounded<TerrariaPacket>(channelOptions);
            this.TerrariaClientProcessed = Channel.CreateBounded<TerrariaPacket>(channelOptions);
            this.InstanceClientRaw = Channel.CreateBounded<TerrariaPacket>(channelOptions);
            this.InstanceClientProcessed = Channel.CreateBounded<TerrariaPacket>(channelOptions);
        }

        public Channel<TerrariaPacket> TerrariaClientRaw { get; }

        /// <summary>
        /// Packet written into this channel will be sent directly to connecting client.
        /// </summary>
        public Channel<TerrariaPacket> TerrariaClientProcessed { get; }

        public Channel<TerrariaPacket> InstanceClientRaw { get; }

        /// <summary>
        /// Packet written into this channel will be sent directly to connecting server.
        /// </summary>
        public Channel<TerrariaPacket> InstanceClientProcessed { get; }
    }

    class Interceptor
    {
        private TerrariaClient terrariaClient;
        private InstanceClient instanceClient;

        private InterceptorChannels interceptorChannels;

        private ObjectPool<TerrariaPacket> terrariaPacketPool;
        private PacketEvents packetEvents;
        private TerrariaClientSocketEvents terrariaClientSocketEvents;
        public Interceptor(
            TerrariaClient terrariaClient,
            InstanceClient instanceClient,
            InterceptorChannels interceptorChannels,
            ObjectPool<TerrariaPacket> terrariaPacketPool,
            PacketEvents packetEvents,
            TerrariaClientSocketEvents terrariaClientSocketEvents)
        {
            this.terrariaClient = terrariaClient;
            this.instanceClient = instanceClient;
            this.interceptorChannels = interceptorChannels;
            this.terrariaPacketPool = terrariaPacketPool;
            this.packetEvents = packetEvents;
            this.terrariaClientSocketEvents = terrariaClientSocketEvents;
        }

        public TerrariaClient TerrariaClient { get => this.terrariaClient; }
        public InstanceClient InstanceClient { get => this.instanceClient; }
        public InterceptorChannels InterceptorChannels { get => this.interceptorChannels; }

        internal async Task Loop(CancellationToken cancellationToken)
        {
            using (var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                try
                {
                    var forcedToDisconnect = await this.terrariaClientSocketEvents.OnTerrariaClientSocketConnected(this, cancellationTokenSource.Token);
                    
                    Task outputLoopTask = Task.CompletedTask;
                    Task inputLoopTask = Task.CompletedTask;
                    Task terrariaClientInputLoop = Task.CompletedTask;
                    if (!forcedToDisconnect)
                    {
                        outputLoopTask = this.OutputLoop(cancellationTokenSource.Token);
                        inputLoopTask = this.InputLoop(cancellationTokenSource.Token);
                        terrariaClientInputLoop = this.terrariaClient.InputLoop(cancellationTokenSource.Token);
                    }
                    Task terrariaClientOutputLoop = this.terrariaClient.OutputLoop(cancellationTokenSource.Token);

                    await Task.WhenAny(inputLoopTask, outputLoopTask, terrariaClientInputLoop, terrariaClientOutputLoop);

                    // Stop receiving and sending more packets with Terraria Client.
                    this.interceptorChannels.TerrariaClientRaw.Writer.TryComplete();
                    this.interceptorChannels.TerrariaClientProcessed.Writer.TryComplete();
                    // Stop receiving packet from Instance Client.
                    this.interceptorChannels.InstanceClientRaw.Writer.TryComplete();
                    await inputLoopTask;
                    await outputLoopTask;

                    // Allow Instance Client receive remaining processed packets.
                    this.interceptorChannels.InstanceClientProcessed.Writer.TryComplete();
                    await Task.WhenAny(
                        this.interceptorChannels.InstanceClientProcessed.Reader.Completion,
                        Task.Delay(1000));
                    cancellationTokenSource.Cancel();

                    this.terrariaClient.Disconnect();
                    this.instanceClient.Disconect();
                    await this.terrariaClientSocketEvents.OnTerrariaClientSocketDisconnected(this, cancellationTokenSource.Token);
                }
                finally
                {
                    this.interceptorChannels.InstanceClientRaw.Writer.TryComplete();
                    this.interceptorChannels.TerrariaClientRaw.Writer.TryComplete();
                    this.interceptorChannels.InstanceClientProcessed.Writer.TryComplete();
                    this.interceptorChannels.TerrariaClientProcessed.Writer.TryComplete();

                    // Return all packets retain in exchange channels to pool.
                    await foreach (var packet in this.interceptorChannels.InstanceClientRaw.Reader.ReadAllAsync())
                    {
                        this.terrariaPacketPool.Return(packet);
                    }
                    await foreach (var packet in this.interceptorChannels.TerrariaClientRaw.Reader.ReadAllAsync())
                    {
                        this.terrariaPacketPool.Return(packet);
                    }
                    await foreach (var packet in this.interceptorChannels.InstanceClientProcessed.Reader.ReadAllAsync())
                    {
                        this.terrariaPacketPool.Return(packet);
                    }
                    await foreach (var packet in this.interceptorChannels.TerrariaClientProcessed.Reader.ReadAllAsync())
                    {
                        this.terrariaPacketPool.Return(packet);
                    }
                }
            }
        }

        private async Task InputLoop(CancellationToken cancellationToken)
        {
            while (await this.interceptorChannels.TerrariaClientRaw.Reader.WaitToReadAsync(cancellationToken))
            {
                while (this.interceptorChannels.TerrariaClientRaw.Reader.TryRead(out var packet))
                {
                    if (await this.packetEvents.OnPacketReceived(this, packet, cancellationToken))
                    {
                        // Return the packet to pool because the packet is ignored or cancellation token is in cancel state.
                        this.terrariaPacketPool.Return(packet);
                    }
                    else
                    {
                        try
                        {
                            await this.interceptorChannels.InstanceClientProcessed.Writer.WriteAsync(packet, cancellationToken);
                        }
                        catch
                        {
                            this.terrariaPacketPool.Return(packet);
                        }
                    }
                }
            }
        }

        private async Task OutputLoop(CancellationToken cancellationToken)
        {
            while (await this.interceptorChannels.InstanceClientRaw.Reader.WaitToReadAsync(cancellationToken))
            {
                while (this.interceptorChannels.InstanceClientRaw.Reader.TryRead(out var packet))
                {
                    if (await this.packetEvents.OnPacketReceived(this, packet, cancellationToken))
                    {
                        // Return the packet to pool because the packet is ignored or cancellation token is in cancel state.
                        this.terrariaPacketPool.Return(packet);
                    }
                    else
                    {
                        try
                        {
                            await this.interceptorChannels.TerrariaClientProcessed.Writer.WriteAsync(packet, cancellationToken);
                        }
                        catch
                        {
                            this.terrariaPacketPool.Return(packet);
                        }
                    }
                }
            }
        }
    }
}
