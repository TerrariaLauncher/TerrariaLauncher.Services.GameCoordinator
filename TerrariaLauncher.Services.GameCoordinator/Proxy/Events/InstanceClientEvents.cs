using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.DomainObjects;

namespace TerrariaLauncher.Services.GameCoordinator.Proxy.Events
{
    class InstanceClientConnectToRealmArgs : HandlerArgs
    {
        public string Realm { get; set; }
        public Instance ResolvedInstance { get; set; }
    }

    class InstanceClientConnectToInstanceArgs : HandlerArgs
    {
        public Instance Instance { get; set; }
    }

    class InstanceClientEvents
    {
        ILoggerFactory loggerFactory;
        public InstanceClientEvents(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
            this.ConnectToRealmHandlers = new HandlerList<InstanceClient, InstanceClientConnectToRealmArgs>(
                this.loggerFactory.CreateLogger($"{typeof(InstanceClientEvents).FullName}.{nameof(ConnectToRealmHandlers)}")
            );
            this.ConnectToInstanceHandlers = new HandlerList<InstanceClient, InstanceClientConnectToInstanceArgs>(
                this.loggerFactory.CreateLogger($"{typeof(InstanceClientEvents).FullName}.{nameof(ConnectToRealmHandlers)}")
            );
        }

        public readonly HandlerList<InstanceClient, InstanceClientConnectToRealmArgs> ConnectToRealmHandlers;
        public readonly HandlerList<InstanceClient, InstanceClientConnectToInstanceArgs> ConnectToInstanceHandlers;

        public async Task<Instance> OnConnectToRealm(InstanceClient sender, string realm, CancellationToken cancellationToken = default)
        {
            var args = new InstanceClientConnectToRealmArgs()
            {
                CancellationToken = cancellationToken,
                Realm = realm
            };

            await this.ConnectToRealmHandlers.Invoke(sender, args);
            return args.ResolvedInstance;
        }

        public async Task OnConnectToInstance(InstanceClient sender, Instance instance, CancellationToken cancellationToken = default)
        {
            var args = new InstanceClientConnectToInstanceArgs()
            {
                CancellationToken = cancellationToken,
                Instance = instance
            };

            await this.ConnectToInstanceHandlers.Invoke(sender, args);
        }
    }
}
