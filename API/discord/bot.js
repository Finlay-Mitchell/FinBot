var bot = require("../data/config.js")
var lastMessage = "No content set yet";
var client = bot.client;

client.on('message', async (message) => {
    if(message.author.bot)
    {
        return;
    }

    lastMessage = `User: [${message.author.username}]<->[${message.author.id}] Discord server: [${message.guild.name}/${message.channel}]
    Channel type: ${message.channel.type} -> [${message.content}]`
});

exports.lastmessage = function lastmessage()
{
    return lastMessage;
}
