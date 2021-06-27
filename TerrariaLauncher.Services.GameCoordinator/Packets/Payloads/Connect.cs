using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads
{
    class Connect : PacketStructure
    {
        public override PacketOpCode OpCode => PacketOpCode.Connect;

        /// <summary>
        /// "Terraria" + Main.curRelease | e.g. "Terraria238"
        /// </summary>
        public string Version { get; set; }
    }
}
