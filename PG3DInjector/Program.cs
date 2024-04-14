using System;
using System.IO;
using System.Diagnostics;
using PG3DInjector;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Preparing to inject...");

        if (!IsProcessOpen("Pixel Gun 3D"))
        {
            Console.WriteLine("Pixel Gun 3D is not open. Please start the game before injecting.");
            KeepConsoleOpen();
            return;
        }

        string[] dlls = { "./minhook.x64.dll", "./PixelGunCheat.dll" };
        foreach (var dll in dlls)
        {
            if (!File.Exists(dll))
            {
                Console.WriteLine($"Failed to find {dll}. Please ensure it exists in the correct path.");
                KeepConsoleOpen();
                return;
            }
        }

        try
        {
            foreach (var dll in dlls)
            {
                Console.WriteLine($"Injecting {dll} into Pixel Gun 3D...");
                Inject.Load("Pixel Gun 3D", Path.GetFullPath(dll));
            }
            Console.WriteLine("Injection completed successfully.");
        }
        catch (Exception e)
        {
            Console.WriteLine("Error during injection:");
            Console.WriteLine(e.Message);
            KeepConsoleOpen();
        }
    }

    static bool IsProcessOpen(string name)
    {
        foreach (Process clsProcess in Process.GetProcesses())
        {
            if (clsProcess.ProcessName.Contains(name))
            {
                return true;
            }
        }
        return false;
    }

    static void KeepConsoleOpen()
    {
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}