using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator
{
    class HookManager
    {
        public HandlerList<object, TerrariaClientConnectedEventArgs> TerrariaClientConnected = new HandlerList<object, TerrariaClientConnectedEventArgs>();
    }
}
