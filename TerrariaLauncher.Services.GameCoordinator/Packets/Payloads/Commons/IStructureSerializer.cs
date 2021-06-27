using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons
{
    interface IStructureSerializer<TStructure> where TStructure : IStructure
    {
        Task Serialize(TStructure structure, PipeWriter pipeWriter, CancellationToken cancellationToken = default);
        Task Serialize(TStructure structure, PipeWriter pipeWriter, bool flush = true, CancellationToken cancellationToken = default);
    }
}
