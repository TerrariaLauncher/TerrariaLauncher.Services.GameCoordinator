using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Structures
{
    class NetworkTextSerializer : StructureSerializer<NetworkText>
    {
        protected override async Task Implementation(NetworkText structure, PipeWriter pipeWriter, CancellationToken cancellationToken = default)
        {
            var stream = pipeWriter.AsStream(true);
            await using (stream.ConfigureAwait(false))
            {
                using (var streamWriter = new BinaryWriter(stream))
                {
                    streamWriter.Write((byte)structure.Mode);
                    streamWriter.Write(structure.Text);
                    if (structure.Mode != NetworkText.ModeType.Literal)
                    {
                        streamWriter.Write(structure.SubstitutionLength);
                    }
                }
            }

            for (int index = 0; index < structure.SubstitutionLength; ++index)
            {
                await base.Serialize(structure.Substitutions[index], pipeWriter, false, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
