
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AForge.Video;
using AForge.Video.DirectShow;
using MobileLinesManager.Models;
using MobileLinesManager.Services;
using ZXing;
using ZXing.Windows.Compatibility;

namespace MobileLinesManager.Views
{
    public partial class QRScannerWindow : Window
    {
        private readonly IQRService _qrService;
        private FilterInfoCollection _videoDevices;
        private VideoCaptureDevice _videoSource;
        private readonly DispatcherTimer _scanTimer;
        private Line? _scannedLine;
        private bool _isScanning = false;
        private readonly SemaphoreSlim _scanSemaphore = new SemaphoreSlim(1, 1);

        public Line? ScannedLine => _scannedLine;

        public QRScannerWindow(IQRService qrService)
        {
            InitializeComponent();
            _qrService = qrService;
            
            _scanTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _scanTimer.Tick += ScanTimer_Tick;

            Loaded += QRScannerWindow_Loaded;
            Closing += QRScannerWindow_Closing;
        }

        private void QRScannerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                
                if (_videoDevices.Count == 0)
                {
                    MessageBox.Show("لم يتم العثور على كاميرا متصلة بالجهاز", "خطأ", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الوصول للكاميرا: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void StartScanButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_videoDevices.Count > 0)
                {
                    _videoSource = new VideoCaptureDevice(_videoDevices[0].MonikerString);
                    _videoSource.NewFrame += VideoSource_NewFrame;
                    _videoSource.Start();

                    _isScanning = true;
                    _scanTimer.Start();

                    StartScanButton.IsEnabled = false;
                    StopScanButton.IsEnabled = true;
                    PlaceholderPanel.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تشغيل الكاميرا: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopScanButton_Click(object sender, RoutedEventArgs e)
        {
            StopScanning();
        }

        private void StopScanning()
        {
            _isScanning = false;
            _scanTimer.Stop();

            if (_videoSource != null && _videoSource.IsRunning)
            {
                _videoSource.SignalToStop();
                _videoSource.WaitForStop();
                _videoSource.NewFrame -= VideoSource_NewFrame;
                _videoSource = null;
            }

            StartScanButton.IsEnabled = true;
            StopScanButton.IsEnabled = false;
            PlaceholderPanel.Visibility = Visibility.Visible;
            CameraImage.Source = null;
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                var bitmap = (Bitmap)eventArgs.Frame.Clone();
                
                Dispatcher.Invoke(() =>
                {
                    CameraImage.Source = BitmapToImageSource(bitmap);
                });
            }
            catch (Exception)
            {
                // Ignore frame processing errors
            }
        }

        private async void ScanTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isScanning || _videoSource == null || !_videoSource.IsRunning)
                return;

            if (!await _scanSemaphore.WaitAsync(0))
                return;

            try
            {
                await Task.Run(() =>
                {
                    try
                    {
                        var bitmap = _videoSource.GetCurrentFrame();
                        if (bitmap == null) return;

                        var reader = new BarcodeReader
                        {
                            AutoRotate = true,
                            Options = new ZXing.Common.DecodingOptions
                            {
                                TryHarder = true,
                                TryInverted = true,
                                PossibleFormats = new[] { BarcodeFormat.QR_CODE }
                            }
                        };

                        var result = reader.Decode(bitmap);
                        
                        if (result != null)
                        {
                            var line = _qrService.ParseQrData(result.Text);
                            
                            if (line != null)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    _scannedLine = line;
                                    DisplayScannedData(line);
                                    ShowSuccessStatus();
                                    StopScanning();
                                });
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore scanning errors
                    }
                });
            }
            finally
            {
                _scanSemaphore.Release();
            }
        }

        private void DisplayScannedData(Line line)
        {
            PhoneNumberText.Text = line.PhoneNumber;
            SerialNumberText.Text = line.SerialNumber;
            AssociatedNameText.Text = line.AssociatedName;
            NationalIdText.Text = line.NationalId;
            CashWalletText.Text = line.CashWalletId;
            GroupIdText.Text = line.GroupId.ToString();

            DataPanel.Visibility = Visibility.Visible;
            SaveButton.IsEnabled = true;
        }

        private async void ShowSuccessStatus()
        {
            StatusOverlay.Visibility = Visibility.Visible;
            await Task.Delay(2000);
            StatusOverlay.Visibility = Visibility.Collapsed;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void QRScannerWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            StopScanning();
            _scanTimer.Stop();
            _scanSemaphore.Dispose();
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using var memory = new MemoryStream();
            bitmap.Save(memory, ImageFormat.Bmp);
            memory.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }
    }
}
