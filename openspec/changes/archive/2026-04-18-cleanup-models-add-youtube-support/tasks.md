## 1. Remove Dead Code

- [x] 1.1 Delete DMFT/Model/TikTokTypes.cs
- [x] 1.2 Remove WatermarkPreference from DMFT/Model/LinkInfo.cs
- [x] 1.3 Remove DownloadFormat from DMFT/Model/LinkInfo.cs
- [x] 1.4 Remove TikTokMetadata from DMFT/Model/LinkInfo.cs
- [x] 1.5 Keep OriginalSoundUrl/Name (used by TikTok feature)

## 2. Update Parser for YouTube

- [x] 2.1 Rename TikTokLinkParser.cs to VideoLinkParser.cs
- [x] 2.2 Add YouTube URL regex patterns (youtube.com, youtu.be)
- [x] 2.3 Update all references to TikTokLinkParser in codebase

## 3. Update UI Labels

- [x] 3.1 Change page title in Main.razor to "Video Downloader"
- [x] 3.2 Change input placeholder to "Enter video URL"
- [x] 3.3 Change "Add TikTok" button to "Add Video"
- [x] 3.4 Update History.razor page title to "Download History"
- [x] 3.5 Update any remaining "TikTok" references in UI

## 4. Build and Verify

- [x] 4.1 Run dotnet build to verify no errors
- [x] 4.2 No tests exist (DMFT.Tests not in repo)