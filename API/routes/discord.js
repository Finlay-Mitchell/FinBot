const router = require("express").Router();

router.get('/', (request, response) => {
    response.send(200);
});

module.exports = router;