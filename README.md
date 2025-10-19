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
  "filter_cjk_lyrics": true,
  "offline_mode": false,
  "lrclib_database_path": "db.sqlite3"
}
```

## Config Options

- **`port`**: Port for the main WebSocket server (default: 8080)
- **`ignorePlayers`**: Array of media player executable names to ignore (e.g., `["spotify.exe"]`)
- **`lyricsPort`**: Port for the lyrics WebSocket server (default: 6555)
- **`DisableLyricsFor`**: Array of lyric sources to disable (e.g., `["lrclib", "netease", "cache", "localdb"]`)
- **`offset_ms`**: Timing offset in milliseconds for lyric synchronization (can be negative)
- **`cache_folder`**: Folder to store cached lyrics (default: "cache")
- **`filter_cjk_lyrics`**: Filter out lyrics that are mostly Chinese/Japanese/Korean (default: true)
- **`offline_mode`**: Disable all online API calls, only use cache and local database (default: false)
- **`lrclib_database_path`**: Path to the local LRCLib database file (default: "db.sqlite3")

# Local Database (Optional)

For instant, offline lyrics access, you can download the complete LRCLib database:

**Download:** [LRCLib Database Files - LINK_HERE]  
**Files Required:** 
- `db.sqlite3` (~69GB) - Main database file
- `db.sqlite3.index` (~230MB) - Pre-built index file

**Note:** You need to download BOTH files separately from the link above. Click each file individually to download.

**System Requirements:**
- **Disk Space:** ~69GB for database + 230MB for index
- **RAM:** 2GB additional (for index in memory)
- **Recommended:** 16GB+ total system RAM

**Installation:**
1. Download both `db.sqlite3` and `db.sqlite3.index` from the link above
2. Place both files in the same folder as `ResoniteMediaReporter.exe`
3. The app will automatically detect and use them on startup

**Performance:**
- ✅ **Instant lookups** - No API delay, catches every lyric line from the start
- ✅ **3.8 million songs** - Comprehensive lyrics coverage
- ✅ **Works offline** - No internet required
- ✅ **No rate limits** - Query as fast as you want

**Index Building (Optional):**
If you only download the database file without the index, the app will automatically build the index on first run (takes ~10-15 minutes for 3.8M tracks). To avoid this wait, download both files. The index only needs to be built once and will be reused on all future runs.

### Fetching Priority

**With Local Database (online mode):**
1. Cache → 2. Local Database → 3. LRCLib API → 4. NetEase → 5. None

**Offline Mode:**
1. Cache → 2. Local Database → 3. None (skips all APIs)

# Features

- **Local Database Support**: Optional 69GB LRCLib database with in-memory indexing for instant lyrics (3.8M songs)
- **Instant Lyric Sync**: No API delay means every lyric line is caught from the very beginning of the song
- **Multiple Lyrics Sources**: Fetches lyrics from cache → local DB → LRCLib → NetEase
- **Offline Mode**: Works completely offline with local database
- **CJK Filtering**: Automatically filters out Chinese/Japanese/Korean lyrics (configurable)
- **Word-by-Word Sync**: Real-time word highlighting with punctuation-aware pausing
- **Lyrics Caching**: Caches fetched lyrics locally for faster loading
- **Flickerless Console**: Smooth console display with emoji indicators
- **Configurable Offset**: Adjust lyrics timing to match your audio

# Known Issues
- May occasionally fetch non-English lyrics if CJK filter is disabled

# Credits
[NetCoreServer](https://github.com/chronoxor/NetCoreServer)  
[Microsoft.Data.Sqlite](https://github.com/dotnet/efcore) - SQLite database access  
[LRCLib](https://lrclib.net/) - Lyrics database and API