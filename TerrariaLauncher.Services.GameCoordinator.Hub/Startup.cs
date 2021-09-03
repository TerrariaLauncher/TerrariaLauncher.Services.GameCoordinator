using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Database;
using TerrariaLauncher.Commons.Database.CQS.Command;
using TerrariaLauncher.Commons.Database.CQS.Extensions;
using TerrariaLauncher.Commons.Database.CQS.Query;
using TerrariaLauncher.Commons.DomainObjects;
using TerrariaLauncher.Commons.EventBusRabbitMQ;
using TerrariaLauncher.Commons.MediatorService;
using TerrariaLauncher.Services.GameCoordinator.Hub.Services;

namespace TerrariaLauncher.Services.GameCoordinator.Hub
{
    public class Startup
    {
        IConfiguration configuration;
        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc();
            services.AddHealthChecks();

            services.AddSingleton<IUnitOfWorkFactory, UnitOfWorkFactory>(serviceProvider =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var connectionString = configuration.GetConnectionString("Database");
                return new UnitOfWorkFactory(connectionString);
            });

            services.AddDatabaseCQS().AddHandlers(Assembly.GetExecutingAssembly());
            services.AddSingleton<RunningProxies>();
            services.AddSingleton<Playing>();
            services.AddSingleton<RunningAgents>();
            services.AddEventBusRabbitMQ(new EventBusRabbitMQConfiguration()
            {
                Host = configuration.GetValue<string>("EventBusRabbitMQ:Host"),
                Port = configuration.GetValue<int>("EventBusRabbitMQ:Port"),
                UserName = configuration.GetValue<string>("EventBusRabbitMQ:UserName"),
                Password = configuration.GetValue<string>("EventBusRabbitMQ:Password"),
                ExchangeName = configuration["EventBusRabbitMQ:ExchangeName"],
                QueueName = configuration.GetValue<string>("EventBusRabbitMQ:QueueName"),
                RetryCount = configuration.GetValue<int>("EventBusRabbitMQ:RetryCount")
            });
            services.AddMediatorService().AddMediatorHandlers(Assembly.GetExecutingAssembly());
        }

        public void Configure(
            IHostApplicationLifetime host,
            IApplicationBuilder app, IWebHostEnvironment env,
            RunningProxies runningProxies,
            RunningAgents runningAgents)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
                endpoints.MapGrpcService<GrpcServices.Instances>();
                endpoints.MapGrpcService<GrpcServices.Players>();
                endpoints.MapGrpcService<GrpcServices.Users>();
                endpoints.MapGrpcService<GrpcServices.Bans>();
            });

            host.ApplicationStarted.Register(runningProxies.Start);
            host.ApplicationStopping.Register(runningProxies.Stop);
            host.ApplicationStarted.Register(runningAgents.Start);
            host.ApplicationStopping.Register(runningAgents.Stop);
        }
    }
}
