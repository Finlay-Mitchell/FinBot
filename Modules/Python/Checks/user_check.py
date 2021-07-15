from Data import config
from discord.ext import commands


def is_owner():
    """
    Checks if user calling the command is the bot owner.
    :return: Whether the command caller is the bot owner.
    """
    async def predicate(ctx):
        return ctx.message.author.id == config.owner_id

    return commands.check(predicate)
