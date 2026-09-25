# SMAPI Silent Launcher

A small Windows launcher for Stardew Valley's [SMAPI](https://smapi.io). It starts SMAPI in a normal toggleable window, displays a compact overlay while the game loads, and lets you hide or show the terminal with `Ctrl+F12`.

## [Download the latest release on Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/48315)

The launcher does not hide the terminal at startup. It keeps the normal SMAPI console available and adds the overlay with the launch process.

## Features

- Starts SMAPI in Windows Terminal, haven't tested with it uninstalled, should fallback to cmd if Terminal was not found.
- Shows a small overlay with the detected mod count, content pack count, and elapsed launch time.
- Hides or shows the SMAPI terminal with `Ctrl+F12`.
- Cancels an active launch when the overlay is clicked.
- Detects SMAPI startup failures and exits before the game starts.
- Uses a single NativeAOT executable with no .NET runtime dependency.
- Forwards command-line arguments to SMAPI.

## Requirements

- Windows.
- Windows Terminal
- Stardew Valley.
- SMAPI installed in the Stardew Valley folder.
- The launcher executable placed next to `Stardew Valley.exe` and `StardewModdingAPI.exe`.

The launcher expects this layout:

```text
Stardew Valley/
├── SMAPI Silent Launcher.exe
├── Stardew Valley.exe
└── StardewModdingAPI.exe
```

## Installation

1. Install SMAPI using the official [SMAPI installer](https://smapi.io).
2. Copy `SMAPI Silent Launcher.exe` into the Stardew Valley folder.
3. Make sure it is next to `Stardew Valley.exe` and `StardewModdingAPI.exe`.
4. Use the launcher as the program launched by Steam or another launcher.

## Steam launch options

Set the launch options for Stardew Valley in Steam to:

```text
"<GameDirectory>\SMAPI Silent Launcher.exe" --mods-path "mods" %command%
```

`%command%` is forwarded to SMAPI.

## Usage from another launcher

Start `SMAPI Silent Launcher.exe` directly. Arguments after the executable path are forwarded to SMAPI.

## Command-line arguments

| Argument | Behavior |
|---|---|
| `--mods-path <path>` | Sets the folder scanned for the overlay's mod count. The argument is also forwarded to SMAPI. |
| Other arguments | Forwarded to SMAPI without modification. |

Relative mod paths are resolved from the launcher folder. Absolute paths are used as supplied.

## Runtime behavior

The launcher performs these steps:

1. Checks for `Stardew Valley.exe` and `StardewModdingAPI.exe`.
2. Refuses to launch if SMAPI, the game, or another launcher instance is already running.
3. Reads `--mods-path` and counts mods and content packs in the first two directory levels.
4. Registers the global `Ctrl+F12` hotkey.
5. Starts SMAPI through Windows Terminal when `wt.exe` is available. Otherwise it starts SMAPI directly with a new console.
6. Shows the overlay and monitors the SMAPI process and game window.
7. Keeps the hotkey resident after the overlay closes so the terminal can be toggled during the session.

The overlay is a layered native window. It uses a shared style definition for colors, fonts, padding, dimensions, and text layout.

## Keyboard and mouse controls

| Input | Action |
|---|---|
| `Ctrl+F12` | Hide or show the SMAPI terminal. |
| Left-click the launch overlay | Abort the active launch. |

The hotkey uses `MOD_NOREPEAT`, so holding `Ctrl+F12` does not generate a stream of toggle events.

## Error messages

| Message | Meaning |
|---|---|
| `Wrong folder: Stardew Valley.exe not found here` | The executable is not in the Stardew Valley folder. |
| `SMAPI is not installed` | `StardewModdingAPI.exe` was not found next to the launcher. |
| `SMAPI or the game is already running` | Another SMAPI or Stardew Valley process is running. |
| `SMAPI Silent Launcher is already running` | Another launcher instance is active. |
| `Launch arguments are too long` | The forwarded command line exceeds the Windows process command-line limit. |
| `Could not register Ctrl+F12` | Another application owns the `Ctrl+F12` hotkey. |
| `Failed to launch SMAPI` | The terminal host or SMAPI process could not be started. |
| `SMAPI closed (code N) before the game started` | SMAPI exited during launch. |
| `Launch aborted` | The launch was cancelled from the overlay. |

An unhandled launcher exception is written to `launcher-crash.txt` beside the executable.

## Build from source

Requirements for building:

- Windows.
- .NET 8 SDK.

Build with the included script:

```bat
build.bat
```

The script publishes the NativeAOT executable and copies it to the project root. It also copies it to the development Steam path configured in the script.

To publish without the copy step:

```bat
dotnet publish -c Release -r win-x64
```

The output is:

```text
bin\Release\net8.0\win-x64\publish\SMAPI Silent Launcher.exe
```

## Project structure

| Path | Purpose |
|---|---|
| `src/Launcher.cs` | Entry point, validation, process launch, polling, and cleanup. |
| `src/Args.cs` | Argument forwarding, `--mods-path` parsing, and mod counting. |
| `src/Overlay.cs` | Native overlay window, rendering, fade, and hotkey window. |
| `src/Terminal.cs` | Terminal resolution and `Ctrl+F12` visibility control. |
| `src/Native.cs` | Win32 P/Invoke declarations, structures, and process/window helpers. |
| `SMAPISilentLauncher.csproj` | .NET 8 NativeAOT project configuration. |
| `build.bat` | Release publish and local deployment helper. |
| `icon.ico` | Embedded application icon. |

## Design notes

- The project uses Win32 P/Invoke directly because the launcher has no UI framework or runtime dependency.
- `CreateProcessW` starts the terminal process with a visible initial state.
- Windows Terminal is addressed by a unique tab title and a `CASCADIA_HOSTING_WINDOW_CLASS` window lookup. The `PseudoConsoleWindow` returned by `GetConsoleWindow` is not used for Windows Terminal because it is not the visible Terminal frame.
- If Windows Terminal is unavailable, the launcher starts SMAPI directly with `CREATE_NEW_CONSOLE` and resolves the attached console window.
- The overlay uses an off-screen memory bitmap for drawing and switches to GDI objects only for the lifetime of the overlay window.
- The launcher uses a named mutex to prevent multiple instances from starting at the same time.
- Mod counting is bounded to 256 directories across two directory levels and reads at most 4 KiB from each manifest.

## License

MIT.
