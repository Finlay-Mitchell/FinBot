var bot = require("../data/config.js")
const mongo = require("mongoose");
var lastMessage = "No content set yet";
var client = bot.client;
var schema = mongo.Schema;
const messages = require("../database/schemas/messages");

client.on('message', async (message) => {
    try 
    {
        const newMessage = await messages.create({
            discordId: message.author.id,
            discordTag: `${message.author.username}#${message.author.discriminator}`,
            avatar: message.author.avatar,
            guildId: message.guild.id,
            channelId: message.channel.id,
            createdTimestamp: message.createdTimestamp,
            content: message.content,
            messageId: message.id,
            deleted: false
        });
    }

    catch(err)
    {
        console.log(err);
    }

    lastMessage = `User: [${message.author.username}]<->[${message.author.id}] Discord server: [${message.guild.name}/${message.channel}]
    Channel type: ${message.channel.type} -> [${message.content}]`
});

client.on('messageDelete', async (message) => {
    try 
    {
        const update = await messages.findOneAndUpdate({messageId: message.id}, {deleted: true}).exec();
    }

    catch(err)
    {
        console.log(err);
    }
});

client.on('messageUpdate', async (oldMessage, newMessage) => {
    try
    {
        const update = await messages.findOneAndUpdate({messageId: newMessage.id}, {deleted: false, content: newMessage.content, $push: {edits: oldMessage.content}}).exec();
    }

    catch(err)
    {
        console.log(err);
    }
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