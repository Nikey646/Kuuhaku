using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using Kuuhaku.Commands.Options;
using Kuuhaku.Infrastructure.Extensions;
using Kuuhaku.Infrastructure.Interfaces;
using Kuuhaku.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Context;

namespace Kuuhaku.Commands
{
    public class PrefixCommandHandler : AbstractCommandHandler<KuuhakuCommandContext>
    {
        private const String InvlaidInputMessage = "The input does not match any overload.";
        private const String InvalidInputEmoji = "🤔";

        protected ILogger<PrefixCommandHandler> logger { get; set; }

        public CommandHandlerOptions Options { get; }

        public PrefixCommandHandler(IServiceProvider provider, DiscordSocketClient client,
            CommandServiceConfig commandServiceConfig, CommandHandlerOptions options,
            IEnumerable<IPluginFactory> pluginFactories,
            ILogger<PrefixCommandHandler> logger)
            : base(provider, client, commandServiceConfig, pluginFactories, logger)
        {
            this.Options = options;
            this.logger = logger;

            this.CommandTriggered += this.CommandTriggeredAsync;
            this.CommandMissing += this.CommandMissingAsync;
            this.CommandFailed += this.CommandFailedAsync;
            this.CommandExecuted += this.CommandExecutedAsync;
        }

        protected override Task<KuuhakuCommandContext> CreateContextAsync(SocketUserMessage message, Stopwatch stopwatch)
        {
            return Task.FromResult(new KuuhakuCommandContext(this._client, message, stopwatch));
        }

        protected override Task<ImmutableArray<String>> GetCommandsAsync(KuuhakuCommandContext context)
        {
            if (context.User.IsBot && !this.Options.AllowBots)
                return Task.FromResult(ImmutableArray<String>.Empty);

            var prefix = "!"; // TODO: Replace with Guild Config ??
            var hasPrefix = !prefix.IsEmpty();

            var commands = new List<String>();

            // TODO: Replace static string with per-guild configuration
            var potentialCommands = context.Message.Content
                .Split(new[] {"//"}, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim());

            foreach (var potentialCommand in potentialCommands)
            {
                var prefixDetails = this.HasPrefix(context, potentialCommand, (hasPrefix, prefix));
                if (prefixDetails.IsSuccess || context.IsPrivate)
                    commands.Add(potentialCommand.Substring(Math.Max(0, prefixDetails.Start)));
            }

            return Task.FromResult(commands.ToImmutableArray());
        }

        protected override String FilterCommandString(KuuhakuCommandContext context, String command)
        {
            // Handle a prefix but no command as the info command
            if (command.IsEmpty())
                return "info";

            // Handle the prefix twice as a repeat command, or return the provided command
            // TODO: Replace with guild config prefix
            return command.Trim() == "!" ? "repeat" : command;
        }

        private (Boolean IsSuccess, Int32 Start) HasPrefix(KuuhakuCommandContext context, String potentialCommand,
            (Boolean HasPrefix, String Prefix) custom)
        {
            Boolean HasMentionPrefix(String message, IUser user, ref Int32 refIdx)
            {
                if (message.Length <= 3 || message[0] != '<' || message[1] != '@')
                    return false;

                var idx = message.IndexOf('>');
                if (idx == -1 || !MentionUtils.TryParseUser(message.Substring(0, idx + 1), out var userId) ||
                    userId != user.Id)
                    return false;

                refIdx = idx + (idx + 2 > message.Length ? 1 : 2);
                return true;
            }

            Boolean HasStringPrefix(String message, String prefix, ref Int32 refIdx)
            {
                if (!message.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return false;
                refIdx = prefix.Length;
                return true;
            }

            // Check to see if the message starts with a mention of the bot, then
            // Check to see if the message starts with a prefix that has a space after it, then
            // Check to see if the message starts with the prefix.

            // We check to see if the prefix has a space after only if it's more then a single character,
            // EG: "catbot5000 cats" would be a valid command, while "catbot5000cats" would be the same thing.

            // If no mention or prefix can be found at the start of the message, then it's not a command.
            // As soon as one of the conditions are met, the if statement exits.

            var position = -1;
            if (!HasMentionPrefix(potentialCommand, context.Client.CurrentUser, ref position) &&
                !(custom.Prefix.Length > 1 && HasStringPrefix(potentialCommand, $"{custom.Prefix} ", ref position)) &&
                !(custom.HasPrefix && HasStringPrefix(potentialCommand, custom.Prefix, ref position)))
                return (false, -1);

            return (true, position);
        }

        private Task CommandTriggeredAsync(KuuhakuCommandContext context)
        {
            var channelName = context.Channel.Name;
            var socketChannel = context.Channel as SocketTextChannel;

            if (socketChannel?.Category != null)
                channelName = socketChannel.Category.Name + "/" + channelName;

            this.logger.Trace("{user} attempted to trigger a command with the input {message} in {server}/{channel}", context.User, context.Message, context.Guild?.Name ?? "Private", channelName);
            return Task.CompletedTask;
        }

        private async Task CommandMissingAsync(KuuhakuCommandContext context, SearchResult result)
        {
            if (String.Equals(result.ErrorReason, InvlaidInputMessage, StringComparison.OrdinalIgnoreCase))
                await context.Message.AddReactionAsync(new Emoji(InvalidInputEmoji)).ConfigureAwait(false);
        }

        private
#if DEBUG
            async
#endif
            Task CommandFailedAsync(KuuhakuCommandContext context, IResult result)
        {
            if (result.Error == CommandError.UnknownCommand)
#if DEBUG
                return;
#else
            return Task.CompletedTask;
#endif

            // TODO: Stats for commands completed unsuccessfully

            if (result is ExceptionResult exceptionResult)
            {
                // TODO: Report Exception
                this.logger.Warning(exceptionResult.Exception,
                    "An exception occurred during the execution of a command.");
            }

            if (result is ExecuteResult executeResult)
            {
                var crap = executeResult.Exception;
                if (crap != null)
                {
                    crap.Data["Context"] = context;
                    crap.Data["Result"] = result;
                    // TODO: Report Exception
                    this.logger.Warning(crap, "An exeception occurred during the execution of a command.");
                }
            }

#if DEBUG
            var embed = new EmbedBuilder()
                .WithColor(EmbedColorType.Failure)
                .WithTitle($"An Error of {result.Error.Humanize()} Occurred")
                .WithDescription(result.ErrorReason.Truncate(EmbedBuilder.MaxDescriptionLength - 1))
                .WithFooter(context);
            await context.Channel.SendMessageAsync(String.Empty, embed: embed.Build()).ConfigureAwait(false);
#else
            return Task.CompletedTask;
#endif
        }

        private Task CommandExecutedAsync(KuuhakuCommandContext context, IResult result)
        {
            // context.Stopwatch.Stop();
            this.logger.Trace("{user} finished executing aa command with a result of {resultType}, and error of {errorType} in {time}",
                context.User, result.GetType().Name, result.Error?.ToString() ?? "No Error", context.Stopwatch.Elapsed.ToDuration(true));
            return Task.CompletedTask;
        }
    }
}
