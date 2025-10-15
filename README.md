# ResoniteMediaReporter with Lyrics!
A WebSocket Server For Resonite That Reports All Playing Media In Windows Media. Coded In .NET Using The NetCoreServer Package.
This version has Lyrics and a progressbar added. Here is the link for the Template in-game: 
```
resrec:///U-1OC9DzBh8IC/R-28999CAF7AC15DC17F5579B2DF4CFFF0E25B73F9E77EA8EED6CAF9F0E651FF91
```

# Building
You Need The .NET 8 SDK To Build. Simply Open The Directory In A Command Line And Type `dotnet build`.

# `config.json`
Here, you can change the port the WS server is running on and configure various lyrics settings.

**NOTE:** For `ignorePlayers`, you need to enter the exact executable name of the player you want ignored, separated with a `,`. You can usually find it in Task Manager.

# Example Config
```json
{
  "port": 8080,
  "ignorePlayers": [],
  "lyricsPort": 6555,
  "DisableLyricsFor": [],
  "offset_ms": -50,
  "cache_folder": "cache",
  "filter_cjk_lyrics": true
}
```

## Config Options

- **`port`**: Port for the main WebSocket server (default: 8080)
- **`ignorePlayers`**: Array of media player executable names to ignore (e.g., `["spotify.exe"]`)
- **`lyricsPort`**: Port for the lyrics WebSocket server (default: 6555)
- **`DisableLyricsFor`**: Array of lyric sources to disable (e.g., `["lrclib", "netease", "cache"]`)
- **`offset_ms`**: Timing offset in milliseconds for lyric synchronization (can be negative)
- **`cache_folder`**: Folder to store cached lyrics (default: "cache")
- **`filter_cjk_lyrics`**: Filter out lyrics that are mostly Chinese/Japanese/Korean (default: true)

# Features

- **Multiple Lyrics Sources**: Fetches lyrics from cache → LRCLib → NetEase
- **CJK Filtering**: Automatically filters out Chinese/Japanese/Korean lyrics (can be disabled)
- **Word-by-Word Sync**: Real-time word highlighting with punctuation-aware pausing
- **Lyrics Caching**: Caches fetched lyrics locally for faster loading
- **Flickerless Console**: Smooth console display with emoji indicators
- **Configurable Offset**: Adjust lyrics timing to match your audio

# Known Issues
- May occasionally fetch non-English lyrics if CJK filter is disabled

# Credits
[NetCoreServer](https://github.com/chronoxor/NetCoreServer)