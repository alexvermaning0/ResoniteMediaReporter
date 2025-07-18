using NetCoreServer;
using ResoniteMediaReporter.Lyrics;
using ResoniteMediaReporter.Services;


namespace ResoniteMediaReporter.Services
{
    public class LyricsWSServer : WsServer
    {
        public LyricsService LyricsService { get; private set; }

        public LyricsWSServer(string address, int port, LyricsService lyricsService) : base(address, port)
        {
            LyricsService = lyricsService;
        }
        protected override TcpSession CreateSession()
        {
            return new LyricsWSSession(this, LyricsService);
        }

    }
}
