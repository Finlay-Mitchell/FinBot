using Discord;
using Discord.Commands;
using DiscColour = Discord.Color;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using FinBot.Handlers;
using System.Collections.Generic;
using FinBot.Services;
using Discord.Rest;
using System.Security.Cryptography;
using System.IO;
using System.Drawing;
using System.Net;
using System.Drawing.Drawing2D;
using Color = Discord.Color;

namespace FinBot.Modules
{
    public class DevCommands : ModuleBase<ShardedCommandContext> //Dev commands hidden from regular users
    {
        public DiscordShardedClient _client;
        public IServiceProvider _services;

        public DevCommands(IServiceProvider services)
        {
            try
            {
                _client = services.GetRequiredService<DiscordShardedClient>();
                _services = services;
            }

            catch (Exception ex)
            {
                Global.ConsoleLog(ex.Message);
            }
        }

        //[Command("testing")]
        //public async Task testing()
        //{
        //    if (Global.IsDev(Context.User))
        //    {
        //        //string rand = "";
        //        //using (var rng = new RNGCryptoServiceProvider())
        //        //{
        //        //    int bit_count = (16 * 6);
        //        //    int byte_count = ((bit_count + 7) / 8); // rounded up
        //        //    byte[] bytes = new byte[byte_count];
        //        //    rng.GetBytes(bytes);
        //        //    rand = Convert.ToBase64String(bytes);
        //        //}

        //        //rand = Regex.Replace(rand, "[\"/]", "");
        //        //rand = Regex.Replace(rand, @"[\\]", "");
        //        //await ReplyAsync(rand);
        //        ////2000, 510
        //        ////Bitmap bitmap = new Bitmap(2000, 1000, PixelFormat.Format32bppPArgb);
        //        //Bitmap bitmap = new Bitmap(1050, 550);
        //        //Graphics graphics = Graphics.FromImage(bitmap);
        //        ////Pen pen = new Pen(Colour.FromKnownColor(KnownColor.Blue), 2);
        //        //Pen pen = new Pen(System.Drawing.Color.FromArgb(255, 0, 0, 0), 5);
        //        ////1000, 500
        //        //graphics.DrawRectangle(pen, 10, 10, 750, 500);


        //        ////Pen pen1 = new Pen(Colour.FromKnownColor(KnownColor.Red), 2);
        //        ////graphics.DrawEllipse(pen1, 10, 10, 900, 700);
        //        //string file = $"image_{rand}.png";
        //        //bitmap.Save(file);
        //        //await Context.Channel.SendFileAsync(file);
        //        //File.Delete(file);


        //        //var avtr = Context.User.GetAvatarUrl();
        //        //WebClient wc = new WebClient();
        //        //byte[] bytes = wc.DownloadData(avtr);
        //        //MemoryStream ms = new MemoryStream(bytes);
        //        //System.Drawing.Image pfp = System.Drawing.Image.FromStream(ms);
        //        //Bitmap baseimg = new Bitmap($"{Environment.CurrentDirectory}/Data/Capture2.PNG"); // DO CHANGE
        //        //Graphics canv = Graphics.FromImage(baseimg);
        //        //float radius = 4; // ADJUST
        //        //var rpfp = ClipToCircle(pfp, new PointF(pfp.Width / 2, pfp.Height / 2), radius, System.Drawing.Color.FromArgb(0, 0, 0, 0));
        //        //canv.DrawImage(rpfp, 25, baseimg.Height / 2 - rpfp.Height / 2/*, baseimg.Width / 2 - rpfp.Width / 2*/); // very much guessed
        //        //var g = RoundedRect(new Rectangle(325, baseimg.Height / 2, baseimg.Width, baseimg.Height), (int)radius); //another guess
        //        //var mxWidth = (int)(baseimg.Width * 0.6) - 30;
        //        //var prc = (double)10 / (double)100; // REPLACE WITH CURRENT XP AND NEXT XP
        //        //var fnl = mxWidth * prc;
        //        //var prg = RoundedRect(new Rectangle(325, (baseimg.Height / 2), (int)fnl, 75), 36);
        //        //canv.SmoothingMode = SmoothingMode.AntiAlias;
        //        //canv.FillPath(Brushes.Black, g);
        //        //canv.FillPath(new SolidBrush(Color.Purple), prg); //CHANGE COLOUR TO BETTER LOOKING IF NEED BE
        //        //canv.DrawString("username", new Font("Arial", 50), new SolidBrush(Color.Purple), new PointF(325, 32));

        //        //canv.Save();

        //        //Bitmap bmp = new Bitmap(100, 100, canv);

        //        //System.Drawing.Image img = bmp;
        //        //MemoryStream ms2 = new MemoryStream(ImageToByteArray(img));
        //        //await Context.Channel.SendFileAsync(ms, $"test-testing.png");
        //    }
        //}

        //public static byte[] ImageToByteArray(System.Drawing.Image img)
        //{
        //    ImageConverter _imageConverter = new ImageConverter();
        //    byte[] xByte = (byte[])_imageConverter.ConvertTo(img, typeof(byte[]));
        //    return xByte;
        //}

        //public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        //{
        //    int diameter = radius * 2;
        //    Size size = new Size(diameter, diameter);
        //    Rectangle arc = new Rectangle(bounds.Location, size);
        //    GraphicsPath path = new GraphicsPath();

        //    if (radius == 0)
        //    {
        //        path.AddRectangle(bounds);
        //        return path;
        //    }

        //    // top left arc  
        //    path.AddArc(arc, 180, 90);

        //    // top right arc  
        //    arc.X = bounds.Right - diameter;
        //    path.AddArc(arc, 270, 90);

        //    // bottom right arc  
        //    arc.Y = bounds.Bottom - diameter;
        //    path.AddArc(arc, 0, 90);

        //    // bottom left arc 
        //    arc.X = bounds.Left;
        //    path.AddArc(arc, 90, 90);

        //    path.CloseFigure();
        //    return path;
        //}

        //public System.Drawing.Image ClipToCircle(System.Drawing.Image srcImage, PointF center, float radius, System.Drawing.Color backGround)
        //{

        //    System.Drawing.Image dstImage = new Bitmap(srcImage.Width, srcImage.Height, srcImage.PixelFormat);

        //    using (Graphics g = Graphics.FromImage(dstImage))
        //    {
        //        RectangleF r = new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2);

        //        // enables smoothing of the edge of the circle (less pixelated)
        //        g.SmoothingMode = SmoothingMode.AntiAlias;

        //        // fills background color
        //        using (Brush br = new SolidBrush(backGround))
        //        {
        //            g.FillRectangle(br, 0, 0, dstImage.Width, dstImage.Height);
        //        }

        //        // adds the new ellipse & draws the image again
        //        GraphicsPath path = new GraphicsPath();
        //        path.AddEllipse(r);
        //        g.SetClip(path);
        //        g.DrawImage(srcImage, 0, 0);

        //        return dstImage;
        //    }
        //}




        [Command("restart")]
        public async Task Reset([Remainder] string reason = "No reason provided.")
        {
            if (Global.IsDev(Context.User))
            {
                await Context.Channel.TriggerTypingAsync();
                await Context.Message.Channel.SendMessageAsync($"Restarting bot with reason \"{reason}\"\n");
                _services.GetRequiredService<ShutdownService>().Shutdown(1);
            }
        }

        [Command("terminate")]
        public async Task Term()
        {
            if (Global.IsDev(Context.User))
            {
                await Context.Message.ReplyAsync($"Shutting down services...");
                _services.GetRequiredService<ShutdownService>().Shutdown(0);
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

                    try
                    {
                        await chn.SendMessageAsync("", false, eb.Build()); //This throws an exception claiming chn is null....yet it still sends the message.
                        await Context.Message.ReplyAsync("Sent support message response successfully");
                    }

                    catch { return; }
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

        [Command("getguilddata")]
        public async Task Getguilddata(params string[] inputOptions)
        {
            if (Global.IsDev(Context.User))
            {
                if (inputOptions.Length == 0)
                {
                    MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                    IMongoDatabase database = mongoClient.GetDatabase("finlay");
                    IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                    ulong _id = Context.Guild.Id;

                    try
                    {
                        BsonDocument data = await MongoHandler.FindById(collection, _id);
                        await ReplyAsync(data.ToString());
                    }

                    catch (KeyNotFoundException)
                    {
                        await ReplyAsync("No data was found for guild data.");
                    }
                }

                else
                {
                    MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                    IMongoDatabase database = mongoClient.GetDatabase("finlay");
                    IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                    ulong _id = Context.Guild.Id;
                    BsonDocument data = await MongoHandler.FindById(collection, _id);
                    string results = "";

                    for (int i = 0; i < inputOptions.Length; i++)
                    {
                        try
                        {
                            results += $"{data.GetElement(inputOptions[i])}\n\n";
                        }

                        catch (KeyNotFoundException)
                        {
                            results += $"No data found for {inputOptions[i]}.\n\n";
                        }
                    }

                    await ReplyAsync(results);
                }
            }
        }

        [Command("clearalldata")]
        public Task Clearalldata()
        {
            if (Global.IsDev(Context.User))
            {
                MongoClient mongoClient = new MongoClient(Global.Mongoconnstr);
                IMongoDatabase database = mongoClient.GetDatabase("finlay");
                IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("guilds");
                ulong _id = Context.Guild.Id;
                collection.DeleteOne(Builders<BsonDocument>.Filter.Eq("_id", _id));
            }

            return Task.CompletedTask;
        }

        [Command("EnBotClientCommands")]
        public async Task EnBotClientCommands(string tof)
        {
            if (Global.IsDev(Context.User))
            {
                if (tof == "true")
                {
                    Global.clientCommands = true;
                    await Context.Message.ReplyAsync($"Success, clientCommands set to {Global.clientCommands}");
                }

                else
                {
                    Global.clientCommands = false;
                    await Context.Message.ReplyAsync($"Success, clientCommands set to {Global.clientCommands}");
                }
            }
        }

        [Command("test")]
        public async Task test(string action = null, SocketUser member = null, SocketTextChannel channel = null)
        {
            //IEnumerable<RestAuditLogEntry> auditlogs = await Context.Guild.GetAuditLogsAsync(3, null, null, id, ActionType.ChannelUpdated).FlattenAsync();
            if (Global.IsDev(Context.User))
            {
                if (action != null)
                {
                    switch (action.ToLower())
                    {
                        case "roles":
                            await AuditRoles(Context, member ?? null);
                            break;
                        case "overwrites":
                            await AuditOverwrites(Context, channel ?? null);
                            break;
                    }
                }

                string result = "";

               

                await ReplyAsync(result);
            }
        }

        public async Task AuditRoles(ShardedCommandContext context, SocketUser user)
        {
            if (user == null)
            {
                await context.Channel.SendMessageAsync("", false, Global.EmbedMessage("Error", "Please mention a user", false, Color.Red).Build());
                return;
            }

            IUserMessage msg = await context.Message.ReplyAsync("Searching...this may take a few seconds");
            EmbedBuilder eb = CreateRoleChangesEmbed(context, user);
            eb.Footer = new EmbedFooterBuilder()
            {
                IconUrl = context.User.GetAvatarUrl() ?? context.User.GetDefaultAvatarUrl(),
                Text = $"{context.User}"
            };
            await Global.ModifyMessage(msg, eb);
            //        await sent_message.add_reaction("⏩")
        }

        public EmbedBuilder CreateRoleChangesEmbed(ShardedCommandContext context, SocketUser user, int startIndex = 0)
        {
            EmbedBuilder embed = new EmbedBuilder();
            embed.WithCurrentTimestamp();
            embed.Color = Color.Blue;
            embed.Title = $"Role changes for {user} - {user.Id}";

            return embed;
        }

        public async Task<string[]> GetRoleUpdates(ShardedCommandContext context, SocketUser user)
        {
            SocketGuild guild = context.Guild;
            ActionType action = ActionType.MemberRoleUpdated;
            string[] entries = { };

            IEnumerable<RestAuditLogEntry> auditSearch = await guild.GetAuditLogsAsync(int.MaxValue, null, null, user.Id, action).FlattenAsync();

            foreach (RestAuditLogEntry AuditLogEntry in auditSearch)
            {
                if (AuditLogEntry.Data is MemberRoleAuditLogData data)
                {
                    if (data.Target == user)
                    {
                        // IReadOnlyCollection<MemberRoleEditInfo> beforeRoles = ; //Woork on
                        DateTime date = AuditLogEntry.CreatedAt.DateTime;

                        // after roles
                        // taken roles
                        // added roles

                        //checks

                        
                    }
                }
            }

            return entries;
        }

        public async Task AuditOverwrites(ShardedCommandContext context, SocketTextChannel channel)
        {
            if(channel == null)
            {
                await context.Channel.SendMessageAsync("", false, Global.EmbedMessage("Error", "Plesse mention a channel", false, Color.Red).Build());
                return;
            }

            IUserMessage msg = await context.Message.ReplyAsync("Searching...this may take a few seconds");
            EmbedBuilder eb = CreateChannelUpdatesEmbed(context, channel, Context.User);
            await Global.ModifyMessage(msg, eb);
            //        await sent_message.add_reaction("⏩")

        }

        public EmbedBuilder CreateChannelUpdatesEmbed(ShardedCommandContext context, SocketTextChannel channel, SocketUser user)
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.WithCurrentTimestamp();
            eb.Color = Color.Blue;
            eb.Title = $"Channel updates for {channel.Name} - {channel.Id}";
            eb.Footer = new EmbedFooterBuilder()
            {
                IconUrl = context.User.GetAvatarUrl() ?? context.User.GetDefaultAvatarUrl(),
                Text = $"{context.User}"
            };

            return eb;
        }

        public async Task<string[]> GetchannelUpdates(ShardedCommandContext context, SocketTextChannel channel)
        {
            SocketGuild guild = context.Guild;
            ActionType action = ActionType.ChannelUpdated;
            string[] entries = { };

            IEnumerable<RestAuditLogEntry> auditSearch = await guild.GetAuditLogsAsync(int.MaxValue, null, null, null, action).FlattenAsync();

            foreach (RestAuditLogEntry AuditLogEntry in auditSearch)
            {
                if (AuditLogEntry.Data is ChannelUpdateAuditLogData data)
                {
                   
                }
            }

            return entries;
        }
    }
}