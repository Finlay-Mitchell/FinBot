using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using FinBot.Modules;
using System.IO;
using System.Text.RegularExpressions;

namespace FinBot.Handlers.AutoMod
{
    public class ChatFilter
    {
        DiscordSocketClient _client;
        //SocketTextChannel chan = (SocketTextChannel)Global.Client.GetChannel(Global.ModLogChannel);

        public ChatFilter(DiscordSocketClient client)
        {
            _client = client;
            //_client.MessageReceived += CheckForCensoredWords;
            //_client.MessageReceived += CheckForLinks;
            //_client.MessageReceived += CheckForPingSpam;
        }

        //private async Task CheckForCensoredWords(SocketMessage arg)
        //{
        //    if (arg.Author.IsBot)
        //    {
        //        return;
        //    }

        //    SocketGuildUser user = Global.Client.GetGuild(Global.GuildId).GetUser(arg.Author.Id);
        //    string[] leetWords = File.ReadAllLines(Global.CensoredWordsPath);
        //    string msg = arg.ToString();
        //    Dictionary<string, string> leetRules = Global.LoadLeetRules();

        //    if (Global.Client.GetGuild(Global.GuildId).GetRole(726453086331338802).Position <= user.Hierarchy)
        //    {
        //        return;
        //    }

        //    foreach (KeyValuePair<string, string> x in leetRules)
        //    {
        //        msg = msg.Replace(x.Key, x.Value);
        //    }

        //    msg = msg.ToLower();

        //    Regex re = new Regex(@"\b(" + string.Join("|", leetWords.Select(word => string.Join(@"\s*", word.ToCharArray()))) + @")\b", RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

        //    if (re.IsMatch(msg))
        //    {

        //        await arg.DeleteAsync();
        //        ModCommands.AddModlogs(arg.Author.Id, ModCommands.Action.Warned, Global.Client.CurrentUser.Id, "Bad word usage", arg.Author.Username);
        //        EmbedBuilder eb = new EmbedBuilder()
        //        {
        //            Title = $"***{arg.Author.Username} has been warned***",
        //            Footer = new EmbedFooterBuilder()
        //            {
        //                IconUrl = arg.Author.GetAvatarUrl(),
        //                Text = $"{arg.Author.Username}#{arg.Author.Discriminator}"
        //            },
        //            Description = $"{arg.Author} has been warned at {DateTime.Now}\n Reason: Bad word usage.",
        //            ThumbnailUrl = Global.KickMessageURL,
        //            Color = Color.Orange
        //        };
        //        eb.WithCurrentTimestamp();
        //        await arg.Channel.TriggerTypingAsync();
        //        await arg.Channel.SendMessageAsync("", false, eb.Build());
        //        eb.AddField("User", $"{user.Username}", true);
        //        eb.AddField("Moderator", $"LexiBot automod", true);
        //        eb.AddField("Reason", $"\"Bad word usage.\"", true);
        //        eb.AddField("Message", arg.ToString(), true);
        //        eb.WithCurrentTimestamp();
        //        await chan.SendMessageAsync("", false, eb.Build());
        //    }

        //    return;
        //}

        //private async Task CheckForLinks(SocketMessage arg)
        //{
        //    SocketGuildUser user = (SocketGuildUser)arg.Author;
        //    Regex r = new Regex(@"(http|ftp|https)://([\w_-]+(?:(?:\.[\w_-]+)+))([\w.,@?^=%&:/~+#-]*[\w@?^=%&/~+#-])?");

        //    if (r.IsMatch(arg.ToString()) && !user.Roles.Any(r => r.Id == 794993948572516392) && !user.GuildPermissions.Administrator)
        //    {
        //        await arg.DeleteAsync();
        //        ModCommands.AddModlogs(arg.Author.Id, ModCommands.Action.Warned, Global.Client.CurrentUser.Id, "Sent a link", arg.Author.Username);
        //        EmbedBuilder eb = new EmbedBuilder()
        //        {
        //            Title = $"***{arg.Author.Username} has been warned***",
        //            Footer = new EmbedFooterBuilder()
        //            {
        //                IconUrl = arg.Author.GetAvatarUrl(),
        //                Text = $"{arg.Author.Username}#{arg.Author.Discriminator}"
        //            },
        //            Description = $"{arg.Author} has been warned at {DateTime.Now}\n Reason: Sent a link.",
        //            ThumbnailUrl = Global.KickMessageURL,
        //            Color = Color.Orange
        //        };
        //        eb.WithCurrentTimestamp();
        //        await arg.Channel.TriggerTypingAsync();
        //        await arg.Channel.SendMessageAsync("", false, eb.Build());
        //        eb.AddField("User", $"{user.Username}", true);
        //        eb.AddField("Moderator", $"LexiBot automod.", true);
        //        eb.AddField("Reason", $"\"Sent a link.\"", true);
        //        eb.AddField("Message", arg.ToString(), true);
        //        eb.WithCurrentTimestamp();
        //        await chan.SendMessageAsync("", false, eb.Build());


        //        return;
        //    }

        //    return;
        //}

        //private async Task CheckForPingSpam(SocketMessage arg)
        //{
        //    SocketGuildUser user = (SocketGuildUser)arg.Author;

        //    if (!user.GuildPermissions.Administrator && arg.MentionedUsers.Count >= Global.MaxUserPingCount || arg.MentionedRoles.Count >= Global.MaxRolePingCount)
        //    {
        //        await arg.DeleteAsync();
        //        ModCommands.AddModlogs(arg.Author.Id, ModCommands.Action.Warned, Global.Client.CurrentUser.Id, "mass ping", arg.Author.Username);
        //        EmbedBuilder eb = new EmbedBuilder()
        //        {
        //            Title = $"***{arg.Author.Username} has been warned***",
        //            Footer = new EmbedFooterBuilder()
        //            {
        //                IconUrl = arg.Author.GetAvatarUrl(),
        //                Text = $"{arg.Author.Username}#{arg.Author.Discriminator}"
        //            },
        //            Description = $"{arg.Author} has been warned at {DateTime.Now}\n Reason: mass ping.",
        //            ThumbnailUrl = Global.KickMessageURL,
        //            Color = Color.Orange
        //        };
        //        eb.WithCurrentTimestamp();
        //        await arg.Channel.TriggerTypingAsync();
        //        await arg.Channel.SendMessageAsync("", false, eb.Build());
        //        eb.AddField("User", $"{user.Username}", true);
        //        eb.AddField("Moderator", $"LexiBot automod", true);
        //        eb.AddField("Reason", $"\"Mass mention.\"", true);
        //        eb.AddField("Message", arg.ToString(), true);
        //        eb.WithCurrentTimestamp();
        //        await chan.SendMessageAsync("", false, eb.Build());

        //        return;
        //    }

        //    return;
        //}
    }
}