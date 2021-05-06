import asyncio
import datetime

from Data import config

import discord
import motor.motor_asyncio

class MongoDB:
    def __init__(self):
        self.client = motor.motor_asyncio.AsyncIOMotorClient(config.mongo_connection_uri)

    @staticmethod
    async def find_by_id(collection, search_id):
        result = await collection.find_one({"_id": search_id})
        if result is None:
            return {}
        return result

    @staticmethod
    async def force_insert(collection, document):
        if "_id" in document:
            await collection.update_one({"_id": document.get("_id")}, {"$set": document}, upsert=True)
        else:
            await collection.insert_one(document)