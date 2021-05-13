import discord
from discord.ext import commands

from main import FinBot


class chatbot(commands.Cog):
    def __init__(self, bot: FinBot):
        self.bot = bot

    # @commands.command()
    # async def chatbot(self, ctx, *, message):
    #     aiml_response = self.bot.aiml_kernel.respond(message)
    #     if aiml_response == '':
    #         await ctx.reply("I don't have a response for that, sorry.")
    #     else:
    #         await ctx.reply(aiml_response)


def setup(bot):
    bot.add_cog(chatbot(bot))
