using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Structures;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Modules
{
    class TextModuleDeserializer : StructureDeserializer<TextModule>
    {
        IStructureDeserializer<NetworkText> networkTextDeserializer;
        IStructureDeserializer<Color> colorDeserializer;
        public TextModuleDeserializer(IStructureDeserializer<NetworkText> networkTextDeserializer, IStructureDeserializer<Color> colorDeserializer)
        {
            this.networkTextDeserializer = networkTextDeserializer;
            this.colorDeserializer = colorDeserializer;
        }

        protected override async Task<TextModule> Implementation(PipeReader pipeReader, CancellationToken cancellationToken)
        {
            var textModule = new TextModule();
            var readResult = await pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = readResult.Buffer;

            textModule.AuthorIndex = buffer.FirstSpan[0];
            buffer = buffer.Slice(sizeof(byte));
            pipeReader.AdvanceTo(buffer.Start);

            textModule.MessageText = await this.networkTextDeserializer.Deserialize(pipeReader, cancellationToken).ConfigureAwait(false);
            textModule.Color = await this.colorDeserializer.Deserialize(pipeReader, cancellationToken).ConfigureAwait(false);

            return textModule;
        }
    }
}
