using Discord.WebSocket;
using System;
using System.Threading.Tasks;
using FinBot.Modules;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Discord.Commands;
using System.Timers;
using MySql.Data.MySqlClient;
using System.Linq;

namespace FinBot.Services
{
    /*
	 *
	 *I NEED TO DEVELOP THIS LATER ON
	 * 
	 */

    public class MuteService : ModuleBase<ShardedCommandContext>
    {
        public static DiscordShardedClient _client;

        public MuteService(IServiceProvider service)
        {
            _client = service.GetRequiredService<DiscordShardedClient>();
            Timer t = new Timer() { AutoReset = true, Interval = new TimeSpan(0, 0, 0, 10).TotalMilliseconds, Enabled = true };
            t.Enabled = true;
            t.Elapsed += UnmuteAsync;
            t.Start();
        }

        public async void UnmuteAsync(object sender, ElapsedEventArgs e)
        {
            MySqlConnection conn = new MySqlConnection(Global.MySQL.ConnStr);
            MySqlConnection QueryConn = new MySqlConnection(Global.MySQL.ConnStr);

            try
            {
                conn.Open();
                long Now = Global.ConvertToTimestamp(DateTime.Now);
                MySqlCommand cmd1 = new MySqlCommand($"SELECT * FROM Mutes WHERE {Now} > reminderTimestamp", conn);
                MySqlDataReader reader = cmd1.ExecuteReader();

                while (reader.Read())
                {
                    SocketGuild guild = _client.GetGuild(reader.GetUInt64(1));
                    SocketGuildUser user = guild.GetUser(reader.GetUInt64(0));
                    SocketTextChannel channel = guild.GetTextChannel(reader.GetUInt64(2));
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.WithTitle($"{user} has been unmuted");
                    eb.WithDescription($"The user {user} has been unmuted");
                    eb.WithAuthor(user);
                    eb.WithFooter($"Reminder set at {Global.UnixTimeStampToDateTime(reader.GetInt64(3))}");
                    eb.WithCurrentTimestamp();
                    await channel.SendMessageAsync("", false, eb.Build());
                    QueryConn.Open();
                    await InsertToDBAsync(1, QueryConn, user.Id, guild.Id);
                    QueryConn.Close();
                    IRole role = (guild as IGuild).Roles.FirstOrDefault(x => x.Name == "Muted");

                    if (role == null)
                    {
                        return;
                    }

                    if (role.Position > guild.CurrentUser.Hierarchy)
                    {
                        return;
                    }

                    if (user.Roles.Contains(role))
                    {
                        await user.RemoveRoleAsync(role);
                        return;
                    }
                }
                conn.Close();
            }

            catch (Exception ex)
            {
                Global.ConsoleLog(ex.Message);
                //implement later
            }

            finally
            {
                conn.Close();
            }
        }

        public async static Task InsertToDBAsync(uint type, MySqlConnection conn, ulong userId, ulong guildId, long nowTimestamp = 0, long reminderTimestamp = 0, SocketTextChannel chan = null)
        {
            try
            {
                if (type == 0)
                {
                    MySqlCommand cmd = new MySqlCommand($"INSERT INTO Mutes(userId, guildId, chanId, timeSet, reminderTimestamp) VALUES ({userId}, {guildId}, {chan.Id}, {nowTimestamp}, {reminderTimestamp})", conn);
                    cmd.ExecuteNonQuery();
                    cmd = new MySqlCommand($"INSERT INTO MutedUsers(userId, guildId) VALUES ({userId}, {guildId})", conn);
                    cmd.ExecuteNonQuery();
                }

                else if (type == 1)
                {
                    MySqlCommand cmd = new MySqlCommand($"DELETE FROM Mutes where userId = {userId} AND guildId = {guildId}", conn);
                    cmd.ExecuteNonQuery();
                    cmd = new MySqlCommand($"DELETE FROM MutedUsers where userId = {userId} AND guildId = {guildId}", conn);
                    cmd.ExecuteNonQuery();
                }

                else if (type == 3)
                {
                    MySqlCommand cmd = new MySqlCommand($"INSERT INTO MutedUsers(userId, guildId) VALUES ({userId}, {guildId})", conn);
                    cmd.ExecuteNonQuery();
                }

                else
                {
                    MySqlCommand cmd = new MySqlCommand($"DELETE FROM MutedUsers where userId = {userId} AND guildId = {guildId}", conn);
                    cmd.ExecuteNonQuery();
                }
            }

            catch (Exception ex)
            {
                Global.ConsoleLog(ex.Message);
                await chan.SendMessageAsync(ex.Message);
            }
        }

        public async static Task SetMute(SocketGuild guild, SocketUser user, SocketTextChannel chan, DateTime timeSet, string duration, string message, ShardedCommandContext context)
        {
            long currentTime = Global.ConvertToTimestamp(timeSet);
            TimeSpan time = TimeSpan.FromSeconds(Convert.ToInt64(await Parse_time(duration)));
            DateTime remindertime = DateTime.Now + time;
            long reminderTimestamp = Global.ConvertToTimestamp(remindertime);
            MySqlConnection QueryConn = new MySqlConnection(Global.MySQL.ConnStr);

            try
            {
                QueryConn.Open();
                await InsertToDBAsync(0, QueryConn, user.Id, guild.Id, currentTime, reminderTimestamp, chan);
                QueryConn.Close();
            }

            catch (Exception ex)
            {
                //if (ex.Message.GetType() != typeof(NullReferenceException))
                //{
                //    EmbedBuilder eb = new EmbedBuilder();
                //    eb.WithAuthor(user);
                //    eb.WithTitle("Error setting reminder message:");
                //    eb.WithDescription($"The database returned an error code:{ex.Message}\n{ex.Source}\n{ex.StackTrace}\n{ex.TargetSite}");
                //    eb.WithCurrentTimestamp();
                //    eb.WithColor(Color.Red);
                //    eb.WithFooter("Please DM the bot ```support <issue>``` about this error and the developers will look at your ticket");
                //    await chan.SendMessageAsync("", false, eb.Build());
                //    return;
                //}
                Global.ConsoleLog(ex.Message);
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
    }
}
