var config = require(`./Data/config.js`);
const Discord = require('discord.js');

const client = new Discord.Client();
var lastMessage = "No content set yet";

client.login(config.token);

client.on('ready', () => {
    console.log("Bot online");
});

client.on('message', (message) => {
    if(message.author.bot)
    {
        return;
    }

    lastMessage = `${message.content} - ${message.author.username}`;
});

exports.test = function test()
{
    return lastMessage;
}
