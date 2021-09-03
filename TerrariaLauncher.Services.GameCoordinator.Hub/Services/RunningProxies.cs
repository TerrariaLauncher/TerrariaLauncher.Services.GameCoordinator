using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Consul.API.Commons;
using TerrariaLauncher.Commons.Consul.API.EndPoints.Agent.Services.Queries;
using TerrariaLauncher.Commons.Consul.API.Filter;
using TerrariaLauncher.Commons.MediatorService;

namespace TerrariaLauncher.Services.GameCoordinator.Hub.Services
{
    public class ProxyStoppedNotification : INotification
    {
        public string Id { get; set; }
    }

    public class ProxyStoppedNotificationHandler : NotificationHandler<ProxyStoppedNotification>
    {
        Playing playing;
        public ProxyStoppedNotificationHandler(Playing playing)
        {
            this.playing = playing;
        }

        protected override Task Implementation(ProxyStoppedNotification notification, CancellationToken cancellationToken)
        {
            this.playing.RemovePlayersBelongToProxyId(notification.Id);
            return Task.CompletedTask;
        }
    }

    public class RunningProxies
    {
        ConcurrentDictionary<string, bool> _proxies = new ConcurrentDictionary<string, bool>();
        CancellationTokenSource cancellationTokenSource;

        IConfiguration configuration;
        IConsulQueryDispatcher consulQueryDispatcher;
        IMediatorService messageService;
        public RunningProxies(
            IConfiguration configuration,
            IConsulQueryDispatcher consulQueryDispatcher,
            IMediatorService messageService)
        {
            this.configuration = configuration;
            this.consulQueryDispatcher = consulQueryDispatcher;
            this.messageService = messageService;
        }

        private async Task Refresh(CancellationToken cancellationToken)
        {
            var getServicesQuery = new GetServicesQuery()
            {
                Query = new Commons.Consul.Filter.ConsulFilter()
                {
                    Expression = new ConsulBinaryExpression()
                    {
                        Left = new ConsulValue() { Value = this.configuration["Services:TerrariaLauncher.Services.GameCoordinator.Proxy:ConsulServiceTag"] },
                        Operator = ConsulOperator.In,
                        Right = new ConsulSelector() { Selector = "Tags" }
                    }
                }
            };

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var servicesQueryResult = await this.consulQueryDispatcher.Dispatch<GetServicesQuery, GetServicesQueryResult>(getServicesQuery, cancellationToken);
                    foreach (var (id, service) in servicesQueryResult.Services)
                    {
                        this._proxies.AddOrUpdate(id, true, (id, oldValue) =>
                        {
                            return true;
                        });
                    }

                    foreach (var (id, isUp) in this._proxies)
                    {
                        var checkHealthQuery = new GetLocalServiceHealth()
                        {
                            Id = id
                        };

                        try
                        {
                            var checkHealthResult = await this.consulQueryDispatcher.Dispatch<GetLocalServiceHealth, GetLocalServiceHealthResult>(checkHealthQuery, cancellationToken);
                            if (checkHealthResult.AggregateStatus != Commons.Consul.API.DTOs.ServiceHealthStatus.Passing)
                            {
                                this._proxies.TryUpdate(id, false, true);
                            }
                        }
                        catch
                        {
                            this._proxies.TryUpdate(id, false, true);
                        }
                    }

                    await Task.Delay(10_000);
                }
                catch { }
            }
        }

        public void Start()
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            _ = this.Refresh(this.cancellationTokenSource.Token);
        }

        public void Stop()
        {
            this.cancellationTokenSource.Cancel();
            this.cancellationTokenSource.Dispose();
        }
    }
}
