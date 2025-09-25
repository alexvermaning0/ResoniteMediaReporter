using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace ResoniteMediaReporter.Lyrics.Fetchers
{
    public static class NetEaseFetcher
    {
        public static List<LyricsLine> GetLyrics(string title, string artist)
        {
            try
            {
                using var wc = new WebClient();
                wc.Headers[HttpRequestHeader.Referer] = "https://music.163.com";
                wc.Headers.Add("Cookie", "appver=2.0.2");

                string query = wc.DownloadString($"https://music.163.com/api/search/get?s={Uri.EscapeDataString(title + "-" + artist)}&type=1&limit=1");
                var idMatch = Regex.Match(query, "\"id\":(\\d+)");
                if (!idMatch.Success) return null;

                string id = idMatch.Groups[1].Value;
                string lyricsJson = wc.DownloadString($"https://music.163.com/api/song/lyric?os=pc&id={id}&lv=-1&kv=-1&tv=-1");

                var lrcMatch = Regex.Match(lyricsJson, "\"lyric\":\"(.*?)\"", RegexOptions.Singleline);
                if (!lrcMatch.Success) return null;

                string decoded = WebUtility.HtmlDecode(lrcMatch.Groups[1].Value).Replace("\\n", "\n");
                return ParseLRC(decoded);
            }
            catch
            {
                return null;
            }
        }

        public static List<LyricsLine> ParseLRC(string raw)
        {
            var result = new List<LyricsLine>();
            var lines = raw.Split('\n');
            var regex = new Regex(@"\[(\d{2}):(\d{2})\.(\d{2,3})]");

            foreach (var line in lines)
            {
                var matches = regex.Matches(line);
                string text = regex.Replace(line, "").Trim();

                if (string.IsNullOrWhiteSpace(text)) continue;

                foreach (Match match in matches)
                {
                    int min = int.Parse(match.Groups[1].Value);
                    int sec = int.Parse(match.Groups[2].Value);
                    int ms = int.Parse(match.Groups[3].Value.PadRight(3, '0'));

                    result.Add(new LyricsLine
                    {
                        Time = min * 60000 + sec * 1000 + ms,
                        Text = text
                    });
                }
            }

            return result.OrderBy(l => l.Time).ToList();
        }
    }
}
