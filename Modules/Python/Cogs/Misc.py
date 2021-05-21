import discord
from discord.ext import commands
from mee6_py_api import API
import motor.motor_asyncio

from io import BytesIO
import asyncio

from main import *
from Checks.Permission_check import is_developer


class Misc(commands.Cog):
    def __init__(self, bot: FinBot):
        self.bot = bot

    @commands.command()
    @is_developer()
    async def split_up(self, ctx):
        message: discord.Message = ctx.message
        if len(message.attachments) != 1:
            await ctx.reply(embed=self.bot.create_error_embed("There wasn't 1 file in that message."))
            return
        attachment = message.attachments[0]
        if attachment.filename[-4:].lower() != ".txt":
            await ctx.reply(embed=self.bot.create_error_embed("I can only do text files."))
            return
        text_file = BytesIO()
        await attachment.save(text_file)
        text_file.seek(0)
        full_text = text_file.read().decode()
        while len(full_text) > 2000:
            newline_indices = [m.end() for m in re.finditer("\n", full_text[:2000])]
            if len(newline_indices) == 0:
                to_send = full_text[:2000]
                full_text = full_text[2000:]
            else:
                to_send = full_text[:newline_indices[-1]]
                full_text = full_text[newline_indices[-1]:]
            await ctx.send(to_send)
        if len(full_text) > 0:
            await ctx.send(content=full_text)

    # Later, append results to MySql levelling database.
    @commands.command(Pass_Context=True)
    @is_developer()
    async def getlevels(self, ctx):
        mee6_api = API(ctx.message.guild.id)
        index = 0
        embed = self.bot.create_completed_embed("Getting user stats...", "generating embed....")
        msg = await ctx.reply(embed=embed)
        for users in [m for m in ctx.guild.members if not m.bot]:
            index += 1
            xp = await mee6_api.levels.get_user_level(users.id)
            level = await mee6_api.levels.get_user_xp(users.id)
            embed.set_footer(text=f"Getting user {index}/{ctx.message.guild.member_count}")
            embed.description = f"{users.name}#{users.discriminator}({users.id})\n{xp}\n{level}"
            await msg.edit(text="", embed=embed)
            await asyncio.sleep(1)
    
    # YES I NEED TO FIX FOR THIS COG
    # @bot.event
    # async def on_member_join(member):
    #  mee6API = API(monkey_guild_id)
    #   Xp = await mee6API.levels.get_user_level(member.id)
    #   Level = await mee6API.levels.get_user_xp(member.id)


def setup(bot):
    bot.add_cog(Misc(bot))
