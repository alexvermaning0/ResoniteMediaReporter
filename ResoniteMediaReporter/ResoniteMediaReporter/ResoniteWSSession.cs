using NetCoreServer;
using ResoniteMediaReporter.Services;
using System;
using System.Text;

namespace ResoniteMediaReporter
{
    public class ResoniteWSSession : WsSession
    {
        private WindowsMediaService WMService { get; set; }
        public ResoniteWSSession(ResoniteWSServer server) : base(server) => WMService = new WindowsMediaService(this, server);

        public override void OnWsConnected(HttpRequest request)
        {
            base.OnWsConnected(request);
            ResoniteWSServer.ConnectedCount++;
            Console.WriteLine($"[WebSocket] Resonite clients connected: {ResoniteWSServer.ConnectedCount}");
        }

        public override async void OnWsReceived(byte[] buffer, long offset, long size)
        {
            string msg = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
            switch (msg)
            {
                case "ForceUpdateMedia":
                    WMService.UpdateAndGetCurrentlyPlayingMedia();
                    SendText("DONE");
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
                case "SkipToPreviousMedia":
                    await WMService.TryMediaControl(WindowsMediaService.MediaControlType.Previous);
                    SendText("DONE");
                    break;
            }
        }

        public override void OnWsDisconnected()
        {
            base.OnWsDisconnected();
            ResoniteWSServer.ConnectedCount = Math.Max(0, ResoniteWSServer.ConnectedCount - 1);
            Console.WriteLine($"[WebSocket] Resonite clients connected: {ResoniteWSServer.ConnectedCount}");
            WMService.Dispose();
        }
    }
}
