# Stream Music AI Manager

Stream Music AI Manager is a Windows 10/11 WPF desktop application for organizing large music libraries for live streams. It analyzes audio metadata and signal energy, detects duplicates, audits copyright metadata, generates playlists and Excel reports, and switches OBS playlists through OBS WebSocket.

## Features

- Import individual audio files, folders, ZIP archives, or drag and drop them onto the dashboard.
- Supports `.mp3`, `.wav`, `.flac`, `.ogg`, and `.m4a`.
- Parallel processing with `System.Threading.Channels` and worker tasks.
- Metadata extraction through TagLibSharp.
- RMS energy analysis and BPM fallback estimation through NAudio.
- SHA-256 exact duplicate detection and acoustic fingerprint matching.
- Automatic categories:
  - `PvP_Hype`
  - `Gameplay_Energy`
  - `Victory_Moments`
  - `Lobby_Chill`
- XML-driven copyright classification:
  - `SAFE`
  - `REVIEW_REQUIRED`
  - `UNSAFE`
- SQLite library persistence through Dapper and Microsoft.Data.Sqlite.
- M3U playlists, including the 60/25/10/5 mega playlist distribution.
- Styled Excel audit report through ClosedXML.
- OBS WebSocket playlist switching.
- Always-on-top floating overlay.
- Global `Ctrl + Shift + M` overlay toggle.

## Requirements

- Windows 10 or Windows 11
- .NET 8 Desktop Runtime to run a published framework-dependent build
- .NET 8 SDK to build from source
- OBS Studio is optional and only required for OBS integration
- OBS WebSocket 5.x enabled on `ws://localhost:4455` for playlist switching

## Build and run from source

```powershell
git clone https://github.com/<your-account>/StreamMusicAIManager.git
cd StreamMusicAIManager
dotnet restore
dotnet run --project .\StreamMusicAIManager.csproj
```

To publish a direct Windows executable:

```powershell
dotnet publish .\StreamMusicAIManager.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o .\publish
```

Launch `publish\StreamMusicAIManager.exe`. The `publish` directory is intentionally ignored by Git because it is a generated build output.

## First use

1. Start the application.
2. Click **Import Library**, select audio files or a ZIP archive, or drag files/folders onto the dashboard.
3. Wait for analysis and organization to finish.
4. Review the dashboard counters and activity log.
5. Open `C:\StreamMusicLibrary` to inspect organized files.
6. Use **Export Report** to create an Excel audit report.
7. Use the stream mode buttons to switch OBS playlists when OBS is configured.
8. Use **Show Overlay** or press `Ctrl + Shift + M` for the floating controller.

## Output structure

The application creates this structure automatically:

```text
C:\StreamMusicLibrary\
├── PvP_Hype\
├── Gameplay_Energy\
├── Victory_Moments\
├── Lobby_Chill\
├── Duplicates\
├── Review_Required\
├── Unsafe_Copyright\
├── Reports\
├── Playlists\
└── library.db
```

## OBS setup

1. Install OBS Studio and enable OBS WebSocket 5.x.
2. Keep the default WebSocket port `4455`, or update `Config\AppConfig.xml`.
3. Add an OBS Media Source named `StreamBGM`.
4. Configure the source to use a local media file.
5. Start the manager and use a stream mode button.

The manager sends `SetInputSettings` to the `StreamBGM` input with the selected `.m3u` path. OBS remains responsible for playback.

## Configuration

- `Config\AppConfig.xml` contains storage, performance, categorization, mega playlist ratios, and OBS settings.
- `Config\CopyrightDatabase.xml` contains safe labels/licenses and unsafe publishers.

Update these files before launching if your stream setup uses different rules. They are copied to the build output automatically.

## Architecture

```text
WPF UI / MVVM
    └── MainViewModel
         ├── ImportPipeline
         │    ├── AudioAnalyzer
         │    ├── DuplicateDetector
         │    ├── CopyrightScanner
         │    └── LibraryOrganizer
         ├── PlaylistGenerator
         ├── ReportExporter
         ├── DatabaseService
         └── ObsWebSocketManager
```

## Troubleshooting

- **The app does not start:** install the .NET 8 Desktop Runtime or publish self-contained.
- **OBS does not switch playlists:** verify OBS WebSocket is enabled, port `4455` is reachable, and the input is named `StreamBGM`.
- **A track goes to review:** metadata did not match a configured safe label/license. Review the XML database or the generated report.
- **A file cannot be analyzed:** confirm it is a supported format and is readable by the current Windows user.
- **Duplicate detection is unexpected:** exact matches use SHA-256; acoustic matches use a lightweight fingerprint and should be manually reviewed for unusual source files.

## License

Add the project license that matches your intended distribution before publishing a production release.
