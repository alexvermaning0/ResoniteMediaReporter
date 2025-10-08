# ResoniteMediaReporter
A WebSocket Server For Resonite That Reports All Playing Media In Windows Media. Coded In .NET Using The NetCoreServer Package.

# Building
You Need The .NET 8 SDK To Build. Simply Open The Directory In A Command Line And Type ``dotnet build``.

# ``config.json``
Here, you can change the port the WS server is running on and put in any media players you don't want it to detect.

**NOTE:** You need to enter the exact executable name of the player you want ignored, seperated with a ``,``. you can usually find it in Task Manager.

# Example Config
```
{
  "port": 8080,
  "ignorePlayers": [],
  "LyricsPort": 6555,
  "DisableLyricsFor": [],
  "offset_ms": -50,
  "cache_folder": "cache"
}
```

# Contributions
I Am Not Accepting Pull Requests. Make Issues And I Will Get To Them As Soon As Possible.

# Credits
[NetCoreServer](https://github.com/chronoxor/NetCoreServer)
