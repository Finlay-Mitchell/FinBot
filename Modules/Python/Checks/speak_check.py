from discord.ext import commands
from main import MongoDB


def speak_changer_check():
    async def predicate(ctx):
        if ctx.author.guild_permissions.administrator:
            return True
        db = MongoDB()
        old_member = await db.client.finlay.tts.perms.find_one({"_id": {"user_id": ctx.author.id, "guild_id": ctx.guild.id}})
        return old_member is not None
    return commands.check(predicate)
