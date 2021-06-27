using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons
{
    class StructureDeserializerLocator: IStructureDeserializerLocator
    {
        IServiceProvider serviceProvider;
        public StructureDeserializerLocator(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IStructureDeserializer<TStructure> Get<TStructure>() where TStructure : IStructure
        {
            return this.serviceProvider.GetRequiredService<IStructureDeserializer<TStructure>>();
        }
    }
}
