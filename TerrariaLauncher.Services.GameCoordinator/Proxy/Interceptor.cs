using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Pools;
using TerrariaLauncher.Services.GameCoordinator.Proxy.Events;

namespace TerrariaLauncher.Services.GameCoordinator.Proxy
{
    class Interceptor : IDisposable
    {
        private TerrariaClient terrariaClient;
        private InstanceClient instanceClient;

        private ObjectPool<TerrariaPacket> terrariaPacketPool;
        private PacketEvents packetEvents;
        InterceptorEvents interceptorEvents;

        Channel<TerrariaPacket> multiplex;

        public Interceptor(
            TerrariaClient terrariaClient,
            InstanceClient instanceClient,
            ObjectPool<TerrariaPacket> terrariaPacketPool,
            PacketEvents packetEvents,
            InterceptorEvents interceptorEvents)
        {
            this.terrariaClient = terrariaClient;
            this.instanceClient = instanceClient;
            this.terrariaPacketPool = terrariaPacketPool;
            this.packetEvents = packetEvents;
            this.interceptorEvents = interceptorEvents;

            this.multiplex = Channel.CreateUnbounded<TerrariaPacket>();
        }

        public TerrariaClient TerrariaClient { get => this.terrariaClient; }
        public InstanceClient InstanceClient { get => this.instanceClient; }

        internal async Task ProcessPackets(CancellationToken cancellationToken)
        {
            await this.interceptorEvents.OnStarted(this, default);

            Task takePacketsFromTerrariaClientTask = this.TakePacketsFromTerrariaClient(cancellationToken);
            Task takePacketsFromInstanceClientTask = this.TakePacketsFromInstanceClient(cancellationToken);
            Task processAndRoutePacketsTask = this.ProcessAndRoutePackets(cancellationToken);

            try
            {
                await Task.WhenAll(
                    takePacketsFromTerrariaClientTask,
                    takePacketsFromInstanceClientTask,
                    processAndRoutePacketsTask);
            }
            finally
            {
                await this.interceptorEvents.OnStopped(this, default);
            }
        }

        private async Task TakePacketsFromTerrariaClient(CancellationToken cancellationToken)
        {
            while (await this.terrariaClient.ReceivingPacketChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (this.terrariaClient.ReceivingPacketChannel.Reader.TryRead(out var packet))
                {
                    try
                    {
                        await this.multiplex.Writer.WriteAsync(packet, cancellationToken);
                    }
                    catch
                    {
                        this.terrariaPacketPool.Return(packet);
                        throw;
                    }
                }
            }
        }

        private async Task TakePacketsFromInstanceClient(CancellationToken cancellationToken)
        {
            while (await this.instanceClient.ReceivingPacketChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (this.instanceClient.ReceivingPacketChannel.Reader.TryRead(out var packet))
                {
                    try
                    {
                        await this.multiplex.Writer.WriteAsync(packet, cancellationToken);
                    }
                    catch
                    {
                        this.terrariaPacketPool.Return(packet);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Process packet from both client and server sequentially, one-by-one.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ProcessAndRoutePackets(CancellationToken cancellationToken)
        {
            while (await this.multiplex.Reader.WaitToReadAsync(cancellationToken))
            {
                while (this.multiplex.Reader.TryRead(out var packet))
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
                            switch (packet.Origin)
                            {
                                case PacketOrigin.Client:
                                    await this.instanceClient.SendingPacketChannel.Writer.WriteAsync(packet, cancellationToken);
                                    break;
                                case PacketOrigin.Server:
                                    await this.terrariaClient.SendingPacketChannel.Writer.WriteAsync(packet, cancellationToken);
                                    break;
                                default:
                                    this.terrariaPacketPool.Return(packet);
                                    break;
                            }
                        }
                        catch
                        {
                            this.terrariaPacketPool.Return(packet);
                            this.multiplex.Writer.TryComplete();
                            throw;
                        }
                    }
                }
            }
        }

        private bool disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    this.multiplex.Writer.TryComplete();
                    while (this.multiplex.Reader.TryRead(out var packet))
                    {
                        this.terrariaPacketPool.Return(packet);
                    }
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
