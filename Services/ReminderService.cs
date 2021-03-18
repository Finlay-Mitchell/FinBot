using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace FinBot.Services
{
	public static class ReminderService
	{
		public static async Task RemindAsyncSeconds(SocketUser guild, int time, string msg, SocketUserMessage DiscMessage)
		{
			int convert = (int)TimeSpan.FromSeconds(time).TotalMilliseconds;
			string timenow = TimeNow();
			await Task.Delay(convert);
			EmbedBuilder embed = new EmbedBuilder();
			embed.WithTitle("Reminder");
			embed.WithDescription(msg);
			embed.WithFooter($"Reminder was set at {timenow}", guild.GetAvatarUrl());
			await DiscMessage.Channel.TriggerTypingAsync();
			await DiscMessage.ReplyAsync("", false, embed.Build());
		}

		public static string TimeNow()
		{
			return DateTime.Now.ToString("hh:mm:ss tt");
		}
	}
}
