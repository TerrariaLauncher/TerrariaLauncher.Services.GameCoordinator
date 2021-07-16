using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.DomainObjects;
using TerrariaLauncher.Services.GameCoordinator.Proxy;
using TerrariaLauncher.Services.GameCoordinator.Proxy.Events;

namespace TerrariaLauncher.Services.GameCoordinator.Plugins
{
    class LoadBalancing : Plugin
    {
        TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Instances.InstancesClient instancesClient;
        InstanceClientEvents instanceClientEvents;

        Dictionary<string, List<Instance>> Realms = new Dictionary<string, List<Instance>>(StringComparer.OrdinalIgnoreCase);

        public LoadBalancing(
            TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Instances.InstancesClient instancesClient,
            InstanceClientEvents instanceClientEvents)
        {
            this.instancesClient = instancesClient;
            this.instanceClientEvents = instanceClientEvents;
        }

        public override async Task Load(CancellationToken cancellationToken = default)
        {
            this.instanceClientEvents.ConnectToRealmHandlers.Register(this.OnConnectToRealm);

            var getInstancesResponse = await this.instancesClient.GetInstancesAsync(new Protos.Services.GameCoordinator.Hub.GetInstancesRequest(), cancellationToken: cancellationToken);
            foreach (var instance in getInstancesResponse.Instances)
            {
                if (!this.Realms.ContainsKey(instance.Realm)) this.Realms[instance.Realm] = new List<Instance>();
                this.Realms[instance.Realm].Add(new Instance()
                {
                    Id = instance.Id,
                    Name = instance.Name,
                    Enabled = instance.Enabled,
                    Realm = instance.Realm,
                    Host = instance.Host,
                    Port = instance.Port,
                    Platform = instance.Platform,
                    Version = instance.Version,
                    MaxSlots = instance.MaxSlots
                });
            }
        }

        public override Task Unload(CancellationToken cancellationToken = default)
        {
            this.instanceClientEvents.ConnectToRealmHandlers.Deregister(this.OnConnectToRealm);
            return Task.CompletedTask;
        }

        public Task OnConnectToRealm(InstanceClient instanceClient, InstanceClientConnectToRealmArgs args)
        {
            if (this.Realms.TryGetValue(args.Realm, out var instancesOfRealm))
            {
                // Naive:
                args.ResolvedInstance = instancesOfRealm[0];
            }

            return Task.CompletedTask;
        }
    }
}
