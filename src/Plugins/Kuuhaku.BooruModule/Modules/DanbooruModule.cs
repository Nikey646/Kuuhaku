using System;
using System.Threading.Tasks;
using BooruViewer.Interop.Boorus;
using BooruViewer.Interop.Interfaces;
using Discord;
using Discord.Commands;
using Kuuhaku.BooruModule.Classes;

namespace Kuuhaku.BooruModule.Modules
{
    public class DanbooruModule : GenericBooruModule
    {
        private readonly SubscriptionService _service;
        public override IBooru Booru { get; }

        // ReSharper disable once SuggestBaseTypeForParameter
        public DanbooruModule(Danbooru booru, BooruRepository repository, SubscriptionService service) : base(repository)
        {
            this._service = service;
            this.Booru = booru;
        }

        [Command("Danbooru")]
        public override Task GetPostsAsync(String tags = "")
        {
            return base.GetPostsAsync(tags);
        }

        [Command("danbooru subscribe")]
        public async Task Subscribe(IChannel where = null)
        {
            where ??= this.Channel;

            await this._service.AddChannelAsync(this.Booru, this.Guild, where);
            await this.ReplyAsync(
                this.Embed.WithDescription(
                    $"You will now see new posts from Danbooru in {MentionUtils.MentionChannel(where.Id)}"));
        }
    }
}
