using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.DomainObjects;
using TerrariaLauncher.Services.GameCoordinator.Pools;
using TerrariaLauncher.Services.GameCoordinator.Proxy;
using TerrariaLauncher.Services.GameCoordinator.Proxy.Events;

namespace TerrariaLauncher.Services.GameCoordinator.Plugins
{
    class Fallback : Plugin
    {
        PacketEvents packetEvents;
        ObjectPool<TerrariaPacket> terrariaPacketPool;

        public Fallback(
            PacketEvents packetEvents,
            ObjectPool<TerrariaPacket> terrariaPacketPool)
        {
            this.packetEvents = packetEvents;
            this.terrariaPacketPool = terrariaPacketPool;
        }

        public override Task Load(CancellationToken cancellationToken = default)
        {            
            this.packetEvents.ConnectHandlers.Register(this.OnConnectPacket);
            this.packetEvents.DisconnectHandlers.Register(this.OnDisconnectPacket);
            return Task.CompletedTask;
        }

        public override Task Unload(CancellationToken cancellationToken = default)
        {
            this.packetEvents.ConnectHandlers.Deregister(this.OnConnectPacket);
            return Task.CompletedTask;
        }

        public async Task OnConnectPacket(Interceptor sender, PacketHandlerArgs args)
        {
            // Only trigger when there is a connect packet from the client.
            // This only happens when the client connect to server for the first time.
            if (args.TerrariaPacket.Origin != PacketOrigin.Client)
            {
                return;
            }

            await sender.InstanceClient.Connect("Entrance", args.CancellationToken);
        }

        public async Task OnDisconnectPacket(Interceptor sender, PacketHandlerArgs args)
        {
            if (args.TerrariaPacket.Origin != PacketOrigin.Server)
            {
                return;
            }

            // Prevent sending disconnect-packet to client.
            args.Ignored = true;
            args.Handled = true;

            await sender.InstanceClient.Connect("Entrance", args.CancellationToken);
        }
    }
}
