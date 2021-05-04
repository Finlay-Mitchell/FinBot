import discord
from discord.ext import commands

import datetime
import traceback

import mysql.connector

from main import FinBot
from Data.config import data
from Checks.Permission_check import logs_perms
from Handlers.PaginationHandler import Paginator


class Modlogs(commands.Cog):
    def __init__(self, bot: FinBot):
        self.bot = bot

    @staticmethod
    def auth():
        connection = mysql.connector.connect(host=data["MySQLServer"], database=data["MySQLDatabase"],
                                             user=data["MySQLUser"], password=data["MySQLPassword"])
        return connection

    @commands.command()
    @logs_perms()
    async def modlogs(self, ctx, *, user: discord.Member = None):
        connection = None
        cursor = None
        try:
            logstr = ""
            if user is not None:
                connection = self.auth()
                cursor = connection.cursor()
                cursor.execute(f"SELECT * FROM modlogs WHERE guildId = {ctx.guild.id} AND userId = {user.id}")
                records = cursor.fetchall()

                for row in records:
                    logstr += f"**{row[1]}**\nReason: {row[3]}\nModerator: <@{row[2]}>\nDate: " \
                              f"{datetime.datetime.utcfromtimestamp(row[5]).strftime('%Y-%m-%d %H:%M:%S')}\nIndex " \
                              f"number: **{row[6]}**\n\n"

                if logstr == "":
                    await ctx.reply(embed=self.create_empty_embed(f"Modlogs for {user.display_name}({user.id})",
                                                                  "This user has no infractions! :D"))

                else:
                    paginator = Paginator(self.bot, ctx.channel, f"Total infractions: {cursor.rowcount}", logstr,
                                          450, reply_message=ctx, colour=0x00ff00)

                    await paginator.start()

            else:
                await ctx.reply(embed=self.bot.create_error_embed("Please mention a user"))

        except Exception as ex:
            print(ex)
            await ctx.reply(f"Can you like, stop writing awful code please?\n {ex}\n\n{traceback.format_exc()}")

        finally:
            if connection is not None and connection.is_connected():
                connection.close()
                if cursor is not None:
                    cursor.close()

    @modlogs.error
    async def on_mod_logs_error(self, ctx, error):
        if isinstance(error, commands.MemberNotFound):
            await ctx.reply(embed=self.bot.create_error_embed(f"Unknown member: "
                                                              f"\"{ctx.message.content.partition(' ')[2]}\""))
            ctx.kwargs["error_handled"] = True

    @staticmethod
    def create_empty_embed(title, text):
        embed = discord.Embed(title=title, description=text, colour=discord.Colour.purple(),
                              timestamp=datetime.datetime.utcnow())
        return embed


def setup(bot):
    bot.add_cog(Modlogs(bot))
