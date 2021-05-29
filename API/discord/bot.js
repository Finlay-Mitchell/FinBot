var bot = require("../data/config.js")
// const canvacord = require("canvacord");
var lastMessage = "No content set yet";
const Discord = require('discord.js');

var client = bot.client;

client.on('message', async (message) => {
    if(message.author.bot)
    {
        return;
    }

    lastMessage = `User: [${message.author.username}]<->[${message.author.id}] Discord server: [${message.guild.name}/${message.channel}]
    Channel type: ${message.channel.type} -> [${message.content}]`
});

exports.rankcard = function rankcard(userId = 0, level = 0, XP = 0, reqXP = 0, chanId = 0)
{
    // var getuser = client.users.fetch(userId);
    // getuser.then(user => {
    //     const rank = new canvacord.Rank()
    //     .setAvatar(user.displayAvatarURL({ dynamic: false, format: 'png' }))
    //     .setCurrentXP(parseFloat(XP))
    //     .setRequiredXP(parseFloat(reqXP))
    //     .setLevel(parseFloat(level))
    //     .setStatus(user.presence.status)
    //     .setProgressBar("#0ff1ce", "COLOR")
    //     .setUsername(user.username)
    //     .setRank(1, "Rank", false)
    //     .setDiscriminator(user.discriminator);
    
    // rank.build()
    //     .then(data => {
    //         const attachment = new Discord.MessageAttachment(data, "RankCard.png");
    //         const chan = client.channels.fetch(chanId);
    //         chan.then(channel => channel.send(attachment));
    //     });
    // });

    return `success`;
}

exports.lastmessage = function lastmessage()
{
    return lastMessage;
}
