using System.Diagnostics;
using Lunar;

namespace PG3DInjector
{
    public class Injector
    {
        public static void Map(string process, string dll)
        {
            Process[] processes = Process.GetProcessesByName(process);
            if (processes.Length == 0)
            {
                Logger.ConsoleWrite("[<ERROR>] Process not found.", ConsoleColor.Red);
                return;
            }

            var mapper = new LibraryMapper(processes[0], dll, MappingFlags.DiscardHeaders);
            mapper.MapLibrary();
        }
    }
}