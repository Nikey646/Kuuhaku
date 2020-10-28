using System;
using System.Threading.Tasks;
using BooruViewer.Interop.Boorus;
using BooruViewer.Interop.Interfaces;
using Discord;
using Discord.Commands;
using Kuuhaku.BooruModule.Classes;
using Kuuhaku.Commands.Attributes;
using Kuuhaku.Commands.Models;

namespace Kuuhaku.BooruModule.Modules
{
    public class KonachanModule : GenericBooruModule
    {
        private readonly SubscriptionService _service;
        public override IBooru Booru { get; }

        // ReSharper disable once SuggestBaseTypeForParameter
        public KonachanModule(KonaChan booru, BooruRepository repository, SubscriptionService service) : base(repository)
        {
            this._service = service;
            this.Booru = booru;
        }

        [Command("Konachan")]
        public override Task GetPostsAsync(String tags = "")
        {
            return base.GetPostsAsync(tags);
        }

        [Command("Konachan subscribe"), RequiredMinPermission(CommandPermissions.Admin)]
        public async Task SubscribeAsync(IChannel where = null)
        {
            where ??= this.Channel;

            var subbed = await this._service.IsChannelSubscribed(this.Booru, this.Guild, where);
            if (subbed)
            {
                await this._service.RemoveChannelAsync(this.Booru, this.Guild, where);
                await this.ReplyAsync(
                    this.Embed.WithDescription(
                        $"You will no longer see new posts from KonaChan in {MentionUtils.MentionChannel(where.Id)}"));
            }
            else
            {
                await this._service.AddChannelAsync(this.Booru, this.Guild, where);
                await this.ReplyAsync(
                    this.Embed.WithDescription(
                        $"You will now see new posts from KonaChan in {MentionUtils.MentionChannel(where.Id)}"));
            }
        }
    }
}
