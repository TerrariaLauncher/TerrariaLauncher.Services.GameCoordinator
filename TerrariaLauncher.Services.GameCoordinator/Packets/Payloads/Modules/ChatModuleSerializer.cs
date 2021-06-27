using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Modules
{
    class ChatModuleSerializer : StructureSerializer<ChatModule>
    {
        protected override async Task Implementation(ChatModule structure, PipeWriter pipeWriter, CancellationToken cancellationToken = default)
        {
            var stream = pipeWriter.AsStream(true);
            await using (stream.ConfigureAwait(false))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(ChatModule.RawCommandTypeLookup[structure.Command]);
                    writer.Write(structure.Text);
                }
            }
        }
    }
}
