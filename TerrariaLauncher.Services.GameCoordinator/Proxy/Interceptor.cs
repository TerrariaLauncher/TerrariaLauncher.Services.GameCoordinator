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
    class Interceptor
    {
        private TerrariaClient terrariaClient;
        private InstanceClient instanceClient;

        private ObjectPool<TerrariaPacket> terrariaPacketPool;
        private PacketEvents packetEvents;

        public Interceptor(
            TerrariaClient terrariaClient,
            InstanceClient instanceClient,
            ObjectPool<TerrariaPacket> terrariaPacketPool,
            PacketEvents packetEvents)
        {
            this.terrariaClient = terrariaClient;
            this.instanceClient = instanceClient;
            this.terrariaPacketPool = terrariaPacketPool;
            this.packetEvents = packetEvents;
        }

        public TerrariaClient TerrariaClient { get => this.terrariaClient; }
        public InstanceClient InstanceClient { get => this.instanceClient; }

        internal async Task ProcessPackets(CancellationToken cancellationToken)
        {
            Task processPacketsFromTerrariaClientTask = this.ProcessPacketsFromTerrariaClient(cancellationToken);
            Task processPacketsFromInstanceClientTask = this.ProcessPacketsFromInstanceClient(cancellationToken);

            await Task.WhenAll(processPacketsFromTerrariaClientTask, processPacketsFromInstanceClientTask);
        }

        private async Task ProcessPacketsFromTerrariaClient(CancellationToken cancellationToken)
        {
            while (await this.terrariaClient.ReceivingPacketChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (this.terrariaClient.ReceivingPacketChannel.Reader.TryRead(out var packet))
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
                            await this.instanceClient.SendingPacketChannel.Writer.WriteAsync(packet, cancellationToken);
                        }
                        catch
                        {
                            this.terrariaPacketPool.Return(packet);
                            throw;
                        }
                    }
                }
            }
        }

        private async Task ProcessPacketsFromInstanceClient(CancellationToken cancellationToken)
        {
            while (await this.instanceClient.ReceivingPacketChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (this.instanceClient.ReceivingPacketChannel.Reader.TryRead(out var packet))
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
                            await this.terrariaClient.SendingPacketChannel.Writer.WriteAsync(packet, cancellationToken);
                        }
                        catch
                        {
                            this.terrariaPacketPool.Return(packet);
                            throw;
                        }
                    }
                }
            }
        }
    }
}
