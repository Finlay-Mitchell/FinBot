import discord
import asyncio
# import sys
import json
import os
import datetime
import time
import subprocess
from discord.ext import commands
from Data import config
from Handlers.storageHandler import DataHelper
from traceback import format_exc, print_tb
# from Data.config import monkey_guild_id
# from mee6_py_api import API
import re


class FinBot(commands.Bot):
    def __init__(self):
        # Initialises the actual commands.Bot class
        intents = discord.Intents.all()
        intents.members = True
        super().__init__(command_prefix=config.prefix, description=config.description, loop=asyncio.new_event_loop(),
                         intents=intents, case_insensitive=True, help_command=None)
        self.guild = None
        self.error_channel = None
        self.data = DataHelper()
        self.database_handler = None

    @staticmethod
    def create_completed_embed(title, text):
        embed = discord.Embed(title=title, description=text, colour=discord.Colour.green(),
                              timestamp=datetime.datetime.utcnow())
        return embed

    @staticmethod
    def create_error_embed(text):
        embed = discord.Embed(title="Error", description=text, colour=discord.Colour.red(),
                              timestamp=datetime.datetime.utcnow())
        return embed

    @staticmethod
    def split_text(full_text):
        while len(full_text) > 2000:
            newline_indices = [m.end() for m in re.finditer("\n", full_text[:2000])]
            if len(newline_indices) == 0:
                to_send = full_text[:2000]
                full_text = full_text[2000:]
            else:
                to_send = full_text[:newline_indices[-1]]
                full_text = full_text[newline_indices[-1]:]
            yield to_send
        if len(full_text) > 0:
            yield full_text


def get_bot():
    bot = FinBot()
    data = DataHelper()

    @bot.event
    async def on_ready():
        print("Ready!")
        bot.guild = bot.get_guild(config.monkey_guild_id)
        bot.error_channel = bot.get_channel(config.error_channel_id)

        for extension_name in config.extensions:
            print("Loading Cog named {}...".format(extension_name))
            bot.load_extension("Cogs.{}".format(extension_name))
            print("Loaded cog {}!".format(extension_name))

        if os.path.exists("restart_info.json"):
            with open("restart_info.json", 'r') as file:
                channel_id, message_id, title, text, old_version_num = json.loads(file.read())

            original_msg = await bot.get_channel(channel_id).fetch_message(message_id)
            embed = bot.create_completed_embed(title, text)
            embed.add_field(name="New Version: {}".format(config.version),
                            value="Previous Version: {}".format(old_version_num))
            last_commit_message = subprocess.check_output(["git", "log", "-1", "--pretty=%s"]).decode("utf-8").strip()
            embed.set_footer(text=last_commit_message)
            await original_msg.edit(embed=embed)
            os.remove("restart_info.json")

    # noinspection PyUnusedLocal
    @bot.event
    async def on_error(method, *args, **kwargs):
        try:
            embed = discord.Embed(title=f" {FinBot.user}  experienced an error when running.",
                                  colour=discord.Colour.red())
            embed.description = format_exc()[:2000]
            print(format_exc())
            # await bot.error_channel.send(embed=embed)
            # bot.restart()

        except Exception as e:
            print("Error in sending error to discord. Error was {}".format(format_exc()))
            print("Error sending to discord was {}".format(e))

    @bot.event
    async def on_command_error(ctx, error):
        if ctx.kwargs.get("error_handled", False):
            return
        if isinstance(error, commands.CommandNotFound) or isinstance(error, commands.DisabledCommand):
            return

        if isinstance(error, commands.CheckFailure):
            await ctx.send(embed=bot.create_error_embed(
                "You don't have permission to do that, {}.".format(ctx.message.author.mention)))
            return

        try:
            embed = discord.Embed(title=f"{FinBot.user} experienced an error in a command.",
                                  colour=discord.Colour.red())
            embed.description = format_exc()[:2000]
            embed.add_field(name="Command passed error", value=error)
            embed.add_field(name="Context", value=ctx.message.content)
            print_tb(error.__traceback__)
            guild_error_channel_id = data.get("guild_error_channels", {}).get(str(ctx.guild.id), 795057163768037376)
            error_channel = bot.get_channel(guild_error_channel_id)
            await error_channel.send(embed=embed)
            await ctx.reply(embed=embed)
            # bot.restart()

        except Exception as e:
            print("Error in sending error to discord. Error was {}".format(error))
            print("Error sending to discord was {}".format(e))

    # @bot.event
    # async def on_member_join(member):
    #  mee6API = API(monkey_guild_id)
    #   Xp = await mee6API.levels.get_user_level(member.id)
    #   Level = await mee6API.levels.get_user_xp(member.id)
    #
    #   if not Xp == "None" and not Level == "None":
    #       data = {}
    #       data["Users"] = []
    #       data["Users"].append({
    #           "UserId": f"{member.id}",
    #           "Level": f"{Level}",
    #           "Xp": f"{Xp}"
    #       })
    #
    #       with open("../../bin/Debug/netcoreapp3.1/Data/LEVELS.json", "a") as outfile:
    #           json.dump(data, outfile, indent=2)

    return bot


if __name__ == '__main__':
    FinBot_bot = get_bot()

    try:
        FinBot_bot.run(config.token)

    except discord.errors.LoginFailure:
        time.sleep(18000)
