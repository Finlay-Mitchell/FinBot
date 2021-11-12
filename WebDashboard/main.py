import asyncio

from quart import Quart, render_template, request, session, redirect, url_for
from quart_discord import DiscordOAuth2Session
from discord.ext import ipc

from Data import config
import mongo

app = Quart(__name__, template_folder="Website/HTML", static_folder="Website")
ipc_client = ipc.Client(secret_key="Swas")

app.config["SECRET_KEY"] = "testing"
app.config["DISCORD_CLIENT_ID"] = config.client_id
app.config["DISCORD_CLIENT_SECRET"] = config.client_secret
app.config["DISCORD_REDIRECT_URI"] = config.discord_redirect_uri
discord = DiscordOAuth2Session(app)


@app.route("/")
async def home():
    return await render_template("index.html", authorized=await discord.authorized, discord=discord)


# @app.route("/")
# async def home():
#     return await render_template("index.html", authorized=await discord.authorized)


@app.route("/login")
async def login():
    return await discord.create_session()


@app.route("/callback")
async def callback():
    try:
        await discord.callback()
    except Exception:
        pass

    return redirect(url_for("dashboard"))


@app.route("/dashboard", strict_slashes=False)
async def dashboard():
    if not await discord.authorized:
        return redirect(url_for("login"))

    guild_count = await ipc_client.request("get_guild_count")
    guild_ids = await ipc_client.request("get_guild_ids")

    user_guilds = await discord.fetch_guilds()

    guilds = []

    for guild in user_guilds:
        if guild.permissions.administrator:
            guild.class_color = "green-border" if guild.id in guild_ids else "red-border"
            guilds.append(guild)

    guilds.sort(key=lambda x: x.class_color == "red-border")
    name = (await discord.fetch_user()).name
    return await render_template("dashboard.html", guild_count=guild_count, guilds=guilds, username=name)


@app.route("/dashboard/<int:guild_id>")
async def dashboard_server(guild_id):
    if not await discord.authorized:
        return redirect(url_for("login"))

    guild = await ipc_client.request("get_guild", guild_id=guild_id)
    if guild is None:
        return redirect(f'https://discord.com/oauth2/authorize?&client_id={config.client_id}&scope=bot&permissions='
                        f'8&guild_id={guild_id}&response_type=code&redirect_uri={config.discord_redirect_uri}')
    return str(guild)


@app.route("/logout")
async def logout():
    discord.revoke()
    return redirect("/")


if __name__ == "__main__":
    asyncio.get_event_loop().run_until_complete(mongo.initiate_mongo())
    app.run(debug=config.debug)
