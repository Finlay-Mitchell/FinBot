using Discord.Commands;
using System.Threading.Tasks;

namespace FinBot.Modules
{
    public class TTSCommands : ModuleBase<SocketCommandContext> // This is purely boilerplate code to allow for the HelpHandler to hold all commands, including Python module.
    {
        [Command("speak"), Summary("Adds/removes user from the TTS speak list"), Remarks("(PREFIX)speak (optional)<user>")]
        public Task speak(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("disconnect"), Summary("Disconnects the bot from active voice channel"), Remarks("(PREFIX)disconnect"), Alias("leave")]
        public Task disconnect(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("speed"), Summary("Changes the playback speed of the TTS message"), Remarks("(PREFIX)speed <speed>")]
        public Task speed(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("lang"), Summary("Changes the language of which the TTS speaks in"), Remarks("(PREFIX)lang <language code>"), Alias("language")]
        public Task lang(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("speakers"), Summary("Gets a list of the current users on the TTS list"), Remarks("(PREFIX)speakers")]
        public Task speakers(params string[] args)
        {
            return Task.CompletedTask;
        }

        /*
         * This code is just not being used at the moment as there are bugs within the python script.
         */
        
        //[Command("reset_speakers"), Summary("Resets all users in the TTS list"), Remarks("(PREFIX)reset_speakers")]
        //public Task reset_speakers(params string[] args)
        //{
        //    return Task.CompletedTask;
        //}

        //[Command("speak_perms"), Summary("Gives other members permissions to the (PREFIX)speak command"), Remarks("(PREFIX)speak_perms <member>")]
        //public async Task speak_perms(params string[] args)
        //{
        //    return Task.CompletedTask;
        //}

        /*
         * Some commands are hidden from the public, since they are only accessable to the bot owner(me), for example:
         * tld(Top Level Domain) which sets the TTS TLD to what you select, e.g: (PREFIX)tld com || (PREFIX)tld co.uk
         */
    }
}
