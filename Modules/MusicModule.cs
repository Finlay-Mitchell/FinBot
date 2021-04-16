using Discord.Commands;
using System.Threading.Tasks;

namespace FinBot.Modules 
{
    public class MusicModule : ModuleBase<ShardedCommandContext> //This is more code just to fill the help command up, much like TTSCommands.cs
    {
        [Command("play"), Summary("plays a song/playlist from YouTube"), Remarks("(PREFIX)play <song name/playlist/song URL>")]
        public Task play(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("pause"), Summary("Stops the currently playing track"), Remarks("(PREFIX)pause"), Alias("stop")]
        public Task pause(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("resume"), Summary("Resumes the currently paused track"), Remarks("(PREFIX)resume")]
        public Task resume(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("skip"), Summary("skips the currently playing track"), Remarks("(PREFIX)skip")]
        public Task skip(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("volume"), Summary("Sets the volume for the audio output of the music"), Remarks("(PREFIX)volume <volume>")]
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

        [Command("shuffle"), Summary("Shuffles the current music playlist"), Remarks("(PREFIX) shuffle")]
        public Task shuffle(params string[] args)
        {
            return Task.CompletedTask;
        }
    }
}
