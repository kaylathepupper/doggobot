﻿using System;
using System.IO;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Addons.InteractiveCommands;

using Microsoft.Extensions.DependencyInjection;

using DoggoBot.Core.Services.Audio;
using DoggoBot.Core.Services.Configuration.Bot;
using DoggoBot.Common.Handlers.CommandHandler;

namespace DoggoBot
{
    public class Start
    {
        internal static void Main(string[] args)
            => new Start().BeginStartupAsync().GetAwaiter().GetResult();

        private DiscordSocketClient borkClient;
        private CommandService borkCommands;

        private async Task BeginStartupAsync()
        {
            borkClient = new DiscordSocketClient(new DiscordSocketConfig()
            {
#if DEBUG
                LogLevel = LogSeverity.Verbose,
#else
                LogLevel = LogSeverity.Info,
#endif
                MessageCacheSize = 100
            });

            borkCommands = new CommandService(new CommandServiceConfig()
            {
                LogLevel = LogSeverity.Verbose,
                DefaultRunMode = RunMode.Async,
                CaseSensitiveCommands = false
            });

            var services = GenerateServices();
            var secrets  = services.GetRequiredService<BotConfiguration>().LoadSecrets();
            services.GetRequiredService<BotConfiguration>().LoadBlockedUsers();
            await services.GetRequiredService<CommandHandler>().InitAsync();

            borkClient.Log += Logger;
            borkCommands.Log += Logger;

            await borkClient.LoginAsync(TokenType.Bot, secrets.DiscordToken);
            await borkClient.StartAsync();

            await borkClient.SetGameAsync(secrets.DiscordGame);

            await Task.Delay(-1);
        }

        private Task Logger(LogMessage msg)
        {
#if DEBUG
            string logFileMsg = $"[{DateTime.Now.ToString("MM/dd/yyyy - HH:mm:ss")}] ({msg.Severity}) {msg.Message} {msg.Exception}";
            string consoleLogMsg = logFileMsg;
#else
            string logFileMsg = $"[{DateTime.Now.ToString("MM/dd/yyyy - HH:mm:ss")}] ({msg.Severity}) {msg.Message} {msg.Exception}";
            string consoleLogMsg = $"[{DateTime.Now.ToString("MM/dd/yyyy - HH:mm:ss")}] ({msg.Severity}) {msg.Message}";
#endif

            Console.WriteLine(consoleLogMsg);

            using (StreamWriter file = new StreamWriter(File.Open(@"Data/Logging/FullLog.txt", FileMode.Append)))
                file.Write(logFileMsg + Environment.NewLine);

            return Task.CompletedTask;
        }

        private IServiceProvider GenerateServices()
        {
            return new ServiceCollection()
                // Core
                .AddSingleton(borkClient)
                .AddSingleton(borkCommands)
                .AddSingleton<CommandHandler>()
                // Configuration
                .AddSingleton<BotConfiguration>()
                // Other
                .AddSingleton<AudioService>()
                .AddSingleton<InteractiveService>()
                .BuildServiceProvider();
        }
    }
}
