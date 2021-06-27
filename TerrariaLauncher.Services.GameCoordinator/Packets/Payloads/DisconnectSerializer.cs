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
    class DisconnectSerializer : StructureSerializer<Disconnect>
    {
        IStructureSerializer<NetworkText> networkTextSerializer;
        public DisconnectSerializer(IStructureSerializer<NetworkText> networkTextSerializer)
        {
            this.networkTextSerializer = networkTextSerializer;
        }

        protected override async Task Implementation(Disconnect disconnect, PipeWriter pipeWriter, CancellationToken cancellationToken = default)
        {
            await this.networkTextSerializer.Serialize(disconnect.Reason, pipeWriter, false, cancellationToken).ConfigureAwait(false);
        }
    }
}
