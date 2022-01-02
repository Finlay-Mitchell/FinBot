import asyncio

import motor.motor_asyncio

from Data import config


async def initiate_mongo():
    config.mongo = motor.motor_asyncio.AsyncIOMotorClient(config.mongo_connection_uri, serverSelectionTimeoutMS=5000)

    try:
        print(await config.mongo.server_info())
    except Exception as ex:
        print(f"Unable to connect to MongoDB server.\n{ex}")


async def find_by_id(collection, search_id):
    result = await collection.find_one({"_id": search_id})

    if result is None:
        return {}

    return result


async def get_guild_prefix(guild_id) -> str:
    test = motor.motor_asyncio.AsyncIOMotorClient(config.mongo_connection_uri, serverSelectionTimeoutMS=5000)

    if test is None:
        return config.prefix

    guild_document = await find_by_id(test.finlay.guilds, guild_id)
    print(guild_document)

    if guild_document is None or guild_document.get("prefix") is None:

        return config.prefix
    else:
        return guild_document.get("prefix")
