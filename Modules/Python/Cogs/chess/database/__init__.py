from sqlalchemy import create_engine
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker

from Data import config

engine = create_engine(config.CHESS_DB_PATH)  # , echo=True)
Base = declarative_base()

from .game import *
from .user import *

Base.metadata.create_all(engine)
Session = sessionmaker(bind=engine)
session = Session()

from .add_to_database import *
