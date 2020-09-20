using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Kuuhaku.Infrastructure.Interfaces
{
    public interface IRepository<TEntity, in TKeyType>
        where TEntity : class
    {
        ValueTask<TEntity> GetAsync(TKeyType id);
        Task<List<TEntity>> GetAllAsync();
        Task<List<TEntity>> FindAsync(Expression<Func<TEntity, Boolean>> predicate);

        ValueTask<EntityEntry<TEntity>> AddAsync(TEntity entity);
        Task AddRangeAsync(IEnumerable<TEntity> entities);

        Task RemoveAsync(TEntity entity);
        Task RemoveRangeAsync(IEnumerable<TEntity> entities);
    }
}
