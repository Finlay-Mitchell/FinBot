const mongoose = require("mongoose");

const messageSchema = new mongoose.Schema({
    _id: {
        type: String,
        required: true,
    },
    guildId: {
        type: String,
        required: true,
    },
    discordTag: {
        type: String,
        required: true,
    },
    discordId: {
        type: String,
        required: true,
    },
    channelId: {
        type: String,
        required: true,
    },
    content: {
        type: String,
        required: false,
    },
    createdTimestamp: {
        type: String,
        required: true,
    },
    edits: {
        type: Array,
        required: false,
    },
    attachments: {
        type: Array,
        required: false,
    },
    embeds: {
        type: Array,
        required: false,
    },
    deleted: {
        type: Boolean,
        required:  true,
    }
});

module.exports = mongoose.model('messages', messageSchema);