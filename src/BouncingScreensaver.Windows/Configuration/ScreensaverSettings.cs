namespace BouncingScreensaver.Windows.Configuration;

public sealed record ScreensaverSettings
{
    public string LogoSource { get; init; } = @"%ProgramData%\BouncingScreensaver\logo.png";
    public int SpeedPxPerSecond { get; init; } = 220;
    public int LogoWidthPercent { get; init; } = 15;
    public string BackgroundColor { get; init; } = "#000000";
    public string FlashColor { get; init; } = "#7B2CFF";
    public bool FlashEnabled { get; init; } = true;
    public int FlashDurationMs { get; init; } = 350;
    public int CornerTolerancePx { get; init; } = 12;
}
