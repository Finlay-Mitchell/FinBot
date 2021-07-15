from discord.ext import commands
from Data import config
import discord


def is_staff_backend(member):
    """
    Checks whether a member has staff permissions.
    :param member: The member to check.
    :return: Whether the user has staff permissions.
    """
    return (member.guild_permissions.administrator or member.guild_permissions.manage_guild or
            member.guild_permissions.manage_roles or member.guild_permissions.manage_channels or
            member.id in config.dev_uids)


def is_staff():
    """
    Checks whether user is staff.
    """
    async def predicate(ctx):
        return is_staff_backend(ctx.message.author)

    return commands.check(predicate)


def is_high_staff():
    """
    Checks whether the user is a high-level member of staff.
    """
    async def predicate(ctx: commands.Context):
        member: discord.Member = ctx.message.author
        return member.guild_permissions.administrator or member.id in config.dev_uids

    return commands.check(predicate)


def logs_perms():
    """
    Checks whether the user has permissions to view the moderation logs.
    """
    async def predicate(ctx: commands.Context):
        member: discord.Member = ctx.message.author
        return member.guild_permissions.manage_messages or member.id == config.owner_id or member.id in config.dev_uids
    
    return commands.check(predicate)


def is_developer():
    """
    Checks whether the user is a registered developer of the bot.
    """
    async def predicate(ctx: commands.Context):
        member: discord.Member = ctx.message.author
        return member.id in config.dev_uids
    
    return commands.check(predicate)
