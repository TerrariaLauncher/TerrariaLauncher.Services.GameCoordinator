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
    class NetStringDeserializer : StructureDeserializer<NetString>
    {
        private static int ReadPrefixLength(ref ReadOnlySequence<byte> buffer, out int numPrefixBytes)
        {
            numPrefixBytes = 0;

            int length = 0;
            int byteIndex = 0;

            var reader = new SequenceReader<byte>(buffer);
            while (byteIndex < sizeof(int) + 1)
            {
                if (!reader.TryRead(out var byteValue)) throw new FormatException();

                length |= (byte)(0b0111_1111U & byteValue) << (byteIndex * 7);
                if ((byte)(0b1000_0000U & byteValue) == 0)
                {
                    numPrefixBytes = byteIndex + 1;
                    return length;
                }
                ++byteIndex;
            }

            throw new FormatException();
        }

        protected override async Task<NetString> Implementation(PipeReader pipeReader, CancellationToken cancellationToken = default)
        {
            var readResult = await pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = readResult.Buffer;

            var stringBufferLength = ReadPrefixLength(ref buffer, out var numPrefixBytes);
            buffer = buffer.Slice(numPrefixBytes);
            var stringBuffer = buffer.Slice(0, stringBufferLength);
            var encoding = new System.Text.UTF8Encoding();
            var netString = new NetString()
            {
                Value = encoding.GetString(stringBuffer)
            };
            buffer = buffer.Slice(stringBufferLength);
            pipeReader.AdvanceTo(buffer.Start);

            return netString;
        }
    }
}
