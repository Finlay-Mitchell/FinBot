using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using FinBot.Modules;
using Discord;

namespace FinBot.Services
{
    class MuteService
    {
		//public static async Task MuteAsyncSeconds(SocketUser user, SocketGuild guild, TimeSpan time, SocketTextChannel channel)
		//{
		//    var convert = time.TotalMilliseconds;
		//    DateTime timeNow = DateTime.Now;
		//    await Task.Delay((int)convert);
		//    await ModCommands.RemoveMutedRole((IGuildUser)user, guild, channel, timeNow);
		//}

		/*
		 * Add database checks with:
		 *  - 1 check per minute for when a time is up
		 *  - save in database: 
		 *   -- userid
		 *   -- guildid
		 *   -- timestamp
		 *   -- reminder
		 */
	}
}
