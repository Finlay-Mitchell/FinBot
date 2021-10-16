from discord.ext import commands
from main import MongoDB
from Data import config


def speak_changer_check():
    """
    Checks whether the user has permissions to change the speaker list.
    :return: Whether the user has the required permissions.
    """
    async def predicate(ctx):
        if ctx.author.guild_permissions.administrator or ctx.author.id in config.dev_uids:
            return True
        db = MongoDB()
        old_member = await db.client.finlay.tts.perms.find_one({"_id": {"user_id": ctx.author.id,
                                                                        "guild_id": ctx.guild.id}})
        return old_member is not None
    return commands.check(predicate)
