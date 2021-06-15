using Discord;
using Discord.Commands;
using DiscColour = Discord.Color;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Timers;
using Discord.Audio;
using VideoLibrary;
using MediaToolkit.Model;
using MediaToolkit;
using System.Drawing;
using System.Drawing.Imaging;

using Colour = System.Drawing.Color;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using FinBot.Handlers;
using Color = Discord.Color;
using Newtonsoft.Json;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;

namespace FinBot.Modules
{
    public class DevCommands : ModuleBase<ShardedCommandContext> //Dev commands hidden from regular users
    {
        private readonly DiscordShardedClient _client;

        public DevCommands(IServiceProvider service)
        {
            _client = service.GetRequiredService<DiscordShardedClient>();
        }

        //[Command("testing")]
        //public async Task testing()
        //{
        //    string rand = "";
        //    using (var rng = new RNGCryptoServiceProvider())
        //    {
        //        int bit_count = (16 * 6);
        //        int byte_count = ((bit_count + 7) / 8); // rounded up
        //        byte[] bytes = new byte[byte_count];
        //        rng.GetBytes(bytes);
        //        rand = Convert.ToBase64String(bytes);
        //    }

        //    rand = Regex.Replace(rand, "[\"/]", "");
        //    rand = Regex.Replace(rand, @"[\\]", "");
        //    await ReplyAsync(rand);
        //    //2000, 510
        //    //Bitmap bitmap = new Bitmap(2000, 1000, PixelFormat.Format32bppPArgb);
        //    Bitmap bitmap = new Bitmap(1050, 550);
        //    Graphics graphics = Graphics.FromImage(bitmap);
        //    //Pen pen = new Pen(Colour.FromKnownColor(KnownColor.Blue), 2);
        //    Pen pen = new Pen(Colour.FromArgb(255, 0, 0, 0), 5);
        //    //1000, 500
        //    graphics.DrawRectangle(pen, 10, 10, 750, 500);


        //    //Pen pen1 = new Pen(Colour.FromKnownColor(KnownColor.Red), 2);
        //    //graphics.DrawEllipse(pen1, 10, 10, 900, 700);
        //    string file = $"image_{rand}.png";
        //    bitmap.Save(file);
        //    await Context.Channel.SendFileAsync(file);
        //    File.Delete(file);
        //}

        [Command("restart")]
        public async Task Reset([Remainder] string reason = "No reason provided.")
        {
            if (Global.IsDev(Context.User))
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync($"Restarting bot with reason \"{reason}\"\n");
                Process.Start($"{AppDomain.CurrentDomain.FriendlyName}.exe");
                Environment.Exit(1);
            }
        }

        [Command("terminate")]
        public async Task Term()
        {
            if (Global.IsDev(Context.User))
            {
                await Context.Message.ReplyAsync($"Shutting down services.");
                Environment.Exit(1);
            }
        }

        [Command("updateSupport")]
        public async Task UpdateSupport(ulong guildId, ulong msgId, [Remainder] string msg)
        {
            if (Global.IsDev(Context.User))
            {
                try
                {
                    SocketDMChannel chn = (SocketDMChannel)await _client.GetDMChannelAsync(guildId);
                    EmbedBuilder eb = new EmbedBuilder();
                    eb.WithTitle("Support ticket update");
                    eb.WithFooter($"Support ticket update for {msgId}");
                    eb.WithCurrentTimestamp();
                    eb.WithDescription(msg);
                    eb.WithColor(DiscColour.Purple);
                    await chn.SendMessageAsync("", false, eb.Build());
                    await Context.Message.ReplyAsync("Sent support message response successfully");
                }

                catch (Exception ex)
                {
                    await Context.Message.ReplyAsync($"I encountered an error trying to respond to that support message. Here are the details:\n{ex.Message}\n{ex.Source}");
                }
            }
        }

        [Command("tld")] //boilerplate code for python TLD module
        public Task Tld(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("exec")] //more boilerplate
        public Task Exec(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("reset_chatbot")]
        public Task ResetChatbot(params string[] arg)
        {
            return Task.CompletedTask;
        }

        //MongoClient MongoClient = new MongoClient(Global.Mongoconnstr);
        //IMongoDatabase database = MongoClient.GetDatabase("finlay");
        //IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
        //ulong _id = Context.Guild.Id;
        //await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", _id), Builders<BsonDocument>.Update.Set("prefix", $"{new_prefix}"));

        //public void test()
        //{
        //    MongoClient MongoClient = new MongoClient(Global.Mongoconnstr);
        //    IMongoDatabase database = MongoClient.GetDatabase("finlay");
        //    IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
        //    //Builders<BsonDocument>.Filter.Eq("_id", );


        //public async Task MesasageTest(SocketMessage msg)
        //{
        //    try
        //    {

        //        if (msg.Author.IsBot)
        //        {
        //            return;
        //        }

        //        MongoClient MongoClient = new MongoClient(Global.Mongoconnstr);
        //        IMongoDatabase database = MongoClient.GetDatabase("finlay");
        //        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
        //        SocketGuildChannel chan = msg.Channel as SocketGuildChannel;
        //        ulong _id = chan.Guild.Id;
        //        BsonDocument item = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", _id)).FirstOrDefaultAsync();
        //        string itemVal = item?.GetValue("blacklistedterms").ToJson();

        //        if (itemVal != null)
        //        {
        //            List<string> stringArray = JsonConvert.DeserializeObject<string[]>(itemVal).ToList();
        //            Regex re = new Regex(@"\b(" + string.Join("|", stringArray.Select(word => string.Join(@"\s*", word.ToCharArray()))) + @")\b", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        //            if(re.IsMatch(msg.Content))
        //            {
        //                await msg.Channel.SendMessageAsync($"Nah, can't say dat");
        //            }

        //        }

        //        else
        //        {
        //            await msg.Channel.SendMessageAsync(itemVal.ToString());
        //        }
        //    }

        //    catch(Exception ex)
        //    {
        //        //await msg.Channel.SendMessageAsync($"broke: {ex.Message}\n\n{ex.StackTrace}");
        //    }

        //}

        [Command("getguilddata")]
        public async Task Getguilddata()
        {
            if (Global.IsDev(Context.User))
            {
                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                BsonDocument data = await MongoHandler.FindById(collection, _id);
                await ReplyAsync(data.ToString());
            }
        }

        //[Command("clearalldata")]
        //public async Task Clearalldata()
        //{
        //    if (Global.IsDev(Context.User))
        //    {
        //        MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
        //        IMongoDatabase database = mongoClient.GetDatabase("finlay");
        //        IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
        //        ulong _id = Context.Guild.Id;
        //        collection.DeleteOne(Builders<BsonDocument>.Filter.Eq("id", _id));
        //        BsonDocument data = await MongoHandler.FindById(collection, _id);
        //        await ReplyAsync(data.ToString());
        //    }
        //}
    }
}