using NetCoreServer;
using ResoniteMediaReporter;

int port = 8080;
if (args.Length > 0) 
    port = int.Parse(args[0]);

Console.WriteLine($"Starting Resonite Media Reporter...");

if (Environment.OSVersion.Platform == PlatformID.Unix)
{
    Console.WriteLine("Unfortunely, RMP Cannot Run Under Linux Due To Windows Specific Libraries Being In Use.");
    Environment.Exit(1);
}

var server = new ResoniteWSServer("127.0.0.1", port);

// start websocket server
server.Start();

Console.WriteLine($"Started WebSocket Server On Port {port}");
Console.WriteLine("Press Any Key In This Window To Stop The Server");
Console.ReadKey();

Console.WriteLine($"Stopping Server...");
server.Stop();
Environment.Exit(0);