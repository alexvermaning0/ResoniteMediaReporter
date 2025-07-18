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

        private long _lastKnownPosition = 0;
        private DateTime _lastPositionUpdateTime = DateTime.MinValue;
        private bool _isPlaying = false;
        private bool _wordSyncMode = false; // default to "line" mode


        public event Action<string> OnLyricUpdate;

        public LyricsService(WindowsMediaService wmService)
        {
            _wmService = wmService;
            _lyricsFetcher = new LyricsFetcher();

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

            long simulatedPosition;
            if (isNowPlaying)
            {
                if (!_isPlaying)
                {
                    _lastKnownPosition = smtcPos;
                    _lastPositionUpdateTime = DateTime.UtcNow;
                    _isPlaying = true;
                }
                else
                {
                    long expected = _lastKnownPosition + (long)(DateTime.UtcNow - _lastPositionUpdateTime).TotalMilliseconds;
                    long difference = smtcPos - expected;

                    // Only resync if position jumped forward significantly or it's off by more than a full second
                    if (difference > 500 || Math.Abs(difference) > 1500 && _lyricsFetcher.NeedsNewSong(title, artist))
                    {
                        _lastKnownPosition = smtcPos;
                        _lastPositionUpdateTime = DateTime.UtcNow;
                    }

                }
                simulatedPosition = _lastKnownPosition + (long)(DateTime.UtcNow - _lastPositionUpdateTime).TotalMilliseconds;
            }
            else
            {
                _isPlaying = false;
                _lastKnownPosition = smtcPos;
                simulatedPosition = _lastKnownPosition;
            }

            if (_lyricsFetcher.NeedsNewSong(title, artist))
            {
                _lyricsFetcher.FetchLyrics(title, artist);
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
                formattedLyric = _lyricsFetcher.GetCurrentLineFormatted(simulatedPosition);
                displayLyric = formattedLyric;
                _currentLyric = formattedLyric;
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
            if (shouldUpdate || (DateTime.UtcNow - _lastBroadcastTime).TotalSeconds >= 4)
            {
                double duration = _lyricsFetcher.GetSongLength() / 1000.0;
                double progress = duration > 0 ? Math.Min(simulatedPosition / (duration * 1000.0), 1.0) : 0;
                string message = $"{formattedLyric} {progress:F3}";

                OnLyricUpdate?.Invoke(message);
                _lastBroadcastTime = DateTime.UtcNow;
            }
            UpdateConsole(_lastTitle, _lastArtist, simulatedPosition);

        }

        private void UpdateConsole(string title, string artist, long position)
        {
            Console.Clear();

            Console.WriteLine($"🎵 Now Playing: {title} - {artist}");
            Console.WriteLine($"📡 Source: {_currentSource}");
            Console.WriteLine($"🕒 Position: {position} ms");
            Console.Write("🎤 Lyric: ");
            WriteLyricWithColor(_currentLyric);
            Console.WriteLine($"🎛 Sync Mode: {(_wordSyncMode ? "Word Sync" : "Full Lyric")}");


            Console.WriteLine("\n\n📋 Debug Log:");
            foreach (var line in _debugLog.Reverse())
                Console.WriteLine(" - " + line);
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

            var parts = lyric.Split(new[] { "<color=yellow>", "</color>" }, StringSplitOptions.None);
            bool inYellow = false;

            foreach (var part in parts)
            {
                if (inYellow)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(part);
                    Console.ResetColor();
                }
                else
                {
                    Console.Write(part);
                }
                inYellow = !inYellow;
            }

            Console.WriteLine();
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
