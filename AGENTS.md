# Repository Guidelines

## Project Structure & Module Organization
This repository is a small Windows Forms application built from C# source files stored at the repository root. Entry point and app lifecycle code live in `Program.cs` and `OverlayAppContext.cs`. UI forms are split across `OverlayForm.cs`, `BorderForm.cs`, and `SettingsForm.cs`. Shared state and geometry helpers live in `AppState.cs`, `Geometry.cs`, `DragMode.cs`, and `NativeMethods.cs`. Build outputs are written to `dist/`, including `dist/ADHDFocusOverlay.exe`.

## Build, Test, and Development Commands
Use the batch script from the repository root:

```bat
build.bat
```

This compiles the WinForms app with the .NET Framework C# compiler and writes the executable to `dist\`.

Run the built app with:

```bat
dist\ADHDFocusOverlay.exe
```

There is no separate package restore or test runner in this repository.

## Coding Style & Naming Conventions
Follow the existing C# style in this codebase:

- Use 4-space indentation and braces on their own lines.
- Keep types `internal` unless public exposure is required.
- Use `PascalCase` for types, methods, properties, and events.
- Use `camelCase` for local variables and private readonly fields.
- Keep files focused on one type each; name the file after the primary type.

Prefer small event handlers and keep platform-specific Windows behavior isolated in helper classes such as `NativeMethods.cs`.

## Testing Guidelines
This repository currently has no automated tests. Validate changes by rebuilding with `build.bat` and manually checking core behaviors: tray icon startup, focus rectangle drag/resize, overlay tint and opacity updates, and startup-toggle persistence. If you add tests later, place them in a dedicated `tests/` directory rather than mixing them into the root.

## Commit & Pull Request Guidelines
Local git history is not available in this snapshot, so use clear, imperative commit messages such as `Fix overlay bounds when resizing`. Keep each commit scoped to one change. Pull requests should include a short summary, manual test notes, and screenshots or short recordings for UI changes.

## Configuration Notes
User settings are stored outside the repo under `%AppData%\ADHDFocusOverlay\settings.ini`. Do not commit generated binaries or user-specific configuration changes unless the release artifact is intentionally updated.
