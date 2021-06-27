using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads
{
    class ConnectSerializer : StructureSerializer<Connect>
    {
        protected override async Task Implementation(Connect structure, PipeWriter pipeWriter, CancellationToken cancellationToken = default)
        {
            var stream = pipeWriter.AsStream(true);
            await using (stream.ConfigureAwait(false))
            {
                using (var streamWriter = new BinaryWriter(stream))
                {
                    streamWriter.Write(structure.Version);
                }
            }
        }
    }
}
