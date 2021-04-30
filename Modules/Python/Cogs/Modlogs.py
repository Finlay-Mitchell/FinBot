import discord
from discord.ext import commands

import datetime

import mysql.connector

from main import FinBot
from Data.config import data
from Checks.Permission_check import is_staff
from Handlers.PaginationHandler import Paginator


class Modlogs(commands.Cog):
    def __init__(self, bot: FinBot):
        self.bot = bot

    def auth(self):
        connection = mysql.connector.connect(host=data["MySQLServer"], database=data["MySQLDatabase"],
                                             user=data["MySQLUser"], password=data["MySQLPassword"])
        return connection

    @commands.command()
    @is_staff()
    async def modlogs(self, ctx, user: discord.Member=None):
        try:
            if user is not None:
                connection = self.auth()
                cursor = connection.cursor()
                cursor.execute(f"SELECT * FROM modlogs WHERE guildId = {ctx.guild.id} AND userId = {user.id}")
                records = cursor.fetchall()

                embed = discord.Embed(title=f"Infractions", description=f"Total "
                f"infractions: {cursor.rowcount}", color=0x00ff00)

                logstr = ""
                run = True

                for row in records:
                    prntstr = ""

                    prntstr= f"**{row[1]}**\nReason: {row[3]}\nModerator: <@{row[2]}>\nDate: " \
                              f"{datetime.datetime.utcfromtimestamp(row[5]).strftime('%Y-%m-%d %H:%M:%S')}\nIndex " \
                              f"number: **{row[6]}**\n\n"

                    logstr += prntstr

                    if logstr is None:

                        run = False

                    paginator = Paginator(self.bot, ctx.channel, f"Total infractions: {cursor.rowcount}", logstr, 450,
                                          reply_message=ctx, colour=0x00ff00)

                if run:
                    await paginator.start()
                else:
                    await ctx.reply("empty")
                # await ctx.reply(embed=embed)

        except:

            print("oh no")

        finally:
            if connection.is_connected():
                connection.close()
                cursor.close()


def setup(bot):
    bot.add_cog(Modlogs(bot))
