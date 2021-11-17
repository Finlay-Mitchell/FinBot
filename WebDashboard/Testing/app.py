import asyncio

from quart import Quart, render_template

from Data import quart_config, config
from routes import blueprint
import mongo

discord = None


def create_app(mode='Development') -> Quart:
    app = Quart(__name__, template_folder="Website/HTML", static_folder="Website")
    app.register_blueprint(blueprint)
    app.config.from_object(f"Data.quart_config.{mode}")
    return app


if __name__ == "__main__":
    app = create_app()
    asyncio.get_event_loop().run_until_complete(mongo.initiate_mongo())
    asyncio.run(app.run_task(debug=config.debug))
