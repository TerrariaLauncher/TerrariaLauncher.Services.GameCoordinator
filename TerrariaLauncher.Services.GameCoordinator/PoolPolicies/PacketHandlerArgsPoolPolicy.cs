using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Pools;
using TerrariaLauncher.Services.GameCoordinator.Proxy.Events;

namespace TerrariaLauncher.Services.GameCoordinator.PoolPolicies
{
    class PacketHandlerArgsPoolPolicy : IObjectPoolPolicy<PacketHandlerArgs>
    {
        public int RetainedSize => 100;

        public PacketHandlerArgs Create()
        {
            return new PacketHandlerArgs();
        }

        public bool Return(PacketHandlerArgs instance)
        {
            instance.CancellationToken = default;
            instance.Handled = false;
            instance.Ignored = false;
            instance.TerrariaPacket = null;
            return true;
        }
    }
}
