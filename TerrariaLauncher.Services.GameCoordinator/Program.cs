using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Threading.Tasks;

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
            var serviceCollection = InitializeServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider(true);
            var server = serviceProvider.GetRequiredService<Server>();

            var packetEvents = serviceProvider.GetRequiredService<PacketEvents>();
            var netModuleEvents = serviceProvider.GetRequiredService<NetModuleEvents>();
            var textCommands = serviceProvider.GetRequiredService<TextCommands>();
            packetEvents.NetModuleHandlers.Register(netModuleEvents.OnNetModule);
            netModuleEvents.ChatModuleHandlers.Register(textCommands.OnNetworkChatModule);

#if DEBUG
            var packetDumpPlugin = serviceProvider.GetRequiredService<Plugins.PacketDump>();
            await packetDumpPlugin.Load();
#endif
            var joinPlugin = serviceProvider.GetRequiredService<Plugins.Bans>();
            await joinPlugin.Load();

            var fallBackPlugin = serviceProvider.GetRequiredService<Plugins.Fallback>();
            await fallBackPlugin.Load();

            var characterNamesPlugin = serviceProvider.GetRequiredService<Plugins.CharacterNames>();
            await characterNamesPlugin.Load();

            var loginPlugin = serviceProvider.GetRequiredService<Plugins.Login>();
            await loginPlugin.Load();

            await server.Start();
            ConsoleKeyInfo keyInfo;
            do
            {
                keyInfo = Console.ReadKey();
            } while (keyInfo.Key != ConsoleKey.Q);
        }

        static IServiceCollection InitializeServiceCollection()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<GameCoordinator>();
            serviceCollection.AddSingleton<Server>();
            serviceCollection.AddSingleton<ServerOptions>(serviceProvider => new ServerOptions()
            {
                Host = "localhost",
                Port = 7777,
                MaxPlayer = 300
            });
            serviceCollection.AddSingleton<ObjectPool<TerrariaPacket>>();
            serviceCollection.AddSingleton<IObjectPoolPolicy<TerrariaPacket>, TerrariaPacketPoolPolicy>();
            serviceCollection.AddSingleton<ObjectPool<PacketHandlerArgs>>();
            serviceCollection.AddSingleton<IObjectPoolPolicy<PacketHandlerArgs>, PacketHandlerArgsPoolPolicy>();

            serviceCollection.AddSingleton<TerrariaClientSocketEvents>();
            serviceCollection.AddSingleton<PacketEvents>();
            serviceCollection.AddSingleton<NetModuleEvents>();
            serviceCollection.AddSingleton<TextCommands>();

            serviceCollection.AddGrpcClient<TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Users.UsersClient>(options =>
            {
                options.Address = new Uri("http://localhost:3300");
            });
            serviceCollection.AddGrpcClient<TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Bans.BansClient>(options =>
            {
                options.Address = new Uri("http://localhost:3300");
            });

            serviceCollection.AddScoped<TerrariaClient>();
            serviceCollection.AddScoped<InstanceClient>();
            serviceCollection.AddScoped<InterceptorChannels>();
            serviceCollection.AddScoped<Interceptor>();

            serviceCollection.AddSingleton<Plugins.Fallback>();
            serviceCollection.AddSingleton<Plugins.PacketDump>();
            serviceCollection.AddSingleton<Plugins.CharacterNames>();
            serviceCollection.AddSingleton<Plugins.Login>();
            serviceCollection.AddSingleton<Plugins.Bans>();

            PacketSerializationCollection.ConfigureServices(serviceCollection);

            return serviceCollection;
        }
    }
}
