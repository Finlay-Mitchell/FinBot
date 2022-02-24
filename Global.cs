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
using System.Drawing;
using Color = Discord.Color;
using System.Text.RegularExpressions;

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
        /// Where all failed command results are sent.
        /// </summary>
        public static ulong ErrorLogChannelId { get; set; }
        /// <summary>
        /// The current directory of the bot for the system it's running on.
        /// </summary>
        public static string BotDirectory { get; set; }
        /// <summary>
        /// The API key for the weather API.
        /// </summary>
        public static string WeatherAPIKey { get; set; }
        /// <summary>
        /// This is our Twitch client Id key.
        /// </summary>
        public static string TwitchClientId { get; set; }
        /// <summary>
        /// This is the twitch client secret.
        /// </summary>
        public static string TwitchClientSecret { get; set; }
        /// <summary>
        /// This is the Twitch.tv redirect URL for generating our new access token.
        /// </summary>
        public static string TwitchRedirectURL { get; set; }
        /// <summary>
        /// This is how much the autoslowmode incremennts the slowmode value by.
        /// </summary>
        public static int SlowModeIncrementValue { get; set; }
        /// <summary>
        /// Defines how many messages can be sent on average per second before autoslowmode takes control.
        /// </summary>
        public static int MaxMessagesPerSecond { get; set; }
        /// <summary>
        /// How many Log files will be maintained by the logging system at one time. 
        /// </summary>
        public static int RetainedLogFileCount { get; set; }

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
        /// The path to the logging location for all messages/commands to be logged.
        /// </summary>
        public static string LogPath = $"{Environment.CurrentDirectory}/Data/logs.json";
        /// <summary>
        /// THe directory to the prefixes to load.
        /// </summary>
        public static string PrefixPath = $"{Environment.CurrentDirectory}/Data/guildPrefixes.load";
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
        public static List<string> hiddenCommands = new List<string> { "restart", "terminate", "updatesupport", "tld", "exec", "reset_chatbot", "getguilddata", "enbotclientcommands", "clearalldata", "update", 
            "execute", "test", "twitch", "twitchchannel", "addtwitch", "ftest", "vertest", "afk" };
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
        /// A regular expression to search a string for any form of URI/Ip address.
        /// Designing this regex was painful and yes, I am undergoing therapy.
        /// </summary>
        public static readonly Regex URIAndIpRegex = new Regex(@"(?i)((http|ftp|https|ldap|mailto|dns|dhcp|imap|smtp|tftp|)://)(([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?)|(([1-9]?\d|[12]\d\d)\.){3}([1-9]?\d|[12]\d\d)|(([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-]))[a-zA-Z._]|(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21\x23-\x5b\x5d-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])*)@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\[(?:(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9]))\.){3}(?:(2(5[0-5]|[0-4][0-9])|1[0-9][0-9]|[1-9]?[0-9])|[a-z0-9-]*[a-z0-9]:(?:[\x01-\x08\x0b\x0c\x0e-\x1f\x21-\x5a\x53-\x7f]|\\[\x01-\x09\x0b\x0c\x0e-\x7f])+)\])", RegexOptions.Compiled, TimeSpan.FromMilliseconds(250));
        /// <summary>
        /// Loads a list of recent guild ids and prefixes.
        /// </summary>
        public static List<Dictionary<ulong, string>> demandPrefixes = new List<Dictionary<ulong, string>>();
        /// <summary>
        /// This is the Twitch oauth key.
        /// </summary>
        public static string TwitchOauthKey { get; set; }

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
            BotDirectory = data.BotDirectory;
            WeatherAPIKey = data.WeatherAPIKey;
            TwitchClientId = data.TwitchClientId;
            TwitchClientSecret = data.TwitchClientSecret;
            TwitchRedirectURL = data.TwitchRedirectURL;
            SlowModeIncrementValue = data.SlowModeIncrementValue;
            MaxMessagesPerSecond = data.MaxMessagesPerSecond;
            RetainedLogFileCount = data.RetainedLogFileCount;

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
            public string BotDirectory { get; set; }
            public string WeatherAPIKey { get; set; }
            public string TwitchClientId { get; set; }
            public string TwitchClientSecret { get; set; }
            public string TwitchRedirectURL { get; set; }
            public int SlowModeIncrementValue { get; set; }
            public int MaxMessagesPerSecond { get; set; }
            public int RetainedLogFileCount { get; set; }
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
            return dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        }

        /// <summary>
        /// Checks whether user who called the command is a listed developer.
        /// </summary>
        /// <param name="user">The user who initiated the command.</param>
        /// <returns>A boolean of whether the user is a developer or not.</returns>
        public static bool IsDev(SocketUser user)
        {
            if (clientCommands)
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
            //try
            //{
            //    /*
            //     * This will be commented out until I fix the prefx command calling an embed whilst using cached prefixes
            //     */

            //    foreach (Dictionary<ulong, string> t in Global.demandPrefixes)
            //    {
            //        foreach (KeyValuePair<ulong, string> f in t)
            //        {
            //            if (context.Guild.Id == f.Key)
            //            {
            //                return f.Value;
            //            }
            //        }
            //    }

            //    MongoClient MongoClient = new MongoClient(Mongoconnstr);
            //    IMongoDatabase database = MongoClient.GetDatabase("finlay");
            //    IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
            //    ulong _id = context.Guild.Id;
            //    BsonDocument item = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
            //    string itemVal = item?.GetValue("prefix").ToString();

            //    if (itemVal != null)
            //    {
            //        itemVal = itemVal.Replace("`", "\\`").Replace("__", "\\__").Replace("~~", "\\~~").Replace("```", "\\```");
            //        //guildPrefix = Regex.Replace(guildPrefix, @"`|~~|__|``````|```|\*{2,3}", $"\\");
            //        return itemVal;
            //    }

            //    else
            //    {
            //        return Prefix;
            //    }
            //}

            //catch
            //{
            //    return Prefix;
            //}

            return "dev.";
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
            embed.Color = AutoChooseColour ? ColourPicker() : colour;
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
            string t = File.ReadAllText(LeetsPath);
            Dictionary<string, string> list = new Dictionary<string, string>();

            if (t == string.Empty)
            {
                return list;
            }

            foreach (string i in t.Split("\n"))
            {
                if (i != string.Empty)
                {
                    string[] spl = i.Split(",");
                    list.Add(spl[0], spl[1]);
                }
            }

            return list;
        }

        /// <summary>
        /// Gets the last saved prefixes from the prefix dictionary file.
        /// </summary>
        public static void LoadPrefixes()
        {
            string data = File.ReadAllText(PrefixPath);
            Dictionary<ulong, string> dict = new Dictionary<ulong, string>();

            if (data == string.Empty)
            {
                ConsoleLog("PREFIX DATA WAS NULL");
            }

            foreach (string str in data.Split("\n"))
            {
                if (str != string.Empty)
                {
                    string[] spl = str.Split(",");
                    dict.Add((ulong)Convert.ToUInt64(spl[0]), spl[1].Substring(1));
                }
            }

            demandPrefixes.Add(dict);
            File.WriteAllText(PrefixPath, string.Empty);
        }

        /// <summary>
        /// Saves all prefixes to the dictionary file within the demandPrefixes List.
        /// </summary>
        /// <param name="dict">Dictionary containing the guild id and prefix.</param>
        public static void savePrefixes()
        {
            foreach (Dictionary<ulong, string> dict in demandPrefixes)
            {
                foreach (KeyValuePair<ulong, string> kvp in dict)
                {
                    File.AppendAllText(PrefixPath, $"{kvp.Key}, {kvp.Value}\n");
                }
            }
        }

        /// <summary>
        /// Appends a prefix to the dictionary list.
        /// </summary>
        /// <param name="_id">The guild id.</param>
        /// <param name="prefix">The guild prefix.</param>
        public static void AppendPrefixes(ulong _id, string prefix)
        {
            if (demandPrefixes.Where(dic => dic.ContainsKey(_id)).Any())
            {
                return;
            }

            Dictionary<ulong, string> dict = new Dictionary<ulong, string>();
            dict.Add(_id, prefix);

            if (demandPrefixes.Count > 1000)
            {
                demandPrefixes.RemoveAt(0);
            }

            demandPrefixes.Add(dict);
        }

        /// <summary>
        /// Updates the prefix list to allow prefix updating.
        /// </summary>
        /// <param name="_id">The guild id.</param>
        /// <param name="prefix">The new prefix to set.</param>
        /// <param name="oldPrefix">The old prefix to replace.</param>
        public static void UpdatePrefix(ulong _id, string prefix, string oldPrefix)
        {
            // It's really not pretty, but it'll work for now until I decide to update it.
            Dictionary<ulong, string> dict = new Dictionary<ulong, string>();
            dict.Add(_id, oldPrefix);
            int loc = 0;

            if (dict.Count != 0)
            {
                foreach (Dictionary<ulong, string> t in demandPrefixes)
                {
                    foreach (KeyValuePair<ulong, string> f in t)
                    {
                        if (_id == f.Key)
                        {
                            ConsoleLog($"Reached - [{_id} | {prefix} | {oldPrefix}]");
                            demandPrefixes[loc].Remove(_id);
                            AppendPrefixes(_id, prefix);
                            return;
                        }
                    }

                    loc++;
                }
            }

            else
            {
                ConsoleLog("Not reached");
            }
        }

        /// <summary>
        /// Converts an image to a byte array.
        /// </summary>
        /// <param name="img">The image to get the byte array of.</param>
        /// <returns>A byte array for the image.</returns>
        public static byte[] ImageToByteArray(System.Drawing.Image img)
        {
            ImageConverter _imageConverter = new ImageConverter();
            return (byte[])_imageConverter.ConvertTo(img, typeof(byte[]));
        }

        /// <summary>
        /// Gets the suggestion channel for a guild.
        /// </summary>
        /// <param name="guild"></param>
        /// <returns>a channel Id || 0</returns>
        public static async Task<string> DetermineSuggestionChannel(SocketGuild guild)
        {
            //If no data found, defaults to 0.

            try
            {
                MongoClient MongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = MongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = guild.Id;
                BsonDocument item = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                string itemVal = item?.GetValue("suggestionschannel").ToString();

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

        public static string GenerateRandom(uint size = 16)
        {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            char[] stringChars = new char[size];
            Random random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            return new string(stringChars);
        }
    }
}
