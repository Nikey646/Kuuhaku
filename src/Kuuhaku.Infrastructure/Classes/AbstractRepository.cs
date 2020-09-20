using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Kuuhaku.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Kuuhaku.Infrastructure.Classes
{
    public abstract class AbstractRepository<TEntity, TKeyType> : IRepository<TEntity, TKeyType>, IDisposable
        where TEntity : class
    {
        private readonly DbSet<TEntity> _entities;
        protected DbContext Context { get; }

        public AbstractRepository(DbContext context)
        {
            this.Context = context;
            this._entities = context.Set<TEntity>();
        }

        public virtual ValueTask<TEntity> GetAsync(TKeyType id)
            => this._entities.FindAsync(id);

        public virtual Task<List<TEntity>> GetAllAsync()
            => this._entities.AsQueryable().ToListAsync();

        public virtual Task<List<TEntity>> FindAsync(Expression<Func<TEntity, Boolean>> predicate)
            => this._entities.AsQueryable().Where(predicate).ToListAsync();

        public ValueTask<EntityEntry<TEntity>> AddAsync(TEntity entity)
            => this._entities.AddAsync(entity);

        public virtual Task AddRangeAsync(IEnumerable<TEntity> entities)
            => this._entities.AddRangeAsync(entities);

        public virtual Task RemoveAsync(TEntity entity)
        {
            this._entities.Remove(entity);
            return Task.CompletedTask;
        }

        public virtual Task RemoveRangeAsync(IEnumerable<TEntity> entities)
        {
            this._entities.RemoveRange(entities);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            this.Context?.Dispose();
        }
    }
}
