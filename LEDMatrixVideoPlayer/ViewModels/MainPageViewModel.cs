using LEDMatrixVideoPlayer.Utils;
using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace LEDMatrixVideoPlayer.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private static string videoFilePath = @"Assets\TestVideo\fantasmic.mp4";
        public MediaSource VideoMediaSource { get; set; } = MediaSource.CreateFromUri(new Uri($"ms-appx:///{videoFilePath.Replace('\\', '/')}"));
        private CoreDispatcher coreDispatcher;
        private SerialClient client;
        private byte[] currentFrameBytes;
        private MediaPlayerElement mediaPlayer;
        private SoftwareBitmapSource _VideoBitmap;
        public SoftwareBitmapSource VideoBitmap
        {
            get { return _VideoBitmap; }
            set
            {
                _VideoBitmap = value;
                RaisePropertyChanged();
            }
        }

        private List<DeviceInformation> _SerialDevices;
        public List<DeviceInformation> SerialDevices
        {
            get { return _SerialDevices; }
            set
            {
                _SerialDevices = value;
                RaisePropertyChanged();
            }
        }

        public DeviceInformation SelectedSerialDevice { get; set; }

        public async void Init()
        {
            IsLoading = true;

            SerialDevices = await SerialClient.ListSerialDevices();

            IsLoading = false;
        }

        public async void PlayBitmap(MediaPlayerElement mediaPlayer, CoreDispatcher dispatcher)
        {
            StorageFolder InstallationFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            StorageFile file = await InstallationFolder.GetFileAsync(videoFilePath);
            coreDispatcher = dispatcher;

            if (SelectedSerialDevice == null && client == null)
            {
                return;
            }

            try
            {
                client = await SerialClient.CreateFromId(SelectedSerialDevice.Id);
            }
            catch (Exception)
            {
                return;
            }

            mediaPlayer.MediaPlayer.IsVideoFrameServerEnabled = true;
            mediaPlayer.MediaPlayer.VideoFrameAvailable += MediaPlayer_VideoFrameAvailable;
            mediaPlayer.MediaPlayer.Play();

            this.mediaPlayer = mediaPlayer;

            SendSerialLoop();
        }

        private int width = 32;
        private int height = 8;
        private int units = 2;
        private async void MediaPlayer_VideoFrameAvailable(MediaPlayer sender, object args)
        {
            CanvasDevice canvasDevice = CanvasDevice.GetSharedDevice();
            await coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                SoftwareBitmap frameServerDest = new SoftwareBitmap(BitmapPixelFormat.Rgba8, width, height * units, BitmapAlphaMode.Premultiplied);

                using (CanvasBitmap canvasBitmap = CanvasBitmap.CreateFromSoftwareBitmap(canvasDevice, frameServerDest))
                {
                    sender.CopyFrameToVideoSurface(canvasBitmap);
                    currentFrameBytes = canvasBitmap.GetPixelBytes(); //to send via serial
                }

                var bitmapSource = new SoftwareBitmapSource();
                await bitmapSource.SetBitmapAsync(SoftwareBitmap.Convert(SoftwareBitmap.CreateCopyFromBuffer(currentFrameBytes.AsBuffer(), BitmapPixelFormat.Rgba8, width, height * units), BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied));
                VideoBitmap = bitmapSource; //to display result
            });
        }

        private async void SendSerialLoop()
        {
            while (mediaPlayer.MediaPlayer.PlaybackSession.PlaybackState != MediaPlaybackState.None)
            {
                if (currentFrameBytes != null)
                {
                    try
                    {
                        Debug.WriteLine($"Source frame bytes: {currentFrameBytes.Length}");
                        await client.WriteByteAsync(currentFrameBytes);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        // dispose and recover client
                        client.Dispose();
                        var serialDevices = await SerialClient.ListSerialDevices();
                        var sameNameDevice = (from serialdevice in serialDevices
                                              where serialdevice.Name == SelectedSerialDevice.Name
                                              select serialdevice).FirstOrDefault();
                        client = await SerialClient.CreateFromId(sameNameDevice.Id);
                    }
                }
                await Task.Delay(20);
            }
        }

        public async void ReadSerial()
        {
            try
            {
                Debug.WriteLine(await client.ReadAsync());
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            client.Dispose();
        }
    }
}
