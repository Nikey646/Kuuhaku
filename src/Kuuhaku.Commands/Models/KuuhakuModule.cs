using System;
using Discord.Commands;
using Discord.WebSocket;
using Kuuhaku.Infrastructure.Models;

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
    }
}
