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
    [PacketStructure(OpCode: PacketOpCode.Disconnect)]
    class Disconnect: PacketStructure
    {
        public override PacketOpCode OpCode => PacketOpCode.Disconnect;

        public NetworkText Reason { get; set; }
    }
}
