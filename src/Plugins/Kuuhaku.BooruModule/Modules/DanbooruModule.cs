using System;
using System.Threading.Tasks;
using BooruViewer.Interop.Boorus;
using BooruViewer.Interop.Interfaces;
using Discord;
using Discord.Commands;
using Kuuhaku.BooruModule.Classes;
using Kuuhaku.BooruModule.Models;
using Kuuhaku.Infrastructure.Extensions;
using Microsoft.Extensions.Options;

namespace Kuuhaku.BooruModule.Modules
{
    public sealed class DanbooruModule : GenericBooruModule
    {
        private readonly SubscriptionService _service;
        public override IBooru Booru { get; }

        // ReSharper disable once SuggestBaseTypeForParameter
        public DanbooruModule(Danbooru booru, BooruRepository repository, SubscriptionService service, IOptionsSnapshot<BooruOptions> options) : base(repository)
        {
            this._service = service;
            this.Booru = booru;

            var opts = options.Get("danbooru");

            if (!opts.Username.IsEmpty())
                this.Booru.WithAuthentication(opts.Username, opts.ApiKey);
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
