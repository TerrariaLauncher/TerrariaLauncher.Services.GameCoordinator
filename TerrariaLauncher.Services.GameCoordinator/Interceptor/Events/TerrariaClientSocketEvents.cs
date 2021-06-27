using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator
{
    class TerrariaClientSocketConnectedEventArgs : HandlerArgs
    {
        public bool ForcedToDisconnect { get; set; }
    }

    class TerrariaClientSocketDisconnectedEventArgs : HandlerArgs
    {
        
    }

    class TerrariaClientSocketEvents
    {
        public TerrariaClientSocketEvents()
        {

        }

        public HandlerList<Interceptor, TerrariaClientSocketConnectedEventArgs> TerrariaClientSocketConnected = new HandlerList<Interceptor, TerrariaClientSocketConnectedEventArgs>();
        public HandlerList<Interceptor, TerrariaClientSocketDisconnectedEventArgs> TerrariaClientSocketDisconnected = new HandlerList<Interceptor, TerrariaClientSocketDisconnectedEventArgs>();

        internal async Task<bool> OnTerrariaClientSocketConnected(Interceptor sender, CancellationToken cancellationToken = default)
        {
            var args = new TerrariaClientSocketConnectedEventArgs()
            {
                CancellationToken = cancellationToken
            };
            
            await this.TerrariaClientSocketConnected.Invoke(sender, args).ConfigureAwait(false);
            return args.ForcedToDisconnect;
        }

        internal Task OnTerrariaClientSocketDisconnected(Interceptor sender, CancellationToken cancellationToken = default)
        {
            var args = new TerrariaClientSocketDisconnectedEventArgs()
            {
                CancellationToken = cancellationToken
            };
            return this.TerrariaClientSocketDisconnected.Invoke(sender, args);
        }
    }
}
