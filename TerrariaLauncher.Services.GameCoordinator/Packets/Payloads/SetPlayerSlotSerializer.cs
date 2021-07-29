using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads
{
    class SetPlayerSlotSerializer : StructureSerializer<SetPlayerSlot>
    {
        protected override Task Implementation(SetPlayerSlot structure, PipeWriter pipeWriter, CancellationToken cancellationToken = default)
        {
            var span = pipeWriter.GetSpan(sizeof(byte));
            span[0] = structure.PlayerId;
            pipeWriter.Advance(sizeof(byte));
            return Task.CompletedTask;
        }
    }
}
