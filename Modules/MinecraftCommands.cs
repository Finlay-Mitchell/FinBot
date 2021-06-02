using Discord.Commands;
using System.Threading.Tasks;

namespace FinBot.Modules
{
    public class MinecraftCommands : ModuleBase<ShardedCommandContext> //Minecraft python boilerplate code
    {
        [Command("bw_stats"), Summary("Gets the statistics of a players bedwars information"), Remarks("(PREFIX)bw_stats <Minecraft username"), Alias("bedwars_stats", "bedwars-stats", "bedwarsstats", "bwstats", "bw-stats", "bw_info", "bwinfo", "bw-info")]
        public Task bw_stats(params string[] arg)
        {
            return Task.CompletedTask;
        }

        [Command("get_skin"), Summary("Gets the skin of the parsed user"), Remarks("(PREFIX)get_skin <Minecraft username>"), Alias("skin", "mc_skin", "minecraft_skin", "mcskin", "minecraftskin", "mc-skin", "minecraft-skin", "getskin", "get-skinThanks")]
        public Task get_skin(params string[] arg)
        {
            return Task.CompletedTask;
        }

        [Command("bw_compare"), Summary("Compares the two inputted users bedwars statistics"), Remarks("(PREFIX) bw_compare <user1> <user2>"), Alias("bedwars_compare", "bedwarscompare", "bwcompare", "bw-compare", "bedwars-compare")]
        public Task bw_compare(params string[] arg)
        {
            return Task.CompletedTask;
        }
    }
}
