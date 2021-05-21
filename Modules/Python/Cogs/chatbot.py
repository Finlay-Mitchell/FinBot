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

    # YES I NEED TO FIX FOR THIS COG
    # @bot.event
    # async def on_message(message):
    #     if message.author.bot or str(message.channel.id) != "840922321266016286":
    #         await bot.process_commands(message)
    #         return
    #
    #     if message.content is None:
    #         return
    #
    #     if message.content.startswith(config.prefix):
    #         return
    #
    #     elif 'shutdown' in message.content and message.author.id in config.dev_uids:
    #         await bot.logout()
    #
    #     else:
    #         aiml_response = bot.aiml_kernel.respond(message.content)
    #         if aiml_response == '':
    #             await message.channel.send("I don't have a response for that, sorry.")
    #         else:
    #             await message.channel.send(aiml_response)


def setup(bot):
    bot.add_cog(chatbot(bot))
