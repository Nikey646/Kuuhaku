using System;
using System.Diagnostics;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Kuuhaku.Infrastructure.Models
{
    public class KuuhakuCommandContext : ICommandContext
    {
        public DiscordSocketClient Client { get; }
        public SocketUser User { get; }
        public SocketUserMessage Message { get;  }
        public ISocketMessageChannel Channel { get; }
        public SocketGuild Guild { get; }

        public Boolean IsPrivate => this.Guild == null;
        public Stopwatch Stopwatch { get; }

        IDiscordClient ICommandContext.Client => this.Client;
        IGuild ICommandContext.Guild => this.Guild;
        IMessageChannel ICommandContext.Channel => this.Channel;
        IUser ICommandContext.User => this.User;
        IUserMessage ICommandContext.Message => this.Message;

        public KuuhakuCommandContext(DiscordSocketClient client, SocketUserMessage message)
        {
            this.Client = client;
            this.User = message.Author;
            this.Message = message;
            this.Channel = message.Channel;
            this.Guild = (this.Channel as SocketGuildChannel)?.Guild;

            this.Stopwatch = Stopwatch.StartNew();
        }
    }
}
