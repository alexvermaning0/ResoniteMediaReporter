using ResoniteMediaReporter;
using System.Text.Json;

int port = 8080;

Console.WriteLine($"Starting Resonite Media Reporter...");

if (Environment.OSVersion.Platform == PlatformID.Unix)
{
    Console.WriteLine("Unfortunely, RMP Cannot Run Under Linux Due To Windows Specific Libraries Being In Use.");
    Environment.Exit(1);
}

if (!File.Exists("config.json"))
{
    // create a new Config object and save it to 'config.json'

    Config config = new Config
    {
        Port = port,
        IgnorePlayers = Array.Empty<string>()
    };

    JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
    string serializedConfig = JsonSerializer.Serialize(config, options);

    Console.WriteLine($"Config Not Found - Writing New Config\n{serializedConfig}");

    File.WriteAllText("config.json", serializedConfig);
}

// deserialize config
Config configFile = JsonSerializer.Deserialize<Config>(File.ReadAllText("config.json"));

var server = new ResoniteWSServer("127.0.0.1", configFile.Port);
server.Config = configFile;

// start websocket server
server.Start();

Console.WriteLine($"Started WebSocket Server On Port {port}");
Console.WriteLine("Press Any Key In This Window To Stop The Server");
Console.ReadKey();

Console.WriteLine($"Stopping Server...");
server.Stop();
Environment.Exit(0);