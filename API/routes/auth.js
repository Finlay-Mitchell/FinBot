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

router.get('/test', (request, response) => {
    if(request.user)
    {
        response.send(`https://cdn.discordapp.com/avatars/${request.user.discordId}/${request.user.avatar}.webp?size=128`);
    }

    else 
    {
        response.status(401).send("Unauthorised");
    }
});

module.exports = router;