using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;

namespace Mika_Bot
{
    class Program
    {
        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;

        public static Hashtable loopEnabled = new Hashtable();
        public static Hashtable searchResults = new Hashtable();
        public static Hashtable currentSong = new Hashtable();
        public static Hashtable queue = new Hashtable();
        public static Hashtable streams = new Hashtable();
        public static Hashtable prefix = new Hashtable();

        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient();
            _commands = new CommandService();

            //var _interactionService = new InteractionService(_client.Rest);

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_commands)
                .AddSingleton<InteractiveService>()
                .BuildServiceProvider();

            string token = Environment.GetEnvironmentVariable("DISCORD_API_TOKEN", EnvironmentVariableTarget.User);

            _client.Log += _client_Log;

            _client.ButtonExecuted += SearchButtonHandler;

            await _client.SetGameAsync("Mika-Bot jetzt in C#! :D");

            await RegisterCommandsAsync();

            await _client.LoginAsync(TokenType.Bot, token);

            await _client.StartAsync();

            await Task.Delay(-1);
        }

        public async Task SearchButtonHandler(SocketMessageComponent component)
        {
            switch (component.Data.CustomId)
            {
                case "one":
                    await component.RespondAsync($"{component.User.Mention} hat Nummer 1 gewählt.");

                    

                    break;
                case "two":
                    await component.RespondAsync($"{component.User.Mention} hat Nummer 2 gewählt.");
                    break;
                case "three":
                    await component.RespondAsync($"{component.User.Mention} hat Nummer 3 gewählt.");
                    break;
                case "four":
                    await component.RespondAsync($"{component.User.Mention} hat Nummer 4 gewählt.");
                    break;
                case "five":
                    await component.RespondAsync($"{component.User.Mention} hat Nummer 5 gewählt.");
                    break;
            }
        }

        private Task _client_Log(LogMessage arg)
        {
            Console.WriteLine(arg);
            return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync()
        {
            _client.MessageReceived += HandleCommandsAsync;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        private async Task HandleCommandsAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;
            var context = new SocketCommandContext(_client, message);

            if (message.Author.IsBot) return;

            int argPos = 0;

            if (message.HasStringPrefix("mika ", ref argPos))
            {
                var result = await _commands.ExecuteAsync(context, argPos, _services);

                if (!result.IsSuccess) 
                { 
                    Console.WriteLine(result.ErrorReason);

                    if (result.ErrorReason == "The input text has too few parameters.")
                    {
                        await context.Channel.SendMessageAsync("Du hast zu wenig Parameter eingegeben.");
                        return;
                    }

                    await context.Channel.SendMessageAsync(result.ErrorReason);
                }
            }
        }
    }
}
