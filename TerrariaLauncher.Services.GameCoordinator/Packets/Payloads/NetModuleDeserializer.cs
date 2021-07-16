using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Modules;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads
{
    class NetModuleDeserializer<TPayload> : StructureDeserializer<NetModule<TPayload>> where TPayload : class, IModuleStructure
    {
        IStructureDeserializer<TPayload> payloadDeserializer;
        public NetModuleDeserializer(IStructureDeserializer<TPayload> payloadDeserializer)
        {
            this.payloadDeserializer = payloadDeserializer;
        }

        protected override async Task<NetModule<TPayload>> Implementation(PipeReader pipeReader, CancellationToken cancellationToken)
        {
            var netModule = new NetModule<TPayload>();
            var readResult = await pipeReader.ReadAsync(cancellationToken);
            var buffer = readResult.Buffer;
            netModule.ModuleId = (NetModuleId)ParseModuleId(ref buffer);
            pipeReader.AdvanceTo(buffer.Start);
            netModule.Payload = await this.payloadDeserializer.Deserialize(pipeReader, cancellationToken).ConfigureAwait(false);
            return netModule;
        }

        private static uint ParseModuleId(ref ReadOnlySequence<byte> buffer)
        {
            var sequenceReader = new System.Buffers.SequenceReader<byte>(buffer);
            var temp = ArrayPool<byte>.Shared.Rent(2);
            try
            {
                sequenceReader.TryRead(out temp[0]);
                sequenceReader.TryRead(out temp[1]);
                buffer = buffer.Slice(sizeof(ushort));
                return BinaryPrimitives.ReadUInt16LittleEndian(temp.AsSpan(0, sizeof(ushort)));
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(temp);
            }
        }
    }
}
