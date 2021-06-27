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
    class NetworkTextDeserializer : StructureDeserializer<NetworkText>
    {
        protected override async Task<NetworkText> Implementation(PipeReader pipeReader, CancellationToken cancellationToken)
        {
            var networkText = new NetworkText();

            var stream = pipeReader.AsStream(true);
            await using (stream.ConfigureAwait(false))
            {
                using (var streamReader = new BinaryReader(stream))
                {
                    networkText.Mode = (NetworkText.ModeType)streamReader.ReadByte();
                    networkText.Text = streamReader.ReadString();
                    if (networkText.Mode != NetworkText.ModeType.Literal)
                    {
                        networkText.SubstitutionLength = streamReader.ReadByte();
                        networkText.Substitutions = new NetworkText[networkText.SubstitutionLength];
                    }
                    else
                    {
                        networkText.SubstitutionLength = 0;
                        networkText.Substitutions = null;
                    }
                }
            }

            for (int index = 0; index < networkText.SubstitutionLength; ++index)
            {
                networkText.Substitutions[index] = await base.Deserialize(pipeReader, cancellationToken).ConfigureAwait(false);
            }

            return networkText;
        }
    }
}
