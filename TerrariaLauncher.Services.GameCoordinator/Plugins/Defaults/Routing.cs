using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.DomainObjects;

namespace TerrariaLauncher.Services.GameCoordinator.Plugins.Defaults
{
    class Routing : Plugin
    {
        TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Instances.InstancesClient instancesClient;
        public Routing(TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Instances.InstancesClient instancesClient)
        {
            this.instancesClient = instancesClient;
        }

        public Dictionary<string, List<Instance>> Realms = new Dictionary<string, List<Instance>>(StringComparer.OrdinalIgnoreCase);

        public override async Task Load(CancellationToken cancellationToken = default)
        {
            var getInstancesResponse = await this.instancesClient.GetInstancesAsync(new Protos.Services.GameCoordinator.Hub.GetInstancesRequest(), cancellationToken: cancellationToken);
            foreach (var instance in getInstancesResponse.Instances)
            {
                var instanceDto = new Instance()
                {
                    Id = instance.Id,
                    Name = instance.Name,
                    Enabled = instance.Enabled,
                    Host = instance.Host,
                    Port = instance.Port,
                    RestPort = instance.RestPort,
                    GrpcPort = instance.GrpcPort,
                    GrpcTls = instance.GrpcTls,
                    Version = instance.Version,
                    Platform = instance.Platform,
                    Realm = instance.Realm
                };

                if (this.Realms.ContainsKey(instance.Realm))
                {
                    this.Realms[instance.Realm].Add(instanceDto);
                }
                else
                {
                    this.Realms[instance.Realm] = new List<Instance>()
                    {
                        instanceDto
                    };
                }
            }
        }

        public override Task Unload(CancellationToken cancellationToken = default)
        {
            this.Realms.Clear();
            return Task.CompletedTask;
        }

        public async Task OnRoutingCommand()
        {
            
        }
    }
}
