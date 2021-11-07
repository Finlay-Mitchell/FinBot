from main import FinBot
from .chess.database import *
from .chess.utils import *
from .chess.constants import *


class chess_cog(commands.Cog):
    def __init__(self, bot: FinBot) -> None:
        self.bot: FinBot = bot

    async def status_func(self, ctx: commands.Context, game_id: int = None, game: Game = None) -> None:
        if game is None:
            game = await get_game_ctx(ctx, ctx.author.id, game_id)
            if game is None:  # check the Game object for validity
                return
        try:
            status_str, img = get_game_status(self.bot, game)
        except RuntimeError as err:
            await ctx.reply(embed=self.bot.create_error_embed("Failed to get status for that game."))
            return

        message = await ctx.reply(file=img)
        embed = self.bot.create_completed_embed("Started new game", status_str)
        embed.set_image(url=message.attachments[0].url)
        await message.delete()
        await ctx.reply(embed=embed)

    @commands.command()
    async def status(self, ctx: commands.Context, game_id: int = None) -> None:
        await self.status_func(ctx, game_id=game_id)

    @commands.command()
    async def accept(self, ctx, game_id: int = None) -> None:
        game = await get_game_ctx(ctx, ctx.author.id, game_id)
        if game is None:
            return

        user = await get_author_user_ctx(ctx)
        if user is None:
            return

        if not is_player(game, user):
            await ctx.reply(embed=self.bot.create_error_embed(f"{ctx.author.mention} tried to illegaly accept in game "
                                                              f"#{game.id}"))
            return

        if game.white_accepted_action != game.black_accepted_action:
            try:
                handle_action_accept(user, game)
            except RuntimeError as err:
                await ctx.reply(embed=self.bot.create_error_embed("You can't accept your own actions"))
                return

            user.last_game = game
            add_to_database(user)
            await self.status_func(ctx, game=game)
        else:
            await ctx.reply(embed=self.bot.create_error_embed("There is nothing to accept for this game"))

    @commands.command()
    async def move(self, ctx, san_move: str, game_id: int = None) -> None:
        game = await get_game_ctx(ctx, ctx.author.id, game_id)
        if game is None:
            return

        if game.winner is not None:
            await ctx.reply(embed=self.bot.create_completed_embed("Game over", f"{ctx.author.mention}, the game is "
                                                                               f"over!"))
            return

        user = await get_author_user_ctx(ctx)
        if user is None:
            return

        if not is_player(game, user):
            await ctx.reply(embed=self.bot.create_error_embed("You can't play this game"))
            return

        try:
            handle_turn_check(user, game)
        except RuntimeError as err:
            await ctx.reply(embed=self.bot.create_error_embed("It is not your turn."))
            return

        try:
            handle_move(game, san_move)
        except ValueError as err:
            await ctx.reply(embed=self.bot.create_error_embed(f"{san_move} is not a valid SAN move in this game."))
            return

        update_game(game, recalculate_expiration_date=True, reset_action=True)
        user.last_game = game
        add_to_database(user)
        await self.status_func(ctx, game=game)

    @commands.command()
    async def offer(self, ctx, action: str, game_id: int = None) -> None:
        game = await get_game_ctx(ctx, ctx.author.id, game_id)
        if game is None:
            return

        if game.winner is not None:
            await ctx.reply(embed=self.bot.create_error_embed("The game is over."))
            return

        user = await get_author_user_ctx(ctx)
        if user is None:
            return

        if not is_player(game, user):
            await ctx.reply(embed=self.bot.create_error_embed("You can't offer an action in this game."))
            return

        action_type = OFFERABLE_ACTIONS.get(action.upper())

        if action_type is None:
            await ctx.reply(embed=self.bot.create_error_embed("This action does not exist."))
            return

        if game.action_proposed == action_type:
            await ctx.reply(embed=self.bot.create_error_embed("This action has already been offered in this game."))
            return

        try:
            handle_action_offer(user, game, action_type)
        except RuntimeError as err:
            await ctx.reply(embed=self.bot.create_error_embed("You can't offer an action in this game."))
            return

        update_game(game)
        user.last_game = game
        add_to_database(user)
        await self.status_func(ctx, game=game)

    @commands.command()
    async def play(self, ctx, user: discord.Member) -> None:
        white = await create_database_user_ctx(ctx, ctx.author)
        black = await create_database_user_ctx(ctx, user)

        if white is None or black is None:
            return

        if white == black:
            await ctx.reply(embed=self.bot.create_error_embed("You can't play against yourself."))
            return

        game = Game(white=white, black=black)
        add_to_database(game)
        white.last_game = game
        add_to_database(white)
        black.last_game = game
        add_to_database(black)

        await self.status_func(ctx, game=game)

    @commands.command()
    async def concede(self, ctx, game_id: int = None) -> None:
        game = await get_game_ctx(ctx, ctx.author, game_id)
        if game is None:
            return

        if game.winner is not None:
            await ctx.reply(embed=self.bot.create_error_embed("The game is over."))
            return

        user = await get_author_user_ctx(ctx)
        if user is None:
            return

        if not is_player(game, user):
            await ctx.reply(embed=self.bot.create_error_embed("You can't play this game."))
            return

        update_game(game, concede_side=which_player(game, user))
        user.last_game = game
        add_to_database(user)
        await self.status_func(ctx, game=game)

    """---------------------------------------------------------------------------------------"""

    @commands.command()
    async def games(self, ctx, all: str = "") -> None:
        user = await get_author_user_ctx(ctx)
        if user is None:
            return

        games = user.ongoing_games
        if all.lower() == "all":
            games = [*games, *user.finished_games]

        outputs = []

        for game in games:
            vs_line = get_vs_line(self.bot, game)
            status = "Ongoing" if game.winner is None else "Finished"
            game_output = f"**[{game.id}]** {vs_line}\t*Status: {status}*"
            outputs.append(game_output)

        output = "\n".join(outputs)
        if output == "":
            await ctx.reply(embed=self.bot.create_error_embed("You don't have any games."))
        else:
            await ctx.reply(embed=self.bot.create_completed_embed("Your games:", output))

    @commands.command()
    async def elo(self, ctx) -> None:
        user = await get_author_user_ctx(ctx)
        if user is None:
            return
        if user.elo is None:
            await ctx.reply(embed=self.bot.create_error_embed("You do not have an elo rating."))
            return

        await ctx.reply(embed=self.bot.create_completed_embed(f"{ctx.author}'s elo rating", f"{user.elo}"))

    @commands.command()
    async def leaderboard(self, ctx, top: int = 10) -> None:
        if top < 3 or top > 50:
            await ctx.reply(embed=self.bot.create_error_embed("Please choose another value of \"Top *N*\""))
            return

        outputs = []
        users = (database.session.query(database.User).order_by(database.User.elo.desc()).limit(top))
        for i, user in enumerate(users):
            username = user.username or user.discord_id
            user_output = f"**[{i + 1}]** {username} - {user.elo} elo."
            outputs.append(user_output)

        output = "\n".join(outputs)
        if output == "":
            await ctx.reply(embed=self.bot.create_error_embed("There aren't any players on the leaderboard yet."))
        else:
            await ctx.reply(embed=self.bot.create_completed_embed("__Global leaderboard__", f"{output}"))

    """---------------------------------------------------------------------------------------"""

    async def cog_before_invoke(self, ctx) -> None:
        update_ongoing_games()

    async def cog_command_error(self, ctx, error) -> None:
        await ctx.reply(embed=self.bot.create_error_embed(f"{error}"))


def setup(bot):
    bot.add_cog(chess_cog(bot))
