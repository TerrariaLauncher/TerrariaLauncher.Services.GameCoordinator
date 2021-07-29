using System;
using System.Collections.Generic;
using System.Linq;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads
{
    enum PlayerSpawnContext: byte
    {
        ReviveFromDeath,
        SpawningIntoWorld,
        RecallFromItem
    }

    class SpawnPlayer : PacketStructure
    {
        public override PacketOpCode OpCode => PacketOpCode.SpawnPlayer;

        public byte PlayerId { get; set; }
        public Int16 SpawnX { get; set; }
        public Int16 SpawnY { get; set; }
        public Int32 RespawnTimeRemaining { get; set; }
        public PlayerSpawnContext PlayerSpawnContext { get; set; }
    }
}
