using System;
using System.Net.Http;
using System.Threading.Tasks;
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

        private HttpClient _client;

        public PetsModule(HttpClient client)
        {
            this._client = client;
        }

        [Command("dog"), Alias("actually a cat")]
        public async Task RandomDogAsync()
        {
            using var typing = this.Channel.EnterTypingState();
            var response = await this._client.GetAsync(DogApi);
            var file = await response.Content.ReadAsStringAsync();

            await this.ReplyAsync(this.Embed.WithImageUrl($"{DogUrl}{file}"));
        }

        [Command("cat"), Alias("actually a dog")]
        public async Task RandomCatAsync()
        {
            using var typing = this.Channel.EnterTypingState();
            var response = await this._client.GetAsync(CatApi);
            var json = await response.Content.ReadAsStringAsync();
            var res = JsonConvert.DeserializeAnonymousType(json, new {file = ""});

            await this.ReplyAsync(this.Embed.WithImageUrl(res.file));
        }

    }
}
