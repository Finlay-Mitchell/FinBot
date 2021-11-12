import json

json_data = open("../bin/Debug/netcoreapp3.1/Data/config.json")
data = json.load(json_data)
json_data.close()

client_id = data["ClientId"]
client_secret = data["ClientSecret"]
redirect_uri = data["RedirectURI"]
discord_login_url = data["DiscordLoginURL"]
token = data["Token"]
mongo_connection_uri = data["mongoconnstr"]
prefix = data["Prefix"]

discord_token_url = "https://discord.com/api/oauth2/token/"
discord_api_url = "https://discord.com/api"
scope = "identify%20email%20guilds"
discord_api_endpoint = "https://discord.com/api/v8"
discord_redirect_uri = "http://127.0.0.1:5000/callback"
debug = True
mongo = None