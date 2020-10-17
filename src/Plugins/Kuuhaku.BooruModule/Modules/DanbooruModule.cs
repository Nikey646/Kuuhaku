using System;
using System.Threading.Tasks;
using BooruViewer.Interop.Boorus;
using BooruViewer.Interop.Interfaces;
using Discord.Commands;
using Kuuhaku.BooruModule.Classes;

namespace Kuuhaku.BooruModule.Modules
{
    public class DanbooruModule : GenericBooruModule
    {
        public override IBooru Booru { get; }

        // ReSharper disable once SuggestBaseTypeForParameter
        public DanbooruModule(Danbooru booru, BooruRepository repository) : base(repository)
        {
            this.Booru = booru;
        }

        [Command("Danbooru")]
        public override Task GetPostsAsync(String tags = "")
        {
            return base.GetPostsAsync(tags);
        }
    }
}
