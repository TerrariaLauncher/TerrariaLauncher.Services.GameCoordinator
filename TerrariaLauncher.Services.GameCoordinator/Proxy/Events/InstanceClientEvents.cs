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
        public InstanceClientEvents()
        {

        }

        public readonly HandlerList<InstanceClient, InstanceClientConnectToRealmArgs> ConnectToRealmHandlers = new HandlerList<InstanceClient, InstanceClientConnectToRealmArgs>();
        public readonly HandlerList<InstanceClient, InstanceClientConnectToInstanceArgs> ConnectToInstanceHandlers = new HandlerList<InstanceClient, InstanceClientConnectToInstanceArgs>();

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
