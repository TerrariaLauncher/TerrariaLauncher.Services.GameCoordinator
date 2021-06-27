using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Plugins
{
    class Login : Plugin
    {
        TextCommands textCommands;
        Plugins.CharacterNames characterNames;
        TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Users.UsersClient usersClient;

        TextCommand textCommand;
        public Login(
            TextCommands textCommands,
            Plugins.CharacterNames characterNames,
            TerrariaLauncher.Protos.Services.GameCoordinator.Hub.Users.UsersClient usersClient)
        {
            this.textCommands = textCommands;
            this.characterNames = characterNames;
            this.usersClient = usersClient;

            this.textCommand = new TextCommand()
            {
                Command = "login",
                Handler = this.OnLoginCommand
            };
        }

        public override Task Load(CancellationToken cancellationToken = default)
        {
            this.textCommands.Register(this.textCommand);
            return Task.CompletedTask;
        }

        public override Task Unload(CancellationToken cancellationToken = default)
        {
            this.textCommands.Deregister(this.textCommand);
            return Task.CompletedTask;
        }

        public async Task OnLoginCommand(Interceptor interceptor, TextCommandHandlerArgs args)
        {
            string name = "";
            string password = "";
            if (args.Arguments.IsEmpty)
            {
                return;
            }

            if (args.Arguments.Length == 2)
            {
                name = args.Arguments.Span[0];
                password = args.Arguments.Span[1];
            }

            if (args.Arguments.Length == 1)
            {
                name = this.characterNames.GetCharacterName(interceptor);
                password = args.Arguments.Span[0];
            }

            var loginResponse = await this.usersClient.LoginAsync(new Protos.Services.GameCoordinator.Hub.LoginRequest()
            {
                Name = name,
                Password = password
            }, cancellationToken: args.CancellationToken);
        }
    }
}
