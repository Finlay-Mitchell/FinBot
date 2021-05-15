import discord
from discord.ext import commands

from Data import config
from Checks.Permission_check import is_high_staff
from main import FinBot


class guild_config(commands.Cog):
    def __init__(self, bot: FinBot):
        self.bot = bot
        self.guild_db = self.bot.mongo.client.finlay.guilds

    @commands.command()
    @is_high_staff()
    async def prefix(self, ctx, *, new_prefix):
        guild_document = await self.bot.mongo.find_by_id(self.guild_db, ctx.guild.id)
        if guild_document is None:
            await self.bot.mongo.insert_guild(ctx.guild)

        guild = await self.guild_db.find_one({"_id": ctx.guild.id})

        if guild is None:
            await self.guild_db.insert_one({"_id": ctx.guild.id, "prefix": new_prefix})
        else:
            await self.guild_db.update_one({"_id": ctx.guild.id}, {"$set": {"prefix": new_prefix}})

        await ctx.reply(embed=self.bot.create_completed_embed("Prefix Updated!", f"Set prefix in this guild to: "
                                                                                 f"{new_prefix}"))

    @prefix.error
    async def on_prefix_error(self, ctx, error):
        if isinstance(error, commands.MissingRequiredArgument):
            await ctx.reply(embed=self.bot.create_error_embed(f"Usage: {config.prefix}prefix new_prefix` \n\n"
                                                              f"For example, `{config.prefix}prefix $` would set the "
                                                              f"prefix to \"$\"."))
            ctx.kwargs["resolved"] = True

    @commands.command(aliases=["set_welcome_channel", "welcomechannel", "welcome_channel", "welcomemessages",
                               "welcome_messages"])
    @is_high_staff()
    async def setwelcomechannel(self, ctx, *, channel: discord.TextChannel):
        guild_document = await self.bot.mongo.find_by_id(self.guild_db, ctx.guild.id)
        if guild_document is None:
            await self.bot.mongo.insert_guild(ctx.guild)

        guild = await self.guild_db.find_one({"_id": ctx.guild.id})

        if guild is None:
            await self.guild_db.insert_one({"_id": ctx.guild.id, "welcomechannel": channel.id})
        else:
            await self.guild_db.update_one({"_id": ctx.guild.id}, {"$set": {"welcomechannel": channel.id}})

        await ctx.reply(embed=self.bot.create_completed_embed("Welcome channel Updated!", f"Set the welcome channel to:"
                                                                                          f" <#{channel.id}>"))

    @setwelcomechannel.error
    async def on_welcome_error(self, ctx, error):
        if isinstance(error, commands.MissingRequiredArgument):
            await ctx.reply(embed=self.bot.create_error_embed(f"Usage: {config.prefix}setwelcomechannel <channel>"))
            ctx.kwargs["resolved"] = True


def setup(bot):
    bot.add_cog(guild_config(bot))
