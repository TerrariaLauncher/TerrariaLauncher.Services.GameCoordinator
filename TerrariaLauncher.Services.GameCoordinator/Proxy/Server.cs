using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

namespace TerrariaLauncher.Services.GameCoordinator.Proxy
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

        public Server(IOptions<ServerOptions> options, IServiceScopeFactory serviceScopeFactory)
        {
            this.options = options.Value;
            this.serviceScopeFactory = serviceScopeFactory;
        }

        public async Task Start()
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.cancellationToken = this.cancellationTokenSource.Token;


            var bindIpAddress = await NetworkUtils.GetIPv4(this.options.Host);
            IPEndPoint bindEndPoint = new IPEndPoint(bindIpAddress, this.options.Port);

            this.listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.listenSocket.Bind(bindEndPoint);
            this.listenSocket.Listen(1024);
            _ = Task.Run(() => this.StartAcceptTerrariaClient(this.cancellationToken), this.cancellationToken);

            Console.Title = bindEndPoint.ToString();
        }

        private async Task StartAcceptTerrariaClient(CancellationToken cancellationToken)
        {
            while (true)
            {
                var acceptSocket = await this.listenSocket.AcceptAsync();

                var scope = this.serviceScopeFactory.CreateScope();
                var interceptorTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var terrariaClient = scope.ServiceProvider.GetRequiredService<TerrariaClient>();
                var connectionRefused = await terrariaClient.Connect(acceptSocket, interceptorTokenSource.Token);
                if (connectionRefused)
                {
                    interceptorTokenSource.Cancel();
                    interceptorTokenSource.Dispose();
                    scope.Dispose();
                    return;
                }

                var interceptor = scope.ServiceProvider.GetRequiredService<Interceptor>();
                var interceptTask = interceptor.ProcessPackets(interceptorTokenSource.Token);
                Interlocked.Increment(ref this.numConnectingClients);
                _ = Task.WhenAny(interceptTask, terrariaClient.Completion)
                    .ContinueWith(task =>
                    {
                        interceptorTokenSource.Cancel();
                        interceptorTokenSource.Dispose();
                        Interlocked.Decrement(ref this.numConnectingClients);
                        scope.Dispose();
                    });
            }
        }

        public event Func<Task> Stopped;

        public async Task Stop()
        {
            this.cancellationTokenSource.Cancel();
            this.cancellationTokenSource.Dispose();
            this.listenSocket.Close();
            this.listenSocket.Dispose();

            foreach (var invocation in this.Stopped.GetInvocationList())
            {
                await ((Func<Task>)invocation)();
            }
        }
    }
}
