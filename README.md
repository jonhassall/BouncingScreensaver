# BouncingScreensaver

A native Windows screensaver that moves one company logo across the entire multi-monitor desktop in the style of the classic bouncing DVD logo. The logo can cross between displays. When it reaches an external desktop corner within a configurable pixel tolerance, every display flashes purple.

## Design goals

- Native C# / .NET 10 WinForms application; no Electron.
- One global logo simulation shared by every monitor.
- Logo can cross monitor seams and can be partially visible on two monitors at once.
- External monitor edges bounce; shared monitor edges are pass-through seams.
- Corner hits have configurable leeway (`CornerTolerancePx`, default `12`).
- Corner hit flashes all screens, then fades back to the configured background.
- PNG logo is loaded at runtime and **is never compiled into the application or installer**.
- A network/UNC logo path is supported and refreshed in the background.
- Last valid logo is cached per user under `%LOCALAPPDATA%\BouncingScreensaver\cache\logo.png`.
- Build, test, installer creation and release all happen in one GitHub Actions workflow.
- The published screensaver is self-contained; target PCs do not need the .NET runtime installed.

## Repository layout

```text
src/BouncingScreensaver.Core/       monitor topology + bounce/corner physics
src/BouncingScreensaver.Windows/    WinForms screensaver, preview, settings, logo cache
 tests/BouncingScreensaver.Core.Tests/ physics/topology unit tests
installer/                         NSIS installer definition
scripts/                           deployment/config helpers
.github/workflows/build.yml        the only build/release pipeline
```

## Screensaver modes

Windows launches `.scr` files with conventional command-line modes:

```text
BouncingScreensaver.scr /s          full-screen screensaver
BouncingScreensaver.scr /c          settings UI
BouncingScreensaver.scr /p 12345    Control Panel preview using parent HWND 12345
```

Development/admin modes:

```text
BouncingScreensaver.scr --debug
BouncingScreensaver.scr --self-test
BouncingScreensaver.scr --set CornerTolerancePx 12
BouncingScreensaver.scr --set-machine LogoSource "\\server\branding\screensaver\logo.png"
```

`--set` writes the current user's overrides. `--set-machine` writes machine defaults and must run elevated.

## Logo

Only PNG is supported in v1. A transparent 32-bit PNG is recommended.

The default source is:

```text
%ProgramData%\BouncingScreensaver\logo.png
```

The installer creates the directory but deliberately does **not** install a logo. You can either copy a PNG there or point `LogoSource` at another local/UNC path.

Example:

```powershell
& "$env:ProgramFiles\BouncingScreensaver\BouncingScreensaver.scr" `
  --set-machine LogoSource "\\fileserver\Branding\Screensaver\logo.png"
```

On startup, the screensaver immediately uses the cached logo if available, then checks the configured source in the background. This means an unavailable VPN/file share does not block the screensaver from starting.

If there has never been a valid PNG, a generic rendered `LOGO` placeholder is shown. There is no embedded company artwork.

## Settings precedence

Highest priority first:

1. `HKLM\Software\Policies\BouncingScreensaver` — managed policy
2. `HKCU\Software\BouncingScreensaver` — user overrides from `/c` or `--set`
3. `HKLM\Software\BouncingScreensaver` — machine defaults installed/configured by IT
4. Built-in defaults

Supported values:

| Name | Type | Default |
|---|---|---:|
| `LogoSource` | string | `%ProgramData%\BouncingScreensaver\logo.png` |
| `SpeedPxPerSecond` | DWORD | `220` |
| `LogoWidthPercent` | DWORD | `15` |
| `BackgroundColor` | string | `#000000` |
| `FlashColor` | string | `#7B2CFF` |
| `FlashEnabled` | DWORD | `1` |
| `FlashDurationMs` | DWORD | `350` |
| `CornerTolerancePx` | DWORD | `12` |

Policy-managed controls are disabled in the `/c` settings dialog.

## Multi-monitor behaviour

Each monitor gets its own borderless rendering window, but there is only one `LogoState`. Coordinates are Windows virtual-desktop coordinates, so monitors to the left/above the primary display (negative coordinates) work normally.

At a shared edge, the logo continues into the adjacent display when the seam overlaps the logo's direction of travel. At an edge with no adjacent display, the logo bounces. Shared seam junctions are not treated as corner hits; only external corners of the combined monitor topology are eligible.

## Build and release

There is intentionally no Docker build.

The GitHub workflow runs on `windows-latest` and:

1. installs .NET 10 using `global.json` (`10.0.401`),
2. fails if image artwork is found in the source tree,
3. runs the physics/topology tests,
4. publishes a self-contained single-file `win-x64` executable,
5. runs a built-in self-test,
6. copies the executable to `BouncingScreensaver.scr`,
7. installs NSIS on the ephemeral GitHub runner,
8. builds `BouncingScreensaver-Setup.exe`,
9. smoke-tests silent install, installed self-test and silent uninstall,
10. uploads the release files as a workflow artifact (GitHub provides the single download ZIP),
11. creates a GitHub Release automatically for tags such as `v1.0.0`.

A normal branch build uses a version such as `0.1.<run-number>`. A `v1.2.3` tag produces version `1.2.3`.

## Installer

Interactive:

```powershell
BouncingScreensaver-Setup.exe
```

Headless:

```powershell
BouncingScreensaver-Setup.exe /S
```

The installer places the binary at:

```text
C:\Program Files\BouncingScreensaver\BouncingScreensaver.scr
```

and creates:

```text
C:\ProgramData\BouncingScreensaver\
```

for an optional locally managed logo. Uninstall deliberately preserves the ProgramData folder so externally managed branding is not deleted.

The installer does not force the current user's Windows screensaver selection. For managed fleets, configure the Windows screen saver path/timeout with Group Policy or Intune. See [DEPLOYMENT.md](DEPLOYMENT.md).

## Local development

A Windows machine with the .NET 10 SDK can run:

```powershell
dotnet test tests\BouncingScreensaver.Core.Tests\BouncingScreensaver.Core.Tests.csproj
dotnet run --project src\BouncingScreensaver.Windows\BouncingScreensaver.Windows.csproj -- --debug
```

Local development tooling is optional: the canonical build is GitHub Actions.
