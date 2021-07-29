using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads
{
    class SpawnPlayerDeserializer : StructureDeserializer<SpawnPlayer>
    {
        protected override async Task<SpawnPlayer> Implementation(PipeReader pipeReader, CancellationToken cancellationToken = default)
        {
            var readResult = await pipeReader.ReadAsync(cancellationToken);
            var buffer = readResult.Buffer;
            var structure = Parse(ref buffer);
            pipeReader.AdvanceTo(buffer.Start);
            return structure;
        }

        private static SpawnPlayer Parse(ref ReadOnlySequence<byte> buffer)
        {
            var reader = new SequenceReader<byte>(buffer);
            reader.TryRead(out byte playerId);
            reader.TryReadLittleEndian(out Int16 spawnX);
            reader.TryReadLittleEndian(out Int16 spawnY);
            reader.TryReadLittleEndian(out Int32 respawnTimeRemaining);
            reader.TryRead(out byte playerSpawnContext);
            
            buffer = reader.UnreadSequence;
            return new SpawnPlayer()
            {
                PlayerId = playerId,
                SpawnX = spawnX,
                SpawnY = spawnY,
                RespawnTimeRemaining = respawnTimeRemaining,
                PlayerSpawnContext = (PlayerSpawnContext)playerSpawnContext
            };
        }
    }
}
