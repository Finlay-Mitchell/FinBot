using Discord;
using Discord.WebSocket;
using FinBot.Handlers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Discord.Commands;
using MongoDB.Bson;

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
        public static string GeniusAPIKey { get; set; }
        public static string LoggingLevel { get; set; }
        public static string Pythoninterpreter { get; set; }
        public static ulong SupportChannelId { get; set; }
        public static ulong SupportGuildId { get; set; }

        public class MySQL
        {
            public static string MySQLServer { get; set; }
            public static string MySQLUser { get; set; }
            public static string MySQLDatabase { get; set; }
            public static string MySQLPort { get; set; }
            public static string MySQLPassword { get; set; }
            public static string connStr { get; set; }
        }
        
        public static string mongoconnstr { get; set; }
        public static string StatusPageAPIKey { get; set; }
        

        private static string ConfigPath = $"{Environment.CurrentDirectory}/Data/Config.json";
        public static string WelcomeMessageURL { get; set; }
        internal static JsonItems CurrentJsonData;
        public static string KickMessageURL { get; set; }
        public static string BanMessageURL { get; set; }
        public static string TopicsPath = $"{Environment.CurrentDirectory}/Data/Topics.txt";
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static List<IEmote> reactions = new List<IEmote>() { new Emoji("✅"), new Emoji("❌") };
        public static List<string> hiddenCommands = new List<string> { "restart", "terminate", "updateSupport", "tld", "exec" }; // These are hidden from being shown in the help command in HelpHandler.cs
        public static List<ulong> DevUIDs = new List<ulong> { 305797476290527235, 230778630597246983 }; // Listed developer Ids

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
            MySQL.MySQLServer = data.MySQLServer;
            MySQL.MySQLUser = data.MySQLUser;
            MySQL.MySQLDatabase = data.MySQLDatabase;
            MySQL.MySQLPort = data.MySQLPort;
            MySQL.MySQLPassword = data.MySQLPassword;
            GeniusAPIKey = data.GeniusAPIKey;
            LoggingLevel = data.LoggingLevel;
            Pythoninterpreter = data.Pythoninterpreter;
            SupportChannelId = data.SupportChannelId;
            SupportGuildId = data.SupportGuildId;
            mongoconnstr = data.mongoconnstr;
            StatusPageAPIKey = data.StatusPageAPIKey;

            MySQL.connStr = $"server={MySQL.MySQLServer};user={MySQL.MySQLUser};database={MySQL.MySQLDatabase};port={MySQL.MySQLPort};password={MySQL.MySQLPassword}";
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
            public string Pythoninterpreter { get; set; }
            public ulong SupportChannelId { get; set; }
            public ulong SupportGuildId { get; set; }
            public string mongoconnstr { get; set; }
            public string StatusPageAPIKey { get; set; }
        }

        public static void ConsoleLog(string ConsoleMessage, ConsoleColor FColor = ConsoleColor.Green, ConsoleColor BColor = ConsoleColor.Black)
        {
            Console.ForegroundColor = FColor;
            Console.BackgroundColor = BColor;
            Console.WriteLine("[ - Internal - ] - " + ConsoleMessage);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.BackgroundColor = ConsoleColor.Black;
        }

        public static long ConvertToTimestamp(DateTime value)
        {
            //converts datetime into unix timestamp.
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

        public static bool IsDev(SocketUser user)
        {
            //checks if user is a listed bot developer
            return DevUIDs.Contains(user.Id);
        }

        public static async Task ModifyMessage(IUserMessage baseMessage, string newMessage)
        {
            //Modifies plain text message
            await baseMessage.ModifyAsync(x => { x.Content = newMessage; });
        }

        public static async Task ModifyMessage(IUserMessage baseMessage, EmbedBuilder embed)
        {
            //Modifies embed message
            await baseMessage.ModifyAsync(x => { x.Embed = embed.Build(); });
        }

        public static async Task<string> DeterminePrefix(SocketCommandContext context)
        {
            //gets the prefix for the guild in question - add Dictionary support for first prefix test.
            try
            {
                MongoClient MongoClient = new MongoClient(mongoconnstr);
                IMongoDatabase database = MongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = context.Guild.Id;
                BsonDocument item = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                string itemVal = item?.GetValue("prefix").ToString();

                if (itemVal != null)
                {
                    return itemVal;
                }

                else
                {
                    return Prefix;
                }
            }

            catch
            {
                return Prefix;
            }
        }

        public static async Task<string> DetermineLevel(SocketGuild guild)
        {
            // Determines whether guild user levelling is enabled or not - also add dictionary soon.
            try
            {
                MongoClient MongoClient = new MongoClient(mongoconnstr);
                IMongoDatabase database = MongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = guild.Id;
                BsonDocument item = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                string itemVal = item?.GetValue("levelling").ToString();

                if (itemVal != null)
                {
                    return itemVal;
                }

                else
                {
                    return "False";
                }
            }

            catch
            {
                return "False";
            }
        }
    }
}
