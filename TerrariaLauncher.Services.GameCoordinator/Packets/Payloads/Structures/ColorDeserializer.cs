using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Structures
{
    class ColorDeserializer : StructureDeserializer<Color>
    {
        protected override async Task<Color> Implementation(PipeReader pipeReader, CancellationToken cancellationToken)
        {
            var color = new Color();
            var readResult = await pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = readResult.Buffer;
            if (buffer.IsSingleSegment)
            {
                color.R = buffer.FirstSpan[0];
                color.G = buffer.FirstSpan[1];
                color.B = buffer.FirstSpan[2];
                buffer = buffer.Slice(sizeof(byte) * 3);
            }
            else
            {
                (color.R, color.G, color.B) = ParseWithSequenceReader(ref buffer);
            }

            pipeReader.AdvanceTo(buffer.Start);
            return color;
        }

        private (byte R, byte G, byte B) ParseWithSequenceReader(ref ReadOnlySequence<byte> buffer)
        {
            var bufferReader = new System.Buffers.SequenceReader<byte>(buffer);
            bufferReader.TryRead(out var r);
            bufferReader.TryRead(out var g);
            bufferReader.TryRead(out var b);
            buffer = bufferReader.UnreadSequence;
            return (r, g, b);
        }
    }
}
