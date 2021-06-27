using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator
{
    class TerrariaPacketPoolPolicy : IObjectPoolPolicy<TerrariaPacket>
    {
        IStructureSerializerDispatcher structureSerializerDispatcher;
        IStructureDeserializerDispatcher structureDeserializerDispatcher;
        public TerrariaPacketPoolPolicy(
            IStructureSerializerDispatcher structureSerializerDispatcher,
            IStructureDeserializerDispatcher structureDeserializerDispatcher)
        {
            this.structureSerializerDispatcher = structureSerializerDispatcher;
            this.structureDeserializerDispatcher = structureDeserializerDispatcher;
        }

        public int RetainedSize => 100;

        public TerrariaPacket Create()
        {
            return new TerrariaPacket(ArrayPool<byte>.Shared, this.structureSerializerDispatcher, this.structureDeserializerDispatcher);
        }

        public bool Return(TerrariaPacket instance)
        {
            // Set length to 0 meaning return internal buffer to array pool.
            instance.Length = 0;
            instance.Origin = PacketOrigin.Client;
            return true;
        }
    }
}
