using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Proxy.Events
{
    class TerrariaClientSocketConnectedEventArgs : HandlerArgs
    {
        public bool ForcedToDisconnect { get; set; }
    }

    class TerrariaClientSocketDisconnectedEventArgs : HandlerArgs
    {
        
    }

    class TerrariaClientEvents
    {
        ILoggerFactory loggerFactory;
        public TerrariaClientEvents(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
            this.TerrariaClientSocketConnected = new HandlerList<TerrariaClient, TerrariaClientSocketConnectedEventArgs>(
                this.loggerFactory.CreateLogger($"{typeof(TerrariaClientEvents).FullName}.{nameof(TerrariaClientSocketConnected)}")
            );
            this.TerrariaClientSocketDisconnected = new HandlerList<TerrariaClient, TerrariaClientSocketDisconnectedEventArgs>(
                this.loggerFactory.CreateLogger($"{typeof(TerrariaClientEvents).FullName}.{nameof(TerrariaClientSocketDisconnected)}")
            );
        }

        public readonly HandlerList<TerrariaClient, TerrariaClientSocketConnectedEventArgs> TerrariaClientSocketConnected;
        public readonly HandlerList<TerrariaClient, TerrariaClientSocketDisconnectedEventArgs> TerrariaClientSocketDisconnected;

        internal async Task<bool> OnTerrariaClientSocketConnected(TerrariaClient sender, CancellationToken cancellationToken = default)
        {
            var args = new TerrariaClientSocketConnectedEventArgs()
            {
                CancellationToken = cancellationToken
            };
            
            await this.TerrariaClientSocketConnected.Invoke(sender, args).ConfigureAwait(false);
            return args.ForcedToDisconnect;
        }

        internal Task OnTerrariaClientSocketDisconnected(TerrariaClient sender, CancellationToken cancellationToken = default)
        {
            var args = new TerrariaClientSocketDisconnectedEventArgs()
            {
                CancellationToken = cancellationToken
            };
            return this.TerrariaClientSocketDisconnected.Invoke(sender, args);
        }
    }
}
