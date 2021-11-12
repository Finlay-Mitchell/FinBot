import asyncio
import time

import discord
from discord.ext import commands, ipc
import motor
from motor import motor_asyncio

import mongo
from Data import config


class FinBotApi(commands.Bot):
    def __init__(self):
        super().__init__(command_prefix=self.determine_prefix, description="API", loop=asyncio.new_event_loop(),
                         intents=discord.Intents.default(), case_insensitive=True, help_command=None)
        self.ipc = ipc.Server(self, secret_key="Swas")

    async def determine_prefix(self, bot, message):
        test = motor.motor_asyncio.AsyncIOMotorClient(config.mongo_connection_uri, serverSelectionTimeoutMS=5000)
        """
        Gets the prefix for the guild when a message is sent.
        :param bot: The bot.
        :param message: The message which was sent.
        :return: Returns the prefix for the guild.
        """
        if not hasattr(message, "guild") or message.guild is None:
            return ""
        if test is None:
            return f"a{config.prefix}"
        guild_document = await mongo.find_by_id(test.client.finlay.guilds, message.guild.id)
        if guild_document is None or guild_document.get("prefix") is None:
            return commands.when_mentioned_or(config.prefix)(bot, message)
        else:
            guild_prefix = guild_document.get("prefix")
            return commands.when_mentioned_or(guild_prefix)(bot, message)

    async def on_ready(self):
        """Called upon the READY event"""
        print("Bot is ready.")

    async def on_ipc_ready(self):
        """Called upon the IPC Server being ready"""
        print("Ipc server is ready.")

    async def on_ipc_error(self, endpoint, error):
        """Called upon an error being raised within an IPC route"""
        print(endpoint, "raised", error)


def get_bot():
    bot = FinBotApi()

    @bot.ipc.route()
    async def get_guild_count(data):
        return len(bot.guilds)

    @bot.ipc.route()
    async def get_guild_ids(data):
        final = []
        for guild in bot.guilds:
            final.append(guild.id)
        return final  # returns the guild ids to the client

    @bot.ipc.route()
    async def get_guild(data):
        guild = bot.get_guild(data.guild_id)
        if guild is None:
            return None

        guild_data = {
            "name": guild.name,
            "id": guild.id,
            "prefix": await mongo.get_guild_prefix(guild.id)
        }

        return guild_data

    @bot.command()
    async def prefix(ctx, new_prefix: str) -> None:
        test = motor.motor_asyncio.AsyncIOMotorClient(config.mongo_connection_uri, serverSelectionTimeoutMS=5000)

        test.client.finlay.guilds.update_one({"_id": ctx.guild.id}, {"$set": {"prefix": new_prefix}}, upsert=True)

        await ctx.send("Hi")

    return bot


APIBot: FinBotApi = get_bot()

try:
    APIBot.ipc.start()
    APIBot.run(config.token)
except discord.errors.LoginFailure:
    time.sleep(18000)
except discord.errors.ConnectionClosed:
    if not asyncio.get_event_loop().is_closed():
        asyncio.get_event_loop().close()
    print("Connection closed, exiting...")
    exit()
