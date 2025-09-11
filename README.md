# Stepstones Media Organizer
Stepstones is a local-first, Windows-only, media organizer built with WPF and .NET 9. It provides a way for users to manage and
browse their local collections of images and videos. As this was my first project of this scale and in this framework, expect bugs,
performance issues, and *strange* 🧙‍♂️ practices.<br>
Heavily inspired by Hladikes' [Pastery](https://github.com/Hladikes/pastery) project<br><br>
![step](https://github.com/user-attachments/assets/3f7366c6-08a8-4c5a-b66c-7fcda3bb3d17)


## ✨ Features

- **Media Library Management:** Select any local folder to act as a media library. The application saves your selection for future sessions.
- **Automatic Data Synchronization:** On startup, the application automatically scans the selected media folder to find new files and cleans up database records for deleted files.
- **Thumbnail Generation:** Automatically creates and caches uniform thumbnails for a smooth and responsive browsing experience.
- **Maximum Format Compatibility:** Ensures a broad support for various media types by using **ImageSharp** for images and **FFmpeg** for videos.
- **Responsive Grid Layout:** A dynamic `UniformGrid` that adjusts the number of columns to perfectly fit the window size, ensuring a clean, gapless layout.
- **Interactive Overlay:** Hovering over a media item reveals an icon-based overlay with four command buttons that interact with the media file in various ways:
  - **Copy:** Copies the file to the system clipboard, allowing it to be pasted in other applications.
  - **Edit:** Opens a dialog to add or edit space-separated tags for an item, which are saved to the database.
  - **Enlarge:** Allows the user to view the media file, which is scaled to fit the application window's dimensions while preserving the original aspect ratio.
  - **Delete:** Permanently deletes the file from media folder, its thumbnail, and its database record after user confirmation.
- **Real-time Tag Filtering:** A debounced search box that filters the media grid in real-time by querying the database based on the tags user has entered.
- **Paging System:** A simple paging system to efficiently manage and browse large media libraries without sacrificing performance.

## 🚀 Getting Started
A ready to use version of the application can be downloaded from the [Releases](https://github.com/boongxs/Stepstones/releases) page. 
Simply download the `.zip` file from the latest version, extract it, and run `stepstones.exe`.

## ✍️ Future Plans
- Tagging system that is able to use boolean operators such as `-` to exclude media items that contain the tag that follows after it
- Audio files and other file formats support
- Ability to select more than one file for bulk deleting or extracting

## 🚨 Known Issues
- The scrollbar is always visible, even when not needed
- UI freeze when loading into a media folder whose files don't have valid thumbnail path
