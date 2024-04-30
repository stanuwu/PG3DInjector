using System.Text.RegularExpressions;

namespace PG3DInjector;

internal partial class Logger
{
    public static void ConsoleWrite(string message, ConsoleColor consoleColor = ConsoleColor.Black)
    {
        foreach (var match in ColorRegex().Matches(message).Cast<Match>())
            if (match.Value.StartsWith('<'))
            {
                Console.ForegroundColor = consoleColor;
                Console.Write(match.Value.Trim('<', '>'));
                Console.ResetColor();
            }
            else
            {
                Console.Write(match.Value);
            }

        Console.WriteLine();
    }

    public static void Log(string message)
    {
        ConsoleWrite($"[<INFO>] {message}", ConsoleColor.Cyan);
    }

    public static void Warn(string message)
    {
        ConsoleWrite($"[<WARN>] {message}", ConsoleColor.Yellow);
    }

    public static void Error(string message)
    {
        ConsoleWrite($"[<ERROR>] {message}", ConsoleColor.Red);
    }

    public static void Okay(string message)
    {
        ConsoleWrite($"[<OKAY>] {message}", ConsoleColor.Green);
    }

    [GeneratedRegex(@"(<[^>]*>)|([^<>]+)")]
    private static partial Regex ColorRegex();
}