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
using TerrariaLauncher.Commons.Database.CQS.Query;
using TerrariaLauncher.Commons.DomainObjects;

namespace TerrariaLauncher.Services.GameCoordinator.Hub
{
    public class Startup
    {
        IConfiguration configuration;
        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
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
            services.AddSingleton<IQueryDispatcher, QueryDispatcher>();
            services.AddSingleton<ICommandDispatcher, CommandDispatcher>();
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsClass) continue;
                if (type.IsAbstract) continue;

                Type typeInterface;
                if ((typeInterface = type.GetInterface(typeof(IQueryHandler<,>).Name)) is not null)
                {
                    services.AddSingleton(typeInterface, type);
                }
                else if ((typeInterface = type.GetInterface(typeof(ICommandHandler<,>).Name)) is not null)
                {
                    services.AddSingleton(typeInterface, type);
                }
            }

            services.Configure<Dictionary<string, Instance>>(this.configuration.GetSection("TerrariaServerInstances"));

            services.AddSingleton<Services.Playing>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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
        }
    }
}
