from discord.ext import commands
from Data import config
import discord

def is_staff_backend(member):
    return (
            member.guild_permissions.administrator or member.id == config.owner_id or member.guild_permissions.manage_guild or member.guild_permissions.manage_roles or member.guild_permissions.manage_channels)

def is_staff():
    async def predicate(ctx):
        member: discord.Member = ctx.message.author
        return is_staff_backend(member)

    return commands.check(predicate)

def is_high_staff():
    async def predicate(ctx: commands.Context):
        member: discord.Member = ctx.message.author
        return (any([True for role in member.roles if role.id in config.high_staff]) or member.guild_permissions.administrator or member.id == config.owner_id)

    return commands.check(predicate)