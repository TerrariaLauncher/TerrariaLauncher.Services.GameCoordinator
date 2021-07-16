using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Structures;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Modules
{
    /// <summary>
    /// NetModule [1] - Text: Server -> Client
    /// </summary>
    class TextModule: ModuleStructure
    {
        public override NetModuleId NetModuleId => NetModuleId.Text;

        public byte AuthorIndex { get; set; }
        public NetworkText MessageText { get; set; }
        public Color Color { get; set; }
    }    
}
