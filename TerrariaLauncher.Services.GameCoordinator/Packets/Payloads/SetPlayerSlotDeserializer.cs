using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads
{
    class SetPlayerSlotDeserializer : StructureDeserializer<SetPlayerSlot>
    {
        protected override async Task<SetPlayerSlot> Implementation(PipeReader pipeReader, CancellationToken cancellationToken = default)
        {
            var readResult = await pipeReader.ReadAsync(cancellationToken);
            var buffer = readResult.Buffer;
            
            var playerId = buffer.FirstSpan[0];
            buffer = buffer.Slice(sizeof(byte));
            
            pipeReader.AdvanceTo(buffer.Start);
            return new SetPlayerSlot()
            {
                PlayerId = playerId
            };
        }
    }
}
