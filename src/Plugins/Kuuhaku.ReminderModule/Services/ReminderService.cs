using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Kuuhaku.Commands.Models;
using Kuuhaku.Infrastructure.Classes;
using Kuuhaku.Infrastructure.Extensions;
using Kuuhaku.Infrastructure.Models;
using Kuuhaku.ReminderModule.Classes;
using Kuuhaku.ReminderModule.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kuuhaku.ReminderModule.Services
{
    public class ReminderService : BackgroundService
    {
        private readonly DiscordSocketClient _client;
        private readonly ReminderRepository _repository;
        private readonly ILogger<ReminderService> _logger;
        private List<ReminderDto> _reminders;

        public ReminderService(DiscordSocketClient client, ReminderRepository repository, ILogger<ReminderService> logger)
        {
            this._client = client;
            this._repository = repository;
            this._logger = logger;
            this._reminders = new List<ReminderDto>();
        }

        public async Task AddNewReminder(KuuhakuCommandContext context, DateTime when, String what)
        {
            var reminder = await this._repository.CreateAsync(context.Guild, context.Message, context.User, when, what);
            this._reminders.Add(reminder);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._logger.Info("Reminder service Started");

            var reminders = await this._repository.GetAllAsync();
            this._reminders.AddRange(reminders);

            while (!stoppingToken.IsCancellationRequested)
            {
                await this.CheckReminders(stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }

            this._logger.Info("Reminder service Stopping");
        }

        private async Task CheckReminders(CancellationToken ct)
        {
            if (this._client.ConnectionState != ConnectionState.Connected)
                return; // Don't try to process.

            var remindersToRemind = this.GetExpiredReminders().ToImmutableList();
            if (remindersToRemind.Count == 0)
                return;

            this._logger.Info("Found {expiredRemindersCount} reminders that have expired. Attempting to send reminders.", remindersToRemind.Count);
            foreach (var reminder in remindersToRemind)
            {
                var reminderChannel = await this.GetChannelAsync(reminder);
                var reminderMessage = new KuuhakuEmbedBuilder()
                    .WithColor(EmbedColorType.Success)
                    .WithTitle("Reminder!")
                    .WithDescription(reminder.Contents)
                    .WithFooter(this.GetCurrentUser(reminder))
                    .WithTimestamp(reminder.CreatedAt);

                var (isLate, howLate) = this.CheckIfLate(reminder);
                if (isLate)
                {
                    reminderMessage.AddField("Late!",
                        $"Sorry about this, your reminder has arrived about {howLate.ToDuration()} late :s");
                }

                await reminderChannel.SendMessageAsync(MentionUtils.MentionUser(reminder.UserId), reminderMessage, ct);

                reminder.IsActive = false;
                await this._repository.UpdateAsync(reminder);
                this._reminders.Remove(reminder);
            }
        }

        private IEnumerable<ReminderDto> GetExpiredReminders()
            => this._reminders.Where(r => r.IsActive && DateTime.UtcNow > r.RemindAt);

        private Task<IMessageChannel> GetChannelAsync(ReminderDto reminder)
        {
            var guild = this._client.GetGuild(reminder.GuildId);
            return Task.FromResult<IMessageChannel>(guild.GetTextChannel(reminder.ChannelId));
        }

        private (Boolean isLate, TimeSpan howLate) CheckIfLate(ReminderDto reminder)
        {
            var currentTime = DateTime.UtcNow;
            var expectedTime = reminder.RemindAt;

            var diff = currentTime.Subtract(expectedTime);

            // Only display the late message if we're > 1 minute late.
            return diff.TotalMinutes >= 1
                ? (true, diff)
                : (false, TimeSpan.Zero);
        }

        private IUser GetCurrentUser(ReminderDto reminder)
        {
            var guild = this._client.GetGuild(reminder.GuildId);
            return guild.CurrentUser;
        }
    }
}
