using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TerrariaLauncher.Protos.Services.GameCoordinator.Hub;
using TerrariaLauncher.Services.GameCoordinator.Hub.Services;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.GrpcServices
{
    class Players : Protos.Services.GameCoordinator.Hub.Players.PlayersBase
    {
        Playing playing;

        public Players(Playing playing)
        {
            this.playing = playing;
        }

        public override Task<JoinResponse> Join(JoinRequest request, ServerCallContext context)
        {
            var player = new GameCoordinatorPlayer()
            {
                Name = request.Name,
                EndPoint = IPEndPoint.Parse(request.EndPoint),
                GameCoordinatorProxyId = request.GameCoordinatorId
            };
            
            if (!this.playing.Join(player))
            {
                return Task.FromResult(new JoinResponse()
                {
                    Rejected = true,
                    Reason = "Player has already joined."
                });
            }

            return Task.FromResult(new JoinResponse()
            {
                Rejected = false
            });
        }

        public override Task<LeaveResponse> Leave(LeaveRequest request, ServerCallContext context)
        {
            if (!this.playing.Leave(request.Name, out _))
            {
                throw new Grpc.Core.RpcException(new Status(StatusCode.NotFound, "Player is not found on the player list."));
            }

            return Task.FromResult(new LeaveResponse()
            {

            });
        }
    }
}
