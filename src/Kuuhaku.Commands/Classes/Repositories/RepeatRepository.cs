using System;
using System.Threading.Tasks;
using Discord;
using Kuuhaku.Commands.Models;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Kuuhaku.Commands.Classes.Repositories
{
    public class RepeatRepository
    {
        private readonly IRedisDatabase _db;

        public RepeatRepository(IRedisCacheClient redis)
        {
            this._db = redis.Db0;
        }

        public async Task CreateAsync(String command, IGuild guild, IUser user)
        {
            var repeatKey = $"repeat:{guild.Id}:{user.Id}";

            var repeatDto = new RepeatCommandDto(command, guild.Id, user.Id);

            await this._db.AddAsync(repeatKey, repeatDto);
        }

        public async Task<RepeatCommandDto> GetAsync(IGuild guild, IUser user)
        {
            var repeatKey = $"repeat:{guild.Id}:{user.Id}";

            return await this._db.GetAsync<RepeatCommandDto>(repeatKey);
        }
    }
}
