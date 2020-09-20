using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Kuuhaku.Infrastructure.Classes
{
    public abstract class UnitOfWork<TDbContext>: IDisposable
        where TDbContext : DbContext
    {
        protected TDbContext Context { get; }

        public UnitOfWork(TDbContext context)
        {
            this.Context = context;
        }

        public async Task<Int32> CompleterAsync()
        {
            return await this.Context.SaveChangesAsync();
        }

        public void Dispose()
        {
            this.Context.Dispose();
        }
    }
}
