using System;
using System.Collections.Generic;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Kuuhaku.Infrastructure.Classes;
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

        public static KuuhakuEmbedBuilder WithColor(this KuuhakuEmbedBuilder embed, EmbedColorType type = EmbedColorType.Default)
            => embed.WithColor(EmbedColorTypeMap[type]);

        public static KuuhakuEmbedBuilder WithAuthor(this KuuhakuEmbedBuilder embed, IUser user)
            => embed.WithAuthor(user.GetName(), user.GetAvatar(32));

        public static KuuhakuEmbedBuilder WithField(this KuuhakuEmbedBuilder embed, String title, String value,
            Boolean isInline = true)
            => embed.AddField(title, value, isInline);

        public static KuuhakuEmbedBuilder WithField(this KuuhakuEmbedBuilder embed, String title, Object value,
            Boolean isInline = true)
            => embed.AddField(title, value, isInline);

        public static KuuhakuEmbedBuilder WithField(this KuuhakuEmbedBuilder embed, String title, in Object value)
            => embed.WithField(title, value.ToString());

        public static KuuhakuEmbedBuilder WithFieldIf(this KuuhakuEmbedBuilder embed, String title, String value,
            Boolean isInline = true, Boolean includeIf = true)
            => includeIf ? embed.WithField(title, value, isInline) : embed;

        // Late evaluation, EG: Potential DB lookup
        public static KuuhakuEmbedBuilder WithFieldIf(this KuuhakuEmbedBuilder embed, String title, Func<String> value,
            Boolean isInline = true, Boolean includeIf = true)
            => includeIf ? embed.WithField(title, value(), isInline) : embed;

        public static KuuhakuEmbedBuilder WithFieldIf(this KuuhakuEmbedBuilder embed, String title, Object value,
            Boolean isInline = true, Boolean includeIf = true)
            => includeIf ? embed.WithField(title, value, isInline) : embed;

        // Late evaluation, EG: Potential DB lookup
        public static KuuhakuEmbedBuilder WithFieldIf(this KuuhakuEmbedBuilder embed, String title, Func<Object> value,
            Boolean isInline = true, Boolean includeIf = true)
            => includeIf ? embed.WithField(title, value(), isInline) : embed;

        public static KuuhakuEmbedBuilder WithFooter(this KuuhakuEmbedBuilder embed, ICommandContext context)
            => embed.WithFooter((context.Guild as SocketGuild)?.CurrentUser ?? (IUser) context.Client.CurrentUser);

        public static KuuhakuEmbedBuilder WithFooter(this KuuhakuEmbedBuilder embed, IUser user)
            => embed.WithFooter(user.GetName(), user.GetAvatar(32));
    }
}
