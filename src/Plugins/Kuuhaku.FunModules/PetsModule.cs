using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Kuuhaku.Commands.Models;
using Newtonsoft.Json;

namespace Kuuhaku.FunModules
{
    public class PetsModule : KuuhakuModule
    {
        private const String DogUrl = "https://random.dog/";
        private const String DogApi = "https://random.dog/woof";
        private const String CatApi = "http://aws.random.cat/meow";

        private const String NekoApi = "https://nekos.life/api/neko";
        private const String LewdNekoApi = "https://nekos.life/api/lewd/neko";

        private HttpClient _client;

        public PetsModule(HttpClient client)
        {
            this._client = client;
        }

        [Command("dog"), Alias("actually a cat")]
        public async Task RandomDogAsync()
        {
            using var typing = this.Channel.EnterTypingState();
            while (true)
            {
                var response = await this._client.GetAsync(DogApi);
                var file = await response.Content.ReadAsStringAsync();

                var image = new Uri($"{DogUrl}{file}");

                var request = new HttpRequestMessage(HttpMethod.Options, image);

                var optsReq = await this._client.SendAsync(request);
                if (!optsReq.IsSuccessStatusCode)
                    continue;

                await this.ReplyAsync(this.Embed.WithImageUrl(image.ToString())
                    .WithAuthor("Random.dog", url: "https://random.dog"));
                break;
            }
        }

        [Command("cat"), Alias("actually a dog")]
        public async Task RandomCatAsync()
        {
            using var typing = this.Channel.EnterTypingState();
            var response = await this._client.GetAsync(CatApi);
            var json = await response.Content.ReadAsStringAsync();
            var res = JsonConvert.DeserializeAnonymousType(json, new {file = ""});

            await this.ReplyAsync(this.Embed.WithImageUrl(res.file)
                .WithAuthor("Random.cat", url: "https://random.cat"));
        }

        [Command("neko")]
        public async Task RandomNekoAsync()
        {
            using var _ = this.Channel.EnterTypingState();

            var isNsfw = (this.Channel as ITextChannel)?.IsNsfw ?? false;

            var response = await this._client.GetAsync(isNsfw ? LewdNekoApi : NekoApi);
            var json = await response.Content.ReadAsStringAsync();
            var res = JsonConvert.DeserializeAnonymousType(json, new {neko = ""});

            await this.ReplyAsync(this.Embed.WithImageUrl(res.neko)
                .WithAuthor("Random Neko", url: "https://nekos.life"));
        }
    }
}
