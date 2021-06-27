using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads
{
    class NetModuleSerializer<TPayload> : StructureSerializer<NetModule<TPayload>> where TPayload : class, IStructure
    {
        IStructureSerializer<TPayload> payloadSerializer;
        public NetModuleSerializer(IStructureSerializer<TPayload> payloadSerializer)
        {
            this.payloadSerializer = payloadSerializer;
        }

        protected override async Task Implementation(NetModule<TPayload> structure, PipeWriter pipeWriter, CancellationToken cancellationToken = default)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(pipeWriter.GetSpan(sizeof(ushort)), (ushort)structure.ModuleId);
            pipeWriter.Advance(sizeof(ushort));
            await this.payloadSerializer.Serialize(structure.Payload as TPayload, pipeWriter, false, cancellationToken).ConfigureAwait(false);
        }
    }
}
