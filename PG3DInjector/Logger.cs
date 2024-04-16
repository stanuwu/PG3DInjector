using System.Text.RegularExpressions;

namespace PG3DInjector
{
    internal class Logger
    {
        public static void ConsoleWrite(string message, ConsoleColor consoleColor = ConsoleColor.Black)
        {
            foreach (Match match in Regex.Matches(message, @"(<[^>]*>)|([^<>]+)").Cast<Match>())
            {
                if (match.Value.StartsWith("<"))
                {
                    Console.ForegroundColor = consoleColor;
                    Console.Write(match.Value.Trim('<', '>'));
                    Console.ResetColor();
                }
                else
                {
                    Console.Write(match.Value);
                }
            }
            Console.WriteLine();
        }
    }
}
