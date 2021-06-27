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
    class DifficultyFlagsSerializer : StructureSerializer<DifficultyFlags>
    {
        protected override Task Implementation(DifficultyFlags structure, PipeWriter pipeWriter, CancellationToken cancellationToken = default)
        {
            BitFlags bitFlags = new BitFlags();
            bitFlags[0] = structure.SoftCore;
            bitFlags[1] = structure.MediumCore;
            bitFlags[2] = structure.ExtraAccessory;
            bitFlags[3] = structure.HardCore;

            var buffer = pipeWriter.GetSpan(sizeof(byte));
            buffer[0] = bitFlags.Value;
            pipeWriter.Advance(sizeof(byte));

            return Task.CompletedTask;
        }
    }
}
