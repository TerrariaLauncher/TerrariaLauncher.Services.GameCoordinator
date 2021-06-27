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
    class TorchFlagsSerializer : StructureSerializer<TorchFlags>
    {
        protected override Task Implementation(TorchFlags structure, PipeWriter pipeWriter, CancellationToken cancellationToken = default)
        {
            var bitFlags = new BitFlags();
            bitFlags[0] = structure.UsingBiomeTorches;
            bitFlags[1] = structure.HappyFunTorchTime;
            bitFlags[2] = structure.UnlockedBiomeTorches;

            var buffer = pipeWriter.GetSpan(sizeof(byte));
            buffer[0] = bitFlags.Value;
            pipeWriter.Advance(sizeof(byte));
            
            return Task.CompletedTask;
        }
    }
}
