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

exports.ServerCount = function ServerCount()
{
    return client.guilds.cache.size;
}

exports.UserCount = function UserCount()
{
    return client.guilds.cache.reduce((a, g) => a + g.memberCount, 0)
}

exports.ChannelCount = function ChannelCount()
{
    return client.guilds.cache.reduce((a, g) => a + g.channels.cache.size, 0)
}