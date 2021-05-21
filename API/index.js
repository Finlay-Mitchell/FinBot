var express = require("express");
var discord = require(`./discord/bot.js`);

var app = express();
app.use(express.json())

app.get('/', (request, response) => {
    response.status(200).send(`${discord.lastMessage()}`)
});

app.get('/status', (request, response) => {
    response.status(200).send('test')
});
// This is REQUIRED for IISNODE to work
// app.listen(process.env.PORT, () => {
    app.listen("8080", () => {

console.log("listening");
});