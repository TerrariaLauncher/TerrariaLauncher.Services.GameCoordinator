using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database.CQS.Query;
using TerrariaLauncher.Protos.Services.GameCoordinator.Hub;
using TerrariaLauncher.Services.GameCoordinator.Hub.Database.Queries;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.GrpcServices
{
    class Instances : TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Instances.InstancesBase
    {
        QueryDispatcher queryDispatcher;
        public Instances(QueryDispatcher queryDispatcher)
        {
            this.queryDispatcher = queryDispatcher;
        }

        public override async Task<GetInstancesResponse> GetInstances(GetInstancesRequest request, ServerCallContext context)
        {
            var getInstancesQueryResult = await this.queryDispatcher
                .DispatchAsync<GetInstancesQuery, GetInstancesQueryResult>(new GetInstancesQuery(), context.CancellationToken)
                .ConfigureAwait(false);
            var instances = getInstancesQueryResult.Instances;

            var response = new GetInstancesResponse();
            foreach (var instance in instances)
            {
                response.Instances.Add(new GetInstancesResponse.Types.Instance()
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
                });
            }
            return response;
        }
    }
}
