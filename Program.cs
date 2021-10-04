using DSharpPlus;
using DSharpPlus.SlashCommands;
using System.IO;
using System.Threading.Tasks;
using scriptBot.SlashCommands;
using Deltin.CustomGameAutomation;
using System.Collections.Generic;
using DSharpPlus.Entities;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;
using System;
using System.Collections.Concurrent;

enum Server : long
{
    Test = 737699811351855155,
    GH = 710669322447749121
}

namespace scriptBot
{
    public struct Command
    {
        public RequestType requestType;
        public InteractionContext ctx;
        public string code;

        public Command(RequestType requestType, InteractionContext ctx)
        {
            this.requestType = requestType;
            this.ctx = ctx;
            this.code = null;
        }

        public Command(RequestType requestType, InteractionContext ctx, string code)
        {
            this.requestType = requestType;
            this.ctx = ctx;
            this.code = code;
        }
    }

    public enum RequestType
    {
        Script,  // requesting the given code's script
        Popular  // requesting to see the popular tab (not implemented)
    }

    class ScriptBot
    {
        private static CustomGame cg;
        private static BlockingCollection<Command> commandQueue;
        //private static BlockingCollection<(string code, InteractionContext ctx, RequestType requestType)> commandQueue;
        private static DiscordClient discord;

        static void Main(string[] args)
        {
            commandQueue = new BlockingCollection<Command>();
            //commandQueue = new BlockingCollection<(string code, InteractionContext ctx, RequestType requestType)>();  // taking from a blockingcollection waits for an element to appear wait when no elements remain
            SetupCustomGame();

            MainAsync().GetAwaiter().GetResult();
        }

        private static void SetupCustomGame()
        {
            if (CustomGame.GetOverwatchProcess() == null)
            {
                string[] owInfoText = File.ReadAllLines("owInfo.txt");
                var owInfo = new OverwatchInfoAuto();
                owInfo.BattlenetExecutableFilePath = owInfoText[0];
                owInfo.OverwatchSettingsFilePath = owInfoText[1];
                Console.WriteLine("starting overwatch...");
                CustomGame.StartOverwatch(owInfo);
                Console.WriteLine("success\n");
            }

            cg = new CustomGame();
            cg.Chat.ClosedChatIsDefault();

        }

        static async Task MainAsync()
        {
            discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = File.ReadAllText("token.txt"),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged
            });

            var slash = discord.UseSlashCommands();
            //slash.RegisterCommands<SlashCommands.SlashCommands>();  // global
            slash.RegisterCommands<SlashCommands.SlashCommands>((long)Server.Test);
            slash.RegisterCommands<SlashCommands.SlashCommands>((long)Server.GH);

            await discord.ConnectAsync();

            while (true)
            {
                await HandleCommandAsync();
                await discord.UpdateStatusAsync(new DiscordActivity());  // clear status
            }
        }

        private static async Task HandleCommandAsync()  // do everything needed to respond to the earliest command in the commandQueue
        {
            var cmd = commandQueue.Take();  // take earliest value. if there are no commands, wait until there is one
            ConsoleColor consoleCol = Console.ForegroundColor;  // used to revert after coloring a line

            switch (cmd.requestType)
            {
                case RequestType.Script:
                    Console.WriteLine($"code: {cmd.code}\nrequest type: script\n----------------------------");
                    await discord.UpdateStatusAsync(new DiscordActivity($"for {cmd.code}'s script", ActivityType.Watching));  // "Watching for {code}'s script"

                    Console.WriteLine($"importing...");
                    if (!cg.Settings.Import(cmd.code))  // importing code failed
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"failed. remaining in queue: {commandQueue.Count}\n");
                        Console.ForegroundColor = consoleCol;

                        var failMsg = new DiscordWebhookBuilder()
                                            .WithContent($"Failed to import {Formatter.InlineCode(cmd.code)}.");

                        await cmd.ctx.EditResponseAsync(failMsg);

                        return;
                    }
                    Console.WriteLine("imported successfully");

                    Console.WriteLine("getting script...");
                    string script = cg.Settings.GetScript();

                    string mainSettings = Regex.Match(script, "^settings\n{\n\tmain\n\t{\n\t\t.*?}", RegexOptions.Singleline).Value;  // take the `settings { main { }` portion of the copied script
                    string modeName = Regex.Match(mainSettings, "^\t\tMode Name: \"(.*)\"$", RegexOptions.Multiline).Groups[1].Value;  // find the "Mode Name: " section and take the value inside the quotes that follow it
                    string desc = Regex.Match(mainSettings, "^\t\tDescription: \"(.*)\"$", RegexOptions.Multiline).Groups[1].Value;

                    modeName = modeName.Replace('`', 'ˋ');  // replace backticks with a character that looks nearly identical in discord code font (to prevent escaping)
                    modeName = Regex.Unescape(modeName);  // replace \n with a newline, etc

                    desc = desc.Replace('`', 'ˋ');
                    desc = Regex.Unescape(desc);

                    Console.WriteLine("writing script to file...");
                    File.WriteAllText("script.txt", script, Encoding.UTF8);

                    Console.WriteLine("building message...");
                    var msg = new DiscordWebhookBuilder();
                    string msgContent = string.Empty;

                    if (modeName != string.Empty)
                        msgContent += Formatter.InlineCode(modeName);

                    if (desc != string.Empty)
                        msgContent += "\n" + Formatter.BlockCode(desc);  // leading newlines are stripped from discord messages, so don't have to worry about lack of mode name messing up formatting

                    if (msgContent != string.Empty)
                        msg = msg.WithContent(msgContent);

                    using (var fs = new FileStream("script.txt", FileMode.Open, FileAccess.Read))
                    {
                        msg = msg.AddFile("script.txt", fs);
                        await cmd.ctx.EditResponseAsync(msg);
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"done. remaining in queue: {commandQueue.Count}\n");
                    Console.ForegroundColor = consoleCol;

                    break;
            }
        }

        /// <summary>
        /// Adds command information to the commandQueue.
        /// </summary>
        /// <param name="code">The import code to add.</param>
        /// <param name="ctx">the InteractionContext to be used for responding.</param>
        /// <param name="requestType">The type of request that has been made.</param>
        public static void AddToQueue(RequestType requestType, InteractionContext ctx, string code)
        {
            Command cmd = new Command(requestType, ctx, code);
            commandQueue.Add(cmd);
            //commandQueue.Add((code, ctx, requestType));
        }
    }
}