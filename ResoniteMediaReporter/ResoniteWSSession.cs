using NetCoreServer;
using ResoniteMediaReporter.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace ResoniteMediaReporter
{
    class ResoniteWSSession : WsSession
    {
        private WindowsMediaService WMService { get; set; }
        public ResoniteWSSession(WsServer server) : base(server) => WMService = new WindowsMediaService();

        public override void OnWsConnected(HttpRequest request)
        {
            Console.WriteLine($"Client Connected With Id {Id}");
        }

        public override async void OnWsReceived(byte[] buffer, long offset, long size)
        {
            string msg = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);

            if (msg == "GetCurrentlyPlayingMedia")
            {
                // update WMService now playing
                var nowPlaying = await WMService.UpdateAndGetCurrentlyPlayingMedia();

                // send now playing
                SendText(nowPlaying);
            }
        }

        public override void OnWsDisconnected()
        {
            Console.WriteLine($"Client With Id {Id} Disconnected");
            WMService.Dispose();
        }
    }
}
