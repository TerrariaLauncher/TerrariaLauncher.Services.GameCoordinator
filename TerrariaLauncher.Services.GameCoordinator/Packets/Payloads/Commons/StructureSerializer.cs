using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons
{
    abstract class StructureSerializer<TStructure> : IStructureSerializer<TStructure> where TStructure : IStructure
    {
        public Task Serialize(TStructure structure, PipeWriter pipeWriter, CancellationToken cancellationToken = default)
        {
            return this.Serialize(structure, pipeWriter, true, cancellationToken);
        }

        public async Task Serialize(TStructure structure, PipeWriter pipeWriter, bool flush = true, CancellationToken cancellationToken = default)
        {
            await this.Implementation(structure, pipeWriter, cancellationToken).ConfigureAwait(false);
            if (flush)
            {
                await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        protected abstract Task Implementation(TStructure structure, PipeWriter pipeWriter, CancellationToken cancellationToken = default);
    }
}
