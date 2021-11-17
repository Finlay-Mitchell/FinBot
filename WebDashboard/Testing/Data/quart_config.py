from Data import config


class Config:
    DEBUG = False
    TESTING = False
    SECRET_KEY = "secret"


class Development(Config):
    DEBUG = config.debug
    TESTING = config.testing


class Production(Config):
    SECRET_KEY = "Test"
