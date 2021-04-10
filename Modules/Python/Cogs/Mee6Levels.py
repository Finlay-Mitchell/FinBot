# from mee6_py_api import API
# from discord.ext import commands
# from main import FinBot
# from Handlers.storageHandler import DataHelper
# from Checks.User_check import is_owner
# import json
# from Data.config import monkey_guild_id
#
# class Mee6Levels(commands.Cog):
#     def __init__(self, bot: FinBot):
#         self.bot = bot
#         self.data = DataHelper()
#
#
#     @commands.command(Pass_Context=True)
#     @is_owner()
#     async def getlevels(self, ctx, path: str):
#         mee6API = API(ctx.message.guild.id)
#         data = {}
#         data["Users"] = []
#         index = 0
#         embed = self.bot.create_completed_embed("Getting user stats...", "generating embed....")
#         msg = await ctx.reply(embed=embed)
#
#         for users in [m for m in ctx.guild.members if not m.bot]:
#             index += 1
#             Xp = await mee6API.levels.get_user_level(users.id)
#             Level = await mee6API.levels.get_user_xp(users.id)
#
#             if not Xp == "None" and not Level == "None":
#                 data["Users"].append({
#                     "UserId": f"{users.id}",
#                     "Level": f"{Level}",
#                     "Xp": f"{Xp}"
#                 })
#
#             embed.set_footer(text=f"Getting user {index}/{ctx.message.guild.member_count}")
#             embed.description = f"{users.name}#{users.discriminator}({users.id})\n{Xp}\n{Level}"
#             await msg.edit(text="", embed=embed)
#
#             with open(path, "a") as outfile:
#                 json.dump(data, outfile, indent=2)
#
# def setup(bot):
#     cog = Mee6Levels(bot)
#     bot.add_cog(cog)
