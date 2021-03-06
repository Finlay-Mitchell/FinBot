from typing import List

from sqlalchemy import Column, String, Integer, ForeignKey, BigInteger
from sqlalchemy.orm import relationship

from . import Base
from .game import Game


class User(Base):
    __tablename__ = "chessusers"

    id = Column(Integer, nullable=False, primary_key=True)
    discord_id = Column(BigInteger, nullable=False, unique=True, index=True)
    username = Column(String(32), index=True)
    elo = Column(Integer, nullable=False, default=1000)

    last_game_id = Column(Integer, ForeignKey("chessgames.id"))
    last_game = relationship("Game", foreign_keys=[last_game_id], post_update=True)

    # white_games (relationship defined in game.py)
    # black_games (relationship defined in game.py)

    @property
    def games(self) -> List[Game]:
        return [*self.white_games, *self.black_games]

    @property
    def ongoing_games(self) -> List[Game]:
        return [game for game in self.games if game.winner is None]

    @property
    def finished_games(self) -> List[Game]:
        return [game for game in self.games if game.winner is not None]

    def __repr__(self) -> str:
        return f"<User discord_id={self.discord_id}; elo={self.elo}>"
