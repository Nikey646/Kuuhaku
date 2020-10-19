using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Kuuhaku.UserRolesModule.Models;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Kuuhaku.UserRolesModule.Classes
{
    public class UserRolesRepository
    {
        private IRedisDatabase _db;

        public UserRolesRepository(IRedisCacheClient redis)
        {
            this._db = redis.Db0;
        }

        public async Task<UserRoleDto> CreateAsync(IGuild guild, IChannel channel, IRole role, IEmote emoji,
            String description)
        {
            var userRole = new UserRoleDto(guild.Id, channel.Id, role.Id, (emoji as Emote)?.Id, emoji.Name, description);

            await this.UpdateAsync(userRole);
            return userRole;
        }

        public Task UpdateAsync(UserRoleDto userRole)
        {
            var userRoleKey = $"userRoles:{userRole.GuildId}:{userRole.ChannelId}:{userRole.RoleId}";

            return this._db.AddAsync(userRoleKey, userRole);
        }

        public Task<UserRoleDto> GetAsync(IGuild guild, IChannel channel, IRole role)
        {
            var userRoleKey = $"userRoles:{guild.Id}:{channel.Id}:{role.Id}";

            return this._db.GetAsync<UserRoleDto>(userRoleKey);
        }

        public Task DeleteAsync(UserRoleDto userRole)
            => this.DeleteAsync(userRole.GuildId, userRole.ChannelId, userRole.RoleId);

        public Task DeleteAsync(IGuild guild, IChannel channel, IRole role)
            => this.DeleteAsync(guild.Id, channel.Id, role.Id);

        public async Task DeleteAsync(UInt64 guildId, UInt64 channelId, UInt64 roleId)
        {
            var userRoleKey = $"userRoles:{guildId}:{channelId}:{roleId}";

            await this._db.RemoveAsync(userRoleKey);
        }

        public async Task<IEnumerable<UserRoleDto>> GetAllAsync()
        {
            const String userRolesPattern = "userRoles:*";
            var userRoleKeys = await this._db.SearchKeysAsync(userRolesPattern);

            var userRoles = await this._db.GetAllAsync<UserRoleDto>(userRoleKeys);
            return userRoles.Values;
        }
    }
}
