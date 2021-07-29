using System;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons
{
    class PacketStructureAttribute : Attribute
    {
        PacketOpCode opCode;
        public PacketStructureAttribute(PacketOpCode OpCode)
        {
            this.opCode = OpCode;
        }

        public PacketOpCode OpCode { get => this.opCode; }
    }
}
