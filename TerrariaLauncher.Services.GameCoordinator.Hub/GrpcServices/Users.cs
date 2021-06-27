using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database.CQS.Query;
using TerrariaLauncher.Protos.Services.GameCoordinator.Hub;
using TerrariaLauncher.Services.GameCoordinator.Hub.Database.Queries;
using TerrariaLauncher.Services.GameCoordinator.Hub.Database.Queries.Handlers;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.GrpcServices
{
    class Users : TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Users.UsersBase
    {
        QueryDispatcher queryDispatcher;
        public Users(QueryDispatcher queryDispatcher)
        {
            this.queryDispatcher = queryDispatcher;
        }

        public override async Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
        {
            var getUserByNameQueryResult = await this.queryDispatcher.DispatchAsync<GetUserByNameQuery, GetUserByNameQueryResult>(new GetUserByNameQuery()
            {
                Name = request.Name
            }, context.CancellationToken).ConfigureAwait(false);
            
            var user = getUserByNameQueryResult.User;
            if (user is null) throw new RpcException(new Status(StatusCode.NotFound, "User is not found."));

            if (BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Password is invalid."));
            }

            return new LoginResponse()
            {
                Id = user.Id,
                Name = user.Name,
                Group = user.Group,
                UUID = user.UUID
            };
        }

        public override async Task<LoginResponse> LoginWithUUID(LoginWithUUIDRequest request, ServerCallContext context)
        {
            var getUserByUUIDQueryResult = await this.queryDispatcher.DispatchAsync<GetUserByUUIDQuery, GetUserByUUIDQueryResult>(new GetUserByUUIDQuery()
            {
                UUID = request.UUID
            }, context.CancellationToken).ConfigureAwait(false);

            var user = getUserByUUIDQueryResult.User;
            if (user is null) throw new RpcException(new Status(StatusCode.NotFound, "User is not found."));

            return new LoginResponse()
            {
                Id = user.Id,
                Name = user.Name,
                Group = user.Group,
                UUID = user.UUID
            };
        }
    }
}
