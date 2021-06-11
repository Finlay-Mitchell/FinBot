var bot = require("../data/config.js")
const mongo = require("mongoose");
var lastMessage = "No content set yet";
var messages = BigInt(0);
var client = bot.client;
var schema = mongo.Schema;

client.on('message', async (message) => {
    if(message.author.bot)
    {
        return;
    }

    // try
    // {
    //     // message.reply(message.guild.id);

    //     // mongo.connection.db.collection("guilds", function(err, collection){
    //     //     data = collection.findOne({_id: message.guild.id}, function(err, value){
    //     //         if(err)
    //     //         {
    //     //             console.log(`\n\n\n\n\n\n\n${err}\n\n\n\n\n\\n\n`);
    //     //         }

    //     //         console.log(value); // it will print your collection data
    //     //         lastMessage = value;
    //     //     })
    //     //     if(err)
    //     //     {
    //     //         console.log(`\n\n\n\n\n\n\n${err}\n\n\n\n\n\\n\n`);
    //     //     }

    //     //     console.log(data);
    //     // });

    //     // message.reply(lastMessage);
    // }

    // catch(err)
    // {
    //     lastMessage = err;
    // }
    messages++;
    lastMessage = `User: [${message.author.username}]<->[${message.author.id}] Discord server: [${message.guild.name}/${message.channel}]
    Channel type: ${message.channel.type} -> [${message.content}]\n\nis command: ${isCommand}`
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

exports.GetMessages = function GetMessages()
{
    return String(messages);
}