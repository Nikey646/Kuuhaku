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
    public class YandereModule : GenericBooruModule
    {
        private readonly SubscriptionService _service;
        public override IBooru Booru { get; }

        // ReSharper disable once SuggestBaseTypeForParameter
        public YandereModule(Yandere booru, BooruRepository repository, SubscriptionService service) : base(repository)
        {
            this._service = service;
            this.Booru = booru;
        }

        [Command("Yandere")]
        public override Task GetPostsAsync(String tags = "")
        {
            return base.GetPostsAsync(tags);
        }

        [Command("Yandere subscribe"), RequiredMinPermission(CommandPermissions.Admin)]
        public async Task SubscribeAsync(IChannel where = null)
        {
            where ??= this.Channel;

            var subbed = await this._service.IsChannelSubscribed(this.Booru, this.Guild, where);
            if (subbed)
            {
                await this._service.RemoveChannelAsync(this.Booru, this.Guild, where);
                await this.ReplyAsync(
                    this.Embed.WithDescription(
                        $"You will no longer see new posts from Yandere in {MentionUtils.MentionChannel(where.Id)}"));
            }
            else
            {
                await this._service.AddChannelAsync(this.Booru, this.Guild, where);
                await this.ReplyAsync(
                    this.Embed.WithDescription(
                        $"You will now see new posts from Yandere in {MentionUtils.MentionChannel(where.Id)}"));
            }
        }
    }
}
