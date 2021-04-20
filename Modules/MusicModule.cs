using Discord.Commands;
using System.Threading.Tasks;

namespace FinBot.Modules 
{
    public class MusicModule : ModuleBase<ShardedCommandContext> //This is more code just to fill the help command up, much like TTSCommands.cs
    {
        [Command("play"), Summary("plays a song/playlist"), Remarks("(PREFIX)play <song(s)>"), Alias("p")]
        public Task play(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("pause"), Summary("Stops the currently playing track"), Remarks("(PREFIX)pause"), Alias("stop", "s", "leave")]
        public Task pause(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("resume"), Summary("Resumes the currently paused track"), Remarks("(PREFIX)resume"), Alias("res", "r", "continue")]
        public Task resume(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("skip"), Summary("skips the currently playing track"), Remarks("(PREFIX)skip"), Alias("next")]
        public Task skip(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("volume"), Summary("Sets the volume for the audio output of the music"), Remarks("(PREFIX)volume <volume>"), Alias("vol")]
        public Task volume(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("mute"), Summary("mutes the bot from sending music to the voice channel"), Remarks("(PREFIX)mute")]
        public Task mute(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("unmute"), Summary("unmutes the bot from sending music to the voice channel"), Remarks("(PREFIX)unmute")]
        public Task unmutemusic(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("shuffle"), Summary("Shuffles the current music playlist"), Remarks("(PREFIX) shuffle"), Alias("shuff", "mix")]
        public Task shuffle(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("queue"), Summary("Shows the queued songs on the guild playlist"), Remarks("(PREFIX)queue"), Alias("que", "cue")]
        public Task queue(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("clear_queue"), Summary("clears the current queue for the current guild"), Remarks("(PREFIX)clear_queue"), Alias("clearqueue")]
        public Task clear_queue(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("dequeue"), Summary("removes item from queue"), Remarks("(PREFIX)dequeue <item>"), Alias("unqueue", "remove")]
        public Task dequeue(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("lyrics"), Summary("Gets the lyrics of a song"), Remarks("(PREFIX)lyrics <song name>")]
        public Task lyrics(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("lyricSearch"), Summary("Tries to match a song to the provided lyrics"), Remarks("(PREFIX)")]
        public Task lyricSearch(params string[] args)
        {
            return Task.CompletedTask;
        }
    }
}
