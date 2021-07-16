using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Modules
{
    /// <summary>
    /// NetModule [1] - Text: Client -> Server
    /// </summary>
    class ChatModule : ModuleStructure
    {
        public enum CommandType
        {
            Help,
            Say,
            Party,
            Roll,
            Playing,
            Emoji,
            Emote,
            RockPaperScissors
        }

        private static IDictionary<CommandType, string> _RawCommandTypeLookup = new Dictionary<CommandType, string>()
        {
            { CommandType.Say, "Say" },
            { CommandType.Help, "Help" },
            { CommandType.Roll, "Roll" },
            { CommandType.Party, "Party" },
            { CommandType.Playing, "Playing" },
            { CommandType.Emoji, "Emoji" },
            { CommandType.Emote, "Emote" },
            { CommandType.RockPaperScissors, "RPS" }
        };
        private static IDictionary<string, CommandType> _CommandTypeLookup = new Dictionary<string, CommandType>()
        {
            {"Say", CommandType.Say },
            {"Help", CommandType.Help },
            {"Roll", CommandType.Roll },
            {"Party", CommandType.Party },
            {"Playing", CommandType.Playing },
            {"Emoji", CommandType.Emoji },
            {"Emote", CommandType.Emote },
            {"RPS", CommandType.RockPaperScissors }
        };

        public static IReadOnlyDictionary<CommandType, string> RawCommandTypeLookup = new ReadOnlyDictionary<CommandType, string>(_RawCommandTypeLookup);
        public static IReadOnlyDictionary<string, CommandType> CommandTypeLookup = new ReadOnlyDictionary<string, CommandType>(_CommandTypeLookup);

        public CommandType Command { get; set; }
        public string Text { get; set; }

        public override NetModuleId NetModuleId => NetModuleId.Text;
    }
}
