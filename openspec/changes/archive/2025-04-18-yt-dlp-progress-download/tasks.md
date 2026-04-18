## 1. Model Changes

- [x] 1.1 Add progress properties to LinkInfo.cs (DownloadedBytes, TotalBytes, Speed, EtaSeconds, ProgressPercent, CurrentFileName)

## 2. MediaDownloader Changes

- [x] 2.1 Create DownloadProgress class with Status, DownloadedBytes, TotalBytes, Speed, EtaSeconds
- [x] 2.2 Add Action<DownloadProgress> callback to IMediaDownloader interface
- [x] 2.3 Implement progress streaming in MediaDownloader.RunYtDlpAsync() using OutputDataReceived event and JSON parsing with --progress-template flag
- [x] 2.4 Add progress callback invocation on each progress update

## 3. DownloadEngineAdapter Changes

- [x] 3.1 Wire progress callback to update LinkInfo progress properties
- [x] 3.2 Update CurrentFileName at start of each download task (for Video + Audio parallel)
- [x] 3.3 Trigger UI refresh via container state change after progress update

## 4. Main.razor UI Changes

- [x] 4.1 Add progress bar in "Currently Downloading" card (% complete)
- [x] 4.2 Add progress details: downloaded/total bytes, speed/sec, ETA
- [x] 4.3 Add current file indicator (e.g., "Downloading: video.mp4")

## 5. Verification

- [x] 5.1 Run dotnet build to verify no compilation errors
- [x] 5.2 Run tests to verify existing functionality preserved