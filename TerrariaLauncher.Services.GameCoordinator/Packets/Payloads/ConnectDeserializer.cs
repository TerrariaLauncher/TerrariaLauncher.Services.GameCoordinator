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
    class ConnectDeserializer : StructureDeserializer<Connect>
    {
        protected override async Task<Connect> Implementation(PipeReader pipeReader, CancellationToken cancellationToken)
        {
            var connect = new Connect();
            var stream = pipeReader.AsStream(true);
            await using (stream.ConfigureAwait(false))
            {
                using (var streamReader = new BinaryReader(stream))
                {
                    connect.Version = streamReader.ReadString();
                }
            }
            return connect;
        }
    }
}
