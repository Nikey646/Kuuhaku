using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kuuhaku.Database;
using Kuuhaku.Database.DbModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Kuuhaku.ReminderModule.Classes
{
    public class ReminderRepository : IDisposable
    {
        private readonly IServiceScope _serviceScope;
        private readonly DisgustingGodContext _context;

        public ReminderRepository(IServiceProvider provider)
        {
            this._serviceScope = provider.CreateScope();
            this._context = this._serviceScope.ServiceProvider.GetService<DisgustingGodContext>();
        }

        public Task<List<Reminder>> GetRemindersAsync(CancellationToken ct = default)
        {
            return this._context.Reminders.Where(r => r.IsActive)
                .ToListAsync(ct);
        }

        public async Task SetReminderActiveAsync(Guid id, Boolean isActive, CancellationToken ct = default)
        {
            var reminder = await this._context.Reminders.Where(r => r.Id == id).FirstOrDefaultAsync(ct);
            reminder.IsActive = isActive;
            await this._context.SaveChangesAsync(ct);
        }

        public async Task<Reminder> AddReminderAsync(Reminder reminder, CancellationToken ct = default)
        {
            var reminderEntry = await this._context.Reminders.AddAsync(reminder, ct);
            await this._context.SaveChangesAsync(ct);
            return reminderEntry.Entity; // TODO: Should return the EntityEntry itself??
        }

        public void Dispose()
        {
            Log.Fatal("Disposing of ReminderRepository");
            this._serviceScope?.Dispose();
            this._context?.Dispose();
        }
    }
}
