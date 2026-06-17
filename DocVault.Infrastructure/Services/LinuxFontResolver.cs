using PdfSharpCore.Fonts;

namespace DocVault.Infrastructure.Services;

public class LinuxFontResolver : IFontResolver
{
    private static readonly string[] SearchPaths =
    [
        "/usr/share/fonts/truetype/liberation",
        "/usr/share/fonts/truetype",
        "/usr/share/fonts",
        "/usr/local/share/fonts",
    ];

    private static readonly Dictionary<string, string> FaceMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "LiberationSans-Regular",    "LiberationSans-Regular.ttf" },
        { "LiberationSans-Bold",       "LiberationSans-Bold.ttf" },
        { "LiberationSans-Italic",     "LiberationSans-Italic.ttf" },
        { "LiberationSans-BoldItalic", "LiberationSans-BoldItalic.ttf" },
    };

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        var suffix = (isBold, isItalic) switch
        {
            (true,  true)  => "-BoldItalic",
            (true,  false) => "-Bold",
            (false, true)  => "-Italic",
            _              => "-Regular",
        };
        return new FontResolverInfo("LiberationSans" + suffix);
    }

    public byte[] GetFont(string faceName)
    {
        if (!FaceMap.TryGetValue(faceName, out var fileName))
            fileName = "LiberationSans-Regular.ttf";

        foreach (var dir in SearchPaths)
        {
            var path = Path.Combine(dir, fileName);
            if (File.Exists(path))
                return File.ReadAllBytes(path);

            // Search subdirectories one level deep
            if (Directory.Exists(dir))
            {
                var found = Directory.GetFiles(dir, fileName, SearchOption.AllDirectories).FirstOrDefault();
                if (found != null)
                    return File.ReadAllBytes(found);
            }
        }

        // Last resort — any TTF on the system
        foreach (var dir in SearchPaths)
        {
            if (!Directory.Exists(dir)) continue;
            var any = Directory.GetFiles(dir, "*.ttf", SearchOption.AllDirectories).FirstOrDefault();
            if (any != null)
                return File.ReadAllBytes(any);
        }

        throw new InvalidOperationException($"No fonts found on this system. Font requested: {faceName}");
    }
}
