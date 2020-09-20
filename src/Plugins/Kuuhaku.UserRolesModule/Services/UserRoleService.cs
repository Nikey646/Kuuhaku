using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Kuuhaku.Database.DbModels;
using Kuuhaku.Infrastructure.Classes;
using Kuuhaku.Infrastructure.Extensions;
using Kuuhaku.Infrastructure.Models;
using Kuuhaku.UserRolesModule.Classes;
using Kuuhaku.UserRolesModule.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kuuhaku.UserRolesModule.Services
{
    public class UserRoleService : IHostedService
    {
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _provider;
        private readonly List<UserRoleLocation> _userRoles;

        private static UserRoleService _instance;

        public UserRoleService(DiscordSocketClient client, IServiceProvider provider)
        {
            this._client = client;
            this._provider = provider;
            this._userRoles = new List<UserRoleLocation>();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = this._provider.CreateScope();
            using var unitOfWork = scope.ServiceProvider.GetRequiredService<UserRolesUoW>();

            var userRoles = await unitOfWork.UserRoles.GetAllAsync();
            this._userRoles.AddRange(userRoles);
            _instance = this;

            this._client.ReactionAdded += this.ReactionAddedAsync;
            this._client.ReactionRemoved += this.ReactionRemovedAsync;
            // this._client.ReactionsCleared += this.ReactionsClearedAsync;

            this._client.MessageUpdated += this.MessageUpdatedAsync;

            // TODO: Rebuild all embeds on start up to ensure that the displayed role names are up to date
            // TODO: Maybe hook up an event to watch for changes to roles and see if we have one, then update it's embed?
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this._client.ReactionAdded -= this.ReactionAddedAsync;
            this._client.ReactionRemoved -= this.ReactionRemovedAsync;
            // this._client.ReactionsCleared -= this.ReactionsClearedAsync;
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

            var self = _instance;

            using var scope = this._provider.CreateScope();
            using var unitOfWork = scope.ServiceProvider.GetRequiredService<UserRolesUoW>();

            var userRole = new UserRole
            {
                EmojiName = reaction.Name,
                RoleId = role.Id,
                ShortDescription = description
            };

            // Emote == custom emoji.
            if (reaction is Emote emote)
                userRole.EmojiId = emote.Id;

            var userRoleLoc =
                self._userRoles.FirstOrDefault(url => url.GuildId == guild.Id && url.ChannelId == channel.Id);
            var selfUserRoleLoc = userRoleLoc;
            if (userRoleLoc == default)
            {
                var entity = await unitOfWork.UserRoles.AddAsync(new UserRoleLocation
                {
                    GuildId = guild.Id,
                    ChannelId = channel.Id,
                    Roles = new List<UserRole>(),
                });
                selfUserRoleLoc = userRoleLoc = entity.Entity;
                self._userRoles.Add(userRoleLoc);
            }
            else
            {
                userRoleLoc = await unitOfWork.UserRoles.GetAsync(userRoleLoc.Id);
            }

            var messageIds = userRoleLoc.Roles
                .Select(r => r.MessageId)
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .ToImmutableArray();

            var activeChannel = await guild.GetTextChannelAsync(channel.Id);
            IEmote emoji;
            IUserMessage message = null;
            if (userRole.EmojiId.HasValue)
                emoji = Emote.Parse($"<:rs:{userRole.EmojiId}>");
            else emoji = new Emoji(userRole.EmojiName);

            if (messageIds.Length > 0)
            {
                // Find the first message id with space available in this channel
                foreach (var messageId in messageIds)
                {
                    message = await activeChannel.GetMessageAsync(messageId) as IUserMessage;
                    if (message == null)
                        throw new Exception(
                            $"The message ({messageId}) received from the channel ({channel.Id}) was not a user message...?");
                    var embed = message.Embeds.First();
                    if (embed.Fields.Length == 25)
                        continue; // We're already full, try the next message

                    var rolesToDisplay = userRoleLoc.Roles
                        .Where(r => r.MessageId == messageId)
                        .ToImmutableArray();

                    // If we have more than the maximum amount of fields, create a new message and embed.
                    if (rolesToDisplay.Length >= EmbedBuilder.MaxFieldCount)
                    {
                        var newEmbed = this.CreateEmbeds(new[] {userRole}, guild);
                        message = await activeChannel.SendMessageAsync(newEmbed);
                        break; // The for loop as useless now.
                    }

                    // Otherwise update the existing message's embed
                    var updatedEmbed = this.CreateEmbeds(rolesToDisplay.Add(userRole), guild);
                    await message.ModifyAsync(m => m.Embed = updatedEmbed.Build());
                    break; // break early in case this isn't the last message id.
                }
            }

            if (message == null)
            {
                var newEmbed = this.CreateEmbeds(new[] {userRole}, guild);
                message = await activeChannel.SendMessageAsync(newEmbed);
            }

            await message.AddReactionAsync(emoji);
            userRole.MessageId = message.Id;

            userRoleLoc.Roles.Add(userRole);
            selfUserRoleLoc.Roles.Add(userRole);

            await unitOfWork.CompleterAsync();
            // self._userRoles.Add(userRoleLoc);
        }

        public async Task RemoveRoleAsync(IGuild guild, IMessageChannel channel, IRole role)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (role == null) throw new ArgumentNullException(nameof(role));

            var self = _instance;
            var userRoleLocation = self._userRoles
                .FirstOrDefault(url => url.GuildId == guild.Id && url.ChannelId == channel.Id);

            var userRole = userRoleLocation?.Roles
                .FirstOrDefault(u => u.RoleId == role.Id);

            if (userRole == null)
                return;

            using var scope = this._provider.CreateScope();
            using var unitOfWork = scope.ServiceProvider.GetRequiredService<UserRolesUoW>();

            var urlDb = await unitOfWork.UserRoles.GetAsync(userRoleLocation.Id);

            if (default == urlDb)
                return;

            var roleToRemove = urlDb.Roles.FirstOrDefault(r => r.RoleId == role.Id);
            urlDb.Roles.Remove(roleToRemove);
            // naughty?
            unitOfWork.UserRoles.Context.UserRoles.Remove(roleToRemove);

            var hasException = false;
            try
            {
                var activeChannel = await guild.GetTextChannelAsync(channel.Id);
                IEmote emoji;
                if (userRole.EmojiId.HasValue)
                    emoji = Emote.Parse($"<:rs:{userRole.EmojiId}>");
                else emoji = new Emoji(userRole.EmojiName);
                var messageId = userRole.MessageId;

                if (!messageId.HasValue)
                    return;

                var message = await activeChannel.GetMessageAsync(messageId.Value) as IUserMessage;
                if (message == null)
                    throw new Exception(
                        $"The message ({messageId}) received from the channel ({channel.Id}) was not a user message...?");

                var rolesToDisplay = urlDb.Roles
                    .Where(r => r.MessageId == messageId)
                    .ToImmutableArray();

                // Otherwise update the existing message's embed
                var updatedEmbed = this.CreateEmbeds(rolesToDisplay, guild);
                await message.ModifyAsync(m => m.Embed = updatedEmbed.Build());

                foreach (var (reaction, metadata) in message.Reactions)
                {
                    var isOldReaction = false;
                    if (reaction is Emote emote && emoji is Emote emojiEmote)
                        isOldReaction = emote.Id == emojiEmote.Id;
                    if (reaction.Name == emoji.Name)
                        isOldReaction = true;

                    if (!isOldReaction)
                        continue;

                    await message.RemoveReactionAsync(reaction, this._client.CurrentUser);
                    break;
                }
            }
            catch (Exception)
            {
                hasException = true;
                throw;
            }
            finally
            {
                if (!hasException)
                {
                    userRoleLocation.Roles.Remove(userRole);
                    await unitOfWork.CompleterAsync();
                }
            }
        }

        private KuuhakuEmbedBuilder CreateEmbeds(IReadOnlyList<UserRole> userRoles, IGuild guild)
        {
            IUser currentUser;
            if (guild is SocketGuild socketGuild)
                currentUser = socketGuild.CurrentUser;
            else currentUser = this._client.CurrentUser;

            var builder = new KuuhakuEmbedBuilder()
                .WithColor()
                .WithFooter(currentUser);

            for (var i = 0; i < userRoles.Count; i++)
            {
                var userRole = userRoles[i];
                var role = guild.GetRole(userRole.RoleId);
                builder.AddField(role.Name, userRole.ShortDescription, true);
            }

            return builder;
        }

        private async Task OnReactionAddedAsync(Cacheable<IUserMessage, UInt64> message, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
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

        // private async Task OnReactionsClearedAsync(Cacheable<IUserMessage, UInt64> message, ISocketMessageChannel channel)
        // {
        //     // TODO: Re-apply all reactions so that users know what to react.
        // }

        private async Task<(IGuildUser, IRole)> GetUserAndRoleAsync(IMessageChannel channel, UInt64 messageId,
            SocketReaction reaction)
        {
            if (!(channel is IGuildChannel guildChannel))
                return (null, null); // We don't care about reactions in channels that aren't from a guild.

            var roles = this._userRoles
                .Where(url => url.GuildId == guildChannel.GuildId && url.ChannelId == guildChannel.Id)
                .SelectMany(url => url.Roles)
                .Where(r => r.MessageId == messageId)
                .ToImmutableArray();

            // There was no roles for this message
            if (roles.Length == 0)
                return (null, null);

            UserRole role;

            var emoji = reaction.Emote;
            if (emoji is Emote emote)
                role = roles.FirstOrDefault(r => r.EmojiId == emote.Id);
            else role = roles.FirstOrDefault(r => r.EmojiName == emoji.Name);

            if (role == default)
                return (null, null);

            var roleId = role.RoleId;
            var roleObj = guildChannel.Guild.GetRole(roleId);

            var user = await guildChannel.Guild.GetUserAsync(reaction.UserId);
            return (user, roleObj);
        }

        private Task ReactionAddedAsync(Cacheable<IUserMessage, UInt64> message, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            Task.Factory.StartNew(async () =>
                await this.OnReactionAddedAsync(message, channel, reaction).ConfigureAwait(false));
            return Task.CompletedTask;
        }

        private Task ReactionRemovedAsync(Cacheable<IUserMessage, UInt64> message, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            Task.Factory.StartNew(async () =>
                await this.OnReactionRemovedAsync(message, channel, reaction).ConfigureAwait(false));
            return Task.CompletedTask;
        }

        // private Task ReactionsClearedAsync(Cacheable<IUserMessage, UInt64> message, ISocketMessageChannel channel)
        // {
        //     Task.Factory.StartNew(async () => await this.OnReactionsClearedAsync(message, channel).ConfigureAwait(false));
        //     return Task.CompletedTask;
        // }

        private async Task OnMessageUpdatedAsync(Cacheable<IMessage, UInt64> oldMessage, SocketMessage newMessage,
            ISocketMessageChannel channel)
        {
            var messageIds = this._userRoles
                .Where(url => url.ChannelId == newMessage.Channel.Id)
                .SelectMany(url => url.Roles
                    .Where(r => r.MessageId.HasValue)
                    .Select(r => r.MessageId.Value))
                .Distinct();

            if (messageIds.Contains(newMessage.Id) && newMessage is IUserMessage userMessage && newMessage.IsSuppressed)
                await userMessage.ModifySuppressionAsync(false);
        }

        private Task MessageUpdatedAsync(Cacheable<IMessage, UInt64> oldMessage, SocketMessage newMessage,
            ISocketMessageChannel channel)
        {
            Task.Factory.StartNew(async () =>
                await this.OnMessageUpdatedAsync(oldMessage, newMessage, channel).ConfigureAwait(false));
            return Task.CompletedTask;
        }
    }
}
