var express = require("express");
var discord = require(`./discord/bot.js`);
var port = process.env.PORT || 3000;
var app = express();
app.use(express.json())

app.get('/', (request, response) => {
    response.status(200).send(`${discord.lastmessage()}`)
});

app.post('/rankcard:user', (request, response) => {
    response.status(200).send(`${discord.rankcard()}`);
});

app.get('/status', (request, response) => {
    response.status(200).send('test')
});

app.post('/rankcard', (request, response) => {
    const {userId} = request.body;
    const {level} = request.body;
    const {XP} = request.body;
    const {reqXP} = request.body;
    const {chanId} = request.body;
    response.status(200).send(`${discord.rankcard(userId, level, XP, reqXP, chanId)}`);
});
// This is REQUIRED for IISNODE to work
app.listen(port, () => {
    // app.listen("8080", () => {

console.log("listening");
});