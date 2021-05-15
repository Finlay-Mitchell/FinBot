using Discord.Commands;
using System.Threading.Tasks;

namespace FinBot.Modules
{
    public class ConfigCommands : ModuleBase<ShardedCommandContext>
    {
        [Command("prefix"), Summary("Sets the new bot prefix for the current guild"), Remarks("(PREFIX)prefix <new_prefix>")]
        public Task prefix(params string[] arg)
        {
            return Task.CompletedTask;
        }

        [Command("setwelcomechannel"), Summary("Sets the channel where welcome messages for new members/leaving members are sent"), 
            Remarks("(PREFIX)setwelcomechannel <channel>"), Alias("set_welcome_channel", "welcomechannel", "welcome_channel", "welcomemessages", "welcome_messages")]
        public Task setwelcomemessage(params string[] arg)
        {
            return Task.CompletedTask;
        }
    }
}
