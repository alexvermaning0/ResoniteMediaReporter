using System;
using System.Collections.Generic;
using System.Linq;

namespace ResoniteMediaReporter.Lyrics.Fetchers
{
    public class LyricsLine
    {
        public long Time { get; set; }
        public string Text { get; set; }
    }

    public class LyricsFetcher
    {
        private List<LyricsLine> _lyrics = new();
        private string _currentTitle = "";
        private string _currentArtist = "";
        public string CurrentSource { get; private set; } = "None";

        // set from LyricsService
        public static string CacheFolder { get; set; } = "cache";
        public static bool FilterCjkLyrics { get; set; } = true;

        // Logging callback
        private static Action<string> _logCallback;
        public static void SetLogCallback(Action<string> callback)
        {
            _logCallback = callback;
        }

        private static void Log(string message)
        {
            _logCallback?.Invoke(message);
        }

        public bool NeedsNewSong(string title, string artist)
        {
            return !string.Equals(title ?? "", _currentTitle ?? "", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(artist ?? "", _currentArtist ?? "", StringComparison.OrdinalIgnoreCase);
        }

        public void FetchLyrics(string title, string artist, int durationMs = 0)
        {
            _currentTitle = title ?? "";
            _currentArtist = artist ?? "";
            _lyrics = new List<LyricsLine>();
            CurrentSource = "None";

            // 1) cache
            Log("Trying: Cache");
            if (CacheHelper.TryLoad(CacheFolder, _currentArtist, _currentTitle, out var cached))
            {
                _lyrics = cached;
                CurrentSource = "cache";
                Log("✓ Found in cache");
                return;
            }
            Log("✗ Not in cache");

            // 2) lrclib
            Log("Trying: LRCLib");
            var lrclines = LRCLibFetcher.GetLyrics(_currentTitle, _currentArtist, durationMs);
            if (lrclines != null && lrclines.Count > 0)
            {
                _lyrics = lrclines;
                CurrentSource = "lrclib";
                Log($"✓ LRCLib found {lrclines.Count} lines");
                CacheHelper.Save(CacheFolder, _currentArtist, _currentTitle, _lyrics, CurrentSource);
                return;
            }
            Log("✗ LRCLib found nothing");

            // 3) netease
            Log("Trying: NetEase");
            var netease = NetEaseFetcher.GetLyrics(_currentTitle, _currentArtist);
            if (netease != null && netease.Count > 0)
            {
                _lyrics = netease;
                CurrentSource = "netease";
                Log($"✓ NetEase found {netease.Count} lines");
                CacheHelper.Save(CacheFolder, _currentArtist, _currentTitle, _lyrics, CurrentSource);
                return;
            }
            Log("✗ NetEase found nothing");

            CurrentSource = "None";
            _lyrics = new List<LyricsLine>();
            Log("✗ No lyrics found from any source");
        }

        public string GetCurrentLine(long positionMs)
        {
            if (_lyrics == null || _lyrics.Count == 0) return "";
            int idx = _lyrics.FindLastIndex(l => l.Time <= positionMs);
            if (idx < 0) return "";
            return _lyrics[idx].Text;
        }

        // Improved per-word sync with punctuation pauses + long-pause handling
        public string GetCurrentLineWordSync(long positionMs)
        {
            if (_lyrics == null || _lyrics.Count == 0) return "";

            int idx = _lyrics.FindLastIndex(l => l.Time <= positionMs);
            if (idx < 0 || idx >= _lyrics.Count - 1) return "";

            var current = _lyrics[idx];
            var next = _lyrics[idx + 1];

            long interval = next.Time - current.Time;
            if (interval <= 0) return "";

            // ---- Tunables ----
            const int LongPauseThresholdMs = 2000;
            const double SpokenPortionCap = 0.60;
            const int MaxWordMs = 350;
            const int MinWordMs = 60;

            // punctuation pauses
            const int CommaPauseMs = 120;
            const int MidPauseMs = 140;
            const int FullStopPauseMs = 180;

            // ---- Tokenize & weight ----
            var rawTokens = (current.Text ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (rawTokens.Count == 0) return "";

            int WeightOf(string s)
            {
                if (string.IsNullOrEmpty(s)) return 1;
                int w = 0;
                foreach (var ch in s)
                    if (char.IsLetterOrDigit(ch)) w++;
                return Math.Max(1, w);
            }

            var weights = rawTokens.Select(WeightOf).ToArray();
            int totalWeight = Math.Max(1, weights.Sum());

            // ---- Allowed content window ----
            long allowedContentMs;
            if (interval >= LongPauseThresholdMs)
            {
                long capByPortion = (long)(interval * SpokenPortionCap);
                long capByPerToken = (long)rawTokens.Count * MaxWordMs;
                allowedContentMs = Math.Min(capByPortion, capByPerToken);
                allowedContentMs = Math.Max(allowedContentMs, Math.Min(interval, rawTokens.Count * MinWordMs));
            }
            else
            {
                allowedContentMs = interval;
            }

            // ---- Initial highlight durations ----
            var idealDurations = new double[rawTokens.Count];
            for (int i = 0; i < rawTokens.Count; i++)
                idealDurations[i] = allowedContentMs * (weights[i] / (double)totalWeight);

            var highlightDur = new double[rawTokens.Count];
            for (int i = 0; i < rawTokens.Count; i++)
                highlightDur[i] = Math.Clamp(idealDurations[i], MinWordMs, MaxWordMs);

            // ---- Pause after punctuation ----
            int PauseForToken(string token)
            {
                if (string.IsNullOrEmpty(token)) return 0;
                char last = token[token.Length - 1];
                if (last == ',') return CommaPauseMs;
                if (last == ';' || last == ':') return MidPauseMs;
                if (last == '.' || last == '!' || last == '?') return FullStopPauseMs;
                return 0;
            }

            var pauseAfter = new int[rawTokens.Count];
            for (int i = 0; i < rawTokens.Count; i++)
                pauseAfter[i] = PauseForToken(rawTokens[i]);

            double baseSum = highlightDur.Sum();
            int pauseSum = pauseAfter.Sum();
            double totalWithPauses = baseSum + pauseSum;

            if (totalWithPauses > allowedContentMs && baseSum > 0)
            {
                double scale = (allowedContentMs - pauseSum) / baseSum;
                scale = Math.Clamp(scale, 0.2, 1.0);
                for (int i = 0; i < highlightDur.Length; i++)
                    highlightDur[i] *= scale;

                baseSum = highlightDur.Sum();
                totalWithPauses = baseSum + pauseSum;
            }

            // ---- Build cumulative segments ----
            var segEnds = new double[rawTokens.Count * 2];
            int seg = 0;
            double acc = 0;
            for (int i = 0; i < rawTokens.Count; i++)
            {
                acc += highlightDur[i];
                segEnds[seg++] = acc;
                acc += pauseAfter[i];
                segEnds[seg++] = acc;
            }

            // ---- Decide what to show ----
            long elapsed = positionMs - current.Time;
            if (elapsed < 0) return "";

            if (elapsed >= (long)totalWithPauses)
                return "";

            int segIndex = Array.FindIndex(segEnds, end => elapsed <= end);
            if (segIndex < 0) segIndex = segEnds.Length - 1;

            if ((segIndex % 2) == 1)
                return "";

            int tokenIndex = segIndex / 2;
            if (tokenIndex >= rawTokens.Count) tokenIndex = rawTokens.Count - 1;

            var rebuilt = new List<string>(rawTokens.Count);
            for (int i = 0; i < rawTokens.Count; i++)
            {
                if (i == tokenIndex)
                    rebuilt.Add("<color=yellow>" + rawTokens[i] + "</color>");
                else
                    rebuilt.Add(rawTokens[i]);
            }
            return string.Join(" ", rebuilt);
        }

        public long GetSongLength()
        {
            return _lyrics?.LastOrDefault()?.Time ?? 0;
        }
    }
}