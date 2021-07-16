using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Modules
{
    abstract class ModuleStructure : Structure, IModuleStructure
    {
        public abstract NetModuleId NetModuleId { get; }
    }
}
