const Discord = require('discord.js');
var config = require(`../Data/config.js`);

var client = new Discord.Client();
exports.client = client;

client.login(config.token);

client.on('ready', () => {
    console.log(`Bot online - logged in as user: ${client.user.username}#${client.user.discriminator}`);
});
