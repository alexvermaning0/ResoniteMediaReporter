using NetCoreServer;
using System;

namespace ResoniteMediaReporter.Services
{
    public class LyricsWSSession : WsSession
    {
        private readonly LyricsService _lyricsService;

        public LyricsWSSession(LyricsWSServer server, LyricsService lyricsService) : base(server)
        {
            _lyricsService = lyricsService;
            _lyricsService.OnLyricUpdate += SendLyric;
        }

        private void SendLyric(string text)
        {
            // sends "lyric progress"
            SendText(text ?? "");
        }

        public override void OnWsConnected(HttpRequest request)
        {
            base.OnWsConnected(request);
            LyricsWSServer.ConnectedCount++;
            Console.WriteLine($"[WebSocket] Lyrics clients connected: {LyricsWSServer.ConnectedCount}");
        }

        public override void OnWsDisconnected()
        {
            base.OnWsDisconnected();
            LyricsWSServer.ConnectedCount = Math.Max(0, LyricsWSServer.ConnectedCount - 1);
            Console.WriteLine($"[WebSocket] Lyrics clients connected: {LyricsWSServer.ConnectedCount}");
        }

        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            var msg = System.Text.Encoding.UTF8.GetString(buffer, (int)offset, (int)size).Trim().ToLowerInvariant();
            if (msg == "wordsync:on")
            {
                _lyricsService.EnableWordSync();
                SendText("Word sync enabled");
            }
            else if (msg == "wordsync:off")
            {
                _lyricsService.DisableWordSync();
                SendText("Line sync enabled");
            }
        }
    }
}
