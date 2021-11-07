from typing import TypeVar

from sqlalchemy.exc import DatabaseError

from . import session
from Data import config

T = TypeVar("T")


def add_to_database(obj: T) -> None:
    try:
        session.add(obj)
        session.commit()
    except DatabaseError as err:
        if config.debug:
            print(err)
        session.rollback()
