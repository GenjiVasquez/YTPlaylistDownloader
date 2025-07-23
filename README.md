# YouTube Playlist Downloader (WPF, .NET 8)

<img width="786" height="443" alt="image" src="https://github.com/user-attachments/assets/74670424-212f-40db-8dce-6f2f641dfa50" />


A modern WPF application for downloading YouTube playlists with support for:
- Audio & Video, Audio Only, or Video Only download modes
- User-selectable download folder
- Multi-threaded downloads (choose 1 - 12 concurrent downloads)
- Progress and status display

## Features
- Download all videos from a YouTube playlist
- Choose between audio+video, audio only (MP3), or video only
- Select the output folder for downloads
- Set the number of concurrent downloads for faster performance
- Simple, user-friendly WPF interface

## Requirements
- .NET 8 SDK
- [FFmpeg](https://ffmpeg.org/download.html) (must be in your system PATH or app directory)

## Getting Started
1. **Clone the repository**
2. **Open in Visual Studio 2022+**
3. **Restore NuGet packages** (YoutubeExplode)
4. **Build and run the project**
5. **Make sure FFmpeg is available** (add to PATH or place `ffmpeg.exe` in the app folder)

## Usage
1. Enter a YouTube playlist URL.
2. Select the download mode (Audio & Video, Audio Only, Video Only).
3. Choose the download folder (optional).
4. Set the number of concurrent downloads (1 - 12).
5. Click **Download**.
6. Monitor progress and status in the app.

## Notes
- Downloaded files are saved in the selected folder.
- For audio-only mode, files are converted to MP3 using FFmpeg.
- For video-only mode, only the video stream is saved (no audio).

## Libraries Used
- [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) (YouTube access)
- FFmpeg (media conversion)

## License
MIT License
