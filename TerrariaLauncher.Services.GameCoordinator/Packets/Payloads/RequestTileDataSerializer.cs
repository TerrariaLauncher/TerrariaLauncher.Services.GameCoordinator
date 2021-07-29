using System;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads
{
    class RequestTileDataSerializer : StructureSerializer<RequestTileData>
    {
        protected override Task Implementation(RequestTileData structure, PipeWriter pipeWriter, CancellationToken cancellationToken = default)
        {
            var numBytes = sizeof(Int32) * 2;
            var span = pipeWriter.GetSpan(numBytes);
            BinaryPrimitives.WriteInt32LittleEndian(span, structure.X);
            span = span.Slice(sizeof(Int32));
            BinaryPrimitives.WriteInt32LittleEndian(span, structure.Y);
            pipeWriter.Advance(numBytes);
            return Task.CompletedTask;
        }
    }
}
