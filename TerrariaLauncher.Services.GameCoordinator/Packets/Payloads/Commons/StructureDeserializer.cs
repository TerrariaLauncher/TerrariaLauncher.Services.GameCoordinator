using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons
{
    abstract class StructureDeserializer<TStructure> : IStructureDeserializer<TStructure>
        where TStructure : IStructure
    {
        public Task<TStructure> Deserialize(PipeReader pipeReader, CancellationToken cancellationToken = default)
        {
            return this.Implementation(pipeReader, cancellationToken);
        }

        protected abstract Task<TStructure> Implementation(PipeReader pipeReader, CancellationToken cancellationToken = default);
    }
}
