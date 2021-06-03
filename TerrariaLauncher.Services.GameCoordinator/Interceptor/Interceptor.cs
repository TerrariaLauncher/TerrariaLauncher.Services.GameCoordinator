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
            var channelOptions = new BoundedChannelOptions(20)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            this.PacketChannelForTerrariaClient = Channel.CreateBounded<TerrariaPacket>(channelOptions);
            this.PacketChannelForInstanceClient = Channel.CreateBounded<TerrariaPacket>(channelOptions);
            this.ProcessedPacketChannelForTerrariaClient = Channel.CreateBounded<TerrariaPacket>(channelOptions);
            this.ProcessedPacketChannelForInstanceClient = Channel.CreateBounded<TerrariaPacket>(channelOptions);
        }

        public Channel<TerrariaPacket> PacketChannelForTerrariaClient { get; }
        public Channel<TerrariaPacket> PacketChannelForInstanceClient { get; }
        public Channel<TerrariaPacket> ProcessedPacketChannelForTerrariaClient { get; }
        public Channel<TerrariaPacket> ProcessedPacketChannelForInstanceClient { get; }
    }

    class Interceptor
    {
        private TerrariaClient terrariaClient;
        private InstanceClient instanceClient;

        private InterceptorChannels interceptorChannels;

        private ObjectPool<TerrariaPacket> terrariaPacketPool;
        private ObjectPool<PacketHandlerArgs> packetHandlerArgsPool;
        private PacketHandlers packetHandlers;

        public Interceptor(
            TerrariaClient terrariaClient,
            InstanceClient instanceClient,
            InterceptorChannels interceptorChannels,
            ObjectPool<TerrariaPacket> terrariaPacketPool,
            ObjectPool<PacketHandlerArgs> packetHandlerArgsPool,
            PacketHandlers packetHandlers)
        {
            this.terrariaClient = terrariaClient;
            this.instanceClient = instanceClient;
            this.interceptorChannels = interceptorChannels;
            this.terrariaPacketPool = terrariaPacketPool;
            this.packetHandlerArgsPool = packetHandlerArgsPool;
            this.packetHandlers = packetHandlers;
        }

        public TerrariaClient TerrariaClient { get => this.terrariaClient; }
        public InstanceClient InstanceClient { get => this.instanceClient; }
        public InterceptorChannels InterceptorChannels { get => this.interceptorChannels; }

        public async Task Loop(CancellationToken cancellationToken)
        {
            using (var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var inputLoopTask = this.InputLoop(cancellationTokenSource.Token);
                var outputLoopTask = this.OutputLoop(cancellationTokenSource.Token);
                var terrariaClientLoopTask = this.terrariaClient.Loop(cancellationTokenSource.Token);

                await Task.WhenAny(inputLoopTask, outputLoopTask, terrariaClientLoopTask);
                cancellationTokenSource.Cancel();

                await this.terrariaClient.Disconnect();
                this.interceptorChannels.PacketChannelForInstanceClient.Writer.Complete();
                this.interceptorChannels.PacketChannelForTerrariaClient.Writer.Complete();
                this.interceptorChannels.ProcessedPacketChannelForInstanceClient.Writer.Complete();
                this.interceptorChannels.ProcessedPacketChannelForTerrariaClient.Writer.Complete();

                this.interceptorChannels.PacketChannelForTerrariaClient.Writer.Complete();
                this.interceptorChannels.ProcessedPacketChannelForInstanceClient.Writer.Complete();
                this.interceptorChannels.ProcessedPacketChannelForTerrariaClient.Writer.Complete();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="terrariaPacket"></param>
        /// <returns>Return true if the packet should be skipped. Return false when packet should be send to server.</returns>
        private async Task<bool> OnPacketArrive(TerrariaPacket terrariaPacket, CancellationToken cancellationToken)
        {
            var args = this.packetHandlerArgsPool.Get();
            args.CancellationToken = cancellationToken;
            args.TerrariaPacket = terrariaPacket;

            await this.packetHandlers.Handle(this, args);
            var ignored = args.Ignored;
            this.packetHandlerArgsPool.Return(args);

            return ignored;
        }

        private async Task InputLoop(CancellationToken cancellationToken)
        {
            while (true)
            {
                var receivedPackets = this.interceptorChannels.PacketChannelForTerrariaClient.Reader.ReadAllAsync(cancellationToken);
                await foreach (var packet in receivedPackets)
                {
                    if (await this.OnPacketArrive(packet, cancellationToken))
                    {
                        // Return early, packet is ignored.
                        this.terrariaPacketPool.Return(packet);
                    }
                    else
                    {
                        await this.interceptorChannels.ProcessedPacketChannelForInstanceClient.Writer.WriteAsync(packet);
                    }
                }
            }
        }

        private async Task OutputLoop(CancellationToken cancellationToken)
        {
            while (true)
            {
                var receivedPackets = this.interceptorChannels.PacketChannelForInstanceClient.Reader.ReadAllAsync(cancellationToken);
                await foreach (var packet in receivedPackets)
                {
                    if (await this.OnPacketArrive(packet, cancellationToken))
                    {
                        this.terrariaPacketPool.Return(packet);
                    }
                    else
                    {
                        await this.interceptorChannels.ProcessedPacketChannelForTerrariaClient.Writer.WriteAsync(packet);
                    }
                }
            }
        }
    }
}
