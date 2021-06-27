using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads;

namespace TerrariaLauncher.Services.GameCoordinator.Plugins
{
    class CharacterNames : Plugin
    {
        private ConcurrentDictionary<Interceptor, string> characterNames = new ConcurrentDictionary<Interceptor, string>();

        PacketEvents packetEvents;
        TerrariaClientSocketEvents terrariaClientSocketEvents;
        Server server;

        public CharacterNames(
            PacketEvents packetEvents,
            TerrariaClientSocketEvents terrariaClientSocketEvents,
            Server server)
        {
            this.packetEvents = packetEvents;
            this.terrariaClientSocketEvents = terrariaClientSocketEvents;
            this.server = server;
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
            this.characterNames.TryAdd(interceptor, syncPlayer.Name);
        }

        internal Task OnTerrariaClientSocketDisconnect(Interceptor interceptor, TerrariaClientSocketDisconnectedEventArgs args)
        {
            this.characterNames.TryRemove(interceptor, out _);
            return Task.CompletedTask;
        }

        internal Task Clear()
        {
            this.characterNames.Clear();
            return Task.CompletedTask;
        }

        public string GetCharacterName(Interceptor interceptor)
        {
            if (!this.characterNames.TryGetValue(interceptor, out var characterName))
            {
                throw new KeyNotFoundException();
            }
            return characterName;
        }
    }
}
