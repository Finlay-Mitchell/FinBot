const router = require("express").Router();
const passport = require("passport");

router.get('/discord', passport.authenticate('discord'));

router.get('/discord/redirect', passport.authenticate('discord'), (request, response) => {
    response.redirect("https://finbot.finlaymitchell.ml");
});

router.get('/', (request, response) => {
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

module.exports = router;