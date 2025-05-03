using NetCoreServer;
using ResoniteMediaReporter.Services;
using System.Text;

namespace ResoniteMediaReporter
{
    class ResoniteWSSession : WsSession
    {
        private WindowsMediaService WMService { get; set; }
        public ResoniteWSSession(ResoniteWSServer server) : base(server) => WMService = new WindowsMediaService(server);

        public override void OnWsConnected(HttpRequest request)
        {
            Console.WriteLine($"Client Connected With Id {Id}");
        }

        public override async void OnWsReceived(byte[] buffer, long offset, long size)
        {
            string msg = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            if (System.Diagnostics.Debugger.IsAttached) Console.WriteLine($"[DEBUG] Received Message - {msg}");
            switch(msg)
            {
                case "GetCurrentlyPlayingMedia":
                    // update WMService now playing
                    var nowPlaying = await WMService.UpdateAndGetCurrentlyPlayingMedia();

                    // send now playing
                    SendText(nowPlaying);
                    break;
                case "PlayMedia":
                    await WMService.TryMediaControl(WindowsMediaService.MediaControlType.Play);

                    SendText("DONE");
                    break;
                case "PauseMedia":
                    await WMService.TryMediaControl(WindowsMediaService.MediaControlType.Pause);

                    SendText("DONE");
                    break;
                case "StopMedia":
                    await WMService.TryMediaControl(WindowsMediaService.MediaControlType.Stop);

                    SendText("DONE");
                    break;
                case "SkipToNextMedia":
                    await WMService.TryMediaControl(WindowsMediaService.MediaControlType.Skip);

                    SendText("DONE");
                    break;
            }
        }

        public override void OnWsDisconnected()
        {
            Console.WriteLine($"Client With Id {Id} Disconnected");
            WMService.Dispose();
        }
    }
}
