from quart import Blueprint

blueprint = Blueprint("routing", __name__)

from . import test
