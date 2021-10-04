using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Threading.Tasks;
using System.IO;
using Deltin.CustomGameAutomation;
using System;
using scriptBot;
using System.Text.RegularExpressions;

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
                await CreateErrorMessage(ctx, "Code must be between 5 and 7 characters long");
                return;
            }

            if (!Regex.IsMatch(code, "^[A-Z0-9]+$"))
            {
                await CreateErrorMessage(ctx, "Code must contain only alphanumeric characters");
                return;
            }

            if (code.Contains(new char[] { 'U', '5' }))
            {
                await CreateErrorMessage(ctx, "Code cannot contain 'U' or '5'");
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            ScriptBot.AddToQueue(RequestType.Script, ctx, code);
        }

        public async Task CreateErrorMessage(InteractionContext ctx, string message)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent(message)
                    .AsEphemeral(true));
        }
    }
}
