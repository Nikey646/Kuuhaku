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
using Kuuhaku.ReminderModule.Classes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kuuhaku.ReminderModule.Services
{
    public class ReminderService : BackgroundService
    {
        private readonly ReminderRepository _repository;
        private readonly DiscordSocketClient _client;
        private readonly ILogger<ReminderService> _logger;
        private readonly List<Reminder> _reminders;

        private static ReminderService _instance { get; set; }

        public ReminderService(ReminderRepository repository, DiscordSocketClient client, ILogger<ReminderService> logger)
        {
            this._repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this._client = client;
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this._reminders = new List<Reminder>();
        }

        public async Task AddNewReminder(Reminder reminder)
        {
            if (reminder == null)
                throw new ArgumentNullException(nameof(reminder));

            var insertedReminder = await this._repository.AddReminderAsync(reminder);
            _instance._reminders.Add(insertedReminder);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this._logger.Info("Reminder Service Started");
            _instance = this;

            var reminders = await this._repository.GetRemindersAsync(stoppingToken);
            this._reminders.AddRange(reminders);
            while (!stoppingToken.IsCancellationRequested)
            {
                await this.ReminderTick(stoppingToken);
                // Wait 1 second before checking again.
                // This can result in 'drifting', but that's fine.
                await Task.Delay(TimeSpan.FromSeconds(1));
            }

            this._logger.Info("Reminder Service Stopping");
        }

        private async Task ReminderTick(CancellationToken ct)
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
                await this._repository.SetReminderActiveAsync(reminder.Id, false, ct);
                this._reminders.Remove(reminder);
            }
        }

        private IEnumerable<Reminder> GetExpiredReminders()
            => this._reminders.Where(r => r.IsActive && DateTime.UtcNow > r.RemindAt);

        private async Task<IMessageChannel> GetChannelAsync(Reminder reminder)
        {
            if (!reminder.GuildId.HasValue)
                return await this._client.GetDMChannelAsync(reminder.ChannelId);

            var guild = this._client.GetGuild(reminder.GuildId.Value);
            return guild.GetTextChannel(reminder.ChannelId);
        }

        private (Boolean isLate, TimeSpan howLate) CheckIfLate(Reminder reminder)
        {
            var currentTime = DateTime.UtcNow;
            var expectedTime = reminder.RemindAt;

            var diff = currentTime.Subtract(expectedTime);

            // Only display the late message if we're > 1 minute late.
            return diff.TotalMinutes >= 1
                ? (true, diff)
                : (false, TimeSpan.Zero);
        }

        private IUser GetCurrentUser(Reminder reminder)
        {
            if (!reminder.GuildId.HasValue)
                return this._client.CurrentUser;

            var guild = this._client.GetGuild(reminder.GuildId.Value);
            return guild.CurrentUser;

        }
    }
}
