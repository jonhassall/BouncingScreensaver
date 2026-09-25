# Deployment guide

## 1. Install

Interactive:

```powershell
.\BouncingScreensaver-Setup.exe
```

Silent/headless:

```powershell
.\BouncingScreensaver-Setup.exe /S
```

Expected installed executable:

```text
C:\Program Files\BouncingScreensaver\BouncingScreensaver.scr
```

Silent uninstall:

```powershell
& "$env:ProgramFiles\BouncingScreensaver\Uninstall.exe" /S
```

## 2. Provide the logo

No production logo is contained in the `.scr`, installer or GitHub build artifacts.

Option A — place a PNG at the default local source:

```text
C:\ProgramData\BouncingScreensaver\logo.png
```

Option B — configure a UNC source:

```powershell
& "$env:ProgramFiles\BouncingScreensaver\BouncingScreensaver.scr" `
  --set-machine LogoSource "\\fileserver\Branding\Screensaver\logo.png"
```

The screensaver copies a validated PNG into each user's local cache. If the source is unavailable, the last valid cached image continues to be used.

## 3. Configure screensaver behaviour

Example machine-wide configuration:

```powershell
$scr = "$env:ProgramFiles\BouncingScreensaver\BouncingScreensaver.scr"

& $scr --set-machine SpeedPxPerSecond 220
& $scr --set-machine LogoWidthPercent 15
& $scr --set-machine CornerTolerancePx 12
& $scr --set-machine FlashColor '#7B2CFF'
& $scr --set-machine FlashDurationMs 350
& $scr --set-machine FlashEnabled true
```

Or use the included helper:

```powershell
.\Set-BouncingScreensaverSetting.ps1 -Machine -Name CornerTolerancePx -Value 15
```

For settings that users must not override, deploy values to:

```text
HKLM\Software\Policies\BouncingScreensaver
```

using the same value names. Policy values take precedence and their controls are disabled in the settings UI.

## 4. Select it as the Windows screensaver

For a managed estate, set the Windows policy **Force specific screen saver** to the fully qualified path:

```text
C:\Program Files\BouncingScreensaver\BouncingScreensaver.scr
```

Microsoft documents that when a screen saver is outside `%SystemRoot%\System32`, the force-screen-saver policy should contain its fully qualified path.

Typically you will also manage:

- Enable screen saver
- Screen saver timeout
- Password protect the screen saver / lock-on-resume policy appropriate to your environment

These are user-scoped Windows personalization policies, so the installer intentionally does not try to set them when running as SYSTEM.

For a single test user, you can set the ordinary per-user value directly:

```powershell
Set-ItemProperty 'HKCU:\Control Panel\Desktop' `
  -Name 'SCRNSAVE.EXE' `
  -Value "$env:ProgramFiles\BouncingScreensaver\BouncingScreensaver.scr"
```

## 5. Change branding later

If `LogoSource` is a file share, replace the source PNG. No application rebuild or reinstall is needed. The next screensaver start checks the source in the background and refreshes the per-user cache.

If you change only speed, colours, flash timing, corner tolerance or logo source, update registry settings. Again, no rebuild is required.

## 6. Release process

Push/PR builds run the single `.github/workflows/build.yml` pipeline.

To create a versioned GitHub release:

```bash
git tag v1.0.0
git push origin v1.0.0
```

The workflow builds and publishes:

```text
BouncingScreensaver.scr
BouncingScreensaver-Setup.exe
BouncingScreensaver-<version>.zip
```

The ZIP also includes this deployment guide as `readme.txt` and the settings helper script.
