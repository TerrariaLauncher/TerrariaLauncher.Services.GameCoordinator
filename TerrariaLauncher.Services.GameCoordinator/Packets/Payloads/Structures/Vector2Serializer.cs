using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Structures
{
    class Vector2Serializer : StructureSerializer<Vector2>
    {
        protected override Task Implementation(Vector2 structure, PipeWriter pipeWriter, CancellationToken cancellationToken = default)
        {
            var buffer = pipeWriter.GetSpan(sizeof(float) * 2);
            BinaryPrimitives.WriteSingleLittleEndian(buffer, structure.X);
            
            buffer = buffer.Slice(sizeof(float));
            BinaryPrimitives.WriteSingleLittleEndian(buffer, structure.Y);
            
            pipeWriter.Advance(sizeof(float) * 2);
            return Task.CompletedTask;
        }
    }
}
