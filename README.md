# OpenSubtitleFetcher

A Windows desktop app (WPF) that finds and downloads subtitles for your video files from [OpenSubtitles.com](https://www.opensubtitles.com).

Pick one or more folders, choose a language, and the app searches OpenSubtitles for every video, downloads the most popular match and saves it next to the video as `<video name>.srt`.

## Features

- Scans selected folders for `.mp4`, `.avi` and `.mkv` files (top level only, not subfolders)
- Polish and English subtitles
- Picks the subtitles with the highest download count
- Optionally skips videos that already have subtitles (`.srt`, `.sub`, `.ass`, `.ssa`, `.vtt`)
- Processes several files in parallel while staying under the API rate limit (5 requests/second), and retries automatically on `429 Too Many Requests`
- Stops cleanly once the daily download quota is used up

## Requirements

- Windows
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- An OpenSubtitles.com account and API key (create a consumer at [opensubtitles.com/consumers](https://www.opensubtitles.com/en/consumers))

## Setup

1. Clone the repository:
   ```
   git clone https://github.com/talgrim/OpenSubtitleFetcher.git
   ```
2. In `OpenSubtitleFetcher.Wpf`, copy `appsettings.Local_sample.json` to `appsettings.Local.json` and fill in your API key:
   ```json
   {
     "OpenSubtitlesConfig": {
       "ApiKey": "your-api-key"
     }
   }
   ```
   `appsettings.Local.json` overrides `appsettings.json` and is ignored by git, so your key stays out of the repository.

3. Build and run:
   ```
   dotnet run --project OpenSubtitleFetcher.Wpf
   ```

## Usage

1. Click **Browse...** and select one or more folders (or type paths separated by `;`).
2. Click **Open** to list the video files found.
3. Untick any files you don't want subtitles for.
4. Pick a language and, if needed, tick **Overwrite existing subtitles**.
5. Click **Download**. Progress and errors appear in the log panel at the bottom.

> The free OpenSubtitles plan has a limited number of downloads per day. When it runs out, the app logs a warning and skips the remaining files.

## Configuration

Settings live in the `OpenSubtitlesConfig` section of `appsettings.json` / `appsettings.Local.json`:

| Key         | Description                         | Default                              |
|-------------|-------------------------------------|--------------------------------------|
| `ApiKey`    | Your OpenSubtitles API key          | *(empty, so you must set it)*        |
| `Url`       | API base URL                        | `https://api.opensubtitles.com/api`  |
| `Version`   | API version                         | `1`                                  |
| `UserAgent` | User-Agent sent with every request  | `OpenSubtitleFetcher v0.1`           |

## Project structure

The solution follows Clean Architecture. Dependencies point inwards, toward the Domain.

```
OpenSubtitleFetcher.Wpf ─────────────────────► Application ──► Domain
    │ (composition root)                           ▲
    ├──► Infrastructure ───────────────────────────┤
    └──► Infrastructure.OpenSubtitles ─────────────┘
```

| Project | Responsibility |
|---|---|
| `OpenSubtitleFetcher.Domain` | Core models and rules: video files, subtitle candidates, choosing the best candidate |
| `OpenSubtitleFetcher.Application` | Use cases (`DownloadSubtitlesUseCase`, `GetVideoFilesUseCase`) and the interfaces they depend on (`ISubtitlesProvider`, repositories, `ILogDisplay`) |
| `OpenSubtitleFetcher.Infrastructure` | File system access: finding video files, checking for and saving subtitles |
| `OpenSubtitleFetcher.Infrastructure.OpenSubtitles` | OpenSubtitles REST API client (RestEase), DTO mapping, request throttling and 429 retries |
| `OpenSubtitleFetcher.Wpf` | UI (MVVM with CommunityToolkit.Mvvm), configuration and dependency injection setup |

## Tests

Each layer has its own xUnit test project (`*.Tests`), using Moq for mocks:

```
dotnet test
```

The OpenSubtitles tests run the real RestEase client against a stubbed HTTP handler, so no API key or network access is needed.
