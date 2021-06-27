using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.BitFlags;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Modules;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Structures;

namespace TerrariaLauncher.Services.GameCoordinator
{
    static class PacketSerializationCollection
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IStructureSerializerLocator>(serviceProvider => new StructureSerializerLocator(serviceProvider));
            services.AddSingleton<IStructureDeserializerLocator>(serviceProvider => new StructureDeserializerLocator(serviceProvider));

            services.AddSingleton<IStructureSerializerDispatcher, StructureSerializerDispatcher>();
            services.AddSingleton<IStructureDeserializerDispatcher, StructureDeserializerDispatcher>();

            // Packet Payloads
            services.AddSingleton<IStructureSerializer<Connect>, ConnectSerializer>();
            services.AddSingleton<IStructureDeserializer<Connect>, ConnectDeserializer>();

            services.AddSingleton<IStructureSerializer<Disconnect>, DisconnectSerializer>();
            services.AddSingleton<IStructureDeserializer<Disconnect>, DisconnectDeserializer>();

            services.AddSingleton<IStructureSerializer<SyncPlayer>, SyncPlayerSerializer>();
            services.AddSingleton<IStructureDeserializer<SyncPlayer>, SyncPlayerDeserializer>();

            services.AddSingleton<IStructureSerializer<NetModule<TextModule>>, NetModuleSerializer<TextModule>>();
            services.AddSingleton<IStructureDeserializer<NetModule<TextModule>>, NetModuleDeserializer<TextModule>>();

            services.AddSingleton<IStructureSerializer<NetModule<ChatModule>>, NetModuleSerializer<ChatModule>>();
            services.AddSingleton<IStructureDeserializer<NetModule<ChatModule>>, NetModuleDeserializer<ChatModule>>();

            // Modules
            services.AddSingleton<IStructureSerializer<TextModule>, TextModuleSerializer>();
            services.AddSingleton<IStructureDeserializer<TextModule>, TextModuleDeserializer>();

            services.AddSingleton<IStructureSerializer<ChatModule>, ChatModuleSerializer>();
            services.AddSingleton<IStructureDeserializer<ChatModule>, ChatModuleDeserializer>();

            // Structures
            services.AddSingleton<IStructureSerializer<NetString>, NetStringSerializer>();
            services.AddSingleton<IStructureDeserializer<NetString>, NetStringDeserializer>();

            services.AddSingleton<IStructureSerializer<Color>, ColorSerializer>();
            services.AddSingleton<IStructureDeserializer<Color>, ColorDeserializer>();

            services.AddSingleton<IStructureSerializer<NetworkText>, NetworkTextSerializer>();
            services.AddSingleton<IStructureDeserializer<NetworkText>, NetworkTextDeserializer>();

            services.AddSingleton<IStructureSerializer<Vector2>, Vector2Serializer>();
            services.AddSingleton<IStructureDeserializer<Vector2>, Vector2Deserializer>();

            // BitFlags
            services.AddSingleton<IStructureSerializer<DifficultyFlags>, DifficultyFlagsSerializer>();
            services.AddSingleton<IStructureDeserializer<DifficultyFlags>, DifficultyFlagsDeserializer>();

            services.AddSingleton<IStructureSerializer<TorchFlags>, TorchFlagsSerializer>();
            services.AddSingleton<IStructureDeserializer<TorchFlags>, TorchFlagsDeserializer>();
        }
    }
}
