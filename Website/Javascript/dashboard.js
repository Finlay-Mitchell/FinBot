function GetAPIData(query)
{
    const p = document.getElementById(query);

    fetch('https://api.finlaymitchell.ml/api/auth', { mode: 'no-cors' })
    .then(function(res) {
        return res.text()
    })
    .then(function(body)
    {
        p.innerText = body
    })
}