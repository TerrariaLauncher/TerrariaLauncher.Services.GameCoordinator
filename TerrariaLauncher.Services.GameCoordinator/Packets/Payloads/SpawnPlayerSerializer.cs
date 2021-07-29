using System;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads
{
    class SpawnPlayerSerializer : StructureSerializer<SpawnPlayer>
    {
        protected override Task Implementation(SpawnPlayer structure, PipeWriter pipeWriter, CancellationToken cancellationToken = default)
        {
            var numBytes = sizeof(byte) + sizeof(Int16) * 2 + sizeof(Int32) + sizeof(PlayerSpawnContext);
            var span = pipeWriter.GetSpan(numBytes);
            
            span[0] = structure.PlayerId;
            span = span.Slice(sizeof(byte));

            BinaryPrimitives.WriteInt16LittleEndian(span, structure.SpawnX);
            span = span.Slice(sizeof(Int16));

            BinaryPrimitives.WriteInt16LittleEndian(span, structure.SpawnY);
            span = span.Slice(sizeof(Int16));

            BinaryPrimitives.WriteInt32LittleEndian(span, structure.RespawnTimeRemaining);
            span = span.Slice(sizeof(Int32));

            span[0] = (byte)structure.PlayerSpawnContext;

            pipeWriter.Advance(numBytes);

            return Task.CompletedTask;
        }
    }
}
