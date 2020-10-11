using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Kuuhaku.Commands;
using Kuuhaku.Commands.Classes;
using Kuuhaku.Commands.Options;
using Kuuhaku.Infrastructure.Interfaces;
using Kuuhaku.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Sprache;

namespace Kuuhaku.QuotedCommandHandler
{
    public class QuotedCommandHandler : PrefixCommandHandler
    {
        public QuotedCommandHandler(IServiceProvider provider, DiscordSocketClient client,
            CommandServiceConfig commandServiceConfig, CommandHandlerOptions options, CustomModuleBuilder moduleBuilder,
            IEnumerable<IPluginFactory> pluginFactories,
            ILogger<PrefixCommandHandler> logger, CommandService commandService)
            : base(provider, client, commandServiceConfig, options, moduleBuilder, pluginFactories, logger, commandService)
        {
            base.logger = logger;
        }

        private static class MentionsParser
        {
            public static readonly Parser<UInt64> UserMention =
                from _ in Parse.AnyChar.Except(Parse.String("<@")).Many()
                from __ in Parse.String("<@").Once()
                from ___ in Parse.Char('!').Once().Optional()
                from id in Parse.Numeric.Many().Text()
                from ending in Parse.Chars('>').Once()
                select UInt64.Parse(id);
        }

        private static class QuotedCommandParser
        {
            private static readonly Parser<IEnumerable<Char>> EscapedDelimiters =
                Parse.String("\\(")
                    .Or(Parse.String("\\)"))
                    .Or(Parse.String("\\["))
                    .Or(Parse.String("\\]"))
                    .Or(Parse.String("\\{"))
                    .Or(Parse.String("\\}"))
                    .Or(Parse.String("\\【"))
                    .Or(Parse.String("\\】"));

            private static readonly Parser<String> SingleEscape =
                Parse.String("\\").Text();

            private static readonly Parser<String> DoubleEscape =
                Parse.String("\\\\").Text();

            private static readonly Parser<IEnumerable<Char>> StartDelimiter =
                Parse.String("(")
                    .Or(Parse.String("["))
                    .Or(Parse.String("{"))
                    .Or(Parse.String("【"));

            private static readonly Parser<IEnumerable<Char>> EndDelimiter =
                Parse.String(")")
                    .Or(Parse.String("]"))
                    .Or(Parse.String("}"))
                    .Or(Parse.String("】"));

            private static readonly Parser<String> SimpleLiteral =
                Parse.AnyChar
                    .Except(SingleEscape)
                    .Except(StartDelimiter.Text())
                    .Except(EndDelimiter.Text())
                    .Many()
                    .Text();

            public static readonly Parser<String> Parser =
                from escapeStart in EscapedDelimiters
                    .Text()
                    .Select(s => s.Replace("\\", ""))
                    .Or(SimpleLiteral)
                    .Optional()
                    .Many()
                from _ in Parse.WhiteSpace.Once().Optional()
                from start in StartDelimiter.Text()
                from contents in EscapedDelimiters
                    .Text()
                    .Select(s => s.Replace("\\", ""))
                    .Or(DoubleEscape)
                    .Or(SingleEscape)
                    .Or(SimpleLiteral)
                    .Many()
                from end in EndDelimiter.Text()
                select String.Concat(contents);
        }

        private static readonly Parser<String> CommandParser =
            from result in MentionsParser.UserMention
                .Then(id => id == 548017698852831245 //Unknown Bot Id 310580206463221762
                    ? QuotedCommandParser.Parser
                    : Parse.Return(""))
            select result;

        // protected override async Task<KuuhakuCommandContext> CreateContextAsync(SocketUserMessage message, Stopwatch stopwatch)
        // {
        //     var context =  await base.CreateContextAsync(message, stopwatch);
        //     var type = context.GetType();
        //     return context;
        // }

        protected override Task<ImmutableArray<String>> GetCommandsAsync(KuuhakuCommandContext context)
        {
            if (context.User.IsBot && !this.Options.AllowBots)
                return Task.FromResult(ImmutableArray<String>.Empty);

            var parseResults = CommandParser
                .Many()
                .TryParse(context.Message.Content);

            var commands = parseResults.Value.ToImmutableArray();
            if (!parseResults.WasSuccessful || commands.IsEmpty)
                return Task.FromResult(ImmutableArray<String>.Empty);

            return Task.FromResult(commands);
        }
    }
}
