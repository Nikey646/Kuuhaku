using System;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Kuuhaku.Commands.Internal.Extensions;
using Kuuhaku.Infrastructure.Models;
using Serilog.Core;
using Serilog.Events;

namespace Kuuhaku.Commands.Internal.Enrichers
{
    public class CommandContextEnricher : ILogEventEnricher
    {
        private ICommandContext Context;

        public CommandContextEnricher(ICommandContext context)
        {
            this.Context = context;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory factory)
        {
            void _(String name, Object val, Boolean destructure = false)
            {
                logEvent.AddOrUpdateProperty(factory.CreateProperty(name, val, destructure));
            }

            var (user, message, guild, channel) = this.Context;
            _("Context.User.Id", user.Id);
            _("Context.User.Name", user.Username);
            _("Context.User.Discriminator", user.DiscriminatorValue);
            _("Context.User.Nickname", (user as IGuildUser)?.Nickname);

            _("Context.Message.Id", message.Id);
            _("Context.Message.Content", message.Content);

            _("Context.Guild.Id", guild?.Id);
            _("Context.Guild.Name", guild?.Name);

            _("Context.Channel.Id", channel.Id);
            _("Context.Channel.Name", channel.Name);

            if (channel is SocketTextChannel textChannel)
            {
                var categoryName = textChannel.Category.Name;
                _("Context.Channel.Category", categoryName);
                _("Context.Channel.IsNsfw", textChannel.IsNsfw);
            }

            if (!(this.Context is KuuhakuCommandContext kuuhakuContext))
                return;

            _("Context.Stopwatch.ElapsedMs", kuuhakuContext.Stopwatch.ElapsedMilliseconds);
            _("Context.Stopwatch.IsRunning", kuuhakuContext.Stopwatch.IsRunning);
        }
    }
}
