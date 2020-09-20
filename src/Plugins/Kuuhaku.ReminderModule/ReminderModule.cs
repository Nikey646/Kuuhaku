using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using Kuuhaku.Commands.Interfaces;
using Kuuhaku.Commands.Models;
using Kuuhaku.Database.DbModels;
using Kuuhaku.Infrastructure.Extensions;
using Kuuhaku.ReminderModule.Services;
using Microsoft.Extensions.Logging;

namespace Kuuhaku.ReminderModule
{
    public class ReminderModule : KuuhakuModule
    {
        private readonly ReminderService _service;
        private readonly IInteractionService _interactionService;
        private readonly ILogger<ReminderModule> _logger;

        public ReminderModule(ReminderService service, IInteractionService interactionService, ILogger<ReminderModule> logger)
        {
            this._service = service ?? throw new ArgumentNullException(nameof(service));
            this._interactionService = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // TODO: Make this a two part command, the first part takes what you want to be reminded about
        // The second part will take when you want to be reminded.
        [Command("remind"), Alias("remindme", "reminder")]
        public async Task CreateReminder([Remainder] String what)
        {
            await this.ReplyAsync($"When would you like to be reminded about {what.Quote()}");
            var response = await this._interactionService.NextMessageAsync(this.Context, true, true, TimeSpan.FromSeconds(30));
            if (response == null)
            {
                await this.Message.AddReactionAsync(new Emoji("☹️"));
                return;
            }

            var when = response.Content;

            var actualWhen = HumanLikeParser.Parse(when);

            if (!actualWhen.HasValue)
            {
                this._logger.Warning("Failed to parse user input {input}", when);
                await this.ReplyAsync($"Unable to parse when you wanted to be reminded!");
                return;
            }

            var remindDuration = actualWhen.Value.Subtract(DateTime.UtcNow);

            this._logger.Trace("Creating reminder for {@time} with message: {what}", actualWhen, what);
            var reminder = new Reminder(this.IsPrivate ? null : this.Guild?.Id, this.Channel.Id, this.User.Id, actualWhen.Value, what);
            await this._service.AddNewReminder(reminder);

            await this.ReplyAsync($"I will remind you in {remindDuration.Humanize()}.");
        }

    }
}
