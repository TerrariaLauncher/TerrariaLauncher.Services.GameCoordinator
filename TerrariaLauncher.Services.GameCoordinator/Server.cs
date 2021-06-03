using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator
{
    class ServerOptions
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public int MaxPlayer { get; set; }
    }

    class Server
    {
        private Socket listenSocket;
        private ServerOptions options;

        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;

        private int numConnectingClients = 0;

        private IServiceScopeFactory serviceScopeFactory;

        public Server(ServerOptions options, IServiceScopeFactory serviceScopeFactory)
        {
            this.options = options;
            this.serviceScopeFactory = serviceScopeFactory;
        }

        public void Start()
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.cancellationToken = this.cancellationTokenSource.Token;

            var endpoint = new IPEndPoint(IPAddress.Parse(this.options.Host), this.options.Port);
            this.listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.listenSocket.Bind(endpoint);
            this.listenSocket.Listen(1024);
            _ = Task.Run(() => this.Loop(this.cancellationToken), this.cancellationToken);
        }

        private async Task Loop(CancellationToken cancellationToken)
        {
            while (true)
            {
                var scope = this.serviceScopeFactory.CreateScope();

                var acceptSocket = await this.listenSocket.AcceptAsync();
                var interceptor = scope.ServiceProvider.GetRequiredService<Interceptor>();
                var terrariaClient = scope.ServiceProvider.GetRequiredService<TerrariaClient>();
                terrariaClient.SetSocket(acceptSocket);
                Interlocked.Increment(ref this.numConnectingClients);
                
                var interceptTask = interceptor.Loop(cancellationToken);
                _ = interceptTask.ContinueWith(task =>
                {
                    Interlocked.Decrement(ref this.numConnectingClients);
                    scope.Dispose();
                });
            }
        }

        public void Stop()
        {
            this.cancellationTokenSource.Cancel();
            this.cancellationTokenSource.Dispose();
            this.listenSocket.Close();
            this.listenSocket.Dispose();
        }
    }
}
