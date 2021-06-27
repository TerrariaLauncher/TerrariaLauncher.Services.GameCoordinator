using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.BitFlags
{
    class TorchFlags : Structure
    {
        public bool UsingBiomeTorches { get; set; }
        public bool HappyFunTorchTime { get; set; }
        public bool UnlockedBiomeTorches { get; set; }
    }
}
