import json

json_data = open("../../bin/Debug/netcoreapp3.1/Data/config.json")
#json_data = open("Data/config.json")
data = json.load(json_data)
json_data.close()

token = data["Token"]
prefix = data["Prefix"]
description = data["Version"]
version = data["Version"]
client_secret = data["SpotifySecret"]
client_Id = data["SpotifyId"]
genius_id = data["GeniusId"]
HypixelAPIKey = data["HypixelAPIKey"]

fast_forward_emoji = u"\u23E9"
rewind_emoji = u"\u23EA"
forward_arrow = u"\u25B6"
backwards_arrow = u"\u25C0"
both_arrow = u"\u2194"
discord_emoji = "<:discord:784309400524292117>"
monkey_guild_id = 725886999646437407
error_channel_id = 795057163768037376
data_path = "../../bin/Debug/netcoreapp3.1/Data/guild_config.json"
# data_path = "Data/guild_config.json"
extensions = ["Modlogs", "tts"]
# extensions = ["audit", "Executer", "api", "tts", "music", "Lyrics", "Minecraft", "Modlogs"]  # This is because we are temp removing Mee6Levels
owner_id = 305797476290527235
mute_emoji = ":mute:"
# genius_secret = data["GeniusSecret"]
