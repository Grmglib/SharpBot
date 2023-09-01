using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace SharpBot
{
    public class BaseModule : BaseCommandModule
    {
        public DiscordClient discord { private get; set; }
        [Command("disc")]
        public async Task Disconnect(CommandContext ctx)
        {
            await ctx.RespondAsync("Desconectando");
            var x = ctx.Client.DisconnectAsync();
        }

        [Command("wel")]
        public async Task GreetCommand(CommandContext ctx)
        {
            await ctx.RespondAsync("Welcome!");
        }

        [Command("mult")]
        public async Task Mult(CommandContext ctx, int first, int second)
        {
            await ctx.RespondAsync((first * second).ToString());
        }
    }
}