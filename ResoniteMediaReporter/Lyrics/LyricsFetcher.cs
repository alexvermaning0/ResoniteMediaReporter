using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace ResoniteMediaReporter.Lyrics.Fetchers
{
    public class LyricsLine
    {
        public long Time { get; set; }
        public string Text { get; set; }
    }

    public class LyricsFetcher
    {
        private List<LyricsLine> _lyrics;
        private string _lastTitle = "";
        private string _lastArtist = "";

        public string CurrentSource { get; private set; }

        public bool NeedsNewSong(string title, string artist)
        {
            return title != _lastTitle || artist != _lastArtist;
        }

        public void FetchLyrics(string title, string artist)
        {
            _lastTitle = title;
            _lastArtist = artist;
            _lyrics = null;
            CurrentSource = null;

            var qq = QQFetcher.GetLyrics(title, artist);
            if (qq != null && qq.Count > 2)
            {
                _lyrics = qq;
                CurrentSource = "QQMusic";
                return;
            }

            var netease = NetEaseFetcher.GetLyrics(title, artist);
            if (netease != null && netease.Count > 2)
            {
                _lyrics = netease;
                CurrentSource = "NetEase";
                return;
            }
        }

        public string GetCurrentLineFormatted(long positionMs)
        {
            if (_lyrics == null || _lyrics.Count == 0)
                return null;

            for (int i = 0; i < _lyrics.Count - 1; i++)
            {
                var curr = _lyrics[i];
                var next = _lyrics[i + 1];

                if (positionMs >= curr.Time && positionMs < next.Time)
                {
                    string line = curr.Text;
                    long duration = next.Time - curr.Time;
                    var words = line.Split(' ');

                    if (words.Length <= 1)
                        return $"<color=yellow>{line}</color>";

                    var weights = words.Select(w => Math.Max(w.Length / 3.0, 1)).ToArray();
                    double totalWeight = weights.Sum();
                    long preRollMs = 200;
                    const double leadFactor = 1.25;
                    const int maxLeadMs = 800;

                    long biasedElapsed = (long)((positionMs - curr.Time + preRollMs) * leadFactor);
                    biasedElapsed = Math.Min(biasedElapsed, duration + maxLeadMs);

                    double target = (biasedElapsed / (double)duration) * totalWeight;
                    double accum = 0;
                    int index = 0;
                    for (; index < weights.Length; index++)
                    {
                        accum += weights[index];
                        if (target < accum)
                            break;
                    }

                    index = Math.Clamp(index, 0, words.Length - 1);
                    for (int j = 0; j < words.Length; j++)
                    {
                        if (j == index)
                            words[j] = $"<color=yellow>{words[j]}</color>";
                    }

                    return string.Join(" ", words);
                }
            }

            if (positionMs < _lyrics.Last().Time + 3000)
                return _lyrics.Last().Text;
            else
                return "";
        }
        public string GetCurrentLine(long positionMs)
        {
            if (_lyrics == null || _lyrics.Count == 0)
                return "";

            for (int i = 0; i < _lyrics.Count - 1; i++)
            {
                var curr = _lyrics[i];
                var next = _lyrics[i + 1];

                if (positionMs >= curr.Time && positionMs < next.Time)
                    return curr.Text;
            }

            if (positionMs < _lyrics.Last().Time + 3000)
                return _lyrics.Last().Text;
            else
                return "";
        }


        public long GetSongLength()
        {
            return _lyrics?.LastOrDefault()?.Time ?? 0;
        }
    }
}
