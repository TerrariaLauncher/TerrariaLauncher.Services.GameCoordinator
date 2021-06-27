using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.BitFlags
{
    class DifficultyFlags: Structure
    {
        public bool SoftCore { get; set; }
        public bool MediumCore { get; set; }
        public bool ExtraAccessory { get; set; }
        public bool HardCore { get; set; }
    }
}
