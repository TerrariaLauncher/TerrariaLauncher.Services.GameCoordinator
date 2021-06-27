using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator
{
    enum PacketOpCode : byte
    {
        Connect = 1,
        Disconnect = 2,
        /// <summary>
        /// Set player index in the index array (0, 255).
        /// 255 is server player.
        /// </summary>
        SetUserSlot = 3,
        PlayerInfo = 4,
        NetModule = 82,

        UnListed = 255
    }
}
