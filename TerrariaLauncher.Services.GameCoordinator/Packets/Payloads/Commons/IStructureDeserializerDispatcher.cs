using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons
{
    interface IStructureDeserializerDispatcher
    {
        Task<TStructure> Deserialize<TStructure>(PipeReader pipeReader, CancellationToken cancellationToken)
            where TStructure : IStructure;
    }
}
