import datetime

from sqlalchemy import (
    Column,
    String,
    Integer,
    DateTime,
    SmallInteger,
    Boolean,
    ForeignKey,
)
from sqlalchemy.orm import relationship

from . import Base
from Data import config
from ..constants import ACTION_NONE


class Game(Base):
    __tablename__ = "chessgames"

    id = Column(Integer, nullable=False, primary_key=True)

    white_id = Column(Integer, ForeignKey("chessusers.id"))
    white = relationship("User", foreign_keys=[white_id], backref="white_games", post_update=True)

    black_id = Column(Integer, ForeignKey("chessusers.id"))
    black = relationship("User", foreign_keys=[black_id], backref="black_games", post_update=True)

    pgn = Column(String(32), default="*", nullable=False)

    winner = Column(SmallInteger)
    win_reason = Column(String(32))

    action_proposed = Column(SmallInteger, default=ACTION_NONE, nullable=False)
    white_accepted_action = Column(Boolean, default=False, nullable=False)
    black_accepted_action = Column(Boolean, default=False, nullable=False)

    expiration_date = Column(DateTime, default=lambda: datetime.datetime.now() + config.EXPIRATION_TIMEDELTA)

    def __repr__(self) -> str:
        winner_str = (
            "In progress"
            if self.winner is None
            else ["White", "Black", "Draw"][self.winner]
        )
        return f"<Game id={self.id}; winner={winner_str}; expiration_date={self.expiration_date}>"
