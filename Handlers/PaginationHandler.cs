using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.WebSocket;
using System.Linq;

namespace FinBot.Handlers
{
    class PaginationHandler
    {
        ShardedCommandContext _context;
        string _title;
        string _fullText;
        int _maxLength;
        Color _colour;
        List<string> pages = new List<string>();
        int pageIndex = 0;
        DiscordShardedClient _client;
        string _remainingText;
        int _length;

        public PaginationHandler(DiscordShardedClient client, ShardedCommandContext context, string title = null, string fullText = null, int maxLength = 2000, Color colour = default(Color))
        {
            _context = context;
            _title = title;
            _fullText = fullText;
            _maxLength = maxLength;
            _colour = colour;
            _client = client;
            _remainingText = fullText;
            _length = maxLength;

            _client.ReactionAdded += onReactionAdd;
        }

        private async Task onReactionAdd(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2, SocketReaction arg3)
        {
            if(arg3.MessageId != _context.Message.Id || arg3.UserId == _context.Guild.CurrentUser.Id)
            {
                return;
            }

            SocketUserMessage message = (SocketUserMessage)await arg1.GetOrDownloadAsync();
            await message.RemoveReactionAsync(arg3.Emote, arg3.UserId);

            if (arg3.Emote.Name == "\u25B6")
            {
                if (pageIndex >= pages.Count - 1)
                {
                    return;
                }

                pageIndex += 1;
            }

            else if (arg3.Emote.Name == "\u23EA")
            {
                if (pageIndex <= 0)
                {
                    return;
                }

                pageIndex -= 1;
            }

            await UpdateMessage();
        }

        public async Task UpdateMessage()
        {
            await _context.Message.ModifyAsync(m =>
            {
                m.Embed = CreatePage().Build();
            });
        }

        public async Task Start()
        {
            //await FillPages();
            RestUserMessage msg = (RestUserMessage)await _context.Message.ReplyAsync("", false, CreatePage().Build());
            await msg.AddReactionAsync(new Emoji("\u23EA"));
            await msg.AddReactionAsync(new Emoji("\u25B6"));
        }

        //public async Task FillPages()
        //{
        //    while(_remainingText.Length > _length)
        //    {
        //        ////var newline_indices = (from m in re.finditer(@"\n", this.remaining_text[::self.length]) select m.end()).ToList();
        //        //var newlineIndicies = (from m in Regex.Matches("\n", _remainingText[_length]) select m).ToList();

        //        if(newlineIndicies.Length == 0)
        //        {

        //        }
        //    }
        //}

        //public virtual object fill_pages()
        //{
        //    while (this.remaining_text.Count > this.length)
        //    {
        //        var newline_indices = (from m in re.finditer(@"\n", this.remaining_text[::self.length])
        //                               select m.end()).ToList();
        //        if (newline_indices.Count == 0)
        //        {
        //            var space_indices = (from m in re.finditer(@"\s", this.remaining_text[::self.length])
        //                                 select m.end()).ToList();
        //            if (space_indices.Count == 0)
        //            {
        //                this.pages.append(this.remaining_text[::self.length]);
        //                this.remaining_text = this.remaining_text[this.length];
        //            }
        //            else
        //            {
        //                this.pages.append(this.remaining_text[::space_indices[-1]]);
        //                this.remaining_text = this.remaining_text[space_indices[-1]];
        //            }
        //        }
        //        else
        //        {
        //            this.pages.append(this.remaining_text[::newline_indices[-1]]);
        //            this.remaining_text = this.remaining_text[newline_indices[-1]];
        //        }
        //    }
        //    if (this.remaining_text != "")
        //    {
        //        this.pages.append(this.remaining_text);
        //    }
        //    return true;
        //}

        public EmbedBuilder CreatePage()
        {
            EmbedBuilder eb = new EmbedBuilder();
            eb.Title = _title;
            eb.Color = _colour;
            eb.Description = pages[pageIndex];
            return eb;
        }
    }
}
