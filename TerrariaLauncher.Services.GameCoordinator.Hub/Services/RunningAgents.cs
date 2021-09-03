using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Consul.API.Commons;
using TerrariaLauncher.Commons.Consul.API.EndPoints.Agent.Services.Queries;
using TerrariaLauncher.Commons.Consul.API.Filter;
using TerrariaLauncher.Commons.DomainObjects;
using TerrariaLauncher.Commons.EventBus;
using TerrariaLauncher.Commons.MediatorService;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.Services
{
    public class Agent : IDisposable
    {
        Grpc.Net.Client.GrpcChannel channel;
        Instance forInstance;
        public Agent(string grpcAddress)
        {
            this.channel = Grpc.Net.Client.GrpcChannel.ForAddress(grpcAddress);
            this.InstanceService = new Protos.InstancePlugins.GameCoordinatorAgent.InstanceService.InstanceServiceClient(this.channel);
            var getInstanceResponse = this.InstanceService.GetInstance(new Protos.InstancePlugins.GameCoordinatorAgent.GetInstanceRequest());
            this.forInstance = new Instance()
            {
                Id = getInstanceResponse.Id,
                Name = getInstanceResponse.Name,
                Realm = getInstanceResponse.Realm,
                Enabled = getInstanceResponse.Enabled,
                Host = getInstanceResponse.Host,
                Port = getInstanceResponse.Port,
                MaxSlots = getInstanceResponse.MaxSlots,
                Platform = getInstanceResponse.Platform,
                Version = getInstanceResponse.Version
            };
        }

        public string Id { get; }
        public Instance Instance => this.forInstance;

        public TerrariaLauncher.Protos.InstancePlugins.GameCoordinatorAgent.InstanceService.InstanceServiceClient InstanceService { get; }

        private bool disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                var task = this.channel.ShutdownAsync();
                SpinWait.SpinUntil(() => task.IsCompleted, 3000);
                this.channel.Dispose();
            }

            disposed = true;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public class RunningAgentsChange : INotification
    {

    }

    public class RunningAgentsChangeHandler : NotificationHandler<RunningAgentsChange>
    {
        public RunningAgentsChangeHandler()
        {

        }

        protected override Task Implementation(RunningAgentsChange notification, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    public class RunningAgents : IDisposable
    {
        IConsulQueryDispatcher consulQueryDispatcher;
        IEventBus eventBus;
        IMediatorService messageService;
        ILogger<RunningAgents> logger;
        ConcurrentDictionary<string, Agent> _agents = new ConcurrentDictionary<string, Agent>();

        public RunningAgents(
            IConsulQueryDispatcher consulQueryDispatcher,
            IEventBus eventBus,
            IMediatorService messageService,
            ILogger<RunningAgents> logger)
        {
            this.consulQueryDispatcher = consulQueryDispatcher;
            this.eventBus = eventBus;
            this.messageService = messageService;
            this.logger = logger;

            this.cancellationTokenSource = new CancellationTokenSource();
            this.cancellationToken = this.cancellationTokenSource.Token;
        }

        CancellationTokenSource cancellationTokenSource;
        CancellationToken cancellationToken;

        public Agent this[string id]
        {
            get => this._agents[id];
        }
        public IReadOnlyDictionary<string, Agent> Agents => this._agents;

        Task _refreshTask;
        public void Start()
        {
            this._refreshTask = this.Refresh(this.cancellationToken);
        }

        public void Stop()
        {
            this.cancellationTokenSource.Cancel();
        }

        private async Task Refresh(CancellationToken cancellationToken)
        {
            var getServicesQuery = new GetServicesQuery()
            {
                Query = new Commons.Consul.Filter.ConsulFilter()
                {
                    Expression = new ConsulBinaryExpression()
                    {
                        Left = new ConsulValue() { Value = "GameCoordinatorAgent" },
                        Operator = ConsulOperator.In,
                        Right = new ConsulSelector() { Selector = "Tags" }
                    }
                }
            };

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    bool hasChanged = false;
                    var servicesQueryResult = await this.consulQueryDispatcher.Dispatch<GetServicesQuery, GetServicesQueryResult>(getServicesQuery, cancellationToken);

                    ISet<string> runningIds = new HashSet<string>();
                    foreach (var (id, service) in servicesQueryResult.Services)
                    {
                        runningIds.Add(id);
                        if (this._agents.ContainsKey(id)) continue;

                        string address;
                        if (IPAddress.TryParse(service.Address, out var ipAddress) && ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                        {
                            address = $"http://[{service.Address}]:{service.Port}";
                        }
                        else
                        {
                            address = $"http://{service.Address}:{service.Port}";
                        }
                        this._agents.TryAdd(id, new Agent(address));

                        hasChanged = true;
                    }

                    IList<string> stallIds = new List<string>();
                    foreach (var (id, _) in this._agents)
                    {
                        if (runningIds.Contains(id)) continue;
                        stallIds.Add(id);
                    }
                    if (stallIds.Count > 0) hasChanged = true;

                    foreach (var id in stallIds)
                    {
                        if (!this._agents.TryRemove(id, out var stallAgent)) continue;
                        try
                        {
                            stallAgent.Dispose();
                        }
                        catch { }
                    }

                    if (hasChanged)
                    {
                        await this.messageService.Publish(new RunningAgentsChange(), cancellationToken);
                    }
                    else
                    {
                        await Task.Delay(10000, cancellationToken);
                    }
                }
                catch { }
            }
        }

        private bool disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                this.cancellationTokenSource.Dispose();
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
