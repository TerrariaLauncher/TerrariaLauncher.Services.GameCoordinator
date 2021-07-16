using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TerrariaLauncher.Protos.Services.GameCoordinator.Proxy;

namespace TerrariaLauncher.Services.GameCoordinator.Proxy.GrpcServices
{
    public class Player : TerrariaLauncher.Protos.Services.GameCoordinator.Proxy.Player.PlayerBase
    {
        public override Task<DisconnectResponse> Disconnect(DisconnectRequest request, ServerCallContext context)
        {
            return base.Disconnect(request, context);
        }
    }
}
