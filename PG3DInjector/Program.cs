// See https://aka.ms/new-console-template for more information

using PG3DInjector;

Console.WriteLine("Injecting...");
Inject.Load("Pixel Gun 3D", Path.GetFullPath("./minhook.x64.dll"));
Inject.Load("Pixel Gun 3D", Path.GetFullPath("./PixelGunCheat.dll"));
Console.WriteLine("Injected");