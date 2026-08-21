# Solara Launcher

A custom, lightweight Minecraft launcher for Windows built with WPF and .NET 8.
Solara Launcher handles the full offline install pipeline (version JSON, client jar, libraries, natives, assets, Java runtime) and runs vanilla Minecraft directly without Mojang or third-party authentication.

## Features

- Play any official Minecraft version offline (vanilla client).
- Automatic download and verification of version manifest, client jar, libraries, native libraries, assets, and logging configuration.
- SHA-1 integrity verification for every downloaded file, with automatic retries on failure.
- Automatic provisioning of the Java runtime required by each Minecraft version (uses Mojang's Java runtime manifest).
- Installations are recorded per version + directory and shown in the **Installations** view. Each entry has Play and Delete actions.
- The launcher hides while the game is running and reappears automatically when Minecraft is closed.
- Settings:
  - Keep launcher open after launching the game.
  - Theme: Dark, Light, or follow System (Windows).
  - Default install directory (browse to choose).
  - Allocated memory slider, minimum 1 GB, maximum capped to the system's physical RAM (detected at runtime).
- Persistent configuration stored in the Windows Registry under `HKCU\Software\MinecraftLauncher`.
- Persistent installation list stored as JSON under `%LOCALAPPDATA%\SolaraLauncher\installations.json`.
- Single-instance guard via named Mutex to prevent two launchers running at the same time.

## Tech Stack

- .NET 8 (`net8.0-windows`)
- WPF (XAML + C#)
- MaterialDesignThemes 5.3.2
- Self-contained, single-file publish (`win-x64`)

## Requirements

- Windows 10 1809 or newer (x64)
- No preinstalled Java required; the launcher downloads the correct Java runtime per Minecraft version.
- Internet connection for the first install of a version (subsequent launches are offline and use cached files).

## Build

```powershell
dotnet build MinecraftLauncher.csproj -c Release
```

Publish a single-file release executable:

```powershell
dotnet publish MinecraftLauncher.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

The output binary is named `Minecraft.exe`.

## Usage

1. Launch `Minecraft.exe`.
2. On the **Play** page, enter a player name and pick a Minecraft version, then press **PLAY**.
3. The launcher downloads and verifies all required files. When Minecraft starts, the launcher window hides.
4. When you close Minecraft, the launcher window reappears automatically.
5. Re-launch any saved version from the **Installations** page.
6. Adjust behaviour on the **Settings** page.

## Configuration

All settings are persisted automatically.

| Setting                | Stored at                                                              | Notes                                                          |
|------------------------|------------------------------------------------------------------------|----------------------------------------------------------------|
| Player name            | `HKCU\Software\MinecraftLauncher\PlayerName`                           |                                                                |
| Version                | `HKCU\Software\MinecraftLauncher\Version`                              |                                                                |
| Allocated RAM (MB)     | `HKCU\Software\MinecraftLauncher\RamMb`                                | Clamped to 1024 - total physical RAM.                          |
| Keep launcher open     | `HKCU\Software\MinecraftLauncher\KeepLauncherOpen`                     | 0 or 1.                                                        |
| Theme                  | `HKCU\Software\MinecraftLauncher\Theme`                                | `Dark`, `Light`, or `System`.                                  |
| Default install dir    | `HKCU\Software\MinecraftLauncher\InstallDirectory`                     | Empty = default `%APPDATA%\.minecraft`.                        |
| Installations list     | `%LOCALAPPDATA%\SolaraLauncher\installations.json`                     | Version, directory, installed timestamp.                       |

## Project Structure

```
APP/
  App.xaml / App.xaml.cs            Application entry, single-instance guard, theme bootstrap.
  MainWindow.xaml / .xaml.cs        Main UI: Play, Installations, Settings views.
  MinecraftLauncher.csproj          Project file (WPF, self-contained, win-x64).
  Config/
    LauncherConfig.cs               Registry-backed settings (load/save).
    InstallationStore.cs            JSON-backed installations list.
  Core/
    DownloadManager.cs              Async HTTP client with SHA-1 verification, retry, throttling.
    GameInstaller.cs                Version manifest resolution, downloads, asset + native extraction.
    GameLauncher.cs                 JVM argument assembly and Minecraft process start.
    HashUtil.cs                     File SHA-1 + size validation helper.
    InstallResult.cs                Aggregated install data passed from installer to launcher.
    JavaRuntimeInstaller.cs         Downloads the Java runtime required by a given version.
    RuleEvaluator.cs                Evaluates Mojang library rules (os / arch filtering).
  Models/
    Installation.cs                 Saved installation record.
    RuntimeModels.cs                DTOs for asset index and Java runtime manifests.
    VersionDetail.cs                DTOs for a Minecraft version JSON.
    VersionManifest.cs              DTOs for the Mojang version manifest.
  Resources/
    Themes/
      Dark.xaml                     Stitch palette (dark variant).
      Light.xaml                    Stitch palette (light variant).
    icon.ico                        Application icon.
```

## Architecture

### Install pipeline (`GameInstaller.InstallAsync`)

1. Resolve the version entry from the Mojang version manifest (`version_manifest_v2.json`), with a local JSON cache fallback when offline.
2. Download the version JSON and `client.jar`, verifying SHA-1.
3. Download libraries (skipping entries whose rules do not match Windows x64) and native jars.
4. Extract natives from jar files into `versions/<version>/natives/`.
5. Download the asset index, then all referenced asset objects from `resources.download.minecraft.net` with parallel downloads.
6. Download the logging configuration (client log config).
7. Download the Java runtime declared by `javaVersion.component` if missing.

### Launch pipeline (`GameLauncher.Launch`)

1. Generate an offline UUID from the player name (MD5 of `OfflinePlayer:<name>`, version-3 style bit mangling).
2. Build the classpath from the verified library jars plus the client jar.
3. Build the JVM arguments, substituting `${...}` placeholders using the standard Mojang substitution map (`auth_player_name`, `version_name`, `game_directory`, `assets_root`, `natives_directory`, `classpath`, `auth_uuid`, `auth_session`, etc.).
4. Build the game arguments from `arguments.game` (modern versions) or `minecraftArguments` (legacy versions).
5. Start the `javaw.exe` runtime returned by the Java runtime installer with `Process.Start`.

### Download reliability

All downloads go through `DownloadManager.DownloadFileAsync`, which:

- Returns early if the destination already matches the expected SHA-1 and size.
- Streams to a `.part` file and atomically renames on success.
- Retries up to 3 times with linear backoff.
- Uses bounded concurrency (`SemaphoreSlim`) for bulk asset and library downloads.

### Theme system

The MaterialDesign `BundledTheme` drives control theming. Stitch color brushes are split into two resource dictionaries (`Resources/Themes/Dark.xaml`, `Resources/Themes/Light.xaml`). At startup and whenever the user changes the theme dropdown, `App.ApplyTheme`:

1. Sets `BundledTheme.BaseTheme` to `Dark` or `Light` (resolved from the configured value or from the Windows `AppsUseLightTheme` registry key when set to `System`).
2. Adds the matching Stitch dictionary to `Application.Current.Resources.MergedDictionaries` so DynamicResource bindings across the window refresh.

## Notes and Limitations

- This launcher only supports offline mode. No Microsoft or Mojang account authentication is performed.
- Only the vanilla Minecraft client is supported. Modded clients (Forge, Fabric, Quilt, etc.) are out of scope.
- Only Windows x64 is supported (`win-x64` runtime, native classifiers filtered to Windows).
- Removing an installation from the list only removes the record. Game files on disk are not deleted.
