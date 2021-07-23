using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database.CQS.Query;
using TerrariaLauncher.Protos.Services.GameCoordinator.Hub;
using TerrariaLauncher.Services.GameCoordinator.Hub.Database.Queries;
using TerrariaLauncher.Services.GameCoordinator.Hub.Database.Queries.Handlers;
using TerrariaLauncher.Services.GameCoordinator.Hub.Services;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.GrpcServices
{
    class Users : TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Users.UsersBase
    {
        Playing playing;
        IQueryDispatcher queryDispatcher;
        public Users(Playing playing,
            IQueryDispatcher queryDispatcher)
        {
            this.playing = playing;
            this.queryDispatcher = queryDispatcher;
        }

        public override async Task<LoginResponse> Login(LoginRequest request, ServerCallContext context)
        {
            if (!this.playing.Get(request.PlayerName, out var player))
            {
                var meta = new Metadata();
                meta.Add(new Metadata.Entry($"not-found",
                    LoginRequest.Descriptor.FindFieldByNumber(LoginRequest.PlayerNameFieldNumber).Name));
                throw new RpcException(new Status(StatusCode.NotFound, "Player is not found."), meta);
            }

            var getUserByNameQueryResult = await this.queryDispatcher.DispatchAsync<GetUserByNameQuery, GetUserByNameQueryResult>(new GetUserByNameQuery()
            {
                Name = request.Name
            }, context.CancellationToken).ConfigureAwait(false);

            var user = getUserByNameQueryResult.User;
            if (user is null)
            {
                var meta = new Metadata();
                meta.Add(new Metadata.Entry("not-found",
                    LoginRequest.Descriptor.FindFieldByNumber(LoginRequest.NameFieldNumber).Name));
                throw new RpcException(new Status(StatusCode.NotFound, "User is not found."), meta);
            }

            if (BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Password is invalid."));
            }

            player.User = getUserByNameQueryResult.User;

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
