const mongoose = require("mongoose");

const messageSchema = new mongoose.Schema({
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
    messageId: {
        type: String,
        required: true,
    },
    createdTimestamp: {
        type: String,
        required: true,
    },
    edits: {
        type: Array,
        required: false,
    },
    deleted: {
        type: Boolean,
        required:  true,
    }
});

module.exports = mongoose.model('messages', messageSchema);