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
    class ChatModuleDeserializer : StructureDeserializer<ChatModule>
    {
        protected override async Task<ChatModule> Implementation(PipeReader pipeReader, CancellationToken cancellationToken)
        {
            var chatModule = new ChatModule();
            var stream = pipeReader.AsStream(true);
            await using (stream.ConfigureAwait(false))
            {
                using (var streamReader = new BinaryReader(stream))
                {
                    chatModule.Command = ChatModule.CommandTypeLookup[streamReader.ReadString()];
                    chatModule.Text = streamReader.ReadString();
                }
            }
            return chatModule;
        }
    }
}
