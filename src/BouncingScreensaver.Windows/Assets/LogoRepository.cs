using System.Drawing.Imaging;

namespace BouncingScreensaver.Windows.Assets;

public sealed class LogoRepository
{
    private readonly string _cachePath;

    public LogoRepository()
    {
        _cachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BouncingScreensaver",
            "cache",
            "logo.png");
    }

    public Image? TryLoadCached() => TryLoadPng(_cachePath);

    public Task<Image?> RefreshFromSourceAsync(string sourcePath)
    {
        return Task.Run(() =>
        {
            try
            {
                var expanded = Environment.ExpandEnvironmentVariables(sourcePath.Trim());
                if (string.IsNullOrWhiteSpace(expanded) ||
                    !expanded.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                    !File.Exists(expanded))
                {
                    return null;
                }

                using var validated = TryLoadPng(expanded);
                if (validated is null)
                {
                    return null;
                }

                var cacheDirectory = Path.GetDirectoryName(_cachePath)!;
                Directory.CreateDirectory(cacheDirectory);
                var temporary = _cachePath + ".tmp";
                validated.Save(temporary, ImageFormat.Png);
                File.Move(temporary, _cachePath, overwrite: true);

                return new Bitmap(validated);
            }
            catch
            {
                return null;
            }
        });
    }

    private static Image? TryLoadPng(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var image = Image.FromStream(stream, useEmbeddedColorManagement: true, validateImageData: true);
            if (!image.RawFormat.Equals(ImageFormat.Png))
            {
                return null;
            }

            return new Bitmap(image);
        }
        catch
        {
            return null;
        }
    }
}
