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
            await ctx.reply(embed=self.bot.create_error_embed(f"Usage: {await self.get_prefix(ctx)}prefix new_prefix` "
                                                              f"\n\n For example, `{await self.get_prefix(ctx)}"
                                                              f"prefix $` would set the prefix to \"$\"."))
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
            await ctx.reply(embed=self.bot.create_error_embed(f"Usage: {await self.get_prefix(ctx)}setwelcomechannel"
                                                              f" <channel>"))
            ctx.kwargs["resolved"] = True

    @commands.command(
        aliases=["membercountchannel", "membercount_channel", "set_membercount_channel", "setmembercount"])
    @is_high_staff()
    async def setmembercountchannel(self, ctx, *, channel: discord.VoiceChannel):
        guild_document = await self.bot.mongo.find_by_id(self.guild_db, ctx.guild.id)
        if guild_document is None:
            await self.bot.mongo.insert_guild(ctx.guild)

        guild = await self.guild_db.find_one({"_id": ctx.guild.id})

        if guild is None:
            await self.guild_db.insert_one({"_id": ctx.guild.id, "membercountchannel": channel.id})
        else:
            await self.guild_db.update_one({"_id": ctx.guild.id}, {"$set": {"membercountchannel": channel.id}})

        await ctx.reply(embed=self.bot.create_completed_embed("membercount channel Updated!", "Set the member count"
                                                                                    f" channel to: <#{channel.id}>"))

    @setmembercountchannel.error
    async def on_membercount_error(self, ctx, error):
        if isinstance(error, commands.MissingRequiredArgument):
            await ctx.reply(embed=self.bot.create_error_embed(f"Usage: {await self.get_prefix(ctx)}"
                                                              "membercountchannel <channel>"))
            ctx.kwargs["resolved"] = True

        if isinstance(error, commands.ChannelNotFound):
            await ctx.reply(embed=self.bot.create_error_embed("Sorry, but that channel was not found"))
            ctx.kwargs["resolved"] = True

    @commands.command(aliases=["enable_levelling", "levelling"])
    @is_high_staff()
    async def enableLevelling(self, ctx, *, toggle: str):

        if toggle != "true" and toggle != "on" and toggle != "false" and toggle != "off":
            await ctx.reply(embed=self.bot.create_error_embed("Please parse in either true/on or false/off"))
            return

        guild_document = await self.bot.mongo.find_by_id(self.guild_db, ctx.guild.id)
        if guild_document is None:
            await self.bot.mongo.insert_guild(ctx.guild)

        guild = await self.guild_db.find_one({"_id": ctx.guild.id})

        if guild is None:
            await self.guild_db.insert_one({"_id": ctx.guild.id, "levelling": toggle})
        else:
            await self.guild_db.update_one({"_id": ctx.guild.id}, {"$set": {"levelling": toggle}})

        if "true" in toggle or "on" in toggle:
            await ctx.reply(embed=self.bot.create_completed_embed("Guild levelling enabled!",
                                                                  "Enabled guild levelling"))
        else:
            await ctx.reply(embed=self.bot.create_completed_embed("Guild levelling disabled!",
                                                                  "Disabled guild levelling"))

    @enableLevelling.error
    async def on_levelling_error(self, ctx, error):
        if isinstance(error, commands.MissingRequiredArgument):
            await ctx.reply(embed=self.bot.create_error_embed(f"Usage: {await self.get_prefix(ctx)}enablelevelling true"
                                                              f"/on or false/off"))
            ctx.kwargs["resolved"] = True

    @commands.command(aliases=["levelling_channel"])
    @is_high_staff()
    async def levellingchannel(self, ctx, *, channel: discord.TextChannel):
        guild_document = await self.bot.mongo.find_by_id(self.guild_db, ctx.guild.id)
        if guild_document is None:
            await self.bot.mongo.insert_guild(ctx.guild)

        guild = await self.guild_db.find_one({"_id": ctx.guild.id})

        if guild is None:
            await self.guild_db.insert_one({"_id": ctx.guild.id, "levellingchannel": channel.id})
        else:
            await self.guild_db.update_one({"_id": ctx.guild.id}, {"$set": {"levellingchannel": channel.id}})

        await ctx.reply(embed=self.bot.create_completed_embed("Levelling channel Updated!", "Set the levelling channel"
                                                                                          f" to: <#{channel.id}>"))

    @setmembercountchannel.error
    async def on_levellingchannel_error(self, ctx, error):
        if isinstance(error, commands.MissingRequiredArgument):
            await ctx.reply(embed=self.bot.create_error_embed(f"Usage: {await self.get_prefix(ctx)}"
                                                              "levellingchannel <channel>"))
            ctx.kwargs["resolved"] = True

        if isinstance(error, commands.ChannelNotFound):
            await ctx.reply(embed=self.bot.create_error_embed("Sorry, but that channel was not found"))
            ctx.kwargs["resolved"] = True

    async def get_prefix(self, ctx):
        if self.bot.mongo is None:
            return f"a{config.prefix}"
        guild_document = await self.bot.mongo.find_by_id(self.bot.mongo.client.finlay.guilds, ctx.guild.id)
        if guild_document is None or guild_document.get("prefix") is None:
            return f"{config.prefix}"
        else:
            guild_prefix = guild_document.get("prefix")
            return f"{guild_prefix}"


def setup(bot):
    bot.add_cog(guild_config(bot))
