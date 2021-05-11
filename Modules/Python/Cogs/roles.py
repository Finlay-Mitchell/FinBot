# import discord
# from discord.ext import commands
#
# from typing import Optional
#
# from Checks.Permission_check import is_staff
# from main import FinBot
#
#
# class roles(commands.Cog):
#     def __init__(self, bot: FinBot):
#         self.bot = bot
#         self.rejoin_guilds = self.bot.mongo.client.finlay.rejoin_guilds
#         self.rejoin_logs = self.bot.mongo.client.finlay.rejoin_logs
#         self.role_assign = self.bot.mongo.client.finlay.role_assign
#
#     @commands.command()
#     @is_staff()
#     async def add_reaction_role(self, ctx, embed_message_id: int, role: discord.Role):
#         assign_document = await self.role_assign.find_one({"_id": embed_message_id})
#         if assign_document is None:
#             await ctx.reply(embed=self.bot.create_error_embed("There is no known role assign embed!"))
#             return
#         roles = assign_document.get("roles", [])
#         emoji, sent = await self.get_emoji(ctx)
#         roles[str(emoji)] = role.id
#         await self.role_assign.update_one({"_id": embed_message_id}, {"$set": {"roles": roles}})
#         message_id = assign_document.get("_id")
#         channel_id = assign_document.get("channel_id")
#         channel = self.bot.get_channel(channel_id)
#         try:
#             message: discord.Message = await channel.fetch_message(message_id)
#         except discord.errors.NotFound:
#             await sent.edit(embed=self.bot.create_error_embed("I couldn't find the message! Was it deleted?"))
#             return
#         await message.add_reaction(emoji)
#         await sent.edit(embed=self.bot.create_completed_embed("Set Reaction Role",
#                                                               f"Set emoji {str(emoji)} as the reaction for "
#                                                               f"{role.mention}"))
#
#     @commands.command()
#     async def remove_reaction_role(self, ctx, embed_message_id: int):
#         assign_document = await self.role_assign.find_one({"_id": embed_message_id})
#         if assign_document is None:
#             await ctx.reply(embed=self.bot.create_error_embed("There is no known role assign embed!"))
#             return
#         roles = assign_document.get("roles", [])
#         emoji, sent = await self.get_emoji(ctx)
#         if str(emoji) in roles:
#             del roles[str(emoji)]
#         else:
#             await sent.edit(embed=self.bot.create_error_embed("That emoji was not set."))
#         await self.role_assign.update_one({"_id": embed_message_id}, {"$set": {"roles": roles}})
#         await sent.edi(embed=self.bot.create_completed_embed("Removed Reaction Role",
#                                                              f"Removed the reaction role "
#                                                              f"associated with {str(emoji)}"))
#
#     @commands.Cog.listener()
#     async def on_raw_reaction_add(self, payload: discord.RawReactionActionEvent):
#         message_id = payload.message_id
#         assign_document = await self.role_assign.find_one({"_id": message_id})
#         if assign_document is None:
#             return
#         guild = self.bot.get_guild(payload.guild_id)
#         roles = assign_document.get("roles")
#         if str(payload.emoji) not in roles:
#             return
#         role_id = roles.get(str(payload.emoji))
#         role = guild.get_role(role_id)
#         await payload.member.add_roles(role)
#
#     @commands.Cog.listener()
#     async def on_raw_reaction_remove(self, payload: discord.RawReactionActionEvent):
#         message_id = payload.message_id
#         assign_document = await self.role_assign.find_one({"_id": message_id})
#         if assign_document is None:
#             return
#         guild: discord.Guild = self.bot.get_guild(payload.guild_id)
#         roles = assign_document.get("roles")
#         if str(payload.emoji) not in roles:
#             return
#         role_id = roles.get(str(payload.emoji))
#         role = guild.get_role(role_id)
#         member = await guild.fetch_member(payload.user_id)
#         await member.remove_roles(role)
#
#     @commands.Cog.listener()
#     async def on_raw_message_delete(self, payload: discord.RawMessageDeleteEvent):
#         message_id = payload.message_id
#         assign_document = await self.role_assign.find_one({"_id": message_id})
#         if assign_document is None:
#             return
#         await self.role_assign.delete_one({"_id": message_id})
#
#     @commands.command()
#     @is_staff()
#     async def set_role_reapply(self, ctx, max_role: Optional[discord.Role]):
#         if max_role is not None:
#             guild_document = {"_id": ctx.guild.id, "max_role": max_role.id}
#         else:
#             guild_document = {"_id": ctx.guild.id, "max_role": None}
#         await self.bot.mongo.force_insert(self.rejoin_guilds, guild_document)
#         if max_role is None:
#             await ctx.reply(embed=self.bot.create_completed_embed("Guild Added", "The guild has been set-up for role "
#                                                                                  "re-application."))
#         else:
#             await ctx.reply(embed=self.bot.create_completed_embed("Guild Added", "The guild has been set-up for role "
#                                                                                  "re-application for all roles below "
#                                                                                  f"{max_role.mention}"))
#
#     @commands.command()
#     @is_staff()
#     async def unset_role_reapply(self, ctx):
#         await self.rejoin_guilds.delete_one({"_id": ctx.guild.id})
#         await ctx.reply(embed=self.bot.create_completed_embed("Guild Added", "The guild has been removed from role "
#                                                                              "re-application."))
#
#     @commands.Cog.listener()
#     async def on_member_remove(self, member):
#         member_role_document = {"_id": {"user_id": member.id, "guild_id": member.guild.id},
#                                 "roles": [role.id for role in member.roles if role != member.guild.default_role]}
#         await self.bot.mongo.force_insert(self.rejoin_logs, member_role_document)
#
#     @commands.Cog.listener()
#     async def on_member_join(self, member: discord.Member):
#         guild_id = member.guild.id
#         guild_doc = await self.rejoin_guilds.find_one({"_id": guild_id})
#         if guild_doc is None:
#             return
#         max_role_id = guild_doc.get("max_role", None)
#         if max_role_id is None:
#             check = lambda x: True
#         else:
#             max_role = member.guild.get_role(max_role_id)
#             check = lambda x: x < max_role
#         member_role_document = await self.rejoin_logs.find_one({"_id": {"user_id": member.id,
#                                                                         "guild_id": member.guild.id}})
#         if member_role_document is None:
#             return
#         valid_roles = []
#         for role_id in member_role_document.get("roles"):
#             role = member.guild.get_role(role_id)
#             if role is None or role == member.guild.default_role:
#                 continue
#             if check(role):
#                 valid_roles.append(role)
#         try:
#             await member.add_roles(*valid_roles)
#         except discord.errors.Forbidden:
#             for role in valid_roles:
#                 try:
#                     await member.add_roles(role)
#                 except discord.errors.Forbidden:
#                     print(f"I am forbidden from adding role {role.name} in guild {member.guild.name} to {member.name}")
#
#
# def setup(bot):
#     bot.add_cog(roles(bot))
