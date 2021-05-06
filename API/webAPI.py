import json
import os

from aiohttp import web


routes = web.RouteTableDef()

@routes.get('/')
async def default(request: web.Request):
    return web.Response(status=418, text="testing")

@routes.post("/test/:test")
async def restart(request: web.Request):
    return web.Response(status=202, text="test")


@routes.get("/test")
async def update(request: web.Request):

    return web.Response(status=200, text="testing")


if __name__ == '__main__':
    app = web.Application()
    app.add_routes(routes)
    web.run_app(app, port=80)
