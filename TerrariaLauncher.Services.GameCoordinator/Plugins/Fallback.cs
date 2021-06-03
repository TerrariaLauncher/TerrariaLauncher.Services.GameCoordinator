using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Plugins
{
    class Fallback
    {
        public async Task OnConnect(Interceptor sender, PacketHandlerArgs args)
        {
            // Only trigger when there is a connect packet from the client.
            // This only happens when the client connect to server for the first time.
            if (args.TerrariaPacket.Origin != PacketOrigin.Client)
            {
                return;
            }

            var endPoint = IPEndPoint.Parse("127.0.0.1:7776");
            await sender.InstanceClient.Connect(endPoint, args.CancellationToken);
            _= Task.Run(() => sender.InstanceClient.Loop(args.CancellationToken));
        }

        public Task OnDisconnect(Interceptor sender, PacketHandlerArgs args)
        {
            return Task.CompletedTask;
        }
    }
}
