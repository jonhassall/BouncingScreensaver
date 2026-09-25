[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('LogoSource','SpeedPxPerSecond','LogoWidthPercent','BackgroundColor','FlashColor','FlashEnabled','FlashDurationMs','CornerTolerancePx')]
    [string]$Name,

    [Parameter(Mandatory = $true)]
    [string]$Value,

    [switch]$Machine,

    [string]$ScreensaverPath = "$env:ProgramFiles\BouncingScreensaver\BouncingScreensaver.scr"
)

if (-not (Test-Path -LiteralPath $ScreensaverPath)) {
    throw "Screensaver not found at '$ScreensaverPath'."
}

$verb = if ($Machine) { '--set-machine' } else { '--set' }
$process = Start-Process -FilePath $ScreensaverPath -ArgumentList @($verb, $Name, $Value) -Wait -PassThru
if ($process.ExitCode -ne 0) {
    throw "BouncingScreensaver returned exit code $($process.ExitCode)."
}
