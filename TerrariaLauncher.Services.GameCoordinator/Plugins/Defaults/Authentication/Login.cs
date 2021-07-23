using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Plugins.Helpers;
using TerrariaLauncher.Services.GameCoordinator.Proxy;
using TerrariaLauncher.Services.GameCoordinator.Proxy.Events;

using HubGrpc = TerrariaLauncher.Protos.Services.GameCoordinator.Hub;

namespace TerrariaLauncher.Services.GameCoordinator.Plugins
{
    class PreLoginArgs : HandlerArgs
    {
        public string PlayerName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool Cancel { get; set; }
    }

    class PostLoginArgs : HandlerArgs
    {
        public string PlayerName { get; set; }
        public string UserName { get; set; }
    }

    class Login : Plugin
    {
        TextCommands textCommands;
        Plugins.CharacterNames characterNames;
        HubGrpc.Users.UsersClient usersClient;
        TextMessageHelper textMessageHelper;
        TerrariaPacketPoolHelper terrariaPacketPoolHelper;
        ILogger<Login> logger;
        ILoggerFactory loggerFactory;

        TextCommand textCommand;
        public Login(
            TextCommands textCommands,
            Plugins.CharacterNames characterNames,
            HubGrpc.Users.UsersClient usersClient,
            TextMessageHelper textMessageHelper,
            TerrariaPacketPoolHelper terrariaPacketPoolHelper,
            ILogger<Login> logger,
            ILoggerFactory loggerFactory)
        {
            this.textCommands = textCommands;
            this.characterNames = characterNames;
            this.usersClient = usersClient;
            this.textMessageHelper = textMessageHelper;
            this.terrariaPacketPoolHelper = terrariaPacketPoolHelper;
            this.logger = logger;
            this.loggerFactory = loggerFactory;

            this.PreLoginHandlers = new HandlerList<Interceptor, PreLoginArgs>(
                this.loggerFactory.CreateLogger($"{typeof(Login).FullName}.{nameof(this.PreLoginHandlers)}")
            );
            this.PostLoginHandlers = new HandlerList<Interceptor, PostLoginArgs>(
                this.loggerFactory.CreateLogger($"{typeof(Login).FullName}.{nameof(this.PostLoginHandlers)}")
            );

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

        public readonly HandlerList<Interceptor, PreLoginArgs> PreLoginHandlers;
        public readonly HandlerList<Interceptor, PostLoginArgs> PostLoginHandlers;

        public async Task OnLoginCommand(Interceptor interceptor, TextCommandHandlerArgs args)
        {
            string name = "";
            string password = "";
            if (args.Arguments.IsEmpty)
            {
                await this.textMessageHelper.SendWarningMessage("Login command received no arguments!", interceptor.TerrariaClient, args.CancellationToken);
                await this.textMessageHelper.SendWarningMessage("Proper syntax: /login \"<name>\" \"<password>\"", interceptor.TerrariaClient, args.CancellationToken);
                return;
            }

            var playerName = this.characterNames.GetCharacterName(interceptor);

            if (args.Arguments.Length == 2)
            {
                name = args.Arguments.Span[0];
                password = args.Arguments.Span[1];
            }

            if (args.Arguments.Length == 1)
            {
                name = playerName;
                password = args.Arguments.Span[0];
            }

            var preLoginArgs = new PreLoginArgs()
            {
                PlayerName = playerName,
                UserName = name,
                Password = password,
                CancellationToken = args.CancellationToken
            };
            await this.PreLoginHandlers.Invoke(interceptor, preLoginArgs);
            if (preLoginArgs.Cancel) return;

            try
            {
                var loginResponse = await this.usersClient.LoginAsync(new Protos.Services.GameCoordinator.Hub.LoginRequest()
                {
                    PlayerName = playerName,
                    Name = name,
                    Password = password
                }, cancellationToken: args.CancellationToken);
            }
            catch (RpcException ex)
            {
                if (ex.StatusCode == StatusCode.NotFound)
                {

                    var fieldName = ex.Trailers.GetValue($"not-found");
                    if (fieldName == HubGrpc.LoginRequest.Descriptor.FindFieldByNumber(HubGrpc.LoginRequest.PlayerNameFieldNumber).Name)
                    {
                        await this.textMessageHelper.SendErrorMessage("System is having a serious problem: [-_-] bR0K3n.", interceptor.TerrariaClient, cancellationToken: args.CancellationToken);
                        this.logger.LogDebug($"Player '{playerName}' was did not found on GameCoordinator.Hub.");
                    }
                    else if (fieldName == HubGrpc.LoginRequest.Descriptor.FindFieldByNumber(HubGrpc.LoginRequest.NameFieldNumber).Name)
                    {
                        await this.textMessageHelper.SendErrorMessage($"User '{name}' [c/000000:not found]! Please carefully check your user name once again or contact maintainers if this problem still happens.", interceptor.TerrariaClient, cancellationToken: args.CancellationToken);
                    }
                }
                else if (ex.StatusCode == StatusCode.InvalidArgument)
                {
                    await this.textMessageHelper.SendErrorMessage($"Password does not match!", interceptor.TerrariaClient, cancellationToken: args.CancellationToken);
                }
                else
                {
                    throw;
                }

                return;
            }

            await this.textMessageHelper.SendSuccessMessage("You logged in successfully, enjoy!", interceptor.TerrariaClient, args.CancellationToken);

            var postLoginArgs = new PostLoginArgs()
            {
                PlayerName = playerName,
                UserName = name,
                CancellationToken = args.CancellationToken
            };
            await this.PostLoginHandlers.Invoke(interceptor, postLoginArgs);
        }
    }
}
