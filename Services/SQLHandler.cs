using System.Text.RegularExpressions;

namespace FinBot.Services
{
    class SQLHandler
    {
        public static string SanitizeSQL(string inp)
        {
            inp = Regex.Replace(inp, "'", "\"");
            inp = Regex.Replace(inp, "\\", "");
            return inp;
        }
    }
}
