using Discord.Commands;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using System;

namespace FinBot.Modules
{
    public class TTSCommands : InteractiveBase// This is purely boilerplate code to allow for the HelpHandler to hold all commands, including Python module.
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

        [Command("reset_speakers"), Summary("Resets all users in the TTS list"), Remarks("(PREFIX)reset_speakers"), Alias("remove_speakers", "clear_speakers")]
        public Task reset_speakers(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("speak_perms"), Summary("Gives other members permissions to the (PREFIX)speak command"), Remarks("(PREFIX)speak_perms <member>")]
        public Task speak_perms(params string[] args)
        {
            return Task.CompletedTask;
        }

        /*
         * Some commands are hidden from the public, since they are only accessable to the bot owner(me), for example:
         * tld(Top Level Domain) which sets the TTS TLD to what you select, e.g: (PREFIX)tld com || (PREFIX)tld co.uk
         */















        [Command("delete")]
        public async Task<RuntimeResult> Test_DeleteAfterAsync()
        {
            await ReplyAndDeleteAsync(content: "this message will delete in 10 seconds", timeout: TimeSpan.FromSeconds(10));
            return Ok();
        }





        [Command("next", RunMode = RunMode.Async)]
        public async Task Test_NextMessageAsync()
        {
            await ReplyAsync("What is 2+2?");
            var response = await NextMessageAsync();
            if (response != null)
                await ReplyAsync($"You replied: {response.Content}");
            else
                await ReplyAsync("You did not reply before the timeout");
        }


        [Command("paginator")]
        public async Task Test_Paginator()
        {
            var pages = new[] { "Page 1", "Page 2", "Page 3", "aaaaaa", "Page 5" };
            await PagedReplyAsync(pages);
        }








    }
}
