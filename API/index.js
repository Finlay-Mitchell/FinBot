var express = require("express");
var discord = require(`./discord/bot.js`);
const config = require("./Data/config");
require("./strategies/discord");
const passport = require("passport");
const mongoose = require("mongoose");
const session = require('express-session');
const Store = require("connect-mongo")
const routes = require("./routes");
var port = process.env.PORT || 3000;
var app = express();
app.use(express.json())
const messages = require("./database/schemas/messages");


app.get('/', (request, response) => {
    response.status(200).send(`${discord.lastmessage()}`)
});

// app.get('/botstats', async (request, response) => {
//      response.header('Access-Control-Allow-Origin', '*');
//      var person = {
//         guildcount: discord.ServerCount(),
//         usercount: discord.UserCount(),
//         channelcount: discord.ChannelCount(),
//         messagecount: await messages.count({}).exec(),
//         deletedmessagecount: await messages.count({deleted: true}).exec(),
//         remainingmessages: await messages.count({deleted: false}).exec()
//       };
//     response.status(200).send(person);
// });

mongoose.connect(`mongodb://localhost:27017/FinBot`, {
    useNewUrlParser: true,
    useUnifiedTopology: true,
});

app.use(session({
    secret: 'secret',
    cookie: {
        maxAge: 60000 * 60 * 24
    },
    resave: false,
    saveUninitialized: false,
    store: Store.create({mongoUrl: `mongodb://localhost:27017/FinBot`})
}))
app.use(passport.initialize());
app.use(passport.session());
app.use('/api', routes);

// This is REQUIRED for IISNODE to work
app.listen(port, () => { 
    console.log("listening");
});