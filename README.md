# FinBot
An advanced Discord utility/commands bot with:
- music features/lyric searching
- mod logging
- levelling
- chatbot/image guessing
- customisable behaviour 
  * Custom guild prefix (in the works)
  * Cusrom welcome messages (in the works)
  * ability to turn on and off commands (in the works)
  * custom member counts (in the works)
 - polling
 - text to speech
 - custom API(in the works)
 - and a lot more
 
 # APIs
 This bot uses a few APIs, Discord, Spotify and Genius.
  If you don't know how to get any of these, here are some links(this is not documentation, just where you can get your tokens):
  * [Spotify developer dashboard](https://developer.spotify.com/dashboard)
  * [Discord developer portal](https://discord.com/developers/applications)
  * [Genius API documentation](https://genius.com/api-clients)
 
 # C#
 ### NuGet packages
 * Discord.net
 * DependancyInjection
 * wiki.net
 * Newtonsoft json
 * quickchart
 * Google.Apis.YouTube.v3
 * MySql
 * ICanHazDadJoke.met
 * Serilog
 * Microsoft.Extensions.Logging
 * MongoDB
 * System.configuration
 
 # Python
 ### Packages
For this project, you will need to install the following libraries:
 * discord.Py
 * PyNaCl
 * spotipy
 * pytube
 * pydub
 * gtts
 * lyricsgenius
 * youtube-search-python
 * aiml
 * mee6
 * YoutubeDL
 * motor
 * mysql
 * aspell
 
# If you want to copy and expand this bot
First, give me credit.
Secondly, in Data -> config.py, you need to add your cog name to the "extensions" array for it to be loaded as a cog for the bot.
If you want to contact me, [then contact me via email](https://mail.google.com/mail/?view=cm&fs=1&to=finlayjosephmitchell@gmail.com)

# HELP! IT'S NOT WORKING AND I'M GETTING ERRORS!!!
If you encounter an error, use this table of contents to try help you:

| Error                 | Fix                                      |
|-----------------------|------------------------------------------|
| Pip isn't recognised? | Try: `py -3 -m pip install -U <package>` |
| Unknown package?      | Do: `pip install -u <package name>`      |
| Out of date package?  | Do: `pip install <package> -U`           |
| Rate limited(Error code 429)?         | You're sending too many requests. Wait a few minutes and try again |
|                       |                                          |
| Other issue?          | Contact me and I will try help you       |
| Failing to compile?   | You've copied something wrong, check all of your files |

# Other features
This bot as well as music support with YouTube, also supports music support with Spotify and includes custom TTS(text to speech). It also has lyric lookup with genius and song searching.

# License
Check License.md for license information.

# Other notes
Thomas is a walking talking God.

The YouTube searching alogrithm is in development still. It should work completely fine without any/many miss-named videos.
This also requires ffmpeg to run for python and C#, and opus/libsodum for C#
