using System.Diagnostics;
using Lunar;

namespace BKC_Injector
{
    internal class Injector
    {
        public delegate void StatusUpdateHandler(string message);

        public static void Inject(Process process, string dll, StatusUpdateHandler statusUpdate)
        {
            try
            {
                var mapper = new LibraryMapper(process, dll, MappingFlags.DiscardHeaders);
                mapper.MapLibrary();
            }
            catch (Exception ex)
            {
                statusUpdate($"{ex.Message}");
            }
        }
    }
}
