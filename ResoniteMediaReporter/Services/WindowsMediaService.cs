using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace ResoniteMediaReporter.Services
{
    public class WindowsMediaService
    {
        public string NowPlaying { get; private set; } = "Not Currently Playing Anything";

        public GlobalSystemMediaTransportControlsSessionManager? MediaTransportControlsSessionManager;
        public GlobalSystemMediaTransportControlsSession? CurrentMediaSession;
        public GlobalSystemMediaTransportControlsSessionMediaProperties? CurrentMediaProperties;

        public WindowsMediaService()
        {
            Console.WriteLine("Initializing Windows Media Service For Client...");
            SetSystemMediaTransportControlsSessionManager().GetAwaiter().GetResult();
            Console.WriteLine("WMS Ready");
        }
        private async Task SetSystemMediaTransportControlsSessionManager() => MediaTransportControlsSessionManager = await GetSystemMediaTransportControlsSessionManager();

        public async Task<string> UpdateAndGetCurrentlyPlayingMedia()
        {
            try
            {
                var session = MediaTransportControlsSessionManager!.GetCurrentSession();
                if (session != null)
                {
                    CurrentMediaSession = session;
                    CurrentMediaProperties = await GetMediaProperties(CurrentMediaSession);

                    var playbackInfo = CurrentMediaSession.GetPlaybackInfo();

                    if (!string.IsNullOrEmpty(CurrentMediaProperties.Artist) && !string.IsNullOrEmpty(CurrentMediaProperties.Title))
                    {
                        NowPlaying = CurrentMediaProperties.Artist + " - " + CurrentMediaProperties.Title;
                    }
                    else
                    {
                        NowPlaying = "Media Not Detected";
                    }

                    if (CurrentMediaProperties.Artist == CurrentMediaProperties.Title)
                    {
                        NowPlaying = "[Amazon Music] " + CurrentMediaProperties.Artist + " - " + CurrentMediaProperties.Title;
                    }

                    if (playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused)
                    {
                        NowPlaying = "[Paused] " + NowPlaying;
                    }

                    return NowPlaying;
                }
                else
                {
                    NowPlaying = "Not Currently Playing Anything";
                    return NowPlaying;
                }
            } catch (Exception ex)
            {
                NowPlaying = "Not Currently Playing Anything";
                Console.WriteLine($"[EXCEPTION CAUGHT IN MEDIA SERVICE] {ex.StackTrace}");
                return NowPlaying;
            }
        }

        public void Dispose()
        {
            Console.WriteLine("Disposing Windows Media Service...");

            MediaTransportControlsSessionManager = null;
            CurrentMediaSession = null;
            CurrentMediaProperties = null;

            NowPlaying = "Not Currently Playing Anything";

            Console.WriteLine("Disposed.");
        }

        private static async Task<GlobalSystemMediaTransportControlsSessionManager> GetSystemMediaTransportControlsSessionManager() => await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
        private static async Task<GlobalSystemMediaTransportControlsSessionMediaProperties> GetMediaProperties(GlobalSystemMediaTransportControlsSession session) => await session.TryGetMediaPropertiesAsync();
    }
}
