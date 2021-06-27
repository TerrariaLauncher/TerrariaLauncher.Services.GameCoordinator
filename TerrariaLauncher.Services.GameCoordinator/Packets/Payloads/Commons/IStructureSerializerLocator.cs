using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons
{
    interface IStructureSerializerLocator
    {
        IStructureSerializer<TStructure> Get<TStructure>() where TStructure : IStructure;
    }
}
