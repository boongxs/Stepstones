# Stepstones Media Organizer
Stepstones is a local-first, Windows-only, media organizer built with WPF and .NET 9. It provides a way for users to manage and
browse their local collections of media files.<br>
As this was my first project of this scale and in this framework, expect bugs, performance issues, and *strange* 🧙‍♂️ practices.<br>
Heavily inspired by Hladikes' [Pastery](https://github.com/Hladikes/pastery) project<br><br>
![Screenshot 2025-10-10 192034](https://github.com/user-attachments/assets/848fbe01-0f1f-4add-88b4-28b866384199)


## ✨ Features

- **Media Library Management:** Select any local folder to act as a media library. The application saves your selection for future sessions.
- **Automatic Data Synchronization:** On startup, the application automatically scans the selected media folder to find new files and cleans up database records for deleted files.
- **Content Support:**
  - **Maximum Format Compatibility**: Ensures a broad support for various media types by using **ImageSharp** for images and **FFmpeg** for videos and audio files.
  - **Automatic Transcoding**: Incompatible video formats (like HEVC) are automatically converted for smooth playback.
- **Responsive Layout:** A dynamic grid layout that adjusts to your window size, ensuring a clean, gapless presentation.
- **Interactive Overlay:** Hovering over any media item reveals an overlay with four commands:
  - 📋 **Copy:** Copies the file to the system clipboard, allowing it to be pasted in other applications.
  - 🏷️ **Edit Tags:** Add or edit space-separated tags to organize and easily find your media.
  - 🔍 **Enlarge:** View media files directly in Stepstones, automatically scaling the enlarged version to fit your window.
  - 🗑️ **Delete:** Permanently deletes the file from your disk after a confirmation.
- **Real-time Tag Filtering:** A debounced search bar filters your entire library as you type.
- **Paging System:** A simple paging system to efficiently manage and browse large media libraries without sacrificing performance.

## 🚀 Getting Started
1. Head over to the Releases page.
2. Download the `stepstones-setup.exe` file from the latest release.
3. Run the installer and follow the on-screen instructions.

## ✍️ Future Plans
- Tagging system that is able to use boolean operators such as `-` to exclude media items that contain the tag that follows after it.
- Ability to select more than one file for bulk deleting or extracting.
- Some way to share media folders with different users.
- Timeline slider for video and audio media items.
- Animations for Enlarge, Edit Tags, Delete commands
- Rework the file uploading process to be more independent.
- Icons in the media items hover overlay need to be replaced with vector ones.

## 🚨 Known Issues
- When viewing video files using Enlarge command, and user resizes their window during playback, the video becomes unresponsive.
- When quickly changing pages, the pagination controls can get locked up and the overall process feels sluggish.
- Enlarge command sometimes takes a small amount of time before showing the media items in the Dialog for the first time.
- Some GIF files take too long to load using Enlarge command.
