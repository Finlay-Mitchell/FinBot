var express = require("express");
var discord = require(`./discord/main.js`);

var app = express();
app.use(express.json())

app.get('/', (request, response) => {
    response.status(200).send(`${discord.lastMessage()}`)
});

app.get('/test', (request, response) => {
    response.status(200).send({
        test: 'working',
        check: 'yep, working'
    })
});

app.post('/test/:id', (request, response) => {
    const {id} = request.params;
    const {testing} = request.body;

    if(!testing)
    {
        response.status(200).send({message: "You need to parse a paramater!"})
    }

    response.send({
        test: `${testing} and ${id}`,
    });
});

app.get('/doesitwork', (request, response) => {
    response.status(200).send('yes, I am working')
});

// This is REQUIRED for IISNODE to work
// app.listen(process.env.PORT, () => {
    app.listen("8080", () => {

console.log("listening");
});
