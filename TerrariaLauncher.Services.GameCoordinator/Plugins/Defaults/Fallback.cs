using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.DomainObjects;

namespace TerrariaLauncher.Services.GameCoordinator.Plugins
{
    class Fallback: Plugin
    {
        PacketEvents packetEvents;
        ObjectPool<TerrariaPacket> terrariaPacketPool;
        Instance entranceInstance;

        public Fallback(
            PacketEvents packetEvents,
            ObjectPool<TerrariaPacket> terrariaPacketPool)
        {
            this.packetEvents = packetEvents;
            this.terrariaPacketPool = terrariaPacketPool;
            this.entranceInstance = new Instance()
            {
                Id = 1,
                Host = "localhost",
                Port = 7776,
                Name = "Entrance"                
            };
        }
        
        public override Task Load(CancellationToken cancellationToken = default)
        {
            this.packetEvents.ConnectHandlers.Register(this.OnConnect);
            return Task.CompletedTask;
        }

        public override Task Unload(CancellationToken cancellationToken = default)
        {
            this.packetEvents.ConnectHandlers.Deregister(this.OnConnect);
            return Task.CompletedTask;
        }

        public async Task OnConnect(Interceptor sender, PacketHandlerArgs args)
        {
            // Only trigger when there is a connect packet from the client.
            // This only happens when the client connect to server for the first time.
            if (args.TerrariaPacket.Origin != PacketOrigin.Client)
            {
                return;
            }

            await sender.InstanceClient.Connect(this.entranceInstance, args.CancellationToken);
            _ = Task.Run(() => sender.InstanceClient.Loop(args.CancellationToken));
        }

        public async Task OnDisconnect(Interceptor sender, PacketHandlerArgs args)
        {
            if (args.TerrariaPacket.Origin != PacketOrigin.Server)
            {
                return;
            }

            // Prevent sending disconnect-packet to client.
            args.Ignored = true;
            args.Handled = true;

            // if (sender.InstanceClient.)

            sender.InstanceClient.Disconect();

            await sender.InstanceClient.Connect(this.entranceInstance, args.CancellationToken);
            _ = Task.Run(() => sender.InstanceClient.Loop(args.CancellationToken));
        }
    }
}
