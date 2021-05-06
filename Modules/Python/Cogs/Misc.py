import discord
from discord.ext import commands
from mee6_py_api import API

from io import BytesIO
import asyncio

from main import *
from Checks.Permission_check import is_developer

import motor.motor_asyncio


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

    @commands.command(Pass_Context=True)
    @is_developer()
    async def getlevels(self, ctx):
        mee6API = API(ctx.message.guild.id)
        index = 0
        embed = self.bot.create_completed_embed("Getting user stats...", "generating embed....")
        msg = await ctx.reply(embed=embed)
        for users in [m for m in ctx.guild.members if not m.bot]:
            index += 1
            Xp = await mee6API.levels.get_user_level(users.id)
            Level = await mee6API.levels.get_user_xp(users.id)
            # if not Xp == "None" and not Level == "None":
            #     data["Users"].append({
            #         "UserId": f"{users.id}",
            #         "Level": f"{Level}",
            #         "Xp": f"{Xp}"
            #     })
            embed.set_footer(text=f"Getting user {index}/{ctx.message.guild.member_count}")
            embed.description = f"{users.name}#{users.discriminator}({users.id})\n{Xp}\n{Level}"
            await msg.edit(text="", embed=embed)
            await asyncio.sleep(1)
            # with open(path, "a") as outfile:
            #     json.dump(data, outfile, indent=2)

    @commands.command()
    @is_developer()
    async def test(self, ctx):
        async for message in self.bot.mongo.client.discord.messages.find():
            print(message)

    @commands.command()
    @is_developer()
    async def testing(self, ctx):
        collections = await self.bot.mongo.client.finlay.list_collections()
        await ctx.reply([x for x in collections])

    @commands.command()
    @is_developer()
    async def dantest(self, ctx):
        cont = ctx.message.content[len(config.prefix):]
        await ctx.send(cont[::-1])


def setup(bot):
    bot.add_cog(Misc(bot))
