using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Kuuhaku.Infrastructure.Classes;
using Kuuhaku.Infrastructure.Models;
using Kuuhaku.Infrastructure.Extensions;

namespace Kuuhaku.Commands.Models
{
    public class KuuhakuModule : ModuleBase<KuuhakuCommandContext>
    {
        protected virtual DiscordSocketClient Client => this.Context.Client;
        protected virtual SocketUser User => this.Context.User;
        protected virtual SocketUserMessage Message => this.Context.Message;
        protected virtual ISocketMessageChannel Channel => this.Context.Channel;
        protected virtual SocketGuild Guild => this.Context.Guild;

        protected virtual Boolean IsPrivate => this.Context.IsPrivate;

        public Task<IUserMessage> ReplyAsync(String message, KuuhakuEmbedBuilder embed, CancellationToken ct = default)
            => this.Channel.SendMessageAsync(message, embed, ct);

        public Task<IUserMessage> ReplyAsync(KuuhakuEmbedBuilder embed, CancellationToken ct = default)
            => this.ReplyAsync("", embed, ct);
    }
}
