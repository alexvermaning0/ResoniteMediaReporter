using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ResoniteMediaReporter
{
    public class Config
    {
        [JsonPropertyName("port")]
        public int Port { get; set; }

        [JsonPropertyName("ignorePlayers")]
        public string[] IgnorePlayers { get; set; }

        [JsonPropertyName("lyricsPort")]
        public int LyricsPort { get; set; }

        public List<string> DisableLyricsFor { get; set; } = new();

        [JsonPropertyName("offset_ms")]
        public int OffsetMs { get; set; } = 0;

        [JsonPropertyName("cache_folder")]
        public string CacheFolder { get; set; } = "cache";

        [JsonPropertyName("filter_cjk_lyrics")]
        public bool FilterCjkLyrics { get; set; } = true;
    }
}