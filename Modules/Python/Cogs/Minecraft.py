import discord
from discord.ext import commands

import aiohttp
import asyncio
import secrets

from Handlers.MinecraftHandler import *
from main import *


class Minecraft(commands.Cog):
    def __init__(self, bot: FinBot):
        self.bot = bot

    @commands.command(aliases=["bedwars_stats", "bedwars-stats", "bedwarsstats", "bwstats", "bw-stats", "bw_info",
                               "bwinfo", "bw-info"])
    async def bw_stats(self, ctx, arg):
        async with aiohttp.ClientSession() as session:
            uuid = await mc_uuid(arg, session)
            if uuid is None:
                ctx.reply(embed=self.create_error_embed(title="Minecraft User Not found"))
                return
            else:
                random_string = secrets.token_urlsafe(16).replace("-", "")
                await ctx.reply(f"https://hypixel.thom.club/{arg}-{random_string}.png")  # stops cached messages

    @commands.command(aliases=["skin", "mc_skin", "minecraft_skin", "mcskin", "minecraftskin", "mc-skin",
                               "minecraft-skin", "get-skin", "getskin"])
    async def get_skin(self, ctx, arg):
        skin = get_skin(get_uuid(arg))

        if skin is None:
            await ctx.reply(embed=self.create_error_embed(f"Could not find user \"{arg}\""))
        else:
            await ctx.reply(skin)

    @commands.command(aliases=["bedwars_compare", "bedwarscompare", "bwcompare", "bw-compare", "bedwars-compare"])
    async def bw_compare(self, ctx, *args):
        if len(args) == 2:
            futures = []
            async with aiohttp.ClientSession() as session:
                for username in args:
                    futures.append(asyncio.create_task(bw_info(username, session)))
                compare_stats = await asyncio.gather(*futures)
            if not all(compare_stats):
                await ctx.reply(embed=self.create_error_embed("**Player either does not exist or has not played  enough"
                                                              " bedwars**"))
                return
            embed = discord.Embed(title="Bedwars Statistics Comparison", color=0x0d0d77)
            embed.set_author(name="{} Vs {}".format(args[0], args[1]))
            embed.add_field(name="Kills", value="{}".format(str(compare_stats[0]['kills'])), inline=True)
            embed.add_field(name="\u200b", value="\u200b", inline=True)
            embed.add_field(name="Kills", value="{}".format(compare_stats[1]['kills']), inline=True)
            embed.add_field(name="Deaths", value="\n {}".format(compare_stats[0]['deaths'], inline=True))
            embed.add_field(name="\u200b", value="\u200b", inline=True)
            embed.add_field(name="Deaths", value="{}".format(compare_stats[1]['deaths'], inline=True))
            embed.add_field(name="Wins ", value=("{} ".format(compare_stats[0]['wins'], inline=True)))
            embed.add_field(name="\u200b", value="\u200b", inline=True)
            embed.add_field(name="Wins ", value=("{} ".format(compare_stats[1]['wins'], inline=True)))
            embed.add_field(name="Kill/Death ratio", value="{}".format(compare_stats[0]['fkd']), inline=True)
            embed.add_field(name="\u200b", value="\u200b", inline=True)
            embed.add_field(name="Kill/Death ratio", value="{}".format(compare_stats[1]['fkd']), inline=True)
            await ctx.reply(embed=embed)

        else:
            await ctx.reply(embed=self.create_error_embed("**Not correct amount or arguments given**"))
            return


def setup(bot):
    bot.add_cog(Minecraft(bot))
