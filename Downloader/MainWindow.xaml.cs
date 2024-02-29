using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using YoutubeExplode;
using System.ComponentModel;

namespace Downloader
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(3, 3);
        private int _progressValue1;
        private int _progressValue2;
        private int _progressValue3;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        public int ProgressValue1
        {
            get { return _progressValue1; }
            set
            {
                _progressValue1 = value;
                OnPropertyChanged(nameof(ProgressValue1));
            }
        }

        public int ProgressValue2
        {
            get { return _progressValue2; }
            set
            {
                _progressValue2 = value;
                OnPropertyChanged(nameof(ProgressValue2));
            }
        }

        public int ProgressValue3
        {
            get { return _progressValue3; }
            set
            {
                _progressValue3 = value;
                OnPropertyChanged(nameof(ProgressValue3));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            string videoUrl = Textbox.Text;
            Textbox.Text = string.Empty;
            string outputDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            try
            {
                await semaphore.WaitAsync();

                await DownloadYouTubeVideo(videoUrl, outputDirectory);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                semaphore.Release();
            }
        }

        static async Task DownloadYouTubeVideo(string videoUrl, string outputDirectory)
        {
            var youtube = new YoutubeClient();
            var video = await youtube.Videos.GetAsync(videoUrl);

          
            string sanitizedTitle = string.Join("_", video.Title.Split(Path.GetInvalidFileNameChars()));

          
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
            var muxedStreams = streamManifest.GetMuxedStreams().OrderByDescending(s => s.VideoQuality).ToList();

            if (muxedStreams.Any())
            {
                var streamInfo = muxedStreams.First();
                using var httpClient = new HttpClient();
                var stream = await httpClient.GetStreamAsync(streamInfo.Url);

                string outputFilePath = Path.Combine(outputDirectory, $"{sanitizedTitle}.{streamInfo.Container}");
                using var outputStream = File.Create(outputFilePath);
                await stream.CopyToAsync(outputStream);

                MessageBox.Show("Download completed!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                Console.WriteLine($"Video saved as: {outputFilePath}");
            }
            else
            {
                MessageBox.Show($"No suitable video stream found for {video.Title}.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
