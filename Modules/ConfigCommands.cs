using System;
using System.Collections.Generic;
using System.Text;
using Discord;
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
    }
}
