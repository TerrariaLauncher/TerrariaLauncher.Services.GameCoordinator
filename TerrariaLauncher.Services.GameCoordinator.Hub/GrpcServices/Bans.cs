using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database.CQS.Query;
using TerrariaLauncher.Commons.DomainObjects;
using TerrariaLauncher.Protos.Services.GameCoordinator.Hub;
using TerrariaLauncher.Services.GameCoordinator.Hub.Database.Queries;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.GrpcServices
{
    class Bans : TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Bans.BansBase
    {
        IQueryDispatcher queryDispatcher;
        public Bans(IQueryDispatcher queryDispatcher)
        {
            this.queryDispatcher = queryDispatcher;
        }

        public override async Task<CheckResponse> IsBanned(CheckRequest request, ServerCallContext context)
        {
            var banQuery = new GetBanByIdentityQuery()
            {
                IdentityType = request.IdentityType,
                Identity = request.Identity
            };
            var banQueryResult = await this.queryDispatcher.DispatchAsync<GetBanByIdentityQuery, GetBanByIdentityQueryResult>(banQuery, context.CancellationToken).ConfigureAwait(false);
            if (!banQueryResult.Found)
            {
                return new CheckResponse()
                {
                    Banned = false
                };
            }

            return new CheckResponse()
            {
                Banned = true,
                Ticket = banQueryResult.Id,
                Reason = banQueryResult.Reason
            };
        }
    }
}
