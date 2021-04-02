using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace FinBot.Handlers.AutoMod
{
    public class LevellingHandler
    {
        DiscordSocketClient _client;

        public LevellingHandler(DiscordSocketClient client)
        {
            _client = client;
            //_client.MessageReceived += AddToDB;
        }

        //public async Task AddToDB(SocketMessage arg)
        //{
        //    if (arg.Author.IsBot || arg.Channel.GetType() == typeof(SocketDMChannel))
        //    {
        //        return;
        //    }

        //    SocketGuildChannel chan = arg.Channel as SocketGuildChannel;
        //    long Now = Global.ConvertToTimestamp(arg.Timestamp.UtcDateTime);
        //    SQLiteConnection conn = new SQLiteConnection($"data source = {Global.LevelPath}");
        //    using SQLiteCommand cmd2 = new SQLiteCommand(conn);
        //    conn.Open();
        //    cmd2.CommandText = $"SELECT * FROM Levels WHERE userId = '{arg.Author.Id}' AND guildId = '{chan.Guild.Id}'";
        //    using SQLiteDataReader reader = cmd2.ExecuteReader();
        //    long TimeStamp = 0;
        //    long XP = 0;
        //    long level = 0;
        //    bool ran = false;
        //    long xpToNextLevel = 0;
        //    long totalXP = 0;

        //    while (reader.Read())
        //    {
        //        ran = true;
        //        TimeStamp = Now - reader.GetInt64(2);
                
        //        if (TimeStamp >= Global.MinMessageTimestamp)
        //        {
        //            XP = reader.GetInt64(4);
        //            level = reader.GetInt64(3);
        //            Random r = new Random();
        //            XP += r.Next(15, 25);
        //            totalXP =+ XP;
        //            xpToNextLevel = (long)(5 * Math.Pow(level, 2) + 50 * level + 100);
        //            using SQLiteCommand cmd1 = new SQLiteCommand(conn);

        //            if (XP >= xpToNextLevel)
        //            {
        //                level += 1;
        //                XP = XP - xpToNextLevel;
        //                await arg.Channel.SendMessageAsync($"Congratulations, {arg.Author.Mention} for reaching level {level}!");
        //            }

        //            cmd1.CommandText = $"UPDATE Levels SET timestamp = {Now}, level = {level}, XP = {XP}, totalXP = {totalXP} WHERE guildId = '{chan.Guild.Id}' AND userId = '{arg.Author.Id}'";
        //            cmd1.ExecuteNonQuery();
        //        }

        //        else
        //        {
        //            return;
        //        }
        //    }

        //    if (!ran)
        //    {
        //        Random r = new Random();
        //        totalXP = +r.Next(15, 25);
        //        using SQLiteCommand cmd3 = new SQLiteCommand(conn);
        //        cmd3.CommandText = $"INSERT INTO Levels(userId, guildId, timestamp, level, XP, totalXP) VALUES({arg.Author.Id}, {chan.Guild.Id}, {Now}, 0, {XP}, {totalXP})";
        //        cmd3.ExecuteNonQuery();
        //    }

        //    conn.Close();
        //}
    }
}
