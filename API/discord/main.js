const client = require('./startup.js')

var lastMessage = "No content set yet";

client.client.on('message', (message) => {
    if(message.author.bot)
    {
        return;
    }

    lastMessage = `User: [${message.author.username}]<->[${message.author.id}] Discord server: [${message.guild.name}/${message.channel}]
    Channel type: ${message.channel.type} -> [${message.content}]`
});

exports.lastMessage = function lastMessage()
{
    return lastMessage;
}
