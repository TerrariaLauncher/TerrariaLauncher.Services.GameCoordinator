using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons
{
    class StructureDeserializerDispatcher : IStructureDeserializerDispatcher
    {
        IStructureDeserializerLocator deserializerLocator;
        public StructureDeserializerDispatcher(IStructureDeserializerLocator deserializerLocator)
        {
            this.deserializerLocator = deserializerLocator;
        }

        public Task<TStructure> Deserialize<TStructure>(PipeReader pipeReader, CancellationToken cancellationToken)
            where TStructure : IStructure
        {
            var deserializer = this.deserializerLocator.Get<TStructure>();
            return deserializer.Deserialize(pipeReader, cancellationToken);
        }
    }
}
