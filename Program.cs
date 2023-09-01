using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace SharpBot
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var config = new DiscordConfiguration()
            {
                Token = "DiscordBotToken",
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All,
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug
            };
            var discord = new DiscordClient(config);
            discord.UseInteractivity(new InteractivityConfiguration()
            {
                PollBehaviour = PollBehaviour.KeepEmojis,
                Timeout = TimeSpan.FromSeconds(30)
            });
            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new[] { "!" },
            });
            //commands.RegisterCommands<MusicModule>();
            //commands.RegisterCommands<BaseModule>();
            var endpoint = new ConnectionEndpoint
            {
                Hostname = "127.0.0.1", // From your server configuration.
                Port = 2333 // From your server configuration
            };

            var lavalinkConfig = new LavalinkConfiguration
            {
                Password = "youshallnotpass", // From your server configuration.
                RestEndpoint = endpoint,
                SocketEndpoint = endpoint
            };
            discord.SocketOpened += SocketOpenedHandler;
            discord.SocketClosed += SocketClosedHandler;
            var lavalink = discord.UseLavalink();
            commands.RegisterCommands(Assembly.GetExecutingAssembly());
            await discord.ConnectAsync();
            await lavalink.ConnectAsync(lavalinkConfig);
            var node = lavalink.ConnectedNodes.Values.First();
            node.PlaybackFinished += MusicModule.PlaybackFinished;
            node.PlaybackStarted += MusicModule.PlaybackStarted;
            await Task.Delay(-1);
        }

        public static async Task SocketOpenedHandler(DiscordClient s, SocketEventArgs e)
        {
            MusicModule.GetVariables();
            Process.Start($@"{AppDomain.CurrentDomain.BaseDirectory}\\StartLavaLink.bat");
        }
        public static async Task SocketClosedHandler(DiscordClient s, SocketCloseEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }
    }
}