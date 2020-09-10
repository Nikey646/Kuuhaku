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

            // TODO: Hook up events for reacting to changes
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task AddRoleAsync(IGuild guild, IMessageChannel channel, IRole role, IEmote reaction, String description)
        {
            if (guild == null) throw new ArgumentNullException(nameof(guild));
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (role == null) throw new ArgumentNullException(nameof(role));
            if (reaction == null) throw new ArgumentNullException(nameof(reaction));
            if (description.IsEmpty() || description.Length > 200) throw new ArgumentException("Description cannot be empty or longer than 200 characters.", nameof(description));

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
            if (userRoleLoc == default)
            {
                var entity = await unitOfWork.UserRoles.AddAsync(new UserRoleLocation
                {
                    GuildId = guild.Id,
                    ChannelId = channel.Id,
                    Roles = new List<UserRole>(),
                    MessageIds = new List<UInt64>(),
                });
                userRoleLoc = entity.Entity;
            }

            userRoleLoc.Roles.Add(userRole);
            var messages = new List<IUserMessage>();

            if (userRoleLoc.MessageIds.Count == 0)
            {
                var embeds = this.CreateEmbeds(userRoleLoc.Roles.ToArray(), guild).ToArray();
                foreach (var embed in embeds)
                {
                    var message = await channel.SendMessageAsync(embed);
                    messages.Add(message);
                    userRoleLoc.MessageIds.Add(message.Id);
                }
            }
            else
            {
                var embeds = this.CreateEmbeds(userRoleLoc.Roles.ToArray(), guild).ToArray();
                foreach (var messageId in userRoleLoc.MessageIds)
                {
                    var message = (IUserMessage) await channel.GetMessageAsync(messageId);
                    messages.Add(message);
                }
                await messages[0].ModifyAsync(m => m.Embed = embeds[0].Build());
            }

            foreach (var message in messages)
            {
                var embed = message.Embeds.First();
                var rolesInMessage = embed.Fields.Select(f => f.Name).ToImmutableArray();
                var roles = guild.Roles.Where(r => rolesInMessage.Contains(r.Name)).Select(r => r.Id).ToImmutableArray();
                var emojis = userRoleLoc.Roles.Where(ur => roles.Contains(ur.RoleId))
                    .Select(ur => new {Id = ur.EmojiId, Name = ur.EmojiName});

                foreach (var emoji in emojis)
                {
                    if (emoji.Id == null)
                        await message.AddReactionAsync(new Emoji(emoji.Name));
                    else await message.AddReactionAsync(Emote.Parse($"<:e:{emoji.Id}>"));
                }
            }

            await unitOfWork.CompleterAsync();
            self._userRoles.Add(userRoleLoc);
        }

        private IEnumerable<KuuhakuEmbedBuilder> CreateEmbeds(UserRole[] userRoles, IGuild guild)
        {
            IUser currentUser;
            if (guild is SocketGuild socketGuild)
                currentUser = socketGuild.CurrentUser;
            else currentUser = this._client.CurrentUser;

            var messageCount = Math.Ceiling(userRoles.Length / 25d);
            var rolesLeft = userRoles.Length;

            for (var i = 0; i < messageCount; i++)
            {
                var builder = new KuuhakuEmbedBuilder()
                    .WithColor()
                    .WithFooter(currentUser);

                for (var k = 0; k < Math.Min(25, rolesLeft); k++)
                {
                    var userRole = userRoles[k * (i + 1)];
                    var role = guild.GetRole(userRole.RoleId);
                    builder.AddField(role.Name, userRole.ShortDescription, true);
                }

                yield return builder;
                rolesLeft -= 25;
            }
        }

        // TODO: Handle message updated events to ensure the embed isn't accidentally deleted?
        // TODO: Watch for reactions added or removed from certain messages.
        // TODO: Add additional roles to (tracked) embed via methods?

    }
}
