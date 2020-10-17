using System;
using System.Threading.Tasks;
using BooruViewer.Interop.Boorus;
using BooruViewer.Interop.Interfaces;
using Discord.Commands;
using Kuuhaku.BooruModule.Classes;

namespace Kuuhaku.BooruModule.Modules
{
    public class KonachanModule : GenericBooruModule
    {
        public override IBooru Booru { get; }

        // ReSharper disable once SuggestBaseTypeForParameter
        public KonachanModule(KonaChan booru, BooruRepository repository) : base(repository)
        {
            this.Booru = booru;
        }

        [Command("Konachan")]
        public override Task GetPostsAsync(String tags = "")
        {
            return base.GetPostsAsync(tags);
        }
    }
}
