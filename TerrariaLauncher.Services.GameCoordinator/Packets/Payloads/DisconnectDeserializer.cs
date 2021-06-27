using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Structures;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads
{
    class DisconnectDeserializer : StructureDeserializer<Disconnect>
    {
        IStructureDeserializer<NetworkText> networkTextDeserializer;
        public DisconnectDeserializer(IStructureDeserializer<NetworkText> networkTextDeserializer)
        {
            this.networkTextDeserializer = networkTextDeserializer;
        }

        protected override async Task<Disconnect> Implementation(PipeReader pipeReader, CancellationToken cancellationToken)
        {
            var disconnect = new Disconnect();
            disconnect.Reason = await this.networkTextDeserializer.Deserialize(pipeReader, cancellationToken).ConfigureAwait(false);
            return disconnect;
        }
    }
}
