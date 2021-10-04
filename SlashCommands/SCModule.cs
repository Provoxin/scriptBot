using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace scriptBot.SlashCommands
{
    public class SlashCommands : ApplicationCommandModule
    {
        [SlashCommand("script", "attaches the script of a workshop code")]
        public async Task SendScriptFile(InteractionContext ctx, [Option("code", "import code to get script of")] string code)
        {
            code = code.ToUpper();

            if (code.Length <  5 || code.Length > 7)
            {
                await CreateEphemeralMessage(ctx, "Code must be between 5 and 7 characters long");
                return;
            }

            if (!Regex.IsMatch(code, "^[A-Z0-9]+$"))
            {
                await CreateEphemeralMessage(ctx, "Code must contain only alphanumeric characters");
                return;
            }

            if (code.Contains(new char[] { 'U' }))
            {
                await CreateEphemeralMessage(ctx, "Code cannot contain 'U'");
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            ScriptBot.AddToQueue(RequestType.Script, ctx, code);
        }

        public async Task CreateEphemeralMessage(InteractionContext ctx, string message)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent(message)
                    .AsEphemeral(true));
        }
    }
}
