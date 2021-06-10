const config = require("../Data/config");
const passport = require("passport");
const DiscordStrategy = require("passport-discord");
const user = require("../database/schemas/user");

passport.serializeUser((user, done) => {
    done(null, user.discordId)
});

passport.deserializeUser(async (discordId, done) => {
    try
    {
        const user = await user.findOne({discordId});
        return user ? done(null, user) : done(null, null);
    }

    catch(err)
    {
        console.log(err);
        done(err, null);
    }
});

passport.use(new DiscordStrategy({
    clientID: config.config.dashboard_client_id,
    clientSecret: config.config.dashboard_client_secret,
    callbackURL: config.config.dashboard_callback_url,
    scope: ['identify', 'guilds'],
}, async(accessToken, refreshToken, profile, done) => {
    const {id, username, discriminator, avatar, guilds} = profile;    
    try 
    {
        const findUser = await user.findOneAndUpdate({discordId: id}, {
            discordTag: `${username}#${discriminator}`,
            avatar,
            guilds,
        }, {new: true}).exec();

        if(findUser) 
        {
            console.log("User was found");
            return done(null, findUser);
        }

        else 
        {
            const newUser = await user.create({
                discordId: id,
                discordTag: `${username}#${discriminator}`,
                avatar,
                guilds,
            });

            return done(null, newUser);
        }
    }

    catch(err)
    {
        console.log(err);
        return done(err, null);
    }
}));