using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.BitFlags
{
    class TorchFlagsDeserializer : StructureDeserializer<TorchFlags>
    {
        protected override async Task<TorchFlags> Implementation(PipeReader pipeReader, CancellationToken cancellationToken = default)
        {
            var readResult = await pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = readResult.Buffer;

            var torchFlagsByte = buffer.FirstSpan[0];
            BitFlags bitFlags = new BitFlags(torchFlagsByte);
            var torchFlags = new TorchFlags()
            {
                UsingBiomeTorches = bitFlags[0],
                HappyFunTorchTime = bitFlags[1],
                UnlockedBiomeTorches = bitFlags[2]
            };

            pipeReader.AdvanceTo(buffer.GetPosition(sizeof(byte)));
            return torchFlags;
        }
    }
}
