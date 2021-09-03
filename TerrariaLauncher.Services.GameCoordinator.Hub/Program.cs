using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Consul;
using TerrariaLauncher.Commons.Consul.API.Agent.Services.Commands;
using TerrariaLauncher.Commons.Consul.API.Commons;
using TerrariaLauncher.Commons.Consul.ConfigurationProvider;
using TerrariaLauncher.Commons.Consul.Extensions;
using TerrariaLauncher.Commons.DomainObjects;

namespace TerrariaLauncher.Services.GameCoordinator.Hub
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            using (var host = CreateHostBuilder(args).Build())
            {
                var consulCommandDispatcher = host.Services.GetRequiredService<IConsulCommandDispatcher>();
                var configuration = host.Services.GetRequiredService<IConfiguration>();

                var hostUrl = configuration["urls"].Trim().Split(';', 1, StringSplitOptions.TrimEntries)[0];
                var hostUri = new Uri(hostUrl);

                var registration = configuration.GetSection("ConsulServiceRegister").Get<Commons.Consul.API.DTOs.Registration>();
                var command = new RegisterServiceCommand()
                {
                    ReplaceExistingChecks = true,
                    Registration = registration
                };
                command.Registration.Address = hostUri.Host;
                command.Registration.Port = hostUri.Port;
                command.Registration.Check.TCP = hostUri.Authority;

                await consulCommandDispatcher.Dispatch<RegisterServiceCommand, RegisterServiceCommandResult>(command);
                await host.RunAsync();
            }
        }

        // Additional configuration is required to successfully run gRPC on macOS.
        // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostBuilderContent, configurationBuilder) =>
                {
                    IConfigurationBuilder tempConfigurationBuilder = new ConfigurationBuilder();
                    var env = hostBuilderContent.HostingEnvironment;
                    tempConfigurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                    tempConfigurationBuilder.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);
                    var tempConfigurationRoot = tempConfigurationBuilder.Build();

                    var consulHost = new Commons.Consul.ConsulHostConfiguration();
                    tempConfigurationRoot.GetSection("Consul").Bind(consulHost);
                    var consulConfigurationKey = tempConfigurationRoot["ConsulConfiguration:Key"];

                    configurationBuilder.UseConsulConfiguration(consulHost, consulConfigurationKey);
                })
                .ConfigureServices((hostBuilderContext, serviceCollection) =>
                {
                    serviceCollection.AddConsulService(config =>
                    {
                        hostBuilderContext.Configuration.GetSection("Consul").Bind(config);
                    });
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
