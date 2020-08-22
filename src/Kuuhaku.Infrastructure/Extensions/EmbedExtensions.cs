using System;
using System.Collections.Generic;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Kuuhaku.Infrastructure.Models;

namespace Kuuhaku.Infrastructure.Extensions
{
    public static class EmbedExtensions
    {
        private static Dictionary<EmbedColorType, Color> EmbedColorTypeMap = new Dictionary<EmbedColorType, Color>
        {
            { EmbedColorType.Info, new Color(33, 150, 243) },
            { EmbedColorType.Success, new Color(39, 195, 74) },
            { EmbedColorType.Warning, new Color(255, 87, 34) },
            { EmbedColorType.Failure, new Color(198, 40, 40) },
            { EmbedColorType.Default, new Color(74, 20, 140) },
        };

        public static EmbedBuilder WithColor(this EmbedBuilder embed, EmbedColorType type = EmbedColorType.Default)
            => embed.WithColor(EmbedColorTypeMap[type]);

        public static EmbedBuilder WithAuthor(this EmbedBuilder embed, IUser user)
            => embed.WithAuthor(user.GetName(), user.GetAvatar(32));

        public static EmbedBuilder WithField(this EmbedBuilder embed, String title, String value,
            Boolean isInline = true)
            => embed.AddField(title, value, isInline);

        public static EmbedBuilder WithFieldIf(this EmbedBuilder embed, String title, String value,
            Boolean isInline = true, Boolean include = true)
            => include ? embed.WithField(title, value, isInline) : embed;

        // Late evaluation, EG: Potential DB lookup
        public static EmbedBuilder WithFieldIf(this EmbedBuilder embed, String title, Func<String> value,
            Boolean isInline = true, Boolean include = true)
            => include ? embed.WithField(title, value(), isInline) : embed;

        public static EmbedBuilder WithFooter(this EmbedBuilder embed, ICommandContext context)
            => embed.WithFooter((context.Guild as SocketGuild)?.CurrentUser ?? (IUser) context.Client.CurrentUser);

        public static EmbedBuilder WithFooter(this EmbedBuilder embed, IUser user)
            => embed.WithFooter(user.GetName(), user.GetAvatar(32));

    }
}
