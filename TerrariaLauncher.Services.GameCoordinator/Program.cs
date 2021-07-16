using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Reflection;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.Consul;
using TerrariaLauncher.Commons.Consul.ConfigurationProvider;
using TerrariaLauncher.Commons.Consul.Extensions;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.BitFlags;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Modules;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Structures;
using TerrariaLauncher.Services.GameCoordinator.PoolPolicies;
using TerrariaLauncher.Services.GameCoordinator.Pools;
using TerrariaLauncher.Services.GameCoordinator.Proxy;
using TerrariaLauncher.Services.GameCoordinator.Proxy.Events;

namespace TerrariaLauncher.Services.GameCoordinator
{
    class GameCoordinator
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            using (IHost host = CreateHostBuilder(args).Build())
            {
                LoadProxyEvents(host.Services);
                await LoadPlugins(host.Services);
                var server = host.Services.GetRequiredService<Server>();
                await server.Start();
                await host.RunAsync();
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(configurationBuilder =>
                {
                    // => HostBuilderContext.Configuration
                })
                .ConfigureAppConfiguration((hostBuilderContext, configurationBuilder) =>
                {
                    // Prevent duplicating configuration providers.
                    // At this phase, these configuration providers are not added and not populated.
                    // These configuration providers will be automatically add by framework later.
                    IConfigurationBuilder tempConfigurationBuilder = new ConfigurationBuilder();
                    var env = hostBuilderContext.HostingEnvironment;
                    tempConfigurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                    tempConfigurationBuilder.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: false);
                    var tempConfigurationRoot = tempConfigurationBuilder.Build();

                    var consulHost = new ConsulHostConfiguration();
                    tempConfigurationRoot.GetSection("Consul").Bind(consulHost);
                    var consulConfigurationKey = tempConfigurationRoot["ConsulConfigurationProvider:Key"];
                    configurationBuilder.UseConsulConfiguration(consulHost, consulConfigurationKey);
                })
                .ConfigureServices(ConfigureCommonServices)
                .ConfigureServices(ConfigurePluginServices)
                .ConfigureServices(ConfigureTerrariaStructureSerializationServices)
                .ConfigureWebHostDefaults(webHostBuilder =>
                {
                    webHostBuilder
                    .ConfigureServices((webHostBuilderContext, serviceCollection) =>
                    {
                        serviceCollection.AddGrpc();
                    })
                    .Configure((webHostBuilderContext, applicationBuilder) =>
                    {
                        applicationBuilder.UseRouting();
                        applicationBuilder.UseEndpoints(endpoints =>
                        {

                        });
                    });
                })
            ;

        static void LoadProxyEvents(IServiceProvider serviceProvider)
        {
            var packetEvents = serviceProvider.GetRequiredService<PacketEvents>();
            var netModuleEvents = serviceProvider.GetRequiredService<NetModuleEvents>();
            var textCommands = serviceProvider.GetRequiredService<TextCommands>();
            packetEvents.NetModuleHandlers.Register(netModuleEvents.OnNetModule);
            netModuleEvents.ChatModuleHandlers.Register(textCommands.OnNetworkChatModule);
        }

        static async Task LoadPlugins(IServiceProvider serviceProvider)
        {
            var executingAssembly = Assembly.GetExecutingAssembly();
            var iPlugin = typeof(Plugins.IPlugin);
            foreach (var type in executingAssembly.GetTypes())
            {
                if (!type.IsClass) continue;
                if (type.IsAbstract) continue;
                if (!iPlugin.IsAssignableFrom(type)) continue;

#if DEBUG
                var packetDumpPlugin = serviceProvider.GetRequiredService<Plugins.PacketDump>();
                await packetDumpPlugin.Load();
#endif
                if (type == typeof(Plugins.PacketDump)) continue;

                var plugin = serviceProvider.GetRequiredService(type) as Plugins.IPlugin;
                await plugin.Load();
            }
        }

        static void ConfigureCommonServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<GameCoordinator>();
            serviceCollection.AddSingleton<Server>();
            serviceCollection.Configure<ServerOptions>(hostBuilderContext.Configuration.GetSection("ProxyServer"));
            serviceCollection.AddSingleton<ObjectPool<TerrariaPacket>>();
            serviceCollection.AddSingleton<IObjectPoolPolicy<TerrariaPacket>, TerrariaPacketPoolPolicy>();
            serviceCollection.AddSingleton<ObjectPool<PacketHandlerArgs>>();
            serviceCollection.AddSingleton<IObjectPoolPolicy<PacketHandlerArgs>, PacketHandlerArgsPoolPolicy>();

            serviceCollection.AddSingleton<TerrariaClientEvents>();
            serviceCollection.AddSingleton<InstanceClientEvents>();
            serviceCollection.AddSingleton<PacketEvents>();
            serviceCollection.AddSingleton<NetModuleEvents>();
            serviceCollection.AddSingleton<TextCommands>();

            var gameCoordinatorHubUrl = hostBuilderContext.Configuration["Services:TerrariaLauncher.Services.GameCoordinator.Hub:Url"];
            serviceCollection.AddGrpcClient<TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Instances.InstancesClient>(options =>
            {
                options.Address = new Uri(gameCoordinatorHubUrl);
            });
            serviceCollection.AddGrpcClient<TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Users.UsersClient>(options =>
            {
                options.Address = new Uri(gameCoordinatorHubUrl);
            });
            serviceCollection.AddGrpcClient<TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Bans.BansClient>(options =>
            {
                options.Address = new Uri(gameCoordinatorHubUrl);
            });

            serviceCollection.AddScoped<TerrariaClient>();
            serviceCollection.AddScoped<InstanceClient>();
            serviceCollection.AddScoped<InterceptorChannels>();
            serviceCollection.AddScoped<Interceptor>();

            var consulHost = new ConsulHostConfiguration();
            hostBuilderContext.Configuration.GetSection("Consul").Bind(consulHost);
            serviceCollection.AddConsulService(consulHost);
        }

        public static void ConfigurePluginServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<Plugins.Helpers.TerrariaPacketPoolHelper>();
            serviceCollection.AddSingleton<Plugins.Helpers.TextMessageHelper>();

            var executingAssembly = Assembly.GetExecutingAssembly();
            var iPlugin = typeof(Plugins.IPlugin);
            foreach (var type in executingAssembly.GetTypes())
            {
                if (!type.IsClass) continue;
                if (type.IsAbstract) continue;
                if (!iPlugin.IsAssignableFrom(type)) continue;

                serviceCollection.AddSingleton(type);
            }
        }

        public static void ConfigureTerrariaStructureSerializationServices(HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<IStructureSerializerLocator>(serviceProvider => new StructureSerializerLocator(serviceProvider));
            serviceCollection.AddSingleton<IStructureDeserializerLocator>(serviceProvider => new StructureDeserializerLocator(serviceProvider));

            serviceCollection.AddSingleton<IStructureSerializerDispatcher, StructureSerializerDispatcher>();
            serviceCollection.AddSingleton<IStructureDeserializerDispatcher, StructureDeserializerDispatcher>();

            // Packet Payloads
            serviceCollection.AddSingleton<IStructureSerializer<Connect>, ConnectSerializer>();
            serviceCollection.AddSingleton<IStructureDeserializer<Connect>, ConnectDeserializer>();

            serviceCollection.AddSingleton<IStructureSerializer<Disconnect>, DisconnectSerializer>();
            serviceCollection.AddSingleton<IStructureDeserializer<Disconnect>, DisconnectDeserializer>();

            serviceCollection.AddSingleton<IStructureSerializer<SyncPlayer>, SyncPlayerSerializer>();
            serviceCollection.AddSingleton<IStructureDeserializer<SyncPlayer>, SyncPlayerDeserializer>();

            serviceCollection.AddSingleton<IStructureSerializer<NetModule<TextModule>>, NetModuleSerializer<TextModule>>();
            serviceCollection.AddSingleton<IStructureDeserializer<NetModule<TextModule>>, NetModuleDeserializer<TextModule>>();

            serviceCollection.AddSingleton<IStructureSerializer<NetModule<ChatModule>>, NetModuleSerializer<ChatModule>>();
            serviceCollection.AddSingleton<IStructureDeserializer<NetModule<ChatModule>>, NetModuleDeserializer<ChatModule>>();

            // Modules
            serviceCollection.AddSingleton<IStructureSerializer<TextModule>, TextModuleSerializer>();
            serviceCollection.AddSingleton<IStructureDeserializer<TextModule>, TextModuleDeserializer>();

            serviceCollection.AddSingleton<IStructureSerializer<ChatModule>, ChatModuleSerializer>();
            serviceCollection.AddSingleton<IStructureDeserializer<ChatModule>, ChatModuleDeserializer>();

            // Structures
            serviceCollection.AddSingleton<IStructureSerializer<NetString>, NetStringSerializer>();
            serviceCollection.AddSingleton<IStructureDeserializer<NetString>, NetStringDeserializer>();

            serviceCollection.AddSingleton<IStructureSerializer<Color>, ColorSerializer>();
            serviceCollection.AddSingleton<IStructureDeserializer<Color>, ColorDeserializer>();

            serviceCollection.AddSingleton<IStructureSerializer<NetworkText>, NetworkTextSerializer>();
            serviceCollection.AddSingleton<IStructureDeserializer<NetworkText>, NetworkTextDeserializer>();

            serviceCollection.AddSingleton<IStructureSerializer<Vector2>, Vector2Serializer>();
            serviceCollection.AddSingleton<IStructureDeserializer<Vector2>, Vector2Deserializer>();

            // BitFlags
            serviceCollection.AddSingleton<IStructureSerializer<DifficultyFlags>, DifficultyFlagsSerializer>();
            serviceCollection.AddSingleton<IStructureDeserializer<DifficultyFlags>, DifficultyFlagsDeserializer>();

            serviceCollection.AddSingleton<IStructureSerializer<TorchFlags>, TorchFlagsSerializer>();
            serviceCollection.AddSingleton<IStructureDeserializer<TorchFlags>, TorchFlagsDeserializer>();
        }
    }
}
