using BarRaider.SdTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Windows.Storage.Streams;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Windows.Media.Control;
using WindowsMediaController;
using static WindowsMediaController.MediaManager;
using System.Drawing;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Phone.Notification.Management;

namespace dbsmtc
{
    [PluginActionId("sd-smtc.pluginaction")]
    public class PluginAction : KeypadBase
    {
        static MediaManager mediaManager;
        Bitmap oldbitmap = new Bitmap(72, 72);
        Bitmap currentbitmap = new Bitmap(72, 72);
        Bitmap pausebitmap = new Bitmap(72, 72);
        String oldtitle = "";
        private bool nSessionState= false;
        private bool PlaybackState = false;

        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.OutputFileName = String.Empty;
                instance.InputString = String.Empty;
                return instance;
            }

            [FilenameProperty]
            [JsonProperty(PropertyName = "outputFileName")]
            public string OutputFileName { get; set; }

            [JsonProperty(PropertyName = "inputString")]
            public string InputString { get; set; }
        }

        #region Private Members

        private PluginSettings settings;

        #endregion
        public PluginAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.settings = PluginSettings.CreateDefaultSettings();
                SaveSettings();
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }
            mediaManager = new MediaManager();
            mediaManager.OnAnySessionOpened += MediaManager_OnAnySessionOpened;
            mediaManager.OnAnySessionClosed += MediaManager_OnAnySessionClosed;
            mediaManager.OnFocusedSessionChanged += MediaManager_OnFocusedSessionChanged;
            mediaManager.OnAnyPlaybackStateChanged += MediaManager_OnAnyPlaybackStateChanged;
            mediaManager.OnAnyMediaPropertyChanged += MediaManager_OnAnyMediaPropertyChanged;
            mediaManager.OnAnyTimelinePropertyChanged += MediaManager_OnAnyTimelinePropertyChanged;
            mediaManager.Start();
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");
            //mediaManager.Dispose();
        }

        public override void KeyPressed(KeyPayload payload)
        {
            if (nSessionState)
            {
                KeyboardSimulator.SendCtrlAltNum2();
            }
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");
        }

        public override void KeyReleased(KeyPayload payload) { }

        public async override void OnTick() {
            if (nSessionState)
            {
                if (PlaybackState)
                {
                    //await Connection.SetTitleAsync("Playing");
                    if (oldbitmap != currentbitmap)
                    {
                        oldbitmap = currentbitmap;
                        await Connection.SetImageAsync(currentbitmap);
                    }
                }
                else
                {
                    //await Connection.SetTitleAsync("Paused");
                    if (oldbitmap != pausebitmap && oldbitmap != null)
                    {
                        oldbitmap = pausebitmap;
                        await Connection.SetImageAsync(pausebitmap);
                    }
                }

            }
            else
            {
                await Connection.SetTitleAsync("Stoped");
            }
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        private void MediaManager_OnAnySessionOpened(MediaManager.MediaSession session)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{session.Id} Opened");
            if ($"{session.Id}" == "cloudmusic.exe")
            {
                nSessionState = true;
            }
        }
        private void MediaManager_OnAnySessionClosed(MediaManager.MediaSession session)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{session.Id} Closed");
            if ($"{session.Id}" == "cloudmusic.exe")
            {
                nSessionState = false;
            }
        }

        private void MediaManager_OnFocusedSessionChanged(MediaManager.MediaSession mediaSession)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Now focused on {mediaSession?.ControlSession?.SourceAppUserModelId}");
            if ($"{mediaSession?.ControlSession?.SourceAppUserModelId}" == "cloudmusic.exe")
            {
                nSessionState = true;
            }
        }

        private void MediaManager_OnAnyPlaybackStateChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionPlaybackInfo args)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{sender.Id} is now {args.PlaybackStatus}");
            if ($"{sender.Id}" == "cloudmusic.exe")
            {
                nSessionState = true;
            }
            if ($"{sender.Id}" == "cloudmusic.exe")
            {
                if ($"{args.PlaybackStatus}" == "Playing")
                {

                    PlaybackState = true;
                }
                else
                {
                    pausebitmap = ConvertToGrayscale(currentbitmap);
                    PlaybackState = false;
                }
            }
        }

        private async void MediaManager_OnAnyMediaPropertyChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionMediaProperties args)
        {
            if ($"{args.Title}" != oldtitle || oldtitle == "")
            {
                if (sender.Id == "cloudmusic.exe")
                {
                    oldtitle = $"{args.Title}";
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"{sender.Id} is now playing {args.Title} {(string.IsNullOrEmpty(args.Artist) ? "" : $"by {args.Artist}")}");
                    nSessionState = true;
                    currentbitmap = await ConvertToBitmap(args.Thumbnail);
                    var playbackinfo = sender.ControlSession.GetPlaybackInfo();
                    if ($"{playbackinfo.PlaybackStatus}" == "Playing")
                    {
                        PlaybackState = true;
                        await Connection.SetImageAsync(currentbitmap);
                        oldbitmap = currentbitmap;
                    }
                    else
                    {
                        PlaybackState = false;
                        pausebitmap = ConvertToGrayscale(currentbitmap);
                        await Connection.SetImageAsync(pausebitmap);
                        oldbitmap = pausebitmap;
                    }
                }
            }
            
        }

        private static void MediaManager_OnAnyTimelinePropertyChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionTimelineProperties args)
        {
            
        }

        #region Private Methods

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        private async Task<Bitmap> ConvertToBitmap(IRandomAccessStreamReference thumbnail)
        {
            if (thumbnail != null)
            {
                using (IRandomAccessStream fileStream = await thumbnail.OpenReadAsync().AsTask())
                {
                    // 创建缓冲区并从流中读取字节
                    var buffer = new byte[fileStream.Size];
                    using (var dataReader = new DataReader(fileStream))
                    {
                        await dataReader.LoadAsync((uint)fileStream.Size);
                        dataReader.ReadBytes(buffer);
                    }

                    // 使用字节数组来初始化Bitmap
                    using (var ms = new MemoryStream(buffer))
                    {
                        var originalBitmap = new Bitmap(ms);

                        // Resize the original bitmap to your desired size (72x72)
                        var resizedBitmap = new Bitmap(originalBitmap, new Size(72, 72));
                        return resizedBitmap;
                    }
                }
            }

            return null;
        }

        private static Bitmap ConvertToGrayscale(Bitmap original)
        {
            Bitmap grayBitmap = new Bitmap(original.Width, original.Height);

            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    Color pixelColor = original.GetPixel(x, y);
                    int grayValue = (int)(pixelColor.R * 0.3 + pixelColor.G * 0.59 + pixelColor.B * 0.11);
                    grayValue -= 50;
                    if (grayValue < 0)
                    {
                        grayValue = 0;
                    }
                    Color newColor = Color.FromArgb(grayValue, grayValue, grayValue);
                    grayBitmap.SetPixel(x, y, newColor);
                }
            }

            return grayBitmap;
        }

        #endregion
    }
}