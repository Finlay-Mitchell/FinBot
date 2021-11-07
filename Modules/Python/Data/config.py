import json
import datetime
import sys

json_data = open("../../bin/Debug/netcoreapp3.1/Data/config.json")  # Opens the config.json file to extract data.
data = json.load(json_data)  # Reads the data from the config.json file.
json_data.close()

# Items to read from the config.json file.
token = data["Token"]  # The Discord bot token.
prefix = data["Prefix"]  # Default prefix for the bot.
version = data["Version"]  # # The current version of the bot.
description = version  # Sets the description to the bot version.
client_secret = data["SpotifySecret"]  # Spotify token.
client_Id = data["SpotifyId"]  # Spotify id key.
genius_id = data["GeniusId"]  # Genius id key.
HypixelAPIKey = data["HypixelAPIKey"]  # API key for Hypixel.
mongo_connection_uri = data["mongoconnstr"]  # MongoDB database connection string.
bot_directory = data["BotDirectory"]  # Gets the directory of the bot.

# Emojis commonly used in the bot.
fast_forward_emoji = u"\u23E9"
rewind_emoji = u"\u23EA"
forward_arrow = u"\u25B6"
backwards_arrow = u"\u25C0"
both_arrow = u"\u2194"
discord_emoji = "<:discord:784309400524292117>"
mute_emoji = ":mute:"

# unique ids used for the bot.
owner_id = 305797476290527235  # The id of the bot owner.
monkey_guild_id = 725886999646437407  # Remove instances for this soon.
error_channel_id = 795057163768037376  # Use json item for this.
dev_uids = [305797476290527235]  # Listed bot developers.

# Other global variables.
data_path = "../../bin/Debug/netcoreapp3.1/Data/guild_config.json"
# extensions = ["audit", "executer", "tts", "lyrics", "minecraft", "music", "misc", "chatbot", "modlogs", "chess"]
client_commands = False
debug = False

# Chess variables
EXPIRATION_TIMEDELTA = datetime.timedelta(days=7)
server = data["MySQLServer"]
user = data["MySQLUser"]
database = data["MySQLDatabase"]
port = data["MySQLPort"]
password = data["MySQLPassword"]
CHESS_DB_PATH = f"mysql+pymysql://{user}:{password}@{server}/{database}"

extensions = ["chess_game"]
