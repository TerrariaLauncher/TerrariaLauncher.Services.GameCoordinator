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
    class NetStringSerializer : StructureSerializer<NetString>
    {
        protected override async Task Implementation(NetString structure, PipeWriter pipeWriter, CancellationToken cancellationToken = default)
        {
            var stream = pipeWriter.AsStream(true);
            await using (stream.ConfigureAwait(false))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(structure.Value);
                }
            }
        }
    }
}
