const config = require('../../bin/Debug/netcoreapp3.1/Data/Config.json')
const bot = require('./../discord/startup.js')

exports.token = config.Token;
exports.prefix = config.Prefix;

exports.client = bot.client;