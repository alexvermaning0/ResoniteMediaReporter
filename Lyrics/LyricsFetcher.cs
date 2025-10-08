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
            if (CacheHelper.TryLoad(CacheFolder, _currentArtist, _currentTitle, out var cached))
            {
                _lyrics = cached;
                CurrentSource = "cache";
                return;
            }

            // 2) lrclib
            var lrclines = LRCLibFetcher.GetLyrics(_currentTitle, _currentArtist, durationMs);
            if (lrclines != null && lrclines.Count > 0)
            {
                _lyrics = lrclines;
                CurrentSource = "lrclib";
                CacheHelper.Save(CacheFolder, _currentArtist, _currentTitle, _lyrics, CurrentSource);
                return;
            }

            // 3) netease
            var netease = NetEaseFetcher.GetLyrics(_currentTitle, _currentArtist);
            if (netease != null && netease.Count > 0)
            {
                _lyrics = netease;
                CurrentSource = "netease";
                CacheHelper.Save(CacheFolder, _currentArtist, _currentTitle, _lyrics, CurrentSource);
                return;
            }

            CurrentSource = "None";
            _lyrics = new List<LyricsLine>();
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
            const int LongPauseThresholdMs = 2000; // treat tail as silence if interval >= this
            const double SpokenPortionCap = 0.60;  // max portion of a very-long interval used for words
            const int MaxWordMs = 350;             // per-token cap
            const int MinWordMs = 60;              // per-token floor

            // punctuation pauses (silent, no highlight)
            const int CommaPauseMs = 120;          // ← requested "a bit of delay" after comma
            const int MidPauseMs = 140;            // ; :
            const int FullStopPauseMs = 180;       // . ! ?

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

            // ---- Allowed content window (do not exceed) ----
            long allowedContentMs;
            if (interval >= LongPauseThresholdMs)
            {
                // for very long gaps, reveal up to SpokenPortionCap of the interval, also
                // bounded by per-token caps to avoid super slow crawl
                long capByPortion = (long)(interval * SpokenPortionCap);
                long capByPerToken = (long)rawTokens.Count * MaxWordMs;
                allowedContentMs = Math.Min(capByPortion, capByPerToken);
                allowedContentMs = Math.Max(allowedContentMs, Math.Min(interval, rawTokens.Count * MinWordMs));
            }
            else
            {
                allowedContentMs = interval;
            }

            // ---- Initial highlight durations (proportional to token weight) ----
            var idealDurations = new double[rawTokens.Count];
            for (int i = 0; i < rawTokens.Count; i++)
                idealDurations[i] = allowedContentMs * (weights[i] / (double)totalWeight);

            // Clamp to min/max per token
            var highlightDur = new double[rawTokens.Count];
            for (int i = 0; i < rawTokens.Count; i++)
                highlightDur[i] = Math.Clamp(idealDurations[i], MinWordMs, MaxWordMs);

            // ---- Pause after punctuation (silent) ----
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

            // Total with pauses
            double baseSum = highlightDur.Sum();
            int pauseSum = pauseAfter.Sum();
            double totalWithPauses = baseSum + pauseSum;

            // If total exceeds allowed window, scale down highlights (keep pauses as is)
            if (totalWithPauses > allowedContentMs && baseSum > 0)
            {
                double scale = (allowedContentMs - pauseSum) / baseSum;
                // avoid negative or ridiculous
                scale = Math.Clamp(scale, 0.2, 1.0);
                for (int i = 0; i < highlightDur.Length; i++)
                    highlightDur[i] *= scale;

                baseSum = highlightDur.Sum();
                totalWithPauses = baseSum + pauseSum;
            }

            // ---- Build cumulative segments: [highlight_i] then [pause_i] ----
            // elapsed falling in a pause_i → return "" (silence).
            var segEnds = new double[rawTokens.Count * 2];
            int seg = 0;
            double acc = 0;
            for (int i = 0; i < rawTokens.Count; i++)
            {
                acc += highlightDur[i];          // highlight segment end
                segEnds[seg++] = acc;
                acc += pauseAfter[i];            // pause segment end
                segEnds[seg++] = acc;
            }

            // ---- Decide what to show for current elapsed ----
            long elapsed = positionMs - current.Time;
            if (elapsed < 0) return "";

            // If we exhausted content before the next line, show nothing
            if (elapsed >= (long)totalWithPauses)
                return "";

            // Find segment
            int segIndex = Array.FindIndex(segEnds, end => elapsed <= end);
            if (segIndex < 0) segIndex = segEnds.Length - 1;

            // Even segIndex → highlight segment; odd segIndex → pause segment
            if ((segIndex % 2) == 1)
                return ""; // in a pause: show nothing

            int tokenIndex = segIndex / 2;
            if (tokenIndex >= rawTokens.Count) tokenIndex = rawTokens.Count - 1;

            // Rebuild with highlight
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
