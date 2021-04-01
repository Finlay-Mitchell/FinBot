using Discord;
using Discord.WebSocket;
using FinBot.Handlers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace FinBot
{
    public class Global
    {
        public static string Token { get; set; }
        public static string Prefix { get; set; }
        public static string Version { get; set; }
        public static string YouTubeAPIKey { get; set; }
        public static uint MaxUserPingCount { get; set; }
        public static uint MaxRolePingCount { get; set; }
        public static uint LevelMultiplier { get; set; }
        public static uint MinMessageTimestamp { get; set; }
        public static string GoogleSearchAPIKey { get; set; }
        public static string MySQLServer { get; set; }
        public static string MySQLUser { get; set; }
        public static string MySQLDatabase { get; set; }
        public static string MySQLPort { get; set; }
        public static string MySQLPassword { get; set; }
        public static string GeniusAPIKey { get; set; }
        public static string LoggingLevel { get; set; }



        private static string ConfigPath = $"{Environment.CurrentDirectory}/Data/Config.json";
        public static DiscordShardedClient Client { get; set; }
        public static string WelcomeMessageURL { get; set; }
        internal static JsonItems CurrentJsonData;
        public static string KickMessageURL { get; set; }
        public static string BanMessageURL { get; set; }
        public static string ModLogsPath = $"{Environment.CurrentDirectory}/Data/Logs/Modlogs.db";
        public static string infractionMessagefilepath = $"{Environment.CurrentDirectory}/Data/infractionCards.txt";
        public static string CensoredWordsPath = $"{Environment.CurrentDirectory}/Data/Censored.txt";
        public static string LeetRulesPath = $"{Environment.CurrentDirectory}/Data/LeetRules.txt";
        public static bool AutoSlowmodeEnabled { get; set; }
        public static string TopicsPath = $"{Environment.CurrentDirectory}/Data/Topics.txt";
        public static string muteRoleFilepath = $"{Environment.CurrentDirectory}/Data/Logs/mutelogs.db";
        public static string LevelPath = $"{Environment.CurrentDirectory}/Data/Logs/Levels.db";
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static string SnipeLogs = $"{Environment.CurrentDirectory}/Data/Logs/SnipedMessage.db";
        public static string Polls = $"{Environment.CurrentDirectory}/Data/Logs/Polls.db";
        public static List<IEmote> reactions = new List<IEmote>()
                        {
                            new Emoji("✅"),
                            new Emoji("❌")
                        };
        public static string connStr = $"server={MySQLServer};user={MySQLUser};database={MySQLDatabase};port={MySQLPort};password={MySQLPassword}";
        public static List<string> FileNames = new List<string>() { ModLogsPath, muteRoleFilepath, LevelPath, SnipeLogs, Polls};

        public static void ReadConfig()
        {
            JsonItems data = JsonConvert.DeserializeObject<JsonItems>(File.ReadAllText(ConfigPath));
            CurrentJsonData = data;
            Token = data.Token;
            Prefix = data.Prefix;
            Version = data.Version;
            YouTubeAPIKey = data.YouTubeAPIKey;
            MaxUserPingCount = data.MaxUserPingCount;
            MaxRolePingCount = data.MaxRolePingCount;
            LevelMultiplier = data.LevelMultiplier;
            MinMessageTimestamp = data.MinMessageTimestamp;
            GoogleSearchAPIKey = data.GoogleSearchAPIKey;
            MySQLServer = data.MySQLServer;
            MySQLUser = data.MySQLUser;
            MySQLDatabase = data.MySQLDatabase;
            MySQLPort = data.MySQLPort;
            MySQLPassword = data.MySQLPassword;
            GeniusAPIKey = data.GeniusAPIKey;
            LoggingLevel = data.LoggingLevel;

            //for(int i = 0; i < FileNames.Count; i++)
            //{
            //    if(!File.Exists(FileNames[i]))
            //    {
            //        SQLiteConnection.CreateFile(FileNames[i]);
            //        SQLiteConnection conn = new SQLiteConnection($"data source = {ModLogsPath}");
            //        using var cmd = new SQLiteCommand(conn);
            //        conn.Open();

            //        switch(FileNames[i])
            //        {
            //            case ModLogsPath:
            //                cmd.CommandText = @"CREATE TABLE modlogs(id INTEGER PRIMARY KEY, userId TEXT, action TEXT, moderatorId TEXT, reason TEXT, guildId TEXT, dateTime TEXT, indx INTEGER)";
            //                break;



            //            default:
            //                ConsoleLog("Error with creating file", ConsoleColor.Red);
            //                break;
            //        }

            //        conn.Close();
            //    }
            //}

            if (!File.Exists(ModLogsPath))
            {
                SQLiteConnection.CreateFile($"{ModLogsPath}");
                SQLiteConnection conn = new SQLiteConnection($"data source = {ModLogsPath}");
                using var cmd = new SQLiteCommand(conn);
                conn.Open();
                cmd.CommandText = @"CREATE TABLE modlogs(id INTEGER PRIMARY KEY, userId TEXT, action TEXT, moderatorId TEXT, reason TEXT, guildId TEXT, dateTime TEXT, indx INTEGER)";
                cmd.ExecuteNonQuery();
                conn.Close();
            }

            if (!File.Exists(muteRoleFilepath))
            {
                SQLiteConnection.CreateFile($"{muteRoleFilepath}");
                SQLiteConnection conn = new SQLiteConnection($"data source = {muteRoleFilepath}");
                using var cmd = new SQLiteCommand(conn);
                conn.Open();
                cmd.CommandText = @"CREATE TABLE muteRole(id INTEGER PRIMARY KEY, guildId TEXT, roleId TEXT)";
                cmd.ExecuteNonQuery();
                conn.Close();
            }

            if (!File.Exists(LevelPath))
            {
                SQLiteConnection.CreateFile($"{LevelPath}");
                SQLiteConnection conn = new SQLiteConnection($"data source = {LevelPath}");
                using var cmd = new SQLiteCommand(conn);
                conn.Open();
                cmd.CommandText = @"CREATE TABLE Levels(userId TEXT, guildId TEXT, timestamp INTEGER, level INTEGER, XP INTEGER, totalXP INTEGER)";
                cmd.ExecuteNonQuery();
                conn.Close();
            }

            if(!File.Exists(SnipeLogs))
            {
                SQLiteConnection.CreateFile($"{SnipeLogs}");
                SQLiteConnection conn = new SQLiteConnection($"data source = {SnipeLogs}");
                using var cmd = new SQLiteCommand(conn);
                conn.Open();
                cmd.CommandText = "CREATE TABLE SnipeLogs(message TEXT, timestamp INTEGER, guildId TEXT, author TEXT)";
                cmd.ExecuteNonQuery();
                conn.Close();
            }

            if (!File.Exists(Polls))
            {
                SQLiteConnection.CreateFile($"{Polls}");
                SQLiteConnection conn = new SQLiteConnection($"data source = {Polls}");
                using var cmd = new SQLiteCommand(conn);
                conn.Open();
                cmd.CommandText = "CREATE TABLE Polls(message TEXT, guildId TEXT, author TEXT, state TEXT, chanId TEXT)";
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
        public class JsonItems
        {
            public string Token { get; set; }
            public string Prefix { get; set; }
            public string Version { get; set; }
            public string YouTubeAPIKey { get; set; }
            public uint MaxUserPingCount { get; set; }
            public uint MaxRolePingCount { get; set; }
            public uint LevelMultiplier { get; set; }
            public uint MinMessageTimestamp { get; set; }
            public string GoogleSearchAPIKey { get; set; }
            public string MySQLServer { get; set; }
            public string MySQLUser { get; set; }
            public string MySQLDatabase { get; set; }
            public string MySQLPort { get; set; }
            public string MySQLPassword { get; set; }
            public string GeniusAPIKey { get; set; }
            public string LoggingLevel { get; set; }
        }

        public static void ConsoleLog(string ConsoleMessage, ConsoleColor FColor = ConsoleColor.Green, ConsoleColor BColor = ConsoleColor.Black)
        {
            Console.ForegroundColor = FColor;
            Console.BackgroundColor = BColor;
            Console.WriteLine("[ - Internal - ] - " + ConsoleMessage);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.BackgroundColor = ConsoleColor.Black;
        }

        public static void SaveInfractionMessageCards()
        {
            string s = "";
            foreach (var item in InfractionMessageHandler.CurrentInfractionMessages)
                s += item.Key + "," + item.Value + "\n";
            File.WriteAllText(infractionMessagefilepath, s);
        }

        public static Dictionary<ulong, ulong> LoadInfractionMessageCards()
        {
            var t = File.ReadAllText(infractionMessagefilepath);
            Dictionary<ulong, ulong> ulist = new Dictionary<ulong, ulong>();
            if (t == "")
                return ulist;
            foreach (var i in t.Split("\n"))
            {
                if (i != "")
                {
                    var spl = i.Split(",");
                    ulist.Add(ulong.Parse(spl[0]), ulong.Parse(spl[1]));
                }
            }

            return ulist;
        }

        public static void SaveCenssor(string word)
        {
            File.AppendAllText(CensoredWordsPath, $"{word}\n");
        }

        public static void SaveLeetRules(string leet, string value)
        {
            File.AppendAllText(LeetRulesPath, $"{leet}, {value}\n");
        }

        public static Dictionary<string, string> LoadLeetRules()
        {
            var t = File.ReadAllText(LeetRulesPath);
            Dictionary<string, string> list = new Dictionary<string, string>();
            if (t == "")
                return list;
            foreach (var i in t.Split("\n"))
            {
                if (i != "")
                {
                    var spl = i.Split(",");
                    list.Add(spl[0], spl[1]);
                }
            }

            return list;
        }

        public static void RemoveCensor(string word)
        {
            string[] oldLines = File.ReadAllLines(CensoredWordsPath);
            IEnumerable<string> newLines = oldLines.Where(line => !line.Contains(word));
            File.WriteAllLines(CensoredWordsPath, newLines);
        }

        public static long ConvertToTimestamp(DateTime value)
        {
            TimeSpan elapsedTime = value - Epoch;
            return (long)elapsedTime.TotalSeconds;
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    }
}
