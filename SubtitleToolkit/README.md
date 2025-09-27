# Subtitle Toolkit

A Windows Presentation Foundation application that helps subtitle editors load SubRip (SRT) files, adjust cue timings based on reading speed, split oversized cues into sentences or word groups, and review diagnostics for short displays.

## Features

- **File management**: Load, reload, and save SRT files with UTF-8 encoding.
- **Timing stretch**: Expand short cues using configurable characters-per-second, minimum duration, and minimum gap values.
- **Sentence/word splitting**: Break dense cues into sequential segments while distributing their original time range proportionally.
- **Diagnostics**: Highlight cues that remain shorter than the configured reading-speed threshold.

## Getting Started

1. Open the project on a Windows machine with the .NET 8 SDK installed.
2. Restore dependencies and build the application:
   ```bash
   dotnet restore
   dotnet build
   ```
3. Run the WPF application (from Visual Studio or `dotnet run`).
4. Load an SRT file and use the tabs to stretch timings, split content, and inspect results.

## Project Structure

- `Models/` – Core SRT entities (`SubRipCue`, `SubRipDocument`).
- `Services/` – Parsing, timing adjustment, and splitting services.
- `ViewModels/` – `MainViewModel`, commands, and UI-facing models.
- `App.xaml` / `MainWindow.xaml` – WPF application and layout.

## Limitations

- Network access is required the first time to restore NuGet feeds.
- The app targets Windows-only WPF; it cannot run on non-Windows platforms.

## License

Released under the MIT License. See `LICENSE` if provided by the host repository.
