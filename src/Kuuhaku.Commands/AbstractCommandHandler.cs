using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Kuuhaku.Commands.Classes;
using Kuuhaku.Commands.Internal;
using Kuuhaku.Commands.Internal.Extensions;
using Kuuhaku.Infrastructure.Extensions;
using Kuuhaku.Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kuuhaku.Commands
{
    public abstract class AbstractCommandHandler<TCommandContext> : IHostedService
        where TCommandContext : ICommandContext
    {
        private ILogger logger { get; }
        protected IServiceProvider _provider;
        protected DiscordSocketClient _client;
        protected CommandServiceConfig _commandServiceConfig;
        private readonly CustomModuleBuilder _moduleBuilder;
        private readonly CommandService _commandService;
        protected IPluginFactory[] _pluginFactories;

        private readonly AsyncEvent<Func<TCommandContext, Task>> _commandTriggered
            = new AsyncEvent<Func<TCommandContext, Task>>();

        private readonly AsyncEvent<Func<TCommandContext, SearchResult, Task>> _commandMissing
            = new AsyncEvent<Func<TCommandContext, SearchResult, Task>>();

        private readonly AsyncEvent<Func<TCommandContext, IResult, Task>> _commandFailed
            = new AsyncEvent<Func<TCommandContext, IResult, Task>>();

        private readonly AsyncEvent<Func<TCommandContext, ExecuteResult, Task>> _commandSucceeded
            = new AsyncEvent<Func<TCommandContext, ExecuteResult, Task>>();

        private readonly AsyncEvent<Func<TCommandContext, IResult, Task>> _commandExecuted
            = new AsyncEvent<Func<TCommandContext, IResult, Task>>();

        /// <summary>
        ///		Occurs before any processing for the command is done
        /// </summary>
        public event Func<TCommandContext, Task> CommandTriggered
        {
            add => this._commandTriggered.Add(value);
            remove => this._commandTriggered.Remove(value);
        }

        /// <summary>
        ///		Occurs when a command cannot be found
        /// </summary>
        public event Func<TCommandContext, SearchResult, Task> CommandMissing
        {
            add => this._commandMissing.Add(value);
            remove => this._commandMissing.Remove(value);
        }

        /// <summary>
        ///		Occurs when a command fails due to an exception
        /// </summary>
        public event Func<TCommandContext, IResult, Task> CommandFailed
        {
            add => this._commandFailed.Add(value);
            remove => this._commandFailed.Add(value);
        }

        /// <summary>
        ///		Occurs when a command was successfully executed
        /// </summary>
        public event Func<TCommandContext, ExecuteResult, Task> CommandSucceeded
        {
            add => this._commandSucceeded.Add(value);
            remove => this._commandSucceeded.Remove(value);
        }

        /// <summary>
        ///		Occurs after a command has finished executing, irrelevant of result
        /// </summary>
        public event Func<TCommandContext, IResult, Task> CommandExecuted
        {
            add => this._commandExecuted.Add(value);
            remove => this._commandExecuted.Remove(value);
        }

        protected CommandService Commands => this._commandService;

        protected AbstractCommandHandler(IServiceProvider provider, DiscordSocketClient client,
            CommandServiceConfig commandServiceConfig, CustomModuleBuilder moduleBuilder,
            IEnumerable<IPluginFactory> pluginFactories, ILogger logger, CommandService commandService)
        {
            this._provider = provider;
            this._client = client;
            // this._commandServiceConfig = commandServiceConfig;
            this._moduleBuilder = moduleBuilder;
            this._commandService = commandService;
            this._pluginFactories = pluginFactories.ToArray();
            this.logger = logger; // Log.Logger.ForContext<AbstractCommandHandler<TCommandContext>>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.logger.Trace("Creating Command Service");
            // this.Commands = new CommandService(this._commandServiceConfig);

            // It appears you need to run this before loading modules
            // because Discord.Net throws an exception if an unsupported type
            // is a param in a module...wtf?
            this.logger.Trace("Adding Custom Type Readers");
            this.InstallTypeReaders();

            foreach (var pluginFactory in this._pluginFactories)
            {
                await pluginFactory.LoadDiscordModulesAsync(this._moduleBuilder);
            }

            this.logger.Trace("Loaded {modules} modules with {commands} commands.", this._moduleBuilder.Modules.Count,
                this._moduleBuilder.Modules.Sum(m => m.Commands.Count));

            // var loadedModules = 0;
            // var loadedCommands = 0;
            // foreach (var pluginFactory in this._pluginFactories)
            // {
            //     var moduleInfos =
            //         (await pluginFactory.LoadDiscordModulesAsync(this._moduleBuilder);
            //     loadedModules += moduleInfos.Length;
            //     loadedCommands += moduleInfos.Sum(m => m.Commands.Count);
            // }
            //
            // this.logger.Trace("Loaded {modules} modules wuth {commands} commands.", loadedModules, loadedCommands);

            this._client.MessageReceived += this.OnMessageReceivedAsync;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this._client.MessageReceived -= this.OnMessageReceivedAsync;
            return Task.CompletedTask;
        }

        protected virtual void InstallTypeReaders()
        {
        }

        protected virtual String FilterCommandString(TCommandContext context, String command)
            => command;

        protected abstract Task<TCommandContext> CreateContextAsync(IServiceProvider serviceProvider, SocketUserMessage message, Stopwatch stopwatch);
        protected abstract Task<ImmutableArray<String>> GetCommandsAsync(TCommandContext context);

        protected async Task OnMessageReceivedAsync(SocketUserMessage message)
        {
            IResult result = null;
            using var scope = this._provider.CreateScope();

            var context = await this.CreateContextAsync(scope.ServiceProvider, message, Stopwatch.StartNew())
                .ConfigureAwait(false);
            using var enrichContext = context.Enrich();
            var commands = await this.GetCommandsAsync(context).ConfigureAwait(false);

            if (commands.Length <= 0)
                return;

            foreach (var command in commands)
            {
                try
                {
                    await this._commandTriggered.InvokeAsync(context).ConfigureAwait(false);

                    var searchResult = (SearchResult) (result =
                        this.Commands.Search(context, this.FilterCommandString(context, command)));
                    if (!result.IsSuccess)
                    {
                        await this._commandMissing.InvokeAsync(context, searchResult).ConfigureAwait(false);
                        return;
                    }

                    var (success, bestResult, possibleMatch) =
                        await this.FindBestCommandAsync(context, searchResult).ConfigureAwait(false);
                    result = bestResult;
                    if (!success || !possibleMatch.HasValue)
                    {
                        if (result is SearchResult searchResult2)
                            await this._commandMissing.InvokeAsync(context, searchResult2).ConfigureAwait(false);
                        else await this._commandFailed.InvokeAsync(context, result).ConfigureAwait(false);
                        return;
                    }

                    var commandMatch = possibleMatch.Value;
                    this.logger.Trace("Found the {commandName} command from the {moduleName} module for {user}",
                        commandMatch.Command.Name, commandMatch.Command.Module.Name, context.User);

                    // TODO: Create a Harmony Plugin to automatically wrap methods that use a certain method to have the typing disposable
                    // TODO: TypingNotifier
                    // TODO: NoTypingAttribute
                    result = await commandMatch.Command.ExecuteAsync(context, (ParseResult) result, scope.ServiceProvider)
                        .ConfigureAwait(false);

                    if (result.IsSuccess)
                        await this._commandSucceeded.InvokeAsync(context, (ExecuteResult) result).ConfigureAwait(false);
                    else await this._commandFailed.InvokeAsync(context, result).ConfigureAwait(false);
                }
                catch (Exception crap)
                {
                    crap.Data["Context"] = context;
                    crap.Data["CurrentCommand"] = command;
                    crap.Data["LastResult"] = result;
                    result = new ExceptionResult(CommandError.Exception,
                        "There was an error while processing a command", crap);
                    await this._commandFailed.InvokeAsync(context, result).ConfigureAwait(false);
                }
                finally
                {
                    await this._commandExecuted.InvokeAsync(context, result).ConfigureAwait(false);
                }
            }
        }

        protected Task OnMessageReceivedAsync(SocketMessage message)
        {
            if (!(message is SocketUserMessage userMessage))
                return Task.CompletedTask;

            Task.Factory.StartNew(async () => await this.OnMessageReceivedAsync(userMessage).ConfigureAwait(false));
            return Task.CompletedTask;
        }

        protected async Task<(Boolean isSuccess, IResult result, CommandMatch? command)> FindBestCommandAsync(
            TCommandContext context,
            SearchResult search, MultiMatchHandling multiMatchHandling = MultiMatchHandling.Exception)
        {
            var commands = search.Commands;
            var preconditionResults = new Dictionary<CommandMatch, PreconditionResult>();

            foreach (var match in commands)
            {
                preconditionResults[match] = await match.Command.CheckPreconditionsAsync(context, this._provider)
                    .ConfigureAwait(false);
            }

            var successfulPreconditions = preconditionResults
                .Where(x => x.Value.IsSuccess)
                .ToArray();

            if (successfulPreconditions.Length == 0)
            {
                var bestCanidate = preconditionResults
                    .OrderByDescending(x => x.Key.Command.Priority)
                    .FirstOrDefault(x => !x.Value.IsSuccess);
                return (false, bestCanidate.Value, null);
            }

            var parseResultsDict = new Dictionary<CommandMatch, ParseResult>();
            foreach (var pair in successfulPreconditions)
            {
                var parseResult = await pair.Key.ParseAsync(context, search, pair.Value, this._provider)
                    .ConfigureAwait(false);

                if (parseResult.Error.HasValue &&
                    parseResult.Error.Value == CommandError.MultipleMatches)
                {
                    IReadOnlyList<TypeReaderValue> argList, paramList;
                    argList = parseResult.ArgValues
                        .Select(x => x.Values
                            .OrderByDescending(y => y.Score)
                            .First())
                        .ToImmutableArray();
                    paramList = parseResult.ParamValues
                        .Select(x => x.Values
                            .OrderByDescending(y => y.Score)
                            .First())
                        .ToImmutableArray();

                    parseResult = ParseResult.FromSuccess(argList, paramList);
                }

                parseResultsDict[pair.Key] = parseResult;
            }

            Single CalculateScore(CommandMatch match, ParseResult parseResult)
            {
                Single argValuesScore = 0, paramValuesScore = 0;

                if (match.Command.Parameters.Count > 0)
                {
                    var argValuesSum =
                        parseResult.ArgValues?.Sum(x =>
                            x.Values.OrderByDescending(y => y.Score).FirstOrDefault().Score) ?? 0;
                    var paramValuesSum =
                        parseResult.ParamValues?.Sum(x =>
                            x.Values.OrderByDescending(y => y.Score).FirstOrDefault().Score) ?? 0;

                    argValuesScore = argValuesSum / match.Command.Parameters.Count;
                    paramValuesScore = paramValuesSum / match.Command.Parameters.Count;
                }

                return match.Command.Priority + (argValuesScore + paramValuesScore) / 2 * 0.99f;
            }

            var parseResults = parseResultsDict
                .OrderByDescending(kv => CalculateScore(kv.Key, kv.Value));
            var successfulParses = parseResults
                .Where(x => x.Value.IsSuccess)
                .ToArray();

            if (successfulParses.Length == 0)
            {
                var bestMatch = parseResults.FirstOrDefault(x => !x.Value.IsSuccess);
                return (false, bestMatch.Value, null);
            }

            return (true, successfulParses[0].Value, successfulParses[0].Key);
        }

        public class ExceptionResult : RuntimeResult
        {
            public Exception Exception { get; }

            public ExceptionResult(CommandError? error, String reason, Exception crap)
                : base(error, reason)
            {
                this.Exception = crap;
            }
        }
    }
}
