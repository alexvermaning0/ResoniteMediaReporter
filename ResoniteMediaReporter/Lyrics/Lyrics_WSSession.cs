using NetCoreServer;
using ResoniteMediaReporter.Services;
using System;
using System.Text;


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

        private void SendLyric(string message)
        {
            if (IsConnected)
            {
                try
                {
                    SendText(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LyricsWSSession] Failed to send lyric: {ex.Message}");
                }
            }
        }

        public override void OnWsConnected(HttpRequest request)
        {
            Console.WriteLine($"Lyrics Client Connected With Id {Id}");
        }

        public override void OnWsDisconnected()
        {
            Console.WriteLine($"Lyrics Client With Id {Id} Disconnected");
            _lyricsService.OnLyricUpdate -= SendLyric;
        }
        public override void OnWsReceived(byte[] buffer, long offset, long size)
        {
            string msg = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);

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
