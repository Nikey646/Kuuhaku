using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Kuuhaku.Database.DbModels;
using Kuuhaku.Infrastructure.Classes;
using Kuuhaku.Infrastructure.Extensions;
using Kuuhaku.UserRolesModule.Classes;
using Kuuhaku.UserRolesModule.Models;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Update;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kuuhaku.UserRolesModule.Services
{
    public class UserRoleService : IHostedService
    {
        private readonly DiscordSocketClient _client;
        private readonly UserRolesRepository _repository;
        private readonly ILogger<UserRoleService> _logger;
        private readonly List<UserRoleDto> _userRoles;

        public UserRoleService(DiscordSocketClient client, UserRolesRepository repository, ILogger<UserRoleService> logger)
        {
            this._client = client;
            this._repository = repository;
            this._logger = logger;
            this._userRoles = new List<UserRoleDto>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this._logger.Info("Starting User Role Service.");

            var userRoleLocations = await this._repository.GetAllAsync();
            this._userRoles.AddRange(userRoleLocations);

            this._client.ReactionAdded += this.ReactionAddedAsync;
            this._client.ReactionRemoved += this.ReactionRemovedAsync;
            this._client.ReactionsCleared += this.ReactionsClearedAsync;

            this._client.MessageUpdated += this.MessageUpdatedAsync;

            async Task SyncAsync()
            {

                var userRoles = this._userRoles
                    .Where(r => r.MessageId.HasValue)
                    .GroupBy(r => r.MessageId.Value)
                    .Select(r => r.First());

                foreach (var userRole in userRoles)
                {
                    var guild = this._client.GetGuild(userRole.GuildId);
                    var channel = guild.GetTextChannel(userRole.ChannelId);
                    // ReSharper disable once PossibleInvalidOperationException
                    var message = await channel.GetMessageAsync(userRole.MessageId.Value) as IUserMessage;
                    await this.SyncReactionsAndRolesAsync(message);
                }

                this._client.Ready -= SyncAsync;
            }

            this._client.Ready += SyncAsync;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this._logger.Info("Stopping User Role Service.");
            return Task.CompletedTask;
        }

        public async Task AddRoleAsync(IGuild guild, IMessageChannel channel, IRole role, IEmote reaction,
            String description)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (role == null) throw new ArgumentNullException(nameof(role));
            if (reaction == null) throw new ArgumentNullException(nameof(reaction));
            if (description.IsEmpty() || description.Length > 200)
                throw new ArgumentException("Description cannot be empty or longer than 200 characters.",
                    nameof(description));

            var userRole = await this._repository.CreateAsync(guild, channel, role, reaction, description);
            this._userRoles.Add(userRole);

            var messageIds = this._userRoles
                .Where(u => u.GuildId == guild.Id && u.ChannelId == channel.Id)
                .Where(u => u.MessageId.HasValue)
                .Select(u => u.MessageId.Value)
                .ToImmutableArray();

            var activeChannel = channel is ITextChannel textChannel
                ? textChannel
                : await guild.GetTextChannelAsync(channel.Id);
            IUserMessage message = null;

            if (messageIds.Length > 0)
            {
                foreach (var messageId in messageIds)
                {
                    message = await activeChannel.GetMessageAsync(messageId) as IUserMessage;
                    if (message == null)
                        throw new InvalidOperationException(
                            $"The message ({messageId}( received from channel ({channel.Id}( was not a user message...?");

                    var embed = message.Embeds.First();
                    if (embed.Fields.Length >= EmbedBuilder.MaxFieldCount)
                        continue;

                    var rolesToDisplay = this._userRoles
                        .Where(r => r.MessageId == messageId)
                        .ToImmutableArray();

                    if (rolesToDisplay.Length > EmbedBuilder.MaxFieldCount)
                    {
                        var newEmbed = this.CreateEmbed(guild, userRole);
                        message = await activeChannel.SendMessageAsync(newEmbed);
                        break;
                    }

                    var updatedEmbed = this.CreateEmbed(guild, rolesToDisplay.Add(userRole).ToArray());
                    await message.ModifyAsync(m => m.Embed = updatedEmbed.Build());
                    break;
                }
            }

            if (message == null)
            {
                var embed = this.CreateEmbed(guild, userRole);
                message = await activeChannel.SendMessageAsync(embed);
            }

            await message.AddReactionAsync(reaction);
            userRole.MessageId = message.Id;

            await this._repository.UpdateAsync(userRole);
        }


        public async Task RemoveRoleAsync(IGuild guild, IMessageChannel channel, IRole role)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (role == null) throw new ArgumentNullException(nameof(role));

            var userRole = this._userRoles
                .FirstOrDefault(r => r.GuildId == guild.Id &&
                                     r.ChannelId == channel.Id &&
                                     r.RoleId == role.Id);

            if (userRole == default)
                return;

            this._userRoles.Remove(userRole);
            await this._repository.DeleteAsync(guild, channel, role);

            var activeChannel = channel is ITextChannel textChannel
                ? textChannel
                : await guild.GetTextChannelAsync(channel.Id);
            var messageId = userRole.MessageId;

            // This should never be triggered?
            if (!messageId.HasValue)
                return;

            var message = await activeChannel.GetMessageAsync(messageId.Value) as IUserMessage;
            if (message == null)
                throw new Exception(
                    $"The message ({messageId}) received from the channel ({channel.Id}) was not a user message...?");

            var rolesToDisplay = this._userRoles
                .Where(r => r.MessageId == messageId);

            var updatedEmbed = this.CreateEmbed(guild, rolesToDisplay.ToArray());
            await message.ModifyAsync(m => m.Embed = updatedEmbed.Build());

            foreach (var (reaction, _) in message.Reactions)
            {
                var isOldReaction = reaction.Name == userRole.EmojiName;
                if (reaction is Emote emote)
                    isOldReaction = emote.Id == userRole.EmojiId;

                if (!isOldReaction)
                    continue;

                await message.RemoveReactionAsync(reaction, this._client.CurrentUser);
                break;
            }
        }

        private async Task OnReactionAddedAsync(Cacheable<IUserMessage, UInt64> message, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            if (reaction.UserId == this._client.CurrentUser.Id)
                return;

            var (user, role) = await this.GetUserAndRoleAsync(channel, message.Id, reaction);
            if (user == null || role == null)
                return;
            await user.AddRoleAsync(role);
        }

        private async Task OnReactionRemovedAsync(Cacheable<IUserMessage, UInt64> message,
            ISocketMessageChannel channel, SocketReaction reaction)
        {
            var (user, role) = await this.GetUserAndRoleAsync(channel, message.Id, reaction);
            if (user == null || role == null)
                return;
            if (user.RoleIds.Contains(role.Id))
                await user.RemoveRoleAsync(role);
        }

        private async Task OnReactionsClearedAsync(Cacheable<IUserMessage, UInt64> cachedMessage, ISocketMessageChannel channel)
        {
            var message = cachedMessage.HasValue
                ? cachedMessage.Value
                : await channel.GetMessageAsync(cachedMessage.Id) as IUserMessage;

            var emojis = this._userRoles.Where(r => r.MessageId == message.Id)
                .Select(r => r.EmojiId.HasValue ? (IEmote) Emote.Parse($"<:rs:{r.EmojiId}>") : new Emoji(r.EmojiName));

            await message.AddReactionsAsync(emojis.ToArray());
        }

        private async Task OnMessageUpdatedAsync(Cacheable<IMessage, UInt64> oldMessage, SocketMessage newMessage,
            ISocketMessageChannel channel)
        {
            var messageIds = this._userRoles.Where(r => r.MessageId == newMessage.Id)
                .Select(r => r.MessageId.Value);

            if (messageIds.Any() && newMessage is IUserMessage userMessage && userMessage.IsSuppressed)
                await userMessage.ModifySuppressionAsync(false);
        }

        private async Task<(IGuildUser, IRole)> GetUserAndRoleAsync(IMessageChannel channel, UInt64 messageId,
            SocketReaction reaction)
        {
            if (!(channel is IGuildChannel guildChannel))
                return (null, null);

            var roles = this._userRoles
                .Where(r => r.MessageId == messageId)
                .ToImmutableArray();

            if (roles.Length == 0)
                return (null, null);

            var emote = reaction.Emote as Emote;
            var userRole = roles.FirstOrDefault(r =>
                r.EmojiId.HasValue && r.EmojiId.Value == emote?.Id || r.EmojiName == reaction.Emote.Name);

            if (userRole == default)
                return (null, null);

            var roleId = userRole.RoleId;
            var role = guildChannel.Guild.GetRole(roleId);

            var user = reaction.User.Value as IGuildUser ??
                       await guildChannel.Guild.GetUserAsync(reaction.UserId);

            return (user, role);
        }

        private async Task SyncReactionsAndRolesAsync(IUserMessage message)
        {
            var roles = this._userRoles.Where(r => r.MessageId == message.Id);
            var expectedReactions = roles.Select(r => (r.EmojiId.HasValue,
                r.EmojiId.HasValue ? Emote.Parse($"<:rs:{r.EmojiId}>") : null,
                r.EmojiId.HasValue ? null : new Emoji(r.EmojiName)));

            var reactions = message.Reactions.Keys
                .ToImmutableArray();

            var emoteIds = reactions.Select(r => (r as Emote)?.Id ?? 0)
                .ToImmutableArray();
            var emojiNames = reactions.Select(r => (r as Emoji)?.Name)
                .ToImmutableArray();

            foreach (var (isEmote, emote, emoji) in expectedReactions)
            {
                if (isEmote && !emoteIds.Contains(emote.Id) ||
                    !isEmote && !emojiNames.Contains(emoji.Name))
                {
                    await message.AddReactionAsync(isEmote ? (IEmote) emote : emoji);
                }
            }
        }

        private KuuhakuEmbedBuilder CreateEmbed(IGuild guild, params UserRoleDto[] userRoles)
        {
            IUser currentUser;
            if (guild is SocketGuild socketGuild)
                currentUser = socketGuild.CurrentUser;
            else currentUser = this._client.CurrentUser;

            var builder = new KuuhakuEmbedBuilder()
                .WithColor()
                .WithFooter(currentUser);

            for (var i = 0; i < userRoles.Length; i++)
            {
                var userRole = userRoles[i];
                var role = guild.GetRole(userRole.RoleId);
                builder.AddField(role.Name, userRole.ShortDescription, true);
            }

            return builder;
        }

        private Task ReactionAddedAsync(Cacheable<IUserMessage, UInt64> message, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            Task.Factory.StartNew(() =>
                this.OnReactionAddedAsync(message, channel, reaction).ConfigureAwait(false));
            return Task.CompletedTask;
        }

        private Task ReactionRemovedAsync(Cacheable<IUserMessage, UInt64> message, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            Task.Factory.StartNew(() =>
                this.OnReactionRemovedAsync(message, channel, reaction).ConfigureAwait(false));
            return Task.CompletedTask;
        }

        private Task ReactionsClearedAsync(Cacheable<IUserMessage, UInt64> message, ISocketMessageChannel channel)
        {
            Task.Factory.StartNew(() => this.OnReactionsClearedAsync(message, channel).ConfigureAwait(false));
            return Task.CompletedTask;
        }

        private Task MessageUpdatedAsync(Cacheable<IMessage, UInt64> oldMessage, SocketMessage newMessage,
            ISocketMessageChannel channel)
        {
            Task.Factory.StartNew(() =>
                this.OnMessageUpdatedAsync(oldMessage, newMessage, channel).ConfigureAwait(false));
            return Task.CompletedTask;
        }
    }
}
