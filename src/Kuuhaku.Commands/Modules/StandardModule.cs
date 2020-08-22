using System.Threading.Tasks;
using Discord.Commands;
using Kuuhaku.Commands.Models;

namespace Kuuhaku.Commands.Modules
{
    public class StandardModule : KuuhakuModule
    {
        [Command("ping")]
        public Task PingAsync()
            => this.ReplyAsync("Pong!");
    }
}
