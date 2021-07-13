# FinBot
An advanced Discord utility/commands bot with:
- music features/lyric searching
- mod logging
- levelling with ability to turn on and off
- chatbot/image guessing
- customizable behavior 
  * Custom guild prefix
  * Cusrom welcome messages
  * ability to turn on and off commands (in the works)
  * custom guild member count
 - polling
 - text to speech
 - custom API
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
 * MongoDB.Driver
 * System.configuration
 * UptimeSharp
 
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
 * mySql

# Node.js
### Packages
* discord.js
* express
 
# If you want to copy and expand this bot
First, give me credit.
Secondly, in Data -> config.py, you need to add your cog name to the "extensions" array for it to be loaded as a cog for the bot.
If you want to contact me, [then contact me via email](https://mail.google.com/mail/?view=cm&fs=1&to=finlayjosephmitchell@gmail.com)

# HELP! IT'S NOT WORKING AND I'M GETTING ERRORS!!!
If you encounter an error, use this table of contents to try help you:

| Error                 | Fix                                      |
|-----------------------|------------------------------------------|
| Pip isn't recognized? | Try: `py -3 -m pip install -U <package>` |
| Unknown package?      | Do: `pip install -u <package name>`      |
| Out of date package?  | Do: `pip install <package> -U`           |
| Rate limited(Error code 429)?         | You're sending too many requests. Wait a few minutes and try again |
|                       |                                          |
| Other issue?          | Contact me and I will try help you       |
| Failing to compile?   | You've copied something wrong, check all of your files |

# License
Check License.md for license information.

# Other notes
Thomas is a walking talking God.

The YouTube searching algorithm is in development still. It should work completely fine without any/many miss-named videos.
This also requires ffmpeg to run for python and C#, and opus/libsodum for C#

#ToDo:
 - Add a dictionary for the first few hundred guild Ids to potentially reduce time taken to execute command, this being because a dictionary is faster than reading from a MongoDB database - impact on non-stored guilds is negligible.
 - Contain the MySql connection string as done with the MongoDB connection string.
 - Redesign poll database implementation.