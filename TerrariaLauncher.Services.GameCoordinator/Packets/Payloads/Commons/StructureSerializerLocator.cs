using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons
{
    class StructureSerializerLocator: IStructureSerializerLocator
    {
        IServiceProvider serviceProvider;
        public StructureSerializerLocator(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public IStructureSerializer<TStructure> Get<TStructure>() where TStructure : IStructure
        {
            return this.serviceProvider.GetRequiredService<IStructureSerializer<TStructure>>();
        }
    }
}
