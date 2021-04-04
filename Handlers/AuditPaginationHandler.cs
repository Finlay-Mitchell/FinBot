using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using System.Linq;
using System.Text.RegularExpressions;

namespace FinBot.Handlers
{
    public class InfractionMessageHandler
    {
        public static int InfractionmsgPerPage = 4;
        public List<string> InfractionPagesPublic = new List<string>();

        public static int InfractionPagesPublicCount = 0;

        public static Dictionary<ulong, ulong> CurrentInfractionMessages = Global.LoadInfractionMessageCards();
        private DiscordShardedClient client;
        private ulong GuildId;
        public InfractionMessageHandler(DiscordShardedClient client, ulong GuildId, Embed e)
        {
            this.client = client;
            this.GuildId = GuildId;
            BuildInfractionPages(e);
            client.ReactionAdded += HandleInfractionMessage;
            InfractionPagesPublicCount = (int)Math.Ceiling((double)InfractionPagesPublic.Count / (double)InfractionmsgPerPage);
        }

        public async Task HandleInfractionMessage(Cacheable<Discord.IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            if (arg3.User.Value.IsBot)
                return;
            if (!CurrentInfractionMessages.Keys.Any(x => x == arg3.MessageId))
                return;
            var msg = (SocketUserMessage)client.GetGuild(GuildId).GetTextChannel(arg3.Channel.Id).GetMessageAsync(arg3.MessageId).Result;
            
            if (CurrentInfractionMessages.Keys.Contains(arg3.MessageId) && msg != null)
            {
                if (arg3.UserId != CurrentInfractionMessages[arg3.MessageId])
                {
                    await msg.RemoveReactionAsync(arg3.Emote, arg3.User.Value);
                    return;
                }
                //is a valid card, lets check what page were on
                var s = msg.Embeds.First().Title;

                Regex r = new Regex(@"\*\*Infractions \((\d)\/(\d)\)");
                var mtc = r.Match(s);
                var curpage = int.Parse(mtc.Groups[1].Value);

                if (arg3.Emote.Name == "⬅")
                {
                    //check if the message is > 2 weeks old or exists in swiss server
                    if (curpage == 1)
                    {
                        await msg.RemoveReactionAsync(arg3.Emote, arg3.User.Value);
                        return;
                    }

                    await msg.ModifyAsync(x => x.Embed = InfractionEmbedBuilder(curpage - 1, CalcInfractionPage(client.GetGuild(GuildId).GetUser(arg3.User.Value.Id))));
                    await msg.RemoveReactionAsync(arg3.Emote, arg3.User.Value);

                }
                else if (arg3.Emote.Name == "➡")
                {
                    await msg.ModifyAsync(x => x.Embed = InfractionEmbedBuilder(curpage + 1, CalcInfractionPage(client.GetGuild(GuildId).GetUser(arg3.User.Value.Id))));
                    await msg.RemoveReactionAsync(arg3.Emote, arg3.User.Value);
                }
                else
                {
                    await msg.RemoveReactionAsync(arg3.Emote, arg3.User.Value);
                }

            }
        }
        public void BuildInfractionPages(Embed e)
        {
            InfractionPagesPublic.Add($"**TOTAL INFRACTIONS: {e.Fields.Count()}**\n");

            foreach (var i in e.Fields)
            {
                InfractionPagesPublic.Add(i.ToString() + "\n");
            }
        }
        public Embed InfractionEmbedBuilder(int page, InfractionPages p)
        {
            if (p == InfractionPages.Public)
            {
                if (page > InfractionPagesPublicCount)
                    page = page - 1;
                var rs = InfractionPagesPublic.Skip((page - 1) * InfractionmsgPerPage).Take(InfractionmsgPerPage);
                var em = new EmbedBuilder()
                {
                    Title = $"**Infractions ({page}/{InfractionPagesPublicCount})**",
                    Color = Color.Green,
                    Description = string.Join("\n", rs),
                    Footer = new EmbedFooterBuilder()
                    {
                        Text = $"To remove an infraction, use the command \"{Global.Prefix}clearlogs <user> <index>\" or to clear all logs do \"{Global.Prefix}clearalllogs <user>"
                    }
                };

                return em.Build();
            }

            else
            {
                var em = new EmbedBuilder()

                {
                    Title = $"**no work**",
                    Color = Color.Green,
                    Description = "not working",
                    Footer = new EmbedFooterBuilder()
                    {
                        Text = $"To remove an infraction, use the command \"{Global.Prefix}clearlogs <user> <index>\" or to clear all logs do \"{Global.Prefix}clearalllogs <user>"
                    }
                };

                return em.Build();
            }
        }
        public static InfractionPages CalcInfractionPage(SocketGuildUser usr)
        {
                return InfractionPages.Public;
        }
        public enum InfractionPages
        {
            Public,
        }
    }
}