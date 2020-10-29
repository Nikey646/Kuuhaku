using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Kuuhaku.Commands.Interfaces;
using Kuuhaku.Commands.Models;
using Kuuhaku.Infrastructure.Classes;
using Kuuhaku.Infrastructure.Extensions;

namespace Kuuhaku.Commands.Services
{
    public class InteractiveSearchService
    {
        private readonly DiscordSocketClient _client;
        private readonly IInteractionService _interactionService;

        public InteractiveSearchService(DiscordSocketClient client, IInteractionService interactionService)
        {
            this._client = client;
            this._interactionService = interactionService;
        }

        public async Task HandleSearchAsync<T>(KuuhakuCommandContext context,
            (IEnumerable<T> items, KuuhakuEmbedBuilder[] pages) what,
            Func<T, (String title, String description)> fieldGenerator,
            Func<KuuhakuEmbedBuilder, KuuhakuEmbedBuilder> embedGenerator,
            TimeSpan? timeout = null)
        {
            timeout ??= TimeSpan.FromMinutes(1);

            var embed = embedGenerator(new KuuhakuEmbedBuilder());
            var items = what.items.ToImmutableArray();

            for (var i = 0; i < items.Length; i++)
            {
                var (title, description) = fieldGenerator(items[i]);
                var pos = $"{i+1}. ";
                embed.WithField($"{pos}{title.Truncate(256 - (pos.Length + 1))}",
                    description.Truncate(1023), false);
            }

            var message = await context.Channel.SendMessageAsync(embed);
            var response = await this._interactionService.NextMessageAsync(context, true, true, timeout.Value);

            if (response == null)
            {
                foreach (var field in embed.Fields)
                    field.Name = field.Name.Substring(field.Name.IndexOf(' '));

                await message.ModifyAsync(m => m.Embed = embed.Build());
                return;
            }

            if (UInt32.TryParse(response.Content, out var id))
            {
                await message.ModifyAsync(m => m.Embed = what.pages[id - 1].Build());
                await response.DeleteAsync();
            }
        }
    }
}
