using Discord.Commands;
using System.Threading.Tasks;

namespace PasteBot.Commands
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        async Task Ping()
        {
            int latency = Context.Client.Latency;

            await ReplyAsync($"The current latency is {latency}ms");
        }
    }
}