using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;

namespace TerrariaLauncher.Services.GameCoordinator
{
    class Program
    {
        static void Main(string[] args)
        {
            var serviceCollection = InitializeServiceCollection();
            var serviceProvider = serviceCollection.BuildServiceProvider(true);
            var server = serviceProvider.GetRequiredService<Server>();
            server.Start();

            var packetHandler = serviceProvider.GetRequiredService<PacketHandlers>();
            var fallBackPlugin = serviceProvider.GetRequiredService<Plugins.Fallback>();
            packetHandler.ConnectHandlers.Register(fallBackPlugin.OnConnect);

            Console.ReadKey();
        }

        static IServiceCollection InitializeServiceCollection()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<Server>();
            serviceCollection.AddSingleton<ServerOptions>(serviceProvider => new ServerOptions()
            {
                Host = "127.0.0.1",
                Port = 7777,
                MaxPlayer = 300
            });
            serviceCollection.AddSingleton<ObjectPool<TerrariaPacket>>();
            serviceCollection.AddSingleton<IObjectPoolPolicy<TerrariaPacket>, TerrariaPacketPoolPolicy>(serviceProvider => new TerrariaPacketPoolPolicy(ArrayPool<byte>.Shared));
            serviceCollection.AddSingleton<ObjectPool<PacketHandlerArgs>>();
            serviceCollection.AddSingleton<IObjectPoolPolicy<PacketHandlerArgs>, PacketHandlerArgsPoolPolicy>(serviceProvider => new PacketHandlerArgsPoolPolicy());

            serviceCollection.AddScoped<TerrariaClient>();
            serviceCollection.AddScoped<InstanceClient>();
            serviceCollection.AddScoped<InterceptorChannels>();
            serviceCollection.AddScoped<Interceptor>();

            serviceCollection.AddSingleton<PacketHandlers>();
            serviceCollection.AddSingleton<HookManager>();
            serviceCollection.AddSingleton<ServerHooks>();

            serviceCollection.AddSingleton<Plugins.Fallback>();

            return serviceCollection;
        }
    }
}
