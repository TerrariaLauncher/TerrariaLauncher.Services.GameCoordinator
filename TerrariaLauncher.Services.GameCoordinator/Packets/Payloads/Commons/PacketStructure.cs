using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons
{
    abstract class PacketStructure : Structure, IPacketStructure
    {
        public abstract PacketOpCode OpCode { get; }
    }
}
