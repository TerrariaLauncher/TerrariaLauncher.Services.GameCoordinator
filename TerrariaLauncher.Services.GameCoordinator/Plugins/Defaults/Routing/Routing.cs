using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.DomainObjects;
using TerrariaLauncher.Services.GameCoordinator.Plugins.Helpers;
using TerrariaLauncher.Services.GameCoordinator.Proxy;
using TerrariaLauncher.Services.GameCoordinator.Proxy.Events;

namespace TerrariaLauncher.Services.GameCoordinator.Plugins.Defaults
{
    class Routing : Plugin
    {
        TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Instances.InstancesClient instancesClient;
        TextCommands textCommands;
        TextMessageHelper textMessageHelper;
        TerrariaPacketPoolHelper terrariaPacketPoolHelper;

        TextCommand routingCommand;
        public Routing(
            TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Instances.InstancesClient instancesClient,
            TextCommands textCommands,
            TextMessageHelper textMessageHelper,
            TerrariaPacketPoolHelper terrariaPacketPoolHelper)
        {
            this.instancesClient = instancesClient;
            this.textCommands = textCommands;
            this.textMessageHelper = textMessageHelper;
            this.terrariaPacketPoolHelper = terrariaPacketPoolHelper;

            this.routingCommand = new TextCommand()
            {
                Command = ">",
                Handler = this.OnRoutingCommand
            };
        }

        public Dictionary<string, List<Instance>> Realms = new Dictionary<string, List<Instance>>(StringComparer.OrdinalIgnoreCase);

        public override async Task Load(CancellationToken cancellationToken = default)
        {
            this.textCommands.Register(routingCommand);

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
                    Version = instance.Version,
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
            this.textCommands.Deregister(routingCommand);
            this.Realms.Clear();
            return Task.CompletedTask;
        }

        public async Task OnRoutingCommand(Interceptor sender, TextCommandHandlerArgs args)
        {
            if (!(args.Arguments.Length > 0)) return;
            string realm = args.Arguments.Span[0];

            if (!this.Realms.ContainsKey(realm))
            {
                await this.textMessageHelper.SendErrorMessage("Realm does not exist! Please check realm name again.", sender.TerrariaClient, args.CancellationToken);
                return;
            }

            await sender.InstanceClient.Connect(realm, args.CancellationToken);
        }
    }
}
