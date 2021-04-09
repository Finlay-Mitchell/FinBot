using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace FinBot.Services
{
	public class ReminderService
	{
        public static DiscordShardedClient _client;
        Timer T;

		public ReminderService(IServiceProvider service)
		{
			_client = service.GetRequiredService<DiscordShardedClient>();
            T = new Timer() { AutoReset = true, Interval = new TimeSpan(0, 0, 0, 20).TotalMilliseconds, Enabled = true };
            T.Elapsed += RemindAsync;
        }

        public async void RemindAsync(object sender, ElapsedEventArgs e)
        {

            MySqlConnection conn = new MySqlConnection(Global.MySQL.connStr);
            MySqlConnection QueryConn = new MySqlConnection(Global.MySQL.connStr);

            try
            {
                conn.Open();
                QueryConn.Open();
                long Now = Global.ConvertToTimestamp(DateTimeOffset.Now.UtcDateTime);
                MySqlCommand cmd1 = new MySqlCommand($"SELECT * FROM Reminders WHERE {Now} >= reminderTimestamp", conn);
                MySqlDataReader reader = (MySqlDataReader)await cmd1.ExecuteReaderAsync();

                while (reader.Read())
                {
                    SocketGuild guild = _client.GetGuild((ulong)reader.GetInt64(1));
                    SocketUser user = guild.GetUser((ulong)reader.GetUInt64(0));
                    SocketTextChannel channel = guild.GetTextChannel(reader.GetUInt64(2));
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.WithTitle("Reminder");
                    eb.WithDescription($"{reader.GetString(5)}");
                    eb.WithAuthor(user);
                    eb.WithFooter($"Reminder set at {Global.UnixTimeStampToDateTime(reader.GetInt64(3))}");
                    eb.WithCurrentTimestamp();
                    await channel.SendMessageAsync("", false, eb.Build());
                    MySqlCommand cmd2 = new MySqlCommand($"DELETE FROM Reminders where userId = {user.Id}", QueryConn);
                    cmd2.ExecuteNonQuery();
                }

                QueryConn.Close();
                conn.Close();
            }

            catch (Exception ex)
            {
                //implement later
            }
        }

        public static async Task setReminder(SocketGuild guild, SocketUser user, SocketTextChannel chan, DateTime timeSet, string duration, string message)
        {
            timeSet = DateTime.Now;
            long currentTime = Global.ConvertToTimestamp(timeSet);
            DateTime f = DateTime.ParseExact(TimeSpan.FromSeconds(Convert.ToInt64(Parse_time(duration).Result)).TotalSeconds.ToString(), "ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None);

            long reminderTimestamp = Global.ConvertToTimestamp(f);
            MySqlConnection conn = new MySqlConnection(Global.MySQL.connStr);
            MySqlConnection QueryConn = new MySqlConnection(Global.MySQL.connStr);

            try
            {
                QueryConn.Open();
                conn.Open();
                bool read = false;
                MySqlCommand cmd1 = new MySqlCommand($"SELECT * FROM Reminders WHERE userId = {user.Id} AND guildId = {guild.Id}", conn);
                MySqlDataReader reader = (MySqlDataReader)await cmd1.ExecuteReaderAsync();


                while (reader.Read())
                {
                    read = true;
                    await chan.SendMessageAsync($"You already have a timer active. Please try again after this has expired or stop the timer by using the {Global.Prefix}stopreminder command.");
                }
                
                if (!read)
                {
                    MySqlCommand cmd2 = new MySqlCommand($"INSERT INTO Reminders(userId, guildId, chanId, timeSet, reminderTimestamp, message) VALUES ({user.Id}, {guild.Id}, {chan.Id}, {currentTime}, {reminderTimestamp}, '{message}')", QueryConn);
                    cmd2.ExecuteNonQuery();
                    await chan.SendMessageAsync($"Set a reminder for {message} for {duration}");
                    
                    
                    chan.SendMessageAsync(reminderTimestamp.ToString());
                }

                conn.Close();
                QueryConn.Close();
            }

            catch (Exception ex)
            {
                if (ex.Message.GetType() != typeof(NullReferenceException))
                {
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.WithAuthor(user);
                    eb.WithTitle("Error setting reminder message:");
                    eb.WithDescription($"The database returned an error code:{ex.Message}\n{ex.Source}\n{ex.StackTrace}\n{ex.TargetSite}");
                    eb.WithCurrentTimestamp();
                    eb.WithColor(Color.Red);
                    eb.WithFooter("Please DM the bot ```support <issue>``` about this error and the developers will look at your ticket");
                    await chan.SendMessageAsync("", false, eb.Build());
                    return;
                }
            }
        }


        public static Task<string> Parse_time(string time)
        {
            time = "time " + time;
            float result = 0.0f;
            var len = time.Length;
            for (var i = len - 1; i > 0; i--)
            {
                float _base;

                switch (time[i])
                {
                    case 's':
                        _base = 1.0f;
                        break;
                    case 'm':
                        _base = 60.0f;
                        break;
                    case 'h':
                        _base = 60.0f * 60.0f;
                        break;
                    case 'd':
                        _base = 60.0f * 60.0f * 24;
                        break;
                    default:
                        continue;
                }

                float exponent = 1.0f;

                for (var j = 1; j <= i + 1; j++)
                {
                    if (char.IsDigit((time[i - j])))
                    {
                        result += (time[i - j] - '0') * _base * exponent;
                        exponent *= 10.0f;
                    }

                    else
                    {
                        break;
                    }
                }
            }

            if (result > 0.0f)
            {
                return Task.FromResult(result.ToString());
            }

            else
            {
                return Task.FromResult(time);
            }
        }


        /*
		 * Add database checks with:
		 *  - 1 check per minute for when a time is up
		 *  - save in database: 
		 *   -- userid
		 *   -- guildid
		 *   -- roleid
		 *   -- timestamp
		 *   -- reason for mute
		 *   -- user who muted them
		 */
    }
}
