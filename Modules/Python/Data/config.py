import json

json_data = open("../../bin/Debug/netcoreapp3.1/Data/config.json")
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
mute_emoji = ":mute:"

owner_id = 305797476290527235
monkey_guild_id = 725886999646437407
error_channel_id = 795057163768037376
dev_uids = [305797476290527235, 368095722442194945, 230778630597246983, 279323004087959552]  # last one is an alt, lol

data_path = "../../bin/Debug/netcoreapp3.1/Data/guild_config.json"
extensions = ["Audit", "Executer", "API", "TTS", "Lyrics", "Minecraft", "Modlogs", "Music"]  # This is because we are
# temp removing Mee6Levels
