using Discord.WebSocket;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FinBot.Handlers
{
    public class TwitchHandler
    {
        public class TwitchData
        {
            public string id { get; set; }
            public string login { get; set; }
            public string display_name { get; set; }
            public string type { get; set; }
            public string broadcaster_type { get; set; }
            public string description { get; set; }
            public string profile_image_url { get; set; }
            public string offline_image_url { get; set; }
            public long view_count { get; set; }
            public DateTime created_at { get; set; }
        }

        public class Category
        {
            public string id { get; set; }
            public string name { get; set; }
        }

        public class Segment
        {
            public string id { get; set; }
            public DateTime start_time { get; set; }
            public DateTime end_time { get; set; }
            public string title { get; set; }
            public object canceled_until { get; set; }
            public Category category { get; set; }
            public bool is_recurring { get; set; }
        }

        public class StreamSchedule
        {
            public List<Segment> segments { get; set; }
            public string broadcaster_id { get; set; }
            public string broadcaster_name { get; set; }
            public string broadcaster_login { get; set; }
            public object vacation { get; set; }
        }

        public class UserStreams
        {
            public string id { get; set; }
            public string user_id { get; set; }
            public string user_login { get; set; }
            public string user_name { get; set; }
            public string game_id { get; set; }
            public string game_name { get; set; }
            public string type { get; set; }
            public string title { get; set; }
            public int viewer_count { get; set; }
            public DateTime started_at { get; set; }
            public string language { get; set; }
            public string thumbnail_url { get; set; }
            public List<string> tag_ids { get; set; }
            public bool is_mature { get; set; }
        }

        public class TwitchUserInfo
        {
            public List<TwitchData> data { get; set; }
        }

        public class TwitchStreamInfo
        {
            public StreamSchedule Twitchdata { get; set; }
            public List<UserStreams> data { get; set; }
        }

        public static async Task<List<TwitchData>> GetTwitchInfo(string username)
        {
            HttpClient HTTPClient = new HttpClient();
            HTTPClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Global.TwitchOauthKey}");
            HTTPClient.DefaultRequestHeaders.Add("Client-Id", $"{Global.TwitchClientId}");
            HttpResponseMessage HTTPResponse = await HTTPClient.GetAsync($"https://api.twitch.tv/helix/users?login={username}");
            string resp = await HTTPResponse.Content.ReadAsStringAsync();
            TwitchUserInfo myDeserializedClass = JsonConvert.DeserializeObject<TwitchUserInfo>(resp);
            return myDeserializedClass.data;
        }

        public static async Task<StreamSchedule> GetStreamSchedule(string userId)
        {
            HttpClient HTTPClient = new HttpClient();
            HTTPClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Global.TwitchOauthKey}");
            HTTPClient.DefaultRequestHeaders.Add("Client-Id", $"{Global.TwitchClientId}");
            HttpResponseMessage HTTPResponse = await HTTPClient.GetAsync($"https://api.twitch.tv/helix/schedule?broadcaster_id={userId}");
            string resp = await HTTPResponse.Content.ReadAsStringAsync();
            TwitchStreamInfo myDeserializedClass = JsonConvert.DeserializeObject<TwitchStreamInfo>(resp);
            return myDeserializedClass.Twitchdata;
        }

        public static async Task<List<UserStreams>> GetStreams(string username)
        {
            HttpClient HTTPClient = new HttpClient();
            HTTPClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Global.TwitchOauthKey}");
            HTTPClient.DefaultRequestHeaders.Add("Client-Id", $"{Global.TwitchClientId}");
            HttpResponseMessage HTTPResponse = await HTTPClient.GetAsync($"https://api.twitch.tv/helix/streams?user_login={username}");
            string resp = await HTTPResponse.Content.ReadAsStringAsync();
            TwitchStreamInfo myDeserializedClass = JsonConvert.DeserializeObject<TwitchStreamInfo>(resp);
            return myDeserializedClass.data;
        }

        public static async Task<List<UserStreams>> GetStreams()
        {
            HttpClient HTTPClient = new HttpClient();
            HTTPClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Global.TwitchOauthKey}");
            HTTPClient.DefaultRequestHeaders.Add("Client-Id", $"{Global.TwitchClientId}");
            HttpResponseMessage HTTPResponse = await HTTPClient.GetAsync($"https://api.twitch.tv/helix/streams");
            string resp = await HTTPResponse.Content.ReadAsStringAsync();
            TwitchStreamInfo myDeserializedClass = JsonConvert.DeserializeObject<TwitchStreamInfo>(resp);
            return myDeserializedClass.data;
        }

        public static async Task GetAccessToken()
        {
            HttpClient HTTPClient = new HttpClient();
            HttpResponseMessage HTTPResponse = await HTTPClient.PostAsync(new Uri($"https://id.twitch.tv/oauth2/token?client_id={Global.TwitchClientId}&client_secret={Global.TwitchClientSecret}&grant_type=client_credentials"), new StringContent(""));
            HTTPResponse.EnsureSuccessStatusCode();
            string body = await HTTPResponse.Content.ReadAsStringAsync();
            TwitchAPIData APIData = JsonConvert.DeserializeObject<TwitchAPIData>(body);
            Global.TwitchOauthKey = APIData.access_token;
        }

        public class TwitchAPIData
        {
            public string access_token { get; set; }
            public string expires_in { get; set; }
            public string token_type { get; set; }
        }

        public static async Task<string> GetTwitchChannel(SocketGuild guild)
        {
            try
            {
                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = guild.Id;
                BsonDocument item = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
                string itemVal = item?.GetValue("TwitchChannel").ToString();

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
    }
}
