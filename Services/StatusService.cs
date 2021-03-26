using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
 
namespace FinBot.Services
{
    public class StatusService
    {
        DiscordSocketClient client;
        public StatusService(DiscordSocketClient client)
        {
            this.client = client;

            SetStatus();
        }

        public async Task SetStatus()
        {
            await client.SetGameAsync($"Watching over {client.Guilds.Count}", "", ActivityType.CustomStatus);
            return;
        }
    }
}
