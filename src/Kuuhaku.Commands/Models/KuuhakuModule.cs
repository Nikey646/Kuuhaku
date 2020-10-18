using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Kuuhaku.Commands.Interfaces;
using Kuuhaku.Infrastructure.Classes;
using Kuuhaku.Infrastructure.Extensions;

namespace Kuuhaku.Commands.Models
{
    public abstract class KuuhakuModule : IModule
    {
        protected internal KuuhakuCommandContext Context { get; private set; }
        protected internal CommandInfo CurrentCommand { get; private set; }

        protected DiscordSocketClient Client => this.Context.Client;
        protected SocketUser User => this.Context.User;
        protected SocketUserMessage Message => this.Context.Message;
        protected ISocketMessageChannel Channel => this.Context.Channel;
        protected SocketGuild Guild => this.Context.Guild;
        protected GuildConfig Config => this.Context.Config;

        protected Boolean IsPrivate => this.Context.IsPrivate;

        public Task<IUserMessage> ReplyAsync(String message, KuuhakuEmbedBuilder embed, CancellationToken ct = default)
            => this.Channel.SendMessageAsync(message, embed, ct);

        public Task<IUserMessage> ReplyAsync(KuuhakuEmbedBuilder embed, CancellationToken ct = default)
            => this.ReplyAsync("", embed, ct);

        public async Task<IUserMessage> ReplyAsync(
            String message = null,
            Boolean isTTS = false,
            Embed embed = null,
            RequestOptions options = null)
        {
            return await this.Context.Channel.SendMessageAsync(message, isTTS, embed, options);
        }

        void IModule.SetContext(KuuhakuCommandContext context)
        {
            this.Context = context;
        }

        public virtual void BeforeExecute(CommandInfo command)
        {
            this.CurrentCommand = command;
        }

        public virtual void AfterExecute(CommandInfo command)
        { }
    }
}
