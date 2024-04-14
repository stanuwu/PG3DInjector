// See https://aka.ms/new-console-template for more information

using PG3DInjector;

Console.WriteLine("Injecting...");
try
{
    Inject.Load("Pixel Gun 3D", Path.GetFullPath("./minhook.x64.dll"));
    Inject.Load("Pixel Gun 3D", Path.GetFullPath("./PixelGunCheat.dll"));
    Console.WriteLine("Injected");
}
catch (Exception e)
{
    Console.WriteLine("Error Injecting:");
    Console.WriteLine(e.Message);
}

Console.ReadKey();