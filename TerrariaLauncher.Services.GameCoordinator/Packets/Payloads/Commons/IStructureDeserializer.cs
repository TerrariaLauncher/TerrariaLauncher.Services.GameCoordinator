using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons
{
    interface IStructureDeserializer<TStructure> where TStructure : IStructure
    {
        Task<TStructure> Deserialize(PipeReader pipeReader, CancellationToken cancellationToken = default);
    }
}
