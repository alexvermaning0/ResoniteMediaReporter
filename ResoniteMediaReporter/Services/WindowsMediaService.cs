using Windows.Media.Control;

namespace ResoniteMediaReporter.Services
{
    public class WindowsMediaService
    {
        public string NowPlaying { get; private set; } = "Not Currently Playing Anything";

        public GlobalSystemMediaTransportControlsSessionManager MediaTransportControlsSessionManager;
        public GlobalSystemMediaTransportControlsSession CurrentMediaSession;
        public GlobalSystemMediaTransportControlsSessionMediaProperties CurrentMediaProperties;

        public ResoniteWSServer server { get; private set; }

        public WindowsMediaService(ResoniteWSServer server)
        {
            this.server = server;

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

                    foreach(var player in server.Config.IgnorePlayers)
                    {
                        if (!CurrentMediaSession.SourceAppUserModelId.Contains(player))
                        {
                            if (System.Diagnostics.Debugger.IsAttached) Console.WriteLine($"[DEBUG] Passing Player - {CurrentMediaSession.SourceAppUserModelId}");
                        }
                        else
                        {
                            if (System.Diagnostics.Debugger.IsAttached) Console.WriteLine($"[DEBUG] Ignoring Player - {CurrentMediaSession.SourceAppUserModelId}");
                            NowPlaying = "Media Not Detected";
                            return NowPlaying;
                        }
                    }

                    if (!string.IsNullOrEmpty(CurrentMediaProperties.Artist) && !string.IsNullOrEmpty(CurrentMediaProperties.Title))
                    {
                        NowPlaying = CurrentMediaProperties.Artist + " - " + CurrentMediaProperties.Title;
                    }
                    else
                    {
                        NowPlaying = "Media Not Detected";
                        if (CurrentMediaSession.SourceAppUserModelId.Contains("Amazon")) NowPlaying = "[Amazon Music] " + NowPlaying;
                    }

                    if (CurrentMediaProperties.Artist == CurrentMediaProperties.Title && CurrentMediaSession.SourceAppUserModelId.Contains("Amazon"))
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
                Console.WriteLine($"[EXCEPTION CAUGHT IN MEDIA SERVICE] {ex.Message}");
                return NowPlaying;
            }
        }

        public async Task TryMediaControl(MediaControlType type)
        {
            try
            {
                var session = MediaTransportControlsSessionManager!.GetCurrentSession();
                if (session != null)
                {
                    switch(type)
                    {
                        case MediaControlType.Play:
                            Console.WriteLine("[WMS] Toggling Play...");
                            await session.TryPlayAsync();
                            break;
                        case MediaControlType.Pause:
                            Console.WriteLine("[WMS] Toggling Pause...");
                            await session.TryPauseAsync();
                            break;
                        case MediaControlType.Stop:
                            Console.WriteLine("[WMS] Toggling Stop...");
                            await session.TryStopAsync();
                            break;
                        case MediaControlType.Skip:
                            Console.WriteLine("[WMS] Skipping...");
                            await session.TrySkipNextAsync();
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("[WMS] Nothing Is Currently Detected");
                }
            } catch (Exception ex)
            {
                Console.WriteLine($"[EXCEPTION CAUGHT IN MEDIA SERVICE] {ex.Message}\n{ex.StackTrace}");
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

        public enum MediaControlType { Play, Pause, Stop, Skip }
    }
}
