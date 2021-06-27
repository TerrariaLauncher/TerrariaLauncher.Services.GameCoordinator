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
    class DifficultyFlagsDeserializer : StructureDeserializer<DifficultyFlags>
    {
        protected override Task<DifficultyFlags> Implementation(PipeReader pipeReader, CancellationToken cancellationToken)
        {
            if (!pipeReader.TryRead(out var readResult))
            {
                throw new InvalidOperationException();
            }
            ref readonly var difficultyFlagsValue = ref readResult.Buffer.FirstSpan[0];
            BitFlags bitFlags = new BitFlags();
            bitFlags.Value = difficultyFlagsValue;
            bitFlags.Retrieval(out var bit0, out var bit1, out var bit2, out var bit3);
            var difficultyFlags = new DifficultyFlags()
            {
                SoftCore = bit0,
                MediumCore = bit1,
                ExtraAccessory = bit2,
                HardCore = bit3
            };
            return Task.FromResult(difficultyFlags);
        }
    }
}
