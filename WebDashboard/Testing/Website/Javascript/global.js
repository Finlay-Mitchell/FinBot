function ToggleTheme() 
{
    var element = document.body;
    element.classList.toggle("light-theme");
}

function GetYear()
{
    document.write(new Date().getFullYear());
}