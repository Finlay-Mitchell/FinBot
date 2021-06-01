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

// This is REQUIRED for IISNODE to work
app.listen(port, () => { 
    console.log("listening");
});