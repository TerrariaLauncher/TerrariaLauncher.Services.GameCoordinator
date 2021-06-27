using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Structures;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Modules
{
    class TextModuleSerializer : StructureSerializer<TextModule>
    {
        IStructureSerializer<NetworkText> networkTextSerializer;
        IStructureSerializer<Color> colorSerializer;
        public TextModuleSerializer(IStructureSerializer<NetworkText> networkTextSerializer, IStructureSerializer<Color> colorSerializer)
        {
            this.networkTextSerializer = networkTextSerializer;
            this.colorSerializer = colorSerializer;
        }

        protected override async Task Implementation(TextModule structure, PipeWriter pipeWriter, CancellationToken cancellationToken = default)
        {
            pipeWriter.GetSpan(sizeof(byte))[0] = structure.AuthorIndex;
            pipeWriter.Advance(sizeof(byte));
            await this.networkTextSerializer.Serialize(structure.MessageText, pipeWriter, false, cancellationToken).ConfigureAwait(false);
            await this.colorSerializer.Serialize(structure.Color, pipeWriter, false, cancellationToken).ConfigureAwait(false);
        }
    }
}
