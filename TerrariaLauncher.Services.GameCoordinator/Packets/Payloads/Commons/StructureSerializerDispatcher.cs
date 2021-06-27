using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons
{
    class StructureSerializerDispatcher : IStructureSerializerDispatcher
    {
        IStructureSerializerLocator serializerLocator;
        public StructureSerializerDispatcher(IStructureSerializerLocator serializerLocator)
        {
            this.serializerLocator = serializerLocator;
        }

        public Task Serialize<TStructure>(TStructure structure, PipeWriter pipeWriter, CancellationToken cancellationToken)
            where TStructure : IStructure
        {
            var serializer = this.serializerLocator.Get<TStructure>();
            return serializer.Serialize(structure, pipeWriter, cancellationToken);
        }
    }
}
