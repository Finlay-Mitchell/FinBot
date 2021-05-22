import re

from discord.ext import commands

from main import FinBot
from Data import config


class chatbot(commands.Cog):
    def __init__(self, bot: FinBot):
        self.bot = bot

    @commands.command()
    async def chatbot(self, ctx, *, message):
        aiml_response = self.bot.aiml_kernel.respond(message)
        if aiml_response == '':
            await ctx.reply("I don't have a response for that, sorry.")
        else:
            response = re.sub(r"[@]", "", aiml_response)
            await ctx.reply(response)

    @commands.Cog.listener()
    async def on_message(self, message):
        if message.author.bot or str(message.channel.id) != "840922321266016286":
            return

        if message.content is None:
            return

        if message.content.startswith(config.prefix):
            return

        else:
            aiml_response = self.bot.aiml_kernel.respond(message.content)
            if aiml_response == '':
                await message.channel.send("I don't have a response for that, sorry.")
            else:
                response = re.sub(r"[@]", "", aiml_response)
                await message.channel.send(response)


def setup(bot):
    bot.add_cog(chatbot(bot))
