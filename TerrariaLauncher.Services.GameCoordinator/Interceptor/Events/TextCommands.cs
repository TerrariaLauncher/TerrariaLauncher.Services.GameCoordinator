using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator
{
    class TextCommandHandlerArgs : HandlerArgs
    {
        public string Command { get; set; }
        public Memory<string> Arguments { get; set; }
    }

    class TextCommand
    {
        public string Command { get; set; }
        public Handler<Interceptor, TextCommandHandlerArgs> Handler { get; set; }
    }

    class TextCommands
    {
        /// <summary>
        /// https://regex101.com/r/uK6kU6
        /// </summary>
        public static readonly Regex CommandRegex = new Regex(@"(""[^""]+""|[^\s""]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        public bool Register(TextCommand textCommand)
        {
            var handlers = new HandlerList<Interceptor, TextCommandHandlerArgs>();
            handlers.Register(textCommand.Handler);
            return this.textCommandHandlers.TryAdd(textCommand.Command, handlers);
        }

        public bool Deregister(TextCommand textCommand)
        {
            if (this.textCommandHandlers.Remove(textCommand.Command, out var handlers))
            {
                handlers.Deregister(textCommand.Handler);
                return true;
            }

            return false;
        }

        private Dictionary<string, HandlerList<Interceptor, TextCommandHandlerArgs>> textCommandHandlers = new Dictionary<string, HandlerList<Interceptor, TextCommandHandlerArgs>>(StringComparer.OrdinalIgnoreCase);

        internal async Task OnNetworkChatModule(Interceptor interceptor, ChatModuleHandlerArgs args)
        {
            if (args.TerrariaPacket.Origin != PacketOrigin.Client) return;
            var matches = CommandRegex.Matches(args.ChatModule.Text);
            if (matches.Count < 0) return;

            var parts = matches.Select(match => match.Value).ToArray();
            if (parts[0][0] != '/' && parts[0][0] != '.') return;
            var command = parts[0].Substring(1);

            if (!this.textCommandHandlers.TryGetValue(command, out var handlers))
            {
                return;
            }

            args.Ignored = true;
            args.Handled = true;
            var textCommandHandlerArgs = new TextCommandHandlerArgs()
            {
                CancellationToken = args.CancellationToken,
                Handled = args.Handled,
                Command = command,
                Arguments = new Memory<string>(parts).Slice(1)
            };
            await handlers.Invoke(interceptor, textCommandHandlerArgs);
        }
    }
}
