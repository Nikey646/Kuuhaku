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
using Kuuhaku.Commands.Classes;
using Kuuhaku.Commands.Classes.Repositories;
using Kuuhaku.Commands.Classes.TypeReaders;
using Kuuhaku.Commands.Options;
using Kuuhaku.Infrastructure.Classes;
using Kuuhaku.Infrastructure.Extensions;
using Kuuhaku.Infrastructure.Interfaces;
using Kuuhaku.Infrastructure.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Context;

namespace Kuuhaku.Commands
{
    public class PrefixCommandHandler : AbstractCommandHandler<KuuhakuCommandContext>
    {
        private readonly RepeatRepository _repeatRepository;
        private const String InvlaidInputMessage = "The input does not match any overload.";
        private const String InvalidInputEmoji = "🤔";

        protected ILogger<PrefixCommandHandler> logger { get; set; }

        public CommandHandlerOptions Options { get; }

        public PrefixCommandHandler(IServiceProvider provider, DiscordSocketClient client,
            CommandServiceConfig commandServiceConfig, CommandHandlerOptions options,
            CustomModuleBuilder moduleBuilder,
            IEnumerable<IPluginFactory> pluginFactories,
            ILogger<PrefixCommandHandler> logger, CommandService commandService, RepeatRepository repeatRepository)
            : base(provider, client, commandServiceConfig, moduleBuilder, pluginFactories, logger, commandService)
        {
            this.Options = options;
            this.logger = logger;

            this.CommandTriggered += this.CommandTriggeredAsync;
            this.CommandMissing += this.CommandMissingAsync;
            this.CommandSucceeded += this.CommandSucceededAsync;
            this.CommandFailed += this.CommandFailedAsync;
            this.CommandExecuted += this.CommandExecutedAsync;

            this._repeatRepository = repeatRepository;
        }

        protected override void InstallTypeReaders()
        {
            this.logger.Trace("Adding EmoteTypeReader for IEmote.");
            this.Commands.AddTypeReader<IEmote, EmoteTypeReader>();
            base.InstallTypeReaders();
        }

        protected override async Task<KuuhakuCommandContext> CreateContextAsync(IServiceProvider provider,
            SocketUserMessage message, Stopwatch stopwatch)
        {
            // TODO: Cache in memory and work from there.

            var context = new KuuhakuCommandContext(this._client, message, stopwatch);
            if (context.IsPrivate)
                return context;


            try
            {
                var repo = provider.GetService<GuildConfigRepository>();
                var configs = await repo.FindAsync(c => c.GuildId == context.Guild.Id);
                var config = configs.FirstOrDefault();

                // This should only occur when bots are sending messages immediately as a user joins,
                // and we receive one before the NewGuildWatcher stores the config.
                if (config == default)
                    return context;

                context.Config = config;
                return context;
            }
            catch (Exception crap)
            {
                this.logger.Warning(crap, "An unexcepted error occurred while trying to create the Command Context");
                throw;
            }
        }

        protected override Task<ImmutableArray<String>> GetCommandsAsync(KuuhakuCommandContext context)
        {
            if (context.User.IsBot && !this.Options.AllowBots)
                return Task.FromResult(ImmutableArray<String>.Empty);

            var prefix = context.Config?.Prefix ?? "";
            var hasPrefix = !prefix.IsEmpty();

            var commands = new List<String>();

            var potentialCommands = context.Message.Content
                .Split(new[] {context.Config?.CommandSeperator ?? "//"}, StringSplitOptions.RemoveEmptyEntries)
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

            var prefix = context.Config?.Prefix ?? "";

            // Handle the prefix twice as a repeat command, or return the provided command
            return command.Trim() == prefix ? "repeat" : command;
        }

        internal Task InternalCommandLauncher(SocketMessage message)
            => this.OnMessageReceivedAsync(message);

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

            this.logger.Trace("{user} attempted to trigger a command with the input {message} in {server}/{channel}",
                context.User, context.Message, context.Guild?.Name ?? "Private", channelName);
            return Task.CompletedTask;
        }

        private async Task CommandMissingAsync(KuuhakuCommandContext context, SearchResult result)
        {
            if (String.Equals(result.ErrorReason, InvlaidInputMessage, StringComparison.OrdinalIgnoreCase))
                await context.Message.AddReactionAsync(new Emoji(InvalidInputEmoji)).ConfigureAwait(false);
        }

        private async Task CommandSucceededAsync(KuuhakuCommandContext context, ExecuteResult result)
        {
            var prefix = context.Config?.Prefix ?? String.Empty;
            var hasPrefix = !prefix.IsEmpty();

            var deets = this.HasPrefix(context, context.Message.Content, (hasPrefix, prefix));

            if (deets.IsSuccess)
            {
                var command = context.Message.Content.Substring(deets.Start);
                if (String.Equals(command, "repeat", StringComparison.OrdinalIgnoreCase))
                    return; // Don't store the repeat command

                if (command.IsEmpty())
                    return;

                // Reformat the command so that it'll always trigger, even if the prefix is changed.
                await this._repeatRepository.CreateAsync($"{context.Client.CurrentUser.Mention} {command}", context.Guild, context.User);
            }
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

#if DEBUG
            await this._repeatRepository.CreateAsync(context.Message.Content, context.Guild, context.User);
#endif

            // TODO: Stats for commands completed unsuccessfully
            // TODO: Investigate why this was slowing down commands finishing by 20+ seconds

            // if (result is ExceptionResult exceptionResult)
            // {
            //
            //     using var _ = LogContext.PushProperty("Exception", exceptionResult.Exception);
            //     // TODO: Report Exception
            //     this.logger.Warning(exceptionResult.Exception,
            //         "An exception occurred during the execution of a command.");
            // }

            if (result is ExecuteResult executeResult)
            {
                var crap = executeResult.Exception;
                if (crap != null)
                {
                    // crap.Data["Context"] = context;
                    // crap.Data["Result"] = result;
                    // TODO: Report Exception
                    this.logger.Warning(crap, "An exeception occurred during the execution of a command.");
                }
            }

#if DEBUG
            var embed = new KuuhakuEmbedBuilder()
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
            this.logger.Trace(
                "{user} finished executing a command with a result of {resultType}, and error of {errorType} in {time}",
                context.User, result.GetType().Name, result.Error?.ToString() ?? "No Error",
                context.Stopwatch.Elapsed.ToDuration(true));
            return Task.CompletedTask;
        }
    }
}
