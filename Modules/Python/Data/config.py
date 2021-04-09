import json

# jsondata = open("../../bin/Debug/netcoreapp3.1/Data/config.json")
jsondata = open("Data/config.json")
data = json.load(jsondata)
jsondata.close()

token = data["Token"]
prefix = data["prefix"]
description = data["Version"]
version = data["Version"]
fast_forward_emoji = u"\u23E9"
rewind_emoji = u"\u23EA"
forward_arrow = u"\u25B6"
backwards_arrow = u"\u25C0"
both_arrow = u"\u2194"
discord_emoji = "<:discord:784309400524292117>"
monkey_guild_id = 725886999646437407
error_channel_id = 795057163768037376
data_path = "Data/tts_config.json"
extensions = ["audit", "Executer", "api", "tts", "Mee6Levels", "music"]
owner_id = 305797476290527235