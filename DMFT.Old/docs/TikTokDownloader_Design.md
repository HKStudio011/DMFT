# TikTok Downloader Design Document

## Objective
Integrate a TikTok downloader into the existing Download Manager (MAUI Blazor) to download original video and audio from TikTok. Extend data models, UI hooks, and download flow while respecting legal and UX constraints.

## Architecture Overview
- TikTokDownloaderService: Orchestrates between parser, metadata extraction, and downloader; returns an updated LinkInfo.
- TikTokLinkParser: Validates TikTok URLs and extracts videoId (and minimal metadata footprint).
- MetadataExtractor: Retrieves supplemental metadata (title, author, duration, thumbnail).
- DownloadEngineAdapter: Bridges with the existing Download Manager flow (LinkInfo, StatusMessage, History).
- MediaDownloader: Optional wrapper around yt-dlp or internal scraper; handles actual media fetch.
- Storage Layer: Extends LinkInfo and History with TikTok-specific fields (VideoId, Thumbnail, Duration, etc.).

## Data Models (Extended)
- LinkInfo (extended)
  - VideoId: string
  - OriginalUrl: string
  - CreatorName: string
  - VideoDurationSeconds: int
  - ThumbnailUrl: string
  - TitleDescription: string
  - WatermarkPreference: enum
  - DownloadFormat: enum
  - TikTokMetadata: TikTokVideoInfo
- TikTokVideoInfo
  - VideoId, Author, Title, DurationSeconds, MusicTitle, HasWatermark, QualityOptions, etc.
- History and StatusMessage: extended with TikTok-origin metadata

## Workflow (End-to-End)
1) User enters a TikTok URL in AddModal.
2) TikTokLinkParser validates and extracts videoId.
3) MetadataExtractor fetches metadata (title, creator, duration, thumbnail).
4) Create/update LinkInfo with TikTok fields and push into the Download Manager workflow.
5) DownloadEngine initiates download using chosen source (no watermark vs. watermark) and saves to destination; update StatusMessage and History.
6) UI shows thumbnail, metadata, and current status.
7) History lists TikTok links with storage paths.

## Technical Decisions (Options)
- URL parsing: best-effort regex with fallback to HTML parsing if needed.
- Download Source: option A - yt-dlp; option B - internal API/scraper (consider legal/usage constraints).
- Metadata: leverage TikTok-provided data or yt-dlp metadata.
- Storage: follow existing naming conventions and folder structure; example: {creator}_{title}_{videoId}.mp4

## UX Considerations
- AddModal: validate TikTok URL; show errors for invalid URLs.
- Main Grid: display thumbnail, title, creator, duration, and status.
- Preview: show metadata/thumbnail before download.
- Legal note: display brief copyright warning when downloading.

## Phased Rollout
- Phase 1: MVP integration into Download Manager
  - Extend LinkInfo with minimal TikTok fields.
  - Implement TikTokLinkParser and basic TikTokVideoInfo.
  - Wire into the download workflow with a basic TikTok download path.
  - Persist and display basic metadata.
- Phase 2: UX enhancements and features
  - Audio-only download, quality controls, batch downloads.
  - Rich metadata in UI; improved error handling.
- Phase 3: Compliance and optimization
  - Logging/telemetry; privacy and copyright guidance.

## Testing Strategy
- Unit tests: parser validity, metadata extraction mocks.
- Integration tests: end-to-end with sample URLs or mocks.
- Manual QA: various URL types (public/private/geo-restricted).

## Risks & Mitigations
- Legal and policy changes: keep explicit user guidance; avoid unauthorized content.
- Anti-scraping measures and API changes: modular design; replaceable components.
- Integration risk: isolate via wrapper/adapter layers.

## Deliverables on Kickoff
- Design docs with API contracts, data flow diagrams, and module responsibilities.
- Skeleton code for TikTok modules (LinkParser, Downloader Service, MetadataExtractor, MediaDownloader, Adapter).
- DI wiring plan and minimal tests skeleton.
- Deployment plan with milestones.

