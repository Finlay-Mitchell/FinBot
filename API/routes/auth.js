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

module.exports = router;