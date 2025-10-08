# ResoniteMediaReporter with Lyrics!
A WebSocket Server For Resonite That Reports All Playing Media In Windows Media. Coded In .NET Using The NetCoreServer Package.
This version has Lyrics and a progressbar added. Here is the link for the Template in-game: 
```
resrec:///U-1OC9DzBh8IC/R-28999CAF7AC15DC17F5579B2DF4CFFF0E25B73F9E77EA8EED6CAF9F0E651FF91
```
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
  "lyricsPort": 6555,
  "DisableLyricsFor": [],
  "offset_ms": 0,
  "cache_folder": "cache"
}
```

# Known Bugs
- has an tendency to provide chineese / japaneese lyrics (this is because i am fetching the first one).

# Credits
[NetCoreServer](https://github.com/chronoxor/NetCoreServer)
