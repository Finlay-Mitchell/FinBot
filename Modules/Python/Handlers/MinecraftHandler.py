import json
from base64 import b64decode
import requests

from Data.config import HypixelAPIKey


async def mc_uuid(username, session):
    try:
        async with session.get(
                f"https://api.mojang.com/users/profiles/minecraft/{username}") as username_raw_data:
            username_data = await username_raw_data.json()
            return username_data['id']
    except TypeError:
        return


def get_skin(uuid):
    info = requests.get(f"https://sessionserver.mojang.com/session/minecraft/profile/{uuid}").json()
    skin_info = json.loads(b64decode(info['properties'][0]['value'], validate=True).decode('utf-8'))
    skin_url = skin_info['textures']['SKIN']['url']
    return skin_url


def get_uuid(name):
    username_data = requests.get(f"https://api.mojang.com/users/profiles/minecraft/{name}").json()
    return username_data['id']


async def get_uuids(name, session):
    try:
        async with session.get(
                "https://api.mojang.com/users/profiles/minecraft/{0}".format(name)) as username_raw_data:
            username_data = await username_raw_data.json()
            return username_data['id']
    except Exception:
        print("non existent player")
        return Exception


async def bw_info(name, session):
    uuid = await get_uuids(name, session)
    if uuid == Exception:
        print("invalid Minecraft username")
        return False
    else:
        try:
            hypixel_raw_data = await session.get(
                "https://api.hypixel.net/player?key={0}&uuid={1}".format(HypixelAPIKey, uuid))
            hypixel_data = await hypixel_raw_data.json()
            try:
                kills = hypixel_data['player']['stats']['Bedwars']['kills_bedwars']
            except:
                kills = 0
            try:
                deaths = hypixel_data['player']['stats']['Bedwars']['deaths_bedwars']
            except:
                deaths = 0
            try:
                final_kills = hypixel_data['player']['stats']['Bedwars']['final_kills_bedwars']
            except KeyError:
                final_kills = 0
            try:
                final_deaths = hypixel_data['player']['stats']['Bedwars']['final_deaths_bedwars']
            except KeyError:
                final_deaths = 1
            player_level = hypixel_data['player']['achievements']['bedwars_level']
            try:
                wins = hypixel_data['player']['stats']['Bedwars']['wins_bedwars']
            except KeyError:
                wins = 0
            fkd = round((final_kills / final_deaths), 2)
            stats = {'level': player_level, 'wins': wins, 'fkd': fkd, 'kills': kills, 'deaths': deaths}
            return stats
        
        except Exception:
            print("player not found on hypixel or has not played enough games got enough kills etc")
            return False
