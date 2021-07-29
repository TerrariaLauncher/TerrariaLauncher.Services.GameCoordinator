using System;
using System.Collections.Generic;
using System.Linq;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads
{
    class SetPlayerSlot : PacketStructure
    {
        public override PacketOpCode OpCode => PacketOpCode.SetPlayerSlot;

        public byte PlayerId { get; set; }
    }
}
