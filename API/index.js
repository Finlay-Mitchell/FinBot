var express = require("express");
var discord = require(`./discord/bot.js`);
var port = process.env.PORT || 3000;
var app = express();
app.use(express.json())

app.get('/', (request, response) => {
    response.status(200).send(`${discord.lastmessage()}`)
});

app.get('/status', (request, response) => {
    response.status(200).send('Working')
});

app.get('/botstats', (request, response) => {
     response.header('Access-Control-Allow-Origin', '*');
     var person = {
        guildcount: discord.ServerCount(),
        usercount: discord.UserCount(),
        channelcount: discord.ChannelCount()
      };
    response.status(200).send(person);
});

// This is REQUIRED for IISNODE to work
app.listen(port, () => { 
    console.log("listening");
});