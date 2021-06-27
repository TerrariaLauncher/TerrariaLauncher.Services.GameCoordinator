using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.DomainObjects;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Plugins
{
    class Bans : Plugin
    {
        TerrariaClientSocketEvents terrariaClientSocketEvents;
        PacketEvents packetEvents;
        ObjectPool<TerrariaPacket> terrariaPacketPool;
        TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Bans.BansClient bansClient;
        public Bans(
            TerrariaClientSocketEvents terrariaClientSocketEvents,
            PacketEvents packetEvents,
            ObjectPool<TerrariaPacket> terrariaPacketPool,
            TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Bans.BansClient bansClient)
        {
            this.terrariaClientSocketEvents = terrariaClientSocketEvents;
            this.packetEvents = packetEvents;
            this.terrariaPacketPool = terrariaPacketPool;
            this.bansClient = bansClient;
        }

        public override Task Load(CancellationToken cancellationToken = default)
        {
            this.terrariaClientSocketEvents.TerrariaClientSocketConnected.Register(this.OnTerrariaClientSocketConnected);
            return Task.CompletedTask;
        }

        public override Task Unload(CancellationToken cancellationToken = default)
        {
            this.terrariaClientSocketEvents.TerrariaClientSocketConnected.Deregister(this.OnTerrariaClientSocketConnected);
            return Task.CompletedTask;
        }

        public async Task OnTerrariaClientSocketConnected(Interceptor interceptor, TerrariaClientSocketConnectedEventArgs args)
        {
            var ipv4Notation = interceptor.TerrariaClient.IPEndPoint.Address.ToString();
            var checkBannedResponse = await this.bansClient.IsBannedAsync(new Protos.Services.GameCoordinator.Hub.CheckRequest()
            {
                IdentityType = BanIdentityType.IPv4,
                Identity = ipv4Notation
            }, cancellationToken: args.CancellationToken);
            if (!checkBannedResponse.Banned)
            {
                return;
            }

            var disconnectPacket = this.terrariaPacketPool.Get();
            var disconnectPayload = new Disconnect();
            disconnectPayload.Reason = new Packets.Payloads.Structures.NetworkText()
            {
                Mode = Packets.Payloads.Structures.NetworkText.ModeType.Literal,
                Text = $"Ticket #{checkBannedResponse.Ticket}: {checkBannedResponse.Reason}"
            };

            await disconnectPacket.SetPayload(PacketOrigin.Server, disconnectPayload, args.CancellationToken).ConfigureAwait(false);

            await interceptor.InterceptorChannels.TerrariaClientProcessed.Writer.WriteAsync(disconnectPacket, args.CancellationToken);
            args.ForcedToDisconnect = true;
        }

        public Task OnUUIDPacket(Interceptor interceptor, PacketHandlerArgs args)
        {
            
        }
    }
}
