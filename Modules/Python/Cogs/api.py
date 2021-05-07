import asyncio
import json
import secrets
# import aspell

from aiohttp import web
from discord.ext import commands

from main import FinBot
from Handlers.storageHandler import DataHelper
from Data import config
#
#
# def get_protocol(bot):
#     class ReceiveAPIMessage(asyncio.Protocol):
#         def data_received(self, data: bytes) -> None:
#             received_message = data.decode()
#             print(received_message)
#             json_content = json.loads(received_message)
#             storage = DataHelper()
#
#             if json_content.get("key", "") not in storage.get("api_keys", {}).keys():
#                 return
#
#             if json_content.get("type", "") == "tts_message":
#                 content = json_content.get("message_content", "")
#
#                 try:
#                     member_id = int(storage.get("api_keys", {}).get(json_content.get("key", "")))
#
#                 except ValueError:
#                     return
#
#                 bot.bot.loop.create_task(bot.speak_id_content(int(member_id), content))
#
#     return ReceiveAPIMessage
#
#
# async def start_server(bot):
#     loop = bot.bot.loop
#     server = await loop.create_server(get_protocol(bot), '0.0.0.0', 42132)
#     async with server:
#         await server.serve_forever()
#
#
# class API(commands.Cog):
#     def __init__(self, bot: FinBot):
#         self.bot = bot
#
#     @commands.command()
#     async def api_key(self, ctx):
#         await ctx.reply(embed=self.bot.create_completed_embed("Generated API Key", "I have DM'd you your api key."))
#         key = secrets.token_urlsafe(16)
#         storage = DataHelper()
#         all_keys = storage.get("api_keys", {})
#
#         for old_key in all_keys.keys():
#             if all_keys[old_key] == str(ctx.author.id):
#                 del all_keys[old_key]
#
#         all_keys[key] = ctx.author.id
#         storage["api_keys"] = all_keys
#         await ctx.author.send("Your API key is: {}".format(key))

class API(commands.Cog):
    def __init__(self, bot: FinBot):
        self.bot = bot
        self.speller = aspell.Speller('lang', 'en')
        self.api_db = self.bot.mongo.finlay.api
        app = web.Application()
        app.add_routes([web.post('/speak', self.handle_speak_message)])
        # noinspection PyProtectedMember
        self.bot.loop.create_task(self.start_site(app))

    async def start_site(self, app):
        runner = web.AppRunner(app)
        await runner.setup()
        site = web.TCPSite(runner, "0.0.0.0", config.api_port)
        self.bot.loop.create_task(site.start())
        return

    def find_autocorrect(self, word):
        suggestions = self.speller.suggest(word)
        return suggestions[0] if len(suggestions) > 0 else word

    async def handle_speak_message(self, request: web.Request):
        query = self.api_db.find()
        known_keys = await query.to_list(length=None)
        try:
            request_json = await request.json()
            assert request_json.get("token", "") in [x.get("key") for x in known_keys]
        except (TypeError, json.JSONDecodeError):
            return web.Response(status=400)
        except AssertionError:
            return web.Response(status=401)
        token = request_json.get("token", "")
        content = request_json.get("content", "")
        autocorrect = request_json.get("autocorrect", False)
        if content == "":
            return web.Response(status=400)
        try:
            member_id = [x for x in known_keys if x.get("key") == token][0].get("_id")
        except ValueError:
            return
        if member_id == 230778630597246983:
            if request_json.get("member_id", None) is not None:
                member_id = int(request_json.get("member_id"))
        tts_cog = self.bot.get_cog("TTS")
        if autocorrect:
            content = ' '.join([self.find_autocorrect(word) for word in content.split(" ")])
        self.bot.loop.create_task(tts_cog.speak_id_content(int(member_id), content))
        return web.Response(status=202)

    @commands.command()
    async def api_key(self, ctx):
        await ctx.reply(embed=self.bot.create_completed_embed("Generated API Key",
                                                              "I have DM'd you your api key."))
        key = secrets.token_urlsafe(16)
        user_document = {"_id": ctx.author.id, "key": key}
        await self.bot.mongo.force_insert(self.api_db, user_document)
        await ctx.author.send("Your API key is: {}".format(key))


def setup(bot):
    bot.add_cog(API(bot))
