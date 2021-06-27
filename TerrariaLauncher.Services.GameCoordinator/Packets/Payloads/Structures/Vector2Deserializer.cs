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

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Structures
{
    class Vector2Deserializer : StructureDeserializer<Vector2>
    {
        protected override async Task<Vector2> Implementation(PipeReader pipeReader, CancellationToken cancellationToken)
        {
            var readResult = await pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = readResult.Buffer;
            var (x, y) = this.ParseXY(ref buffer);
            var vector2 = new Vector2()
            {
                X = x,
                Y = y
            };
            pipeReader.AdvanceTo(buffer.Start);
            return vector2;
        }

        private (float X, float Y) ParseXY(ref ReadOnlySequence<byte> buffer)
        {
            var temp = ArrayPool<byte>.Shared.Rent(sizeof(float));
            try
            {
                var reader = new SequenceReader<byte>(buffer);
                reader.TryRead(out temp[0]);
                reader.TryRead(out temp[1]);
                reader.TryRead(out temp[2]);
                reader.TryRead(out temp[3]);
                float x = BinaryPrimitives.ReadSingleLittleEndian(temp);

                reader.TryRead(out temp[0]);
                reader.TryRead(out temp[1]);
                reader.TryRead(out temp[2]);
                reader.TryRead(out temp[3]);
                float y = BinaryPrimitives.ReadSingleLittleEndian(temp);

                buffer = reader.UnreadSequence;

                return (x, y);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(temp);
            }
        }
    }
}
