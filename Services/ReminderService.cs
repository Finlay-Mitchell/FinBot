using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace FinBot.Services
{
	public static class ReminderService
	{
		public static async Task RemindAsyncSeconds(SocketUser guild, int time, string msg, ISocketMessageChannel chan)
		{
			int convert = (int)TimeSpan.FromSeconds(time).TotalMilliseconds;
			string timenow = TimeNow();
			await Task.Delay(convert);
			EmbedBuilder embed = new EmbedBuilder();
			embed.WithTitle("Reminder");
			embed.WithDescription(msg);
			embed.WithFooter($"Reminder was set at {timenow}", guild.GetAvatarUrl());
			await chan.TriggerTypingAsync();
			await chan.SendMessageAsync("", false, embed.Build());
		}

		public static string TimeNow()
		{
			return DateTime.Now.ToString("hh:mm:ss tt");
		}
	}
}
