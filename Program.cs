using ResoniteMediaReporter;
using ResoniteMediaReporter.Services;
using System.Text.Json;

int port = 8080;
int lyricsPort = 6555;

Console.WriteLine("Starting Resonite Media Reporter...");
Console.WriteLine("Offset (ms) will be applied to lyric timing if set in config.");
Console.OutputEncoding = System.Text.Encoding.UTF8;


if (Environment.OSVersion.Platform == PlatformID.Unix)
{
    Console.WriteLine("Unfortunately, RMR cannot run under Linux due to Windows-specific libraries being in use.");
    Environment.Exit(1);
}
if (!File.Exists("config.json"))
{
    Config config = new Config
    {
        Port = port,
        IgnorePlayers = Array.Empty<string>(),
        LyricsPort = lyricsPort,
        DisableLyricsFor = new List<string>()
    };

    JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
    string serializedConfig = JsonSerializer.Serialize(config, options);

    Console.WriteLine($"Config not found - writing new config\n{serializedConfig}");
    File.WriteAllText("config.json", serializedConfig);
}

Config configFile = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"));

// Initialize Resonite WebSocket Server
var server = new ResoniteWSServer("127.0.0.1", configFile.Port)
{
    Config = configFile
};

// Create shared Windows Media Service instance
var dummySession = new ResoniteWSSession(server);
var wmService = new WindowsMediaService(dummySession, server);

// Start Resonite WebSocket Server
try
{
    server.Start();
    Console.WriteLine($"Started Resonite WebSocket Server on port {configFile.Port}");
}
catch (System.Net.Sockets.SocketException ex) when (ex.SocketErrorCode == System.Net.Sockets.SocketError.AddressAlreadyInUse)
{
    Console.WriteLine($"[ERROR] Could not start Resonite WebSocket Server: Port {configFile.Port} is already in use.");
    Environment.Exit(1);
}

// Start Lyrics WebSocket Server
var lyricsService = new LyricsService(wmService);
var lyricsServer = new LyricsWSServer("127.0.0.1", lyricsPort, lyricsService);
try
{
    lyricsServer.Start();
    Console.WriteLine($"Started Lyrics WebSocket Server on port {lyricsPort}");
}
catch (System.Net.Sockets.SocketException ex) when (ex.SocketErrorCode == System.Net.Sockets.SocketError.AddressAlreadyInUse)
{
    Console.WriteLine($"[ERROR] Could not start Lyrics WebSocket Server: Port {lyricsPort} is already in use.");
    server.Stop();
    Environment.Exit(1);
}
Console.WriteLine("Press any key to stop the servers...");
Console.ReadKey();

Console.WriteLine("Stopping servers...");
server.Stop();
lyricsServer.Stop();
lyricsService.Dispose();
wmService.Dispose();
Environment.Exit(0);