using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using Kuuhaku.Commands.Interfaces;
using Kuuhaku.Commands.Models;
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
            var processingTime = Stopwatch.StartNew();

            var actualWhen = HumanLikeParser.Parse(when);

            if (!actualWhen.HasValue)
            {
                this._logger.Warning("Failed to parse user input {input}", when);
                await this.ReplyAsync($"Unable to parse when you wanted to be reminded!");
                return;
            }


            this._logger.Trace("Creating reminder for {@time} with message: {what}", actualWhen, what);
            await this._service.AddNewReminder(this.Context, actualWhen.Value, what);

            var remindDuration = actualWhen.Value.Subtract(DateTime.UtcNow)
                // Add how long it took since we get the time to wait
                .Add(TimeSpan.FromMilliseconds(processingTime.ElapsedMilliseconds))
                // Add in the rough latency of the discord client.
                .Add(TimeSpan.FromMilliseconds(this.Client.Latency));

            await this.ReplyAsync($"I will remind you in {remindDuration.Humanize()}.");
        }

    }
}
