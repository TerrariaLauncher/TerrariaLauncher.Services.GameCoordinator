using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator
{
    class TerrariaClientConnectedEventArgs : EventArgs
    {
        public TerrariaClient TerrariaClient { get; set; }
    }

    class ServerHooks
    {
        HookManager hookManager;

        public ServerHooks(HookManager hookManager)
        {
            this.hookManager = hookManager;
        }

        public Task OnTerrariaClientConnected(Server sender, TerrariaClient terrariaClient)
        {
            var args = new TerrariaClientConnectedEventArgs()
            {
                TerrariaClient = terrariaClient
            };

            return this.hookManager.TerrariaClientConnected.Invoke(sender, args);
        }
    }
}
