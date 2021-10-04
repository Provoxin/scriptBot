using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Threading.Tasks;

namespace scriptBot.SlashCommands
{
    public class OwnerCommands : ApplicationCommandModule
    {
        [SlashCommand("pause", "pause running the queue")]
        public async Task Pause(InteractionContext ctx)
        {
            await ctx.Client.UpdateStatusAsync(new DiscordActivity("paused"), UserStatus.DoNotDisturb);
            await CreateEphemeralMessage(ctx, "Queue paused");

            ScriptBot.paused = true;
        }

        [SlashCommand("unpause", "resume running the queue")]
        public async Task Unpause(InteractionContext ctx)
        {
            await ctx.Client.UpdateStatusAsync(new DiscordActivity("paused", ActivityType.Playing), UserStatus.Idle);
            await CreateEphemeralMessage(ctx, "Queue resumed");

            ScriptBot.paused = false;
        }

        public async Task CreateEphemeralMessage(InteractionContext ctx, string message)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                                                                                                .WithContent(message)
                                                                                                .AsEphemeral(true));
        }
    }
}
