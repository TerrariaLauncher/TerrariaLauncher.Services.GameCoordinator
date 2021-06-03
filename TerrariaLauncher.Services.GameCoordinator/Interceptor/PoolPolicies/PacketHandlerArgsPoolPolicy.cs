using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator
{
    class PacketHandlerArgsPoolPolicy : IObjectPoolPolicy<PacketHandlerArgs>
    {
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
