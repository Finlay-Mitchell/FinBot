from typing import Any

from main import FinBot
from discord.ext import commands
from Handlers.GeniusHandler import GeniusSearcher
from Handlers.PaginationHandler import Paginator
import discord

# TODO:
"""
1. Fix if <argument> is None: statement
"""


class Lyrics(commands.Cog):
    def __init__(self, bot: FinBot):
        self.bot: FinBot = bot
        self.Genius = GeniusSearcher(self.bot)

    @commands.command()
    async def lyrics(self, ctx, *, song):
        results = self.Genius.get_track(song)
        song_lyrics = ""

        if results is None:
            await ctx.reply(embed=self.bot.create_error_embed(f"Could not find the song `{song}`"))

        else:
            for song in results.lyrics:
                song_lyrics += song
                paginator: Any = Paginator(self.bot, ctx.channel, f"{results.artist} - {results.title}", song_lyrics,
                                           1000, reply_message=ctx, colour=discord.Colour.orange())
            await paginator.start()

    @commands.command()
    async def lyricSearch(self, ctx, *, lyrics):
        song = self.Genius.get_track_by_lyrics(lyrics)
        author = self.Genius.get_track(song['title'])

        """
        This statement below likes to not work if it can't get a title/author.
        It doesn't throw the error embed, it just gives a:  TypeError: 'NoneType' object is not subscriptable
        """

        if song['title'] is not None:
            if author.artist is None:
                await ctx.reply(embed=self.bot.create_completed_embed("Is this the song you're looking for?",
                                                                      f"{song['title']}"))
            else:
                await ctx.reply(embed=self.bot.create_completed_embed("Is this the song you're looking for?",
                                                                      f"{author.artist} - {song['title']}"))
        else:
            await ctx.reply(embed=self.bot.create_error_embed("Couldn't get song or author"))


def setup(bot):
    bot.add_cog(Lyrics(bot))
