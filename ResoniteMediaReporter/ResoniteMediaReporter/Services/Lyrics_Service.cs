using ResoniteMediaReporter.Lyrics.Fetchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Timer = System.Timers.Timer;
using Windows.Media.Control;

namespace ResoniteMediaReporter.Services
{
    public class LyricsService
    {
        private readonly WindowsMediaService _wmService;
        private readonly Timer _timer;
        private readonly LyricsFetcher _lyricsFetcher;
        private readonly List<string> _disabledSources;

        private string _currentLyric = "";
        private string _lastTitle = "";
        private string _lastArtist = "";
        private string _currentSource = "None";
        private DateTime _lastLyricUpdateTime = DateTime.MinValue;
        private DateTime _lastBroadcastTime = DateTime.MinValue;
        private string _lastDisplay = "";
        private readonly Queue<string> _debugLog = new();

        // position simulation (SMTC can be slow)
        private long _lastKnownPosition = 0;
        private DateTime _lastPositionUpdateTime = DateTime.MinValue;
        private bool _isPlaying = false;

        // modes
        private bool _wordSyncMode = false; // default to "line" mode

        public event Action<string> OnLyricUpdate;

        public LyricsService(WindowsMediaService wmService)
        {
            _wmService = wmService;
            _lyricsFetcher = new LyricsFetcher();
            LyricsFetcher.CacheFolder = _wmService?.Config?.CacheFolder ?? "cache";

            _disabledSources = wmService.Config?.DisableLyricsFor?
                .Select(x => x.ToLowerInvariant())
                .ToList() ?? new List<string>();

            _timer = new Timer(200);
            _timer.Elapsed += Tick;
            _timer.Start();

            DebugLog("LyricsService initialized");
        }

        private void Tick(object sender, ElapsedEventArgs e)
        {
            if (_wmService?.CurrentMediaSession == null || _wmService.CurrentMediaProperties == null)
                return;

            string title = _wmService.CurrentMediaProperties.Title ?? "";
            string artist = _wmService.CurrentMediaProperties.Artist ?? "";
            if (string.IsNullOrEmpty(title)) return;

            var timeline = _wmService.CurrentMediaSession.GetTimelineProperties();
            var playback = _wmService.CurrentMediaSession.GetPlaybackInfo();

            bool isNowPlaying = playback.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            long smtcPos = (long)timeline.Position.TotalMilliseconds;

            // apply configurable offset (can be negative)
            int offset = _wmService?.Config?.OffsetMs ?? 0;
            long smtcPosWithOffset = smtcPos + offset;

            long simulatedPosition;
            if (isNowPlaying)
            {
                if (!_isPlaying)
                {
                    // just started playing -> lock to SMTC (+offset)
                    _lastKnownPosition = smtcPosWithOffset;
                    _lastPositionUpdateTime = DateTime.UtcNow;
                    _isPlaying = true;
                }
                else
                {
                    // expected local time since last tick
                    long expected = _lastKnownPosition + (long)(DateTime.UtcNow - _lastPositionUpdateTime).TotalMilliseconds;
                    long difference = smtcPosWithOffset - expected;

                    // ORIGINAL SNAP LOGIC from your ZIP:
                    // snap if jumped forward > 500ms OR if |drift| > 1500ms AND NeedsNewSong(...)
                    if (difference > 500 || (Math.Abs(difference) > 1500 && _lyricsFetcher.NeedsNewSong(title, artist)))
                    {
                        _lastKnownPosition = smtcPosWithOffset;
                        _lastPositionUpdateTime = DateTime.UtcNow;
                    }
                }
                simulatedPosition = _lastKnownPosition + (long)(DateTime.UtcNow - _lastPositionUpdateTime).TotalMilliseconds;
            }
            else
            {
                _isPlaying = false;
                _lastKnownPosition = smtcPosWithOffset;
                simulatedPosition = _lastKnownPosition;
            }

            // new song fetch?
            if (_lyricsFetcher.NeedsNewSong(title, artist))
            {
                _lyricsFetcher.FetchLyrics(title, artist, (int)timeline.EndTime.TotalMilliseconds);
                _currentSource = _lyricsFetcher.CurrentSource ?? "None";
                _lastTitle = title;
                _lastArtist = artist;
                _currentLyric = "";
                _lastLyricUpdateTime = DateTime.MinValue;

                DebugLog($"Fetched new lyrics for: {title} - {artist} (via {_currentSource})");
            }

            bool shouldUpdate = false;
            string formattedLyric = "";
            string displayLyric = "";

            if (_disabledSources.Contains(_currentSource.ToLowerInvariant()))
            {
                formattedLyric = "";
                displayLyric = "";
            }
            else if (_wordSyncMode)
            {
                // per-word highlight using next-line interval; returns "" if interval invalid/too big
                formattedLyric = _lyricsFetcher.GetCurrentLineWordSync(simulatedPosition);
                displayLyric = formattedLyric;
                _currentLyric = formattedLyric;
                if (!string.IsNullOrEmpty(formattedLyric))
                    shouldUpdate = true;
            }
            else
            {
                string newLine = _lyricsFetcher.GetCurrentLine(simulatedPosition);
                if (newLine != _currentLyric)
                {
                    _currentLyric = newLine;
                    _lastLyricUpdateTime = DateTime.UtcNow;
                    shouldUpdate = true;
                }
                else if (!string.IsNullOrEmpty(_currentLyric) &&
                         (DateTime.UtcNow - _lastLyricUpdateTime).TotalMilliseconds > 5000)
                {
                    _currentLyric = "";
                    shouldUpdate = true;
                }

                formattedLyric = _currentLyric; // for broadcast
                displayLyric = _currentLyric;
            }

            // progress (always send; prefer SMTC duration)
            double durationMs = timeline.EndTime.TotalMilliseconds > 0
                ? timeline.EndTime.TotalMilliseconds
                : _lyricsFetcher.GetSongLength();

            double progress = durationMs > 0
                ? Math.Min(Math.Max(simulatedPosition, 0) / durationMs, 1.0)
                : 0;

            // broadcast cadence: lyric change OR heartbeat each 4s
            if (shouldUpdate || (DateTime.UtcNow - _lastBroadcastTime).TotalSeconds >= 4)
            {
                string message = $"{(formattedLyric ?? "")} {progress:F3}";
                OnLyricUpdate?.Invoke(message);
                _lastBroadcastTime = DateTime.UtcNow;
            }

            // keep your console vibe; add mm:ss + offset
            UpdateConsole(_lastTitle, _lastArtist, simulatedPosition);
        }

        private void UpdateConsole(string title, string artist, long position)
        {
            Console.Clear();

            Console.WriteLine($"🎵 Now Playing: {title} - {artist}");
            Console.WriteLine($"📡 Source: {_currentSource}");
            Console.WriteLine($"🕒 Position: {FormatTime(position)}");
            Console.WriteLine($"⏱ Offset: {_wmService?.Config?.OffsetMs ?? 0} ms");
            Console.Write("🎤 Lyric: ");
            WriteLyricWithColor(_currentLyric);
            Console.WriteLine($"🎛 Sync Mode: {(_wordSyncMode ? "Word Sync" : "Full Lyric")}");

            Console.WriteLine("\n\n📋 Debug Log:");
            foreach (var line in _debugLog.Reverse())
                Console.WriteLine(" - " + line);
        }

        private string FormatTime(long ms)
        {
            long absMs = Math.Abs(ms);
            long minutes = absMs / 60000;
            long seconds = (absMs % 60000) / 1000;
            string sign = ms < 0 ? "-" : "";
            return $"{sign}{minutes:00}:{seconds:00} ({ms} ms)";
        }

        private void WriteLyricWithColor(string lyric)
        {
            if (string.IsNullOrEmpty(lyric))
            {
                Console.WriteLine();
                return;
            }

            if (!_wordSyncMode)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(lyric);
                Console.ResetColor();
                return;
            }

            // parse <color=yellow>...</color> spans
            int idx = 0;
            while (idx < lyric.Length)
            {
                int startTag = lyric.IndexOf("<color=yellow>", idx, StringComparison.OrdinalIgnoreCase);
                if (startTag == -1)
                {
                    Console.ResetColor();
                    Console.WriteLine(lyric.Substring(idx));
                    break;
                }

                if (startTag > idx)
                {
                    Console.ResetColor();
                    Console.Write(lyric.Substring(idx, startTag - idx));
                }

                int endTag = lyric.IndexOf("</color>", startTag, StringComparison.OrdinalIgnoreCase);
                if (endTag == -1)
                {
                    Console.ResetColor();
                    Console.WriteLine(lyric.Substring(startTag));
                    break;
                }

                int highlightStart = startTag + "<color=yellow>".Length;
                string highlighted = lyric.Substring(highlightStart, endTag - highlightStart);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(highlighted);

                idx = endTag + "</color>".Length;
                if (idx >= lyric.Length)
                    Console.WriteLine();
            }

            Console.ResetColor();
        }

        public void EnableWordSync()
        {
            _wordSyncMode = true;
            DebugLog("Switched to Word Sync mode");
        }

        public void DisableWordSync()
        {
            _wordSyncMode = false;
            DebugLog("Switched to Line Sync mode");
        }

        private void DebugLog(string message)
        {
            if (_debugLog.Count >= 5)
                _debugLog.Dequeue();
            _debugLog.Enqueue($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
    }
}
