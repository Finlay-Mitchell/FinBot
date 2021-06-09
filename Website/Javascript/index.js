  $(window).on('load', function () {
    $('#loading').hide();
  })

console.log(`
FFFFFFFFFFFFFFFFFFFFFF  iiii                    BBBBBBBBBBBBBBBBB                             tttt
F::::::::::::::::::::F i::::i                   B::::::::::::::::B                         ttt:::t
F::::::::::::::::::::F  iiii                    B::::::BBBBBB:::::B                        t:::::t
FF::::::FFFFFFFFF::::F                          BB:::::B     B:::::B                       t:::::t
  F:::::F       FFFFFFiiiiiii nnnn  nnnnnnnn      B::::B     B:::::B   ooooooooooo   ttttttt:::::ttttttt
  F:::::F             i:::::i n:::nn::::::::nn    B::::B     B:::::B oo:::::::::::oo t:::::::::::::::::t
  F::::::FFFFFFFFFF    i::::i n::::::::::::::nn   B::::BBBBBB:::::B o:::::::::::::::ot:::::::::::::::::t
  F:::::::::::::::F    i::::i nn:::::::::::::::n  B:::::::::::::BB  o:::::ooooo:::::otttttt:::::::tttttt
  F:::::::::::::::F    i::::i   n:::::nnnn:::::n  B::::BBBBBB:::::B o::::o     o::::o      t:::::t
  F::::::FFFFFFFFFF    i::::i   n::::n    n::::n  B::::B     B:::::Bo::::o     o::::o      t:::::t
  F:::::F              i::::i   n::::n    n::::n  B::::B     B:::::Bo::::o     o::::o      t:::::t
  F:::::F              i::::i   n::::n    n::::n  B::::B     B:::::Bo::::o     o::::o      t:::::t    tttttt
FF:::::::FF           i::::::i  n::::n    n::::nBB:::::BBBBBB::::::Bo:::::ooooo:::::o      t::::::tttt:::::t
F::::::::FF           i::::::i  n::::n    n::::nB:::::::::::::::::B o:::::::::::::::o      tt::::::::::::::t
F::::::::FF           i::::::i  n::::n    n::::nB::::::::::::::::B   oo:::::::::::oo         tt:::::::::::tt
FFFFFFFFFFF           iiiiiiii  nnnnnn    nnnnnnBBBBBBBBBBBBBBBBB      ooooooooooo             ttttttttttt
`);

function GetAPIData(query)
{
  const p = document.getElementById(query)

  switch(query)
  {
    case "guildcount":
      fetch('http://localhost:3000/botstats')
      .then(function (res) {
        return res.json()
      })
      .then(function (body) {
        p.append(body.guildcount)
      })
      break;

    case "usercount":
      fetch('http://localhost:3000/botstats')
      .then(function (res) {
        return res.json()
      })
      .then(function (body) {
        p.append(body.usercount)
      })
      break;

    case "channelcount":
      fetch('http://localhost:3000/botstats')
      .then(function (res) {
        return res.json()
      })
      .then(function (body) {
        p.append(body.channelcount)
      })
      break;
  }
}