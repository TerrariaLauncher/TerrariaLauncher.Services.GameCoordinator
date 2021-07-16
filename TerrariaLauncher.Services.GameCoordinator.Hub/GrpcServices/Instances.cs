using Grpc.Core;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database.CQS.Query;
using TerrariaLauncher.Commons.DomainObjects;
using TerrariaLauncher.Protos.Services.GameCoordinator.Hub;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.GrpcServices
{
    class Instances : TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Instances.InstancesBase
    {
        Dictionary<string, Instance> instances;
        IQueryDispatcher queryDispatcher;

        public Instances(IOptionsSnapshot<Dictionary<string, Instance>> instancesOptions, IQueryDispatcher queryDispatcher)
        {
            this.instances = instancesOptions.Value;
            this.queryDispatcher = queryDispatcher;
        }

        public override Task<GetInstancesResponse> GetInstances(GetInstancesRequest request, ServerCallContext context)
        {
            var response = new GetInstancesResponse();
            foreach (var instance in instances)
            {
                response.Instances.Add(new GetInstancesResponse.Types.Instance()
                {
                    Id = instance.Value.Id,
                    Name = instance.Value.Name,
                    Realm = instance.Value.Realm,
                    Enabled = instance.Value.Enabled,
                    Host = instance.Value.Host,
                    Port = instance.Value.Port,
                    Version = instance.Value.Version
                });
            }
            return Task.FromResult(response);
        }
    }
}
