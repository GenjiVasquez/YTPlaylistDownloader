using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
// Add this for Windows Forms interop
using System.Windows.Forms;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Playlists;

namespace YTPlaylistDownloader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _downloadFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) + "\\YTDownloads";

        public MainWindow()
        {
            InitializeComponent();
            FolderPathTextBox.Text = _downloadFolder;
        }

        private void ChooseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = _downloadFolder;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _downloadFolder = dialog.SelectedPath;
                FolderPathTextBox.Text = _downloadFolder;
            }
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            string playlistUrl = PlaylistUrlTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(playlistUrl))
            {
                System.Windows.MessageBox.Show("Please enter a playlist URL.");
                return;
            }

            DownloadButton.IsEnabled = false;
            StatusListBox.Items.Clear();
            DownloadProgressBar.Value = 0;

            var mode = (ModeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var youtube = new YoutubeClient();
            int concurrency = (int)ConcurrencySlider.Value;
            try
            {
                var playlist = await youtube.Playlists.GetAsync(playlistUrl);
                var videos = await youtube.Playlists.GetVideosAsync(playlistUrl);
                var videoList = videos.ToList();
                int total = videoList.Count;
                int completed = 0;
                var semaphore = new SemaphoreSlim(concurrency);
                var tasks = videoList.Select(async video =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        await DownloadVideoAsync(youtube, video, mode);
                    }
                    finally
                    {
                        semaphore.Release();
                        Interlocked.Increment(ref completed);
                        Dispatcher.Invoke(() => DownloadProgressBar.Value = (double)completed / total * 100);
                    }
                }).ToList();
                await Task.WhenAll(tasks);
                StatusListBox.Items.Add("Download complete.");
            }
            catch (Exception ex)
            {
                StatusListBox.Items.Add($"Error: {ex.Message}");
            }
            finally
            {
                DownloadButton.IsEnabled = true;
            }
        }

        private async Task DownloadVideoAsync(YoutubeClient youtube, PlaylistVideo video, string mode)
        {
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
            string safeTitle = string.Join("_", video.Title.Split(System.IO.Path.GetInvalidFileNameChars()));
            string outputDir = _downloadFolder;
            Directory.CreateDirectory(outputDir);

            if (mode == "Audio & Video")
            {
                var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();
                if (streamInfo != null)
                {
                    string filePath = System.IO.Path.Combine(outputDir, $"{safeTitle}.mp4");
                    await youtube.Videos.Streams.DownloadAsync(streamInfo, filePath);
                    StatusListBox.Items.Add($"Saved: {filePath}");
                }
            }
            else if (mode == "Audio Only")
            {
                var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                if (audioStreamInfo != null)
                {
                    string tempFile = System.IO.Path.Combine(outputDir, $"{safeTitle}_audio.{audioStreamInfo.Container}");
                    string outFile = System.IO.Path.Combine(outputDir, $"{safeTitle}.mp3");
                    await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, tempFile);
                    await RunFfmpegAsync(tempFile, outFile, "-vn -ab 192k -ar 44100 -y");
                    File.Delete(tempFile);
                    StatusListBox.Items.Add($"Saved: {outFile}");
                }
            }
            else if (mode == "Video Only")
            {
                var videoStreamInfo = streamManifest.GetVideoOnlyStreams().GetWithHighestVideoQuality();
                var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                if (videoStreamInfo != null && audioStreamInfo != null)
                {
                    string videoFile = System.IO.Path.Combine(outputDir, $"{safeTitle}_video.{videoStreamInfo.Container}");
                    string audioFile = System.IO.Path.Combine(outputDir, $"{safeTitle}_audio.{audioStreamInfo.Container}");
                    string outFile = System.IO.Path.Combine(outputDir, $"{safeTitle}_videoonly.mp4");
                    await youtube.Videos.Streams.DownloadAsync(videoStreamInfo, videoFile);
                    await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, audioFile);
                    await RunFfmpegAsync(videoFile, outFile, "-an -c:v copy");
                    File.Delete(videoFile);
                    File.Delete(audioFile);
                    StatusListBox.Items.Add($"Saved: {outFile}");
                }
            }
        }

        private Task RunFfmpegAsync(string input, string output, string args)
        {
            return Task.Run(() =>
            {
                var ffmpegPath = "ffmpeg"; // Assumes ffmpeg is in PATH
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-i \"{input}\" {args} \"{output}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var process = System.Diagnostics.Process.Start(startInfo);
                process.WaitForExit();
            });
        }
    }
}