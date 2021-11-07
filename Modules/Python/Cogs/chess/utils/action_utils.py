from .game_utils import which_player, update_game
from .. import database, constants
from Data import config


def handle_action_offer(user: database.User, game: database.Game, action: int) -> None:
    if action not in [
        constants.ACTION_NONE,
        constants.ACTION_DRAW,
        constants.ACTION_UNDO,
    ]:
        if config.debug:
            raise RuntimeError("Impossible action")

    try:
        player = which_player(game, user)
    except RuntimeError as err:
        raise err

    if player == constants.WHITE:
        game.white_accepted_action, game.black_accepted_action = True, False
    else:
        game.white_accepted_action, game.black_accepted_action = False, True

    game.action_proposed = action
    database.add_to_database(game)


def handle_action_accept(user: database.User, game: database.Game) -> None:
    if (game.white_accepted_action and game.white == user) or (game.black_accepted_action and game.black == user):
        if config.debug:
            raise RuntimeError("Can't accept your own action offer")
    elif game.white == user:
        game.white_accepted_action = True
    elif game.black == user:
        game.black_accepted_action = True

    update_game(game, reset_action=True)
