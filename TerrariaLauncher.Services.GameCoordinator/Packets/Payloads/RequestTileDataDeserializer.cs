using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads
{
    class RequestTileDataDeserializer : StructureDeserializer<RequestTileData>
    {
        protected override async Task<RequestTileData> Implementation(PipeReader pipeReader, CancellationToken cancellationToken = default)
        {
            var readResult = await pipeReader.ReadAsync(cancellationToken);
            var buffer = readResult.Buffer;
            var structure = Parse(ref buffer);
            pipeReader.AdvanceTo(buffer.Start);
            return structure;
        }

        private static RequestTileData Parse(ref ReadOnlySequence<byte> buffer)
        {
            var reader = new SequenceReader<byte>(buffer);
            reader.TryReadLittleEndian(out Int32 X);
            reader.TryReadLittleEndian(out Int32 Y);
            buffer = reader.UnreadSequence;
            return new RequestTileData()
            {
                X = X,
                Y = Y
            };
        }
    }
}
