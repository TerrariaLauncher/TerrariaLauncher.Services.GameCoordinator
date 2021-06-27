using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Structures;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads
{
    class Disconnect: PacketStructure
    {
        public NetworkText Reason { get; set; }

        public override PacketOpCode OpCode => PacketOpCode.Disconnect;
    }
}
