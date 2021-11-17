const router = require("express").Router();
const passport = require("passport");
const config = require("./../Data/config");
const fetch = require("node-fetch");
const discord = require("./../discord/bot")
const mongoose = require("mongoose");
const messages = require("./../database/schemas/messages")
const users = require("./../database/schemas/user")

var port = process.env.PORT || 3000;

router.get('/discord', passport.authenticate('discord'));

router.get('/discord/redirect', passport.authenticate('discord'), (request, response) => {
    // response.redirect("https://finbot.finlaymitchell.ml");
    response.redirect("http://localhost:3000/api/auth/")
});

router.get('/user', (request, response) => {
    response.header('Access-Control-Allow-Origin', '*');
    
    if(request.user)
    {
        response.send(request.user);
    }

    else 
    {
        response.status(401).send("unauthorised");
    }
});

router.get('/stats', async (request, response) => {
    response.header('Access-Control-Allow-Origin', '*');
    var person = {}
    if(!request.user)
    {
        person = {
            guildcount: discord.ServerCount(),
            usercount: discord.UserCount(),
            channelcount: discord.ChannelCount(),
            messagecount: await messages.count({}).exec(),
            deletedmessagecount: await messages.count({deleted: true}).exec(),
            remainingmessages: await messages.count({deleted: false}).exec()
        };
    }

    else 
    {
        person = {
            guildcount: discord.ServerCount(),
            usercount: discord.UserCount(),
            channelcount: discord.ChannelCount(),
            messagecount: await messages.count({}).exec(),
            deletedmessagecount: await messages.count({deleted: true}).exec(),
            messageCount: await messages.count({deleted: false}).exec(),
            TotaluserMessages: await messages.count({discordId: request.user.discordId}).exec(),
            userDeletedMessageCount: await messages.count({discordId: request.user.discordId, deleted: true}),
            username: request.user.discordTag,
            userId: request.user.discordId,
            //userinfo: request.user,
        };
    }

   response.status(200).send(person);
});


function contains(arr, key, val) 
{
    for (var i = 0; i < arr.length; i++) 
    {
      if (arr[i][key] === val) 
      {
          return true;
      }
    }

    return false;
}

router.get('/test/', (request, response) => {
    if(request.user)
    {
        if(contains(request.user.guilds, "id", request.query.guildid))
        {
            response.send(request.user.guilds.filter(x => x.id === request.query.guildid))
        }

        else 
        {
            response.send(`${request.user.discordTag}\https://cdn.discordapp.com/avatars/${request.user.discordId}/${request.user.avatar}.webp?size=128`);
        }
    }

    else 
    {
        response.status(401).send("Unauthorised");
    }
});


router.get('/', async ({ query }, response) => {
	const { code } = query;

    //console.log(query)
    //console.log(`http://localhost:${port}/api/auth/`)

	if (code) {
		try {
			const oauthResult = await fetch('https://discord.com/api/v8/oauth2/token', { // Change to https://discord.com/api/v8
				method: 'POST',
				body: new URLSearchParams({
					client_id: config.config.ClientId,
					client_secret: config.config.ClientSecret,
					code,
					grant_type: "authorization_code",
					redirect_uri: `http://localhost:3000/api/auth/`,
					scope: 'identify',
				}),
				headers: {
					'Content-Type': 'application/x-www-form-urlencoded',
				},
			});

			const oauthData = await oauthResult.json();

            const userResult = await fetch('https://discord.com/api/users/@me', {
                headers: {
                    authorization: `${oauthData.token_type} ${oauthData.access_token}`,
                },
            });
            
            //console.log(await userResult.json());

			//console.log(oauthData);
		} catch (error) {
			// NOTE: An unauthorized token will not throw an error;
			// it will return a 401 Unauthorized response in the try block above
			console.error(error);
		}
	}

	return response.sendFile('dash.html', { root: '.' });
});

module.exports = router;
