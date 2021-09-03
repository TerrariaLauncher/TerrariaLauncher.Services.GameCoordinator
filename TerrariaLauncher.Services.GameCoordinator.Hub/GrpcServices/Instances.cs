using Grpc.Core;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database.CQS.Query;
using TerrariaLauncher.Commons.DomainObjects;
using TerrariaLauncher.Protos.Services.GameCoordinator.Hub;
using TerrariaLauncher.Services.GameCoordinator.Hub.Services;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.GrpcServices
{
    class Instances : TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Instances.InstancesBase
    {
        IQueryDispatcher queryDispatcher;
        RunningAgents runningAgents;
        public Instances(RunningAgents runningAgents,
            IQueryDispatcher queryDispatcher)
        {
            this.runningAgents = runningAgents;
            this.queryDispatcher = queryDispatcher;
        }

        public override Task<CountPlayerResponse> CountPlayer(CountPlayerRequest request, ServerCallContext context)
        {
            return base.CountPlayer(request, context);
        }

        public override Task<GetInstancesResponse> GetInstances(GetInstancesRequest request, ServerCallContext context)
        {
            var response = new GetInstancesResponse();
            foreach (var (id, agent) in runningAgents.Agents)
            {
                response.Instances.Add(new GetInstancesResponse.Types.Instance()
                {
                    Id = agent.Instance.Id,
                    Name = agent.Instance.Name,
                    Realm = agent.Instance.Realm,
                    Enabled = agent.Instance.Enabled,
                    Host = agent.Instance.Host,
                    Port = agent.Instance.Port,
                    MaxSlots = agent.Instance.MaxSlots,
                    Platform = agent.Instance.Platform,
                    Version = agent.Instance.Version
                });
            }
            return Task.FromResult(response);
        }

        public override Task<InstanceUpResponse> InstanceUp(InstanceUpRequest request, ServerCallContext context)
        {
            return Task.FromResult(new InstanceUpResponse() { });
        }
    }
}
