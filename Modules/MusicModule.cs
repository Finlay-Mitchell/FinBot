using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace FinBot.Modules 
{
    public class MusicModule : ModuleBase<ShardedCommandContext> //Music python boilerplate code
    {
        [Command("play"), Summary("plays a song/playlist"), Remarks("(PREFIX)play <song(s)>"), Alias("p")]
        [RequireBotPermission(ChannelPermission.EmbedLinks | (ChannelPermission)GuildPermission.Speak)]
        public Task play(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("pause"), Summary("Stops the currently playing track"), Remarks("(PREFIX)pause"), Alias("stop", "leave", "quit")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public Task pause(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("resume"), Summary("Resumes the currently paused track"), Remarks("(PREFIX)resume"), Alias("res", "continue")]
        [RequireBotPermission(ChannelPermission.EmbedLinks), RequireBotPermission(GuildPermission.Speak)]
        public Task resume(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("skip"), Summary("skips the currently playing track"), Remarks("(PREFIX)skip"), Alias("next")]
        [RequireBotPermission(ChannelPermission.EmbedLinks | (ChannelPermission)GuildPermission.Speak)]
        public Task skip(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("volume"), Summary("Sets the volume for the audio output of the music"), Remarks("(PREFIX)volume <volume>"), Alias("vol")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public Task volume(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("shuffle"), Summary("Shuffles the current music playlist"), Remarks("(PREFIX) shuffle"), Alias("shuff", "mix")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public Task shuffle(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("queue"), Summary("Shows the queued songs on the guild playlist"), Remarks("(PREFIX)queue"), Alias("que", "cue")]
        [RequireBotPermission(ChannelPermission.EmbedLinks | ChannelPermission.AddReactions | ChannelPermission.ManageMessages)]
        public Task queue(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("clear_queue"), Summary("clears the current queue for the current guild"), Remarks("(PREFIX)clear_queue"), Alias("clearqueue")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public Task clear_queue(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("dequeue"), Summary("removes item from queue"), Remarks("(PREFIX)dequeue <item>"), Alias("unqueue", "remove")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public Task dequeue(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("lyrics"), Summary("Gets the lyrics of a song"), Remarks("(PREFIX)lyrics <song name>")]
        [RequireBotPermission(ChannelPermission.EmbedLinks | ChannelPermission.AddReactions | ChannelPermission.ManageMessages)]
        public Task lyrics(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("lyricSearch"), Summary("Tries to match a song to the provided lyrics"), Remarks("(PREFIX)lyricsearch <your lyrics>")]
        [RequireBotPermission(ChannelPermission.EmbedLinks | ChannelPermission.AddReactions | ChannelPermission.ManageMessages)]
        public Task lyricSearch(params string[] args)
        {
            return Task.CompletedTask;
        }
    }
}
