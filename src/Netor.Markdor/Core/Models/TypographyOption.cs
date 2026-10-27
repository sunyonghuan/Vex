namespace Netor.Markdor.Core.Models;

public sealed record TypographyOption(string Name, string? Key)
{
    public CodeWF.Avalonia.Markdown.Themes.ArticleTypography Definition =>
        CodeWF.Avalonia.Markdown.Themes.ArticleTypographyCatalog.Find(Key)!;
    public string PurposeKey => "Markdor.MarkdorL.TypographyPurpose" + Key;
    public bool IsRecommended => Definition.Recommended;
    public string SwatchColor => Key switch
    {
        "OrangeHeart" => "#F59E7F", "ColorfulPurple" => "#9574CD",
        "TenderGreen" => "#62B78A", "RedScarlet" => "#E78294",
        "InkBlack" or "GeekBlack" => "#7C7C87", "Yamabuki" => "#D6AD54", "LanQing" => "#5AB5B4", _ => "#8E9DFB"
    };
    public string NameKey => "Markdor.MarkdorL.Typography" + Key;
}
