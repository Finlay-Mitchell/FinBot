'use strict';

exports.config = require('../../bin/Debug/netcoreapp3.1/Data/Config.json')
const bot = require('./../discord/startup.js')

exports.client = bot.client;