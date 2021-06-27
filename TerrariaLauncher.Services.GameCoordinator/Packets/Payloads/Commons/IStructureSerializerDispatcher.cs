using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons
{
    interface IStructureSerializerDispatcher
    {
        Task Serialize<TStructure>(TStructure structure, PipeWriter pipeWriter, CancellationToken cancellationToken)
            where TStructure : IStructure;
    }
}
