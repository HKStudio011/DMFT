## ADDED Requirements

### Requirement: YouTube URL Detection
The system SHALL recognize and process YouTube URLs in addition to TikTok URLs when user inputs a link.

#### Scenario: Standard YouTube URL
- **WHEN** user enters `https://www.youtube.com/watch?v=dQw4w9WgXcQ`
- **THEN** system identifies it as a valid video URL and queues for download

#### Scenario: YouTube Short URL
- **WHEN** user enters `https://youtu.be/dQw4w9WgXcQ`
- **THEN** system identifies it as a valid video URL and queues for download

#### Scenario: YouTube Playlist URL
- **WHEN** user enters `https://www.youtube.com/playlist?list=PL123456789`
- **THEN** system identifies it as a valid playlist URL

### Requirement: Generic UI Labels
The system SHALL use generic labels that apply to any video platform instead of hardcoded "TikTok" references.

#### Scenario: Page Title
- **WHEN** user opens the application
- **THEN** page title displays "Video Downloader" not "TikTok Downloader"

#### Scenario: Input Placeholder
- **WHEN** user views the URL input field
- **THEN** placeholder text shows "Enter video URL" not "Enter TikTok URL"