using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Kuuhaku.ReminderModule.Models;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Kuuhaku.ReminderModule.Classes
{
    public class ReminderRepository
    {
        private IRedisDatabase _db;

        public ReminderRepository(IRedisCacheClient redis)
        {
            this._db = redis.Db0;
        }

        public async Task<ReminderDto> CreateAsync(IGuild guild, IMessage message, IUser user, DateTime when, String what)
        {
            var reminder = new ReminderDto(guild.Id, message.Channel.Id, message.Id, user.Id, when, what);

            await this.UpdateAsync(reminder);
            return reminder;
        }

        public async Task UpdateAsync(ReminderDto reminder)
        {
            var reminderKey = $"reminders:{reminder.GuildId}:{reminder.ChannelId}:{reminder.MessageId}";

            await this._db.AddAsync(reminderKey, reminder);
        }

        public Task<ReminderDto> GetAsync(IGuild guild, IMessage message)
            => this.GetAsync(guild.Id, message.Channel.Id, message.Id);

        public Task<ReminderDto> GetAsync(UInt64 guildId, UInt64 channelId, UInt64 messageId)
        {
            var reminderKey = $"reminders:{guildId}:{channelId}:{messageId}";

            return this._db.GetAsync<ReminderDto>(reminderKey);
        }

        public async Task<IEnumerable<ReminderDto>> GetAllAsync()
        {
            const String reminderPattern = "reminders:*";
            var reminderKeys = await this._db.SearchKeysAsync(reminderPattern);

            var reminders = await this._db.GetAllAsync<ReminderDto>(reminderKeys);
            return reminders.Values;
        }
    }
}
