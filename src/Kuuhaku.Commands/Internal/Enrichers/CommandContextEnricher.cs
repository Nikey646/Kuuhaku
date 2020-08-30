using System;
using System.Linq.Expressions;
using Discord;
using Discord.Commands;
using Kuuhaku.Commands.Internal.Extensions;
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
        }
    }
}
