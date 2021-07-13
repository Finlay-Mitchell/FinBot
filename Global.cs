using Discord;
using Discord.WebSocket;
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
        /*
         * Our variables we read from config.json, their values are assigned in ReadConfig().
        */

        /// <summary>
        /// The token for the Discord bot.
        /// </summary>
        public static string Token { get; set; }
        /// <summary>
        /// The default prefix for the bot.
        /// </summary>
        public static string Prefix { get; set; }
        /// <summary>
        /// The current version of the bot.
        /// </summary>
        public static string Version { get; set; }
        /// <summary>
        /// The YouTube API key for YouTube searching.
        /// </summary>
        public static string YouTubeAPIKey { get; set; }
        /// <summary>
        /// The maximum amount of users a given message can ping before it is removed and the sender is warned.
        /// </summary>
        public static uint MaxUserPingCount { get; set; }
        /// <summary>
        /// The maximum amount of roles that can be pinged in a single message before removal & warning of the user.
        /// </summary>
        public static uint MaxRolePingCount { get; set; }
        /// <summary>
        /// The mandatory duration - in seconds - that must pass before a user can gain XP on a message since the last one they sent.
        /// </summary>
        public static uint MinMessageTimestamp { get; set; }
        /// <summary>
        /// The Genius API key used for searching Lyrics to a song/Getting a song via its lyrics from Genius.com
        /// </summary>
        public static string GeniusAPIKey { get; set; }
        /// <summary>
        /// The logging level for console logging of the bot.
        /// </summary>
        public static string LoggingLevel { get; set; }
        /// <summary>
        /// The id of the bots support channel(located inside the support server.
        /// </summary>
        public static ulong SupportChannelId { get; set; }
        /// <summary>
        /// The id for the support server.
        /// </summary>
        public static ulong SupportGuildId { get; set; }
        /// <summary>
        /// This class contains the elements needed to build a MySql connection string: The server name; User; database name; port number; password and the final connection string.
        /// </summary>
        public class MySQL
        {
            public static string MySQLServer { get; set; }
            public static string MySQLUser { get; set; }
            public static string MySQLDatabase { get; set; }
            public static string MySQLPort { get; set; }
            public static string MySQLPassword { get; set; }
            public static string ConnStr { get; set; }
        }

        /// <summary>
        /// The connection string for the MongoDB database.
        /// </summary>
        public static string Mongoconnstr { get; set; }
        /// <summary>
        /// UptimeRobot API key for getting the stauts of the bot.
        /// </summary>
        public static string StatusPageAPIKey { get; set; } //By default this is set to false, to avoid unwanted command execution & privilege abuse. 
        /// <summary>
        /// The custom prefix the bot uses to execute its own commands.
        /// </summary>
        public static string clientPrefix { get; set; }
        /// <summary>
        /// Where all failed command results are sent;
        /// </summary>
        public static ulong ErrorLogChannelId { get; set; }

        /*
         * Our global variables that we do not read from the config.
        */

        /// <summary>
        /// The class listing the json items.
        /// </summary>
        internal static JsonItems CurrentJsonData;
        /// <summary>
        /// The path to the config.json file.
        /// </summary>
        private static readonly string ConfigPath = $"{Environment.CurrentDirectory}/Data/Config.json";
        /// <summary>
        /// The path to the Topics.txt file.
        /// </summary>
        public static string TopicsPath = $"{Environment.CurrentDirectory}/Data/Topics.txt";
        /// <summary>
        /// The path to the LeetRules.txt file - contains a dictionary for rules to assist better chat filtration in automod.
        /// </summary>
        public static string LeetsPath = $"{Environment.CurrentDirectory}/Data/LeetRules.txt";
        /// <summary>
        /// The epoch time set to the first of Jan, 1970. 
        /// </summary>
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        /// <summary>
        /// Tick and cross reaction emotes.
        /// </summary>
        public static List<IEmote> reactions = new List<IEmote>() { new Emoji("✅"), new Emoji("❌") };
        /// <summary>
        /// Commands hidden from regular users - available to developers.
        /// </summary>
        public static List<string> hiddenCommands = new List<string> { "restart", "terminate", "updateSupport", "tld", "exec", "reset_chatbot", "getguilddata", "EnBotClientCommands", "clearalldata" };
        /// <summary>
        /// Listed developer ids.
        /// </summary>
        public static List<ulong> DevUIDs = new List<ulong> { 305797476290527235 };
        /// <summary>
        /// Loads the leet rules from LeetRules.txt
        /// </summary>
        public static Dictionary<string, string> leetRules = LoadLeetRules();
        /// <summary>
        /// Determines whether the bot can run its own commands, toggelable by developers.
        /// </summary>
        public static bool clientCommands { get; set; }
        /// <summary>
        /// The Discord id of the bot user.
        /// </summary>
        public static ulong clientId = 730015197980262424;
        /// <summary>
        /// Command errors which won't print to Discord when called.
        /// </summary>
        public static List<CommandError> ErorrsToIgnore = new List<CommandError> { CommandError.UnknownCommand, CommandError.BadArgCount };

        /// <summary>
        /// This reads data from json.config and assigns the values to the variables labeled above.
        /// </summary>
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
            MinMessageTimestamp = data.MinMessageTimestamp;
            MySQL.MySQLServer = data.MySQLServer;
            MySQL.MySQLUser = data.MySQLUser;
            MySQL.MySQLDatabase = data.MySQLDatabase;
            MySQL.MySQLPort = data.MySQLPort;
            MySQL.MySQLPassword = data.MySQLPassword;
            GeniusAPIKey = data.GeniusAPIKey;
            LoggingLevel = data.LoggingLevel;
            SupportChannelId = data.SupportChannelId;
            SupportGuildId = data.SupportGuildId;
            Mongoconnstr = data.Mongoconnstr;
            StatusPageAPIKey = data.StatusPageAPIKey;
            clientPrefix = data.clientPrefix;
            ErrorLogChannelId = data.ErrorLogChannelId;

            MySQL.ConnStr = $"server={MySQL.MySQLServer};user={MySQL.MySQLUser};database={MySQL.MySQLDatabase};port={MySQL.MySQLPort};password={MySQL.MySQLPassword}"; //The connection string for the MySql server.
        }

        /// <summary>
        /// The items held within config.json
        /// </summary>
        public class JsonItems
        {
            public string Token { get; set; }
            public string Prefix { get; set; }
            public string Version { get; set; }
            public string YouTubeAPIKey { get; set; }
            public uint MaxUserPingCount { get; set; }
            public uint MaxRolePingCount { get; set; }
            public uint MinMessageTimestamp { get; set; }
            public string MySQLServer { get; set; }
            public string MySQLUser { get; set; }
            public string MySQLDatabase { get; set; }
            public string MySQLPort { get; set; }
            public string MySQLPassword { get; set; }
            public string GeniusAPIKey { get; set; }
            public string LoggingLevel { get; set; }
            public ulong SupportChannelId { get; set; }
            public ulong SupportGuildId { get; set; }
            public string Mongoconnstr { get; set; }
            public string StatusPageAPIKey { get; set; }
            public string clientPrefix { get; set; }
            public ulong ErrorLogChannelId { get; set; }
        }

        /// <summary>
        /// Logs a message to the console.
        /// </summary>
        /// <param name="ConsoleMessage">The message to print.</param>
        /// <param name="FColor">The foreground colour.</param>
        /// <param name="BColor">The background colour.</param>
        public static void ConsoleLog(string ConsoleMessage, ConsoleColor FColor = ConsoleColor.Green, ConsoleColor BColor = ConsoleColor.Black)
        {
            Console.ForegroundColor = FColor;
            Console.BackgroundColor = BColor;
            Console.WriteLine("[ - Internal - ] - " + ConsoleMessage);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.BackgroundColor = ConsoleColor.Black;
        }

        /// <summary>
        /// converts datetime into unix timestamp. We convert a type DateTime into a long.
        /// </summary>
        /// <param name="value">The DateTime value of the timestamp we want</param>
        /// <returns>The timestamp as a 'long' type.</returns>
        public static long ConvertToTimestamp(DateTime value)
        {
            TimeSpan elapsedTime = value - Epoch;
            return (long)elapsedTime.TotalSeconds;
        }

        /// <summary>
        /// Unix timestamp is seconds past epoch, we convert a value of double to this.
        /// </summary>
        /// <param name="unixTimeStamp">The unix timestamp of the DateTime we want.</param>
        /// <returns>A DateTime value of the timestamp we parsed in.</returns>
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        /// <summary>
        /// Checks whether user who called the command is a listed developer.
        /// </summary>
        /// <param name="user">The user who initiated the command.</param>
        /// <returns>A boolean of whether the user is a developer or not.</returns>
        public static bool IsDev(SocketUser user)
        {
            if (clientCommands == true)
            {
                return DevUIDs.Contains(user.Id) || user.Id == clientId; //True if the user is a developer OR if clientCommands is manually set to true, if it's the bot calling the command.
            }

            else
            {
                return DevUIDs.Contains(user.Id);
            }
        }

        /// <summary>
        /// Modifies plain text message to 'newMessage'.
        /// </summary>
        /// <param name="baseMessage">The message we want to modify.</param>
        /// <param name="newMessage">The new contents we want to assign.</param>
        public static async Task ModifyMessage(IUserMessage baseMessage, string newMessage)
        {
            await baseMessage.ModifyAsync(x => { x.Content = newMessage; });
        }

        /// <summary>
        /// Modifies embed message to the value of 'embed'.
        /// </summary>
        /// <param name="baseMessage">The message we want to modify.</param>
        /// <param name="embed">The new value we want to set 'baseMssage' too.</param>
        public static async Task ModifyMessage(IUserMessage baseMessage, EmbedBuilder embed)
        {
            await baseMessage.ModifyAsync(x => { x.Embed = embed.Build(); });
        }

        /// <summary>
        /// gets the prefix for the guild in question.
        /// </summary>
        /// <param name="context">The context of the command.</param>
        /// <returns>A string containing the prefix for the guild.</returns>
        public static async Task<string> DeterminePrefix(SocketCommandContext context)
        {
            //Add Dictionary support for first prefix test to reduce time for the top guilds.
            try
            {
                MongoClient MongoClient = new MongoClient(Mongoconnstr);
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

        /// <summary>
        /// Determines whether guild user levelling is enabled or not - if a value is not found or an error occurs, we return "False" to be handled to the user.
        /// </summary>
        /// <param name="guild">The guild that we want to get the leveling channel of.</param>
        /// <returns>A string containing the levelling channel id.</returns>
        public static async Task<string> DetermineLevel(SocketGuild guild)
        {
            try
            {
                MongoClient MongoClient = new MongoClient(Mongoconnstr);
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

        /// <summary>
        /// gets the modlog channel for `guild`. If a value is not found or an error occurs, we return "0" to be handled to the user.
        /// </summary>
        /// <param name="guild">The guild in question.</param>
        /// <returns>A string of the channel Id.</returns>
        public static async Task<string> GetModLogChannel(SocketGuild guild)
        {
            try
            {
                MongoClient mongoClient = new MongoClient(Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = guild.Id;
                BsonDocument item = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                string itemVal = item?.GetValue("modlogchannel").ToString();

                if (itemVal != null)
                {
                    return itemVal;
                }

                else
                {
                    return "0";
                }
            }

            catch
            {
                return "0";
            }
        }

        /// <summary>
        /// This function just generates a very basic embed.
        /// </summary>
        /// <param name="title">Title of the embed.</param>
        /// <param name="msg">the message contents.</param>
        /// <returns>An EmbedBuilder.</returns>
        public static EmbedBuilder EmbedMessage(string title = "", string msg = "", bool AutoChooseColour = false, Color colour = default(Color))
        {
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithTitle(title);
            embed.WithDescription(msg);
            embed.Color = AutoChooseColour ? colour : ColourPicker();
            return embed;
        }

        /// <summary>
        /// Generates a "random" colour from the list.
        /// </summary>
        /// <returns>A colour.</returns>
        public static Color ColourPicker()
        {
            Color[] Colours = {Color.Blue, Color.DarkBlue, Color.DarkerGrey, Color.DarkGreen, Color.DarkGrey, Color.DarkMagenta, Color.DarkOrange, Color.DarkPurple, Color.DarkRed, Color.DarkTeal, Color.Default, Color.Gold, Color.Green,
            Color.LighterGrey, Color.LightGrey, Color.LightOrange, Color.Magenta, Color.Orange, Color.Purple, Color.Red, Color.Teal };
            Random rand = new Random();
            int index = rand.Next(Colours.Length);
            return Colours[index];
        }

        /// <summary>
        /// Loads the keys and values of the leet rules for automod(chat filtration system).
        /// </summary>
        /// <returns>A dictionary containing the keys respective values.</returns>
        public static Dictionary<string, string> LoadLeetRules()
        {
            var t = File.ReadAllText(LeetsPath);
            Dictionary<string, string> list = new Dictionary<string, string>();
            
            if (t == "")
            {
                return list;
            }

            foreach (string i in t.Split("\n"))
            {
                if (i != "")
                {
                    string[] spl = i.Split(",");
                    list.Add(spl[0], spl[1]);
                }
            }

            return list;
        }
    }
}
