using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ResoniteMediaReporter.Lyrics.Fetchers
{
    public static class LRCLibFetcher
    {
        private class SearchResult
        {
            public long id { get; set; }
            public string trackName { get; set; }
            public string artistName { get; set; }
            public string albumName { get; set; }
            public int duration { get; set; }
            public bool instrumental { get; set; }
        }

        private class LyricGet
        {
            public long id { get; set; }
            public string syncedLyrics { get; set; }
            public string plainLyrics { get; set; }
        }

        public static List<LyricsLine> GetLyrics(string title, string artist, int durationMs = 0)
        {
            try
            {
                using var wc = new WebClient();
                wc.Headers[HttpRequestHeader.UserAgent] = "ResoniteMediaReporter";

                var url = $"https://lrclib.net/api/search?track_name={Uri.EscapeDataString(title ?? string.Empty)}&artist_name={Uri.EscapeDataString(artist ?? string.Empty)}";
                if (durationMs > 0)
                {
                    int durationSec = (int)Math.Round(durationMs / 1000.0);
                    url += $"&duration={durationSec}";
                }

                var json = wc.DownloadString(url);
                var results = JsonSerializer.Deserialize<List<SearchResult>>(json) ?? new List<SearchResult>();
                if (results.Count == 0) return new List<LyricsLine>();

                var id = results[0].id;

                var getJson = wc.DownloadString($"https://lrclib.net/api/get/{id}");
                var get = JsonSerializer.Deserialize<LyricGet>(getJson);
                var lrc = get?.syncedLyrics ?? get?.plainLyrics ?? "";
                if (string.IsNullOrWhiteSpace(lrc)) return new List<LyricsLine>();

                return ParseLrc(lrc);
            }
            catch
            {
                return new List<LyricsLine>();
            }
        }

        private static List<LyricsLine> ParseLrc(string lrc)
        {
            var result = new List<LyricsLine>();
            var lines = (lrc ?? "").Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            var regex = new Regex(@"\[(\d{2}):(\d{2})\.(\d{2,3})\](.*)");
            foreach (var line in lines)
            {
                var match = regex.Match(line);
                if (!match.Success) continue;

                int min = int.Parse(match.Groups[1].Value);
                int sec = int.Parse(match.Groups[2].Value);
                int ms = int.Parse(match.Groups[3].Value.PadRight(3, '0'));
                string text = match.Groups[4].Value.Trim();

                result.Add(new LyricsLine
                {
                    Time = min * 60000 + sec * 1000 + ms,
                    Text = text
                });
            }

            return result.OrderBy(l => l.Time).ToList();
        }
    }
}
