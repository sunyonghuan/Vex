namespace Vex.Modules.Workspace.Services;

internal sealed record MarkdownExportStyle(
    string BodyFontFamily,
    string MonoFontFamily,
    double BodyFontSize,
    double Heading1FontSize,
    double Heading2FontSize,
    double Heading3FontSize,
    double Heading4FontSize,
    double Heading5FontSize,
    double Heading6FontSize,
    double CodeFontSize,
    double TableFontSize,
    double LineHeightRatio,
    string PageBackgroundColor,
    string BodyColor,
    string HeadingColor,
    string MutedColor,
    string BorderColor,
    string CodeBackgroundColor,
    string CodeForegroundColor,
    string InlineCodeBackgroundColor,
    string InlineCodeForegroundColor,
    string LinkColor,
    string TableHeaderBackgroundColor,
    string QuoteBorderColor)
{
    private const string DefaultBodyFontFamily = "Inter, Microsoft YaHei UI, Segoe UI";
    private const string DefaultMonoFontFamily = "Cascadia Mono, Consolas";
    private const string CompactTypographySize = "Small";

    public static MarkdownExportStyle Resolve(string? typographyTheme, string? typographySize)
    {
        var palette = ResolvePalette(typographyTheme);
        var scale = string.Equals(typographySize, CompactTypographySize, StringComparison.OrdinalIgnoreCase)
            ? 0.92d
            : 1d;

        return new MarkdownExportStyle(
            DefaultBodyFontFamily,
            DefaultMonoFontFamily,
            Scale(15, scale),
            Scale(32, scale),
            Scale(26, scale),
            Scale(22, scale),
            Scale(19, scale),
            Scale(17, scale),
            Scale(16, scale),
            Scale(13, scale),
            Scale(14, scale),
            1.55d,
            palette.PageBackground,
            palette.Body,
            palette.Heading,
            palette.Muted,
            palette.Border,
            palette.CodeBackground,
            palette.CodeForeground,
            palette.InlineCodeBackground,
            palette.InlineCodeForeground,
            palette.Link,
            palette.TableHeaderBackground,
            palette.QuoteBorder);
    }

    private static double Scale(double value, double scale)
    {
        return Math.Round(value * scale, 1);
    }

    private static ExportPalette ResolvePalette(string? typographyTheme)
    {
        return typographyTheme?.Trim().ToLowerInvariant() switch
        {
            "inkblack" or "geekblack" => new ExportPalette(
                "#111827",
                "#e5e7eb",
                "#f9fafb",
                "#cbd5e1",
                "#334155",
                "#020617",
                "#e5e7eb",
                "#1f2937",
                "#f3f4f6",
                "#93c5fd",
                "#1e293b",
                "#64748b"),
            "orangeheart" or "yamabuki" => new ExportPalette(
                "#fffaf5",
                "#2f251e",
                "#1f1712",
                "#6b5a4c",
                "#fed7aa",
                "#2f1d12",
                "#fff7ed",
                "#ffedd5",
                "#7c2d12",
                "#c2410c",
                "#fff7ed",
                "#fb923c"),
            "tendergreen" or "verdant" or "cutegreen" => new ExportPalette(
                "#fbfefc",
                "#1f2f27",
                "#102018",
                "#4b6356",
                "#bbf7d0",
                "#102018",
                "#f0fdf4",
                "#dcfce7",
                "#14532d",
                "#15803d",
                "#f0fdf4",
                "#4ade80"),
            "redscarlet" => new ExportPalette(
                "#fffafa",
                "#332020",
                "#1f1212",
                "#6b4a4a",
                "#fecaca",
                "#2b1212",
                "#fef2f2",
                "#fee2e2",
                "#7f1d1d",
                "#dc2626",
                "#fef2f2",
                "#f87171"),
            "colorfulpurple" or "rosepurple" or "frontendpeak" => new ExportPalette(
                "#fffbff",
                "#2d2433",
                "#211827",
                "#65556f",
                "#e9d5ff",
                "#211827",
                "#faf5ff",
                "#f3e8ff",
                "#581c87",
                "#7c3aed",
                "#faf5ff",
                "#c084fc"),
            "blueglow" or "technologyblue" or "lanqing" or "fullstackblue" => new ExportPalette(
                "#f8fbff",
                "#1f2937",
                "#0f172a",
                "#475569",
                "#bfdbfe",
                "#0f172a",
                "#eff6ff",
                "#dbeafe",
                "#1e3a8a",
                "#2563eb",
                "#eff6ff",
                "#60a5fa"),
            _ => new ExportPalette(
                "#ffffff",
                "#1f2937",
                "#111827",
                "#4b5563",
                "#e5e7eb",
                "#111827",
                "#f9fafb",
                "#f3f4f6",
                "#111827",
                "#2563eb",
                "#f9fafb",
                "#d1d5db")
        };
    }

    private readonly record struct ExportPalette(
        string PageBackground,
        string Body,
        string Heading,
        string Muted,
        string Border,
        string CodeBackground,
        string CodeForeground,
        string InlineCodeBackground,
        string InlineCodeForeground,
        string Link,
        string TableHeaderBackground,
        string QuoteBorder);
}
