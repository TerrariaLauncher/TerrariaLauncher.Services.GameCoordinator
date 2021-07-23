using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.DomainObjects;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads;
using TerrariaLauncher.Services.GameCoordinator.Proxy;
using TerrariaLauncher.Services.GameCoordinator.Proxy.Events;

namespace TerrariaLauncher.Services.GameCoordinator.Plugins
{
    class CharacterNames : Plugin
    {
        class Player
        {
            public string Name { get; set; }
            public User User { get; set; }
        }

        private ConcurrentDictionary<TerrariaClient, string> characterNames = new ConcurrentDictionary<TerrariaClient, string>();

        PacketEvents packetEvents;
        TerrariaClientEvents terrariaClientSocketEvents;
        Server server;
        TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Players.PlayersClient playersClient;
        IConfiguration configuration;
        string gameCoordinatorId;

        public CharacterNames(
            PacketEvents packetEvents,
            TerrariaClientEvents terrariaClientSocketEvents,
            Server server,
            TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Players.PlayersClient playersClient,
            IConfiguration configuration)
        {
            this.packetEvents = packetEvents;
            this.terrariaClientSocketEvents = terrariaClientSocketEvents;
            this.server = server;
            this.playersClient = playersClient;
            this.configuration = configuration;

            this.gameCoordinatorId = this.configuration.GetValue<string>("Id");
        }

        public override Task Load(CancellationToken cancellationToken = default)
        {
            this.packetEvents.PlayerInfoHandlers.Register(this.OnPlayerInfoPacket);
            this.terrariaClientSocketEvents.TerrariaClientSocketDisconnected.Register(this.OnTerrariaClientSocketDisconnect);
            this.server.Stopped += this.Clear;
            return Task.CompletedTask;
        }

        public override Task Unload(CancellationToken cancellationToken = default)
        {
            this.packetEvents.PlayerInfoHandlers.Register(this.OnPlayerInfoPacket);
            this.terrariaClientSocketEvents.TerrariaClientSocketDisconnected.Deregister(this.OnTerrariaClientSocketDisconnect);
            this.server.Stopped -= this.Clear;
            return Task.CompletedTask;
        }

        internal async Task OnPlayerInfoPacket(Interceptor interceptor, PacketHandlerArgs args)
        {
            if (args.TerrariaPacket.Origin != PacketOrigin.Client) return;
            var syncPlayer = await args.TerrariaPacket.DeserializePayload<SyncPlayer>(args.CancellationToken);
            this.characterNames.TryAdd(interceptor.TerrariaClient, syncPlayer.Name);
            await this.playersClient.JoinAsync(new Protos.Services.GameCoordinator.Hub.JoinRequest()
            {
                Name = syncPlayer.Name,
                EndPoint = interceptor.TerrariaClient.IPEndPoint.ToString(),
                GameCoordinatorId = this.gameCoordinatorId
            });
        }

        internal Task OnTerrariaClientSocketDisconnect(TerrariaClient terrariaClient, TerrariaClientSocketDisconnectedEventArgs args)
        {
            this.characterNames.TryRemove(terrariaClient, out _);
            return Task.CompletedTask;
        }

        internal Task Clear()
        {
            this.characterNames.Clear();
            return Task.CompletedTask;
        }

        public string GetCharacterName(Interceptor interceptor)
        {
            return this.GetCharacterName(interceptor.TerrariaClient);
        }

        public string GetCharacterName(TerrariaClient interceptor)
        {
            if (!this.characterNames.TryGetValue(interceptor, out var characterName))
            {
                throw new KeyNotFoundException();
            }
            return characterName;
        }
    }
}
