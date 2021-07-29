using System;
using System.Collections.Generic;
using System.Linq;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads
{
    class RequestTileData : PacketStructure
    {
        public override PacketOpCode OpCode => PacketOpCode.RequestTileData;

        /// <summary>
        /// Player spawn X.
        /// </summary>
        public Int32 X { get; set; }

        /// <summary>
        /// Player spawn Y.
        /// </summary>
        public Int32 Y { get; set; }
    }
}
