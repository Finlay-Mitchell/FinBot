const mongoose = require("mongoose");

const userSchema = new mongoose.Schema({
    discordId: {
        type: String,
        required: true,
        unique: true,
    },
    discordTag: {
        type: String, 
        required: true,
    },
    avatar: {
        type: String,
        required: false,
    },
    guilds: {
        type: Array,
        required: true,
    }
});

module.exports = mongoose.model('user', userSchema);