const express = require('express');
const app = express();
const PORT = 8080;
//const PORT = 80;
app.use(express.json())


app.get('/test', (request, response) => {
    response.status(200).send({
        test: 'working',
        check: 'yep shes working'
    })
});

app.post('/test/:id', (request, response) => {
    const {id} = request.params;
    const {testing} = request.body;

    if(!testing)
    {
        response.status(418).send({message: "You need to parse a paramater!"})
    }

    response.send({
        test: `${testing} and ${id}`,
    });
});

app.get('/doesitwork', (request, response) => {
    response.status(200).send('yes, I am working')
});

app.listen(PORT, () => console.log(`It's alive on http://localhost:${PORT}`))