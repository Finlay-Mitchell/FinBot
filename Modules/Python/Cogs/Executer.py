from discord.ext import commands
import discord

from main import FinBot
from Checks.permission_check import is_developer
from Data import config

class Executer(commands.Cog):
    def __init__(self, bot: FinBot):
        self.bot: FinBot = bot

    @commands.command(pass_context=True)
    @is_developer()
    async def exec(self, ctx):
        author = ctx.message.author

        tmp_dic = {}
        executing_string = """async def temp_func():
    {}
""".format(ctx.message.content.partition("\n")[2].strip("`").replace("\n", "\t\n\t"))

        if config.debug:
            print(executing_string)

        exec(executing_string, {**globals(), **locals()}, tmp_dic)

        if config.debug:
            print(tmp_dic)
            print(tmp_dic['temp_func'])

        function = tmp_dic['temp_func']
        await function()


def setup(bot):
    bot.add_cog(Executer(bot))
