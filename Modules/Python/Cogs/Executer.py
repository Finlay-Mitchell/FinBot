from discord.ext import commands
from main import FinBot

class Executer(commands.Cog):
    def __init__(self, bot: FinBot):
        self.bot: FinBot = bot

    @commands.command(pass_context=True)
    async def exec(self, ctx):
        author = ctx.message.author

        if author.id != 305797476290527235:
            return

        else:
            tmp_dic = {}
            executing_string = """async def temp_func():
        {}
    """.format(ctx.message.content.partition("\n")[2].strip("`").replace("\n", "    \n    "))
            print(executing_string)
            exec(executing_string, {**globals(), **locals()}, tmp_dic)
            print(tmp_dic)
            print(tmp_dic['temp_func'])
            function = tmp_dic['temp_func']
            await function()

def setup(bot):
    cog = Executer(bot)
    bot.add_cog(cog)