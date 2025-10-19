# Changelog

## [Unreleased]

### ⚠️ Important
**Performance improvements require the optional local database.** Without the database, the app works as before using online APIs. [Download database here](http://minekingshosting.nl:8686/web/client/pubshares/pwC58sHnMdWyZLF4i3SDBX/browse)

### Added
- **Local LRCLib Database Support** - Optional 69GB database with 3.8 million songs for instant, offline lyrics
- **In-Memory Index System** - 230MB index file loaded into RAM for instant lookups
- **Automatic Index Building** - If index file is missing, automatically builds on first run (~10-15 minutes)
- **Instant Lyric Synchronization** - No API delay means lyrics appear immediately when song starts, catching every line from the beginning
- **Offline Mode** - New `offline_mode` config option to disable all online API calls
- **Offline Mode Indicator** - Console shows "🔌 Mode: OFFLINE" when offline mode is enabled
- **Database Path Configuration** - New `lrclib_database_path` config option to specify database location

### Changed
- **Improved Lyrics Fetching Order** - Now: Cache → Local Database → LRCLib API → NetEase → None
- **Local Database Shows "lrclib (local)"** - Properly credits LRCLib while indicating local source

### Performance
- **Instant Lyrics Lookups (with database)** - Local database with RAM-based index provides instant results (< 1ms)
- **No More API Delays** - Perfect lyric synchronization from the very first line of every song when using local database
- **Startup Optimization** - Index loads in ~1 second on subsequent runs

### Technical
- SQLite connection stays open with prepared statements for optimal performance
- Database index uses `Dictionary<string, long>` for O(1) lookup performance
- Added `DatabaseIndex` class for building and loading in-memory index
- Index file format: JSON with artist|title → lyrics_id mappings