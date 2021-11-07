from typing import Union

import discord
from discord.ext import commands

from .game_utils import get_game, update_game
from .user_utils import get_database_user, create_database_user
from .. import database
from main import FinBot
from Data import config


async def get_game_ctx(ctx: commands.Context, user_id: int, game_id: int) -> Union[None, database.Game]:
    try:
        game = get_game(user_id, game_id)
    except RuntimeError:
        if game_id is None:
            await ctx.reply(embed=FinBot.create_error_embed(f"You don't have a last game."))
        else:
            await ctx.reply(embed=FinBot.create_error_embed(f"Couldn't find that game."))

        return None

    update_game(game)
    return game


async def get_author_user_ctx(ctx: commands.Context) -> Union[None, database.User]:
    try:
        user = get_database_user(ctx.author.id)
        return user
    except RuntimeError as err:
        if config.debug:
            print(err)

        await ctx.reply(embed=FinBot.create_error_embed("Couldn't get data from database!"))
        return None


async def create_database_user_ctx(ctx: commands.Context, discord_user: discord.User) -> Union[None, database.User]:
    try:
        database_user = create_database_user(discord_user)
    except RuntimeError as err:
        if config.debug:
            print(err)  # not an error, the user already exists

        try:
            database_user = get_database_user(discord_user.id)
        except RuntimeError as err:
            if config.debug:
                print(err)

            await ctx.reply(embed=FinBot.create_error_embed("failed to find you in the database."))
            return None

    return database_user
