using CodeWF.Avalonia.Markdown.Themes;
using Netor.Markdor.Core.Services;

namespace Netor.Markdor.Modules.Appearance.Services;

public sealed class EditorAppearanceState : IEditorAppearanceState
{
    private string _typographySize = MarkdownTypographySizes.Normal;
    private string? _typographyTheme;

    public event EventHandler? Changed;

    public string TypographySize => _typographySize;

    public string? TypographyTheme => _typographyTheme;

    public void UpdateTypography(string? typographyTheme, string typographySize)
    {
        var normalizedSize = string.IsNullOrWhiteSpace(typographySize)
            ? MarkdownTypographySizes.Normal
            : typographySize;

        if (_typographyTheme == typographyTheme && _typographySize == normalizedSize)
        {
            return;
        }

        _typographyTheme = typographyTheme;
        _typographySize = normalizedSize;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
