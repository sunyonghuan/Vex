using System.Globalization;
using System.Net;
using System.Diagnostics;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Vex.Core.Models;
using Vex.Core.Services;

namespace Vex.Modules.Workspace.Services;

public sealed class MarkdownExportService : IMarkdownExportService
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();
    private static readonly DataFormat<string> HtmlMimeFormat = DataFormat.CreateStringPlatformFormat("text/html");
    private static readonly DataFormat<string> MacHtmlFormat = DataFormat.CreateStringPlatformFormat("public.html");
    private static readonly DataFormat<string> WindowsHtmlFormat = DataFormat.CreateStringPlatformFormat("HTML Format");
    private readonly IEditorAppearanceState _appearanceState;
    private readonly IAppLocalizer _localizer;
    private readonly MarkdownPdfRenderer _pdfRenderer;
    private readonly MarkdownPngRenderer _pngRenderer;

    public MarkdownExportService(IAppLocalizer localizer, IEditorAppearanceState appearanceState)
    {
        _localizer = localizer;
        _appearanceState = appearanceState;
        _pdfRenderer = new MarkdownPdfRenderer(localizer);
        _pngRenderer = new MarkdownPngRenderer(localizer);
    }

    public async Task<string?> ExportHtmlAsync(DocumentSnapshot document)
    {
        var owner = GetMainWindow();
        if (owner is null)
        {
            return null;
        }

        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = _localizer.Get(VexL.DialogExportHtmlTitle),
            SuggestedFileName = Path.ChangeExtension(document.FileName, ".html"),
            DefaultExtension = "html",
            FileTypeChoices = CreateHtmlFileTypes()
        });

        var path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        await File.WriteAllTextAsync(path, BuildHtml(document), Utf8NoBom);
        return path;
    }

    public async Task<string?> ExportPdfAsync(DocumentSnapshot document)
    {
        var owner = GetMainWindow();
        if (owner is null)
        {
            return null;
        }

        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = _localizer.Get(VexL.DialogExportPdfTitle),
            SuggestedFileName = Path.ChangeExtension(document.FileName, ".pdf"),
            DefaultExtension = "pdf",
            FileTypeChoices = CreatePdfFileTypes()
        });

        var path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        _pdfRenderer.Render(document, path, ResolveExportStyle());
        return path;
    }

    public async Task<string?> ExportPngAsync(DocumentSnapshot document)
    {
        var owner = GetMainWindow();
        if (owner is null)
        {
            return null;
        }

        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = _localizer.Get(VexL.DialogExportPngTitle),
            SuggestedFileName = Path.ChangeExtension(document.FileName, ".png"),
            DefaultExtension = "png",
            FileTypeChoices = CreatePngFileTypes()
        });

        var path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        using var bitmap = _pngRenderer.Render(document, ResolveExportStyle());
        bitmap.Save(path);
        return path;
    }

    public async Task<bool> CopyHtmlAsync(DocumentSnapshot document, string? target)
    {
        var clipboard = GetMainWindow()?.Clipboard;
        if (clipboard is null)
        {
            return false;
        }

        var copyHtml = ResolveSocialCopyProfile(target) is { } socialProfile
            ? BuildSocialCopyHtml(document, socialProfile, includeFragmentMarkers: true)
            : BuildGenericCopyHtml(document, target);
        var transfer = CreateHtmlClipboardData(copyHtml.Text, copyHtml.Html);
        await clipboard.SetDataAsync(transfer);
        return true;
    }

    public async Task<string?> OpenHtmlPrintPreviewAsync(DocumentSnapshot document)
    {
        var folder = Path.Combine(Path.GetTempPath(), "Vex", "PrintPreview");
        Directory.CreateDirectory(folder);
        var fileName = CreatePrintPreviewFileName(document.FileName);
        var path = Path.Combine(folder, $"{fileName}-{Guid.NewGuid():N}.html");
        await File.WriteAllTextAsync(path, BuildHtml(document, HtmlDocumentMode.PrintPreview), Utf8NoBom);
        var process = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        if (process is null)
        {
            throw new InvalidOperationException(_localizer.Get(VexL.PrintDetailPreviewOpenFailed));
        }

        return path;
    }

    private static string CreatePrintPreviewFileName(string? fileName)
    {
        const string FallbackName = "VexPrintPreview";
        const int MaxNameLength = 80;

        var stem = string.IsNullOrWhiteSpace(fileName)
            ? FallbackName
            : Path.GetFileNameWithoutExtension(fileName);

        if (string.IsNullOrWhiteSpace(stem))
        {
            return FallbackName;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(stem.Length);
        foreach (var character in stem.Trim())
        {
            builder.Append(Array.IndexOf(invalidChars, character) >= 0 ? '_' : character);
        }

        var sanitized = builder.ToString().Trim(' ', '.');
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return FallbackName;
        }

        return sanitized.Length <= MaxNameLength
            ? sanitized
            : sanitized[..MaxNameLength].TrimEnd(' ', '.');
    }

    private string BuildHtml(
        DocumentSnapshot document,
        HtmlDocumentMode mode = HtmlDocumentMode.Export,
        string? target = null,
        bool includeFragmentMarkers = false)
    {
        var title = WebUtility.HtmlEncode(Path.GetFileNameWithoutExtension(document.FileName));
        var body = RenderMarkdownHtml(document);
        var language = WebUtility.HtmlEncode(_localizer.Culture.Name);
        var style = ResolveExportStyle();
        var cssBodyFont = FormatCssFontFamily(style.BodyFontFamily);
        var cssMonoFont = FormatCssFontFamily(style.MonoFontFamily);
        var bodyFontSize = FormatCssNumber(style.BodyFontSize);
        var lineHeight = FormatCssNumber(style.LineHeightRatio + 0.17d);
        var layout = ResolveSocialLayout(target);
        var targetName = WebUtility.HtmlEncode(layout.TargetName);
        var printToolbar = mode == HtmlDocumentMode.PrintPreview ? BuildPrintPreviewToolbar() : string.Empty;
        var printMetadata = mode == HtmlDocumentMode.PrintPreview ? BuildPrintPreviewMetadata(document) : string.Empty;
        var printStyles = mode == HtmlDocumentMode.PrintPreview ? BuildPrintPreviewStyles(style, cssBodyFont) : string.Empty;
        var printScript = mode == HtmlDocumentMode.PrintPreview ? PrintPreviewScript : string.Empty;
        var startFragment = includeFragmentMarkers ? "              <!--StartFragment-->" : string.Empty;
        var endFragment = includeFragmentMarkers ? "              <!--EndFragment-->" : string.Empty;
        return $$"""
            <!doctype html>
            <html lang="{{language}}">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="vex-copy-target" content="{{targetName}}">
              <title>{{title}}</title>
              <style>
                body { margin: 0; color: {{style.BodyColor}}; background: {{style.PageBackgroundColor}}; font-family: {{cssBodyFont}}; font-size: {{bodyFontSize}}px; line-height: {{lineHeight}}; }
                article { max-width: {{layout.MaxWidth}}px; margin: 0 auto; padding: {{layout.PaddingTop}}px {{layout.PaddingX}}px {{layout.PaddingBottom}}px; }
                h1, h2, h3, h4, h5, h6 { color: {{style.HeadingColor}}; line-height: 1.28; margin: 1.35em 0 .55em; }
                h1 { font-size: {{FormatCssNumber(style.Heading1FontSize)}}px; }
                h2 { font-size: {{FormatCssNumber(style.Heading2FontSize)}}px; border-bottom: 1px solid {{style.BorderColor}}; padding-bottom: .25em; }
                h3 { font-size: {{FormatCssNumber(style.Heading3FontSize)}}px; }
                h4 { font-size: {{FormatCssNumber(style.Heading4FontSize)}}px; }
                h5 { font-size: {{FormatCssNumber(style.Heading5FontSize)}}px; }
                h6 { font-size: {{FormatCssNumber(style.Heading6FontSize)}}px; }
                p, ul, ol, blockquote, pre, table { margin: 0 0 1em; }
                code { background: {{style.InlineCodeBackgroundColor}}; color: {{style.InlineCodeForegroundColor}}; border-radius: 4px; padding: .12em .32em; font-family: {{cssMonoFont}}; font-size: {{FormatCssNumber(style.CodeFontSize)}}px; }
                pre { overflow: auto; background: {{style.CodeBackgroundColor}}; color: {{style.CodeForegroundColor}}; border-radius: 8px; padding: 16px; }
                pre code { background: transparent; color: inherit; padding: 0; }
                blockquote { border-left: 4px solid {{style.QuoteBorderColor}}; color: {{style.MutedColor}}; padding-left: 16px; }
                table { border-collapse: collapse; width: 100%; }
                th, td { border: 1px solid {{style.BorderColor}}; padding: 8px 10px; }
                th { background: {{style.TableHeaderBackgroundColor}}; }
                a { color: {{style.LinkColor}}; }
                img { max-width: 100%; }
                {{printStyles}}
              </style>
              {{printScript}}
            </head>
            <body>
            {{printToolbar}}
            {{printMetadata}}
            {{startFragment}}
              <article data-vex-copy-target="{{targetName}}" style="max-width: {{layout.MaxWidth}}px; margin: 0 auto; padding: {{layout.PaddingTop}}px {{layout.PaddingX}}px {{layout.PaddingBottom}}px; color: {{style.BodyColor}}; background: {{style.PageBackgroundColor}}; font-family: {{cssBodyFont}}; font-size: {{bodyFontSize}}px; line-height: {{lineHeight}};">
            {{body}}
              </article>
            {{endFragment}}
            </body>
            </html>
            """;
    }

    private HtmlCopyContent BuildGenericCopyHtml(DocumentSnapshot document, string? target)
    {
        var html = BuildHtml(document, HtmlDocumentMode.Export, target, includeFragmentMarkers: true);
        return new HtmlCopyContent(html, html);
    }

    private HtmlCopyContent BuildSocialCopyHtml(
        DocumentSnapshot document,
        SocialCopyProfile profile,
        bool includeFragmentMarkers = false)
    {
        var title = WebUtility.HtmlEncode(Path.GetFileNameWithoutExtension(document.FileName));
        var language = WebUtility.HtmlEncode(_localizer.Culture.Name);
        var style = ResolveExportStyle();
        var targetName = WebUtility.HtmlEncode(profile.TargetName);
        var startFragment = includeFragmentMarkers ? "<!--StartFragment-->" : string.Empty;
        var endFragment = includeFragmentMarkers ? "<!--EndFragment-->" : string.Empty;
        var section = RenderSocialSection(document, style, profile);
        var html = $$"""
            <!doctype html>
            <html lang="{{language}}">
            <head>
              <meta charset="utf-8">
              <meta name="vex-copy-target" content="{{targetName}}">
              <title>{{title}}</title>
            </head>
            <body>
            {{startFragment}}
            {{section}}
            {{endFragment}}
            </body>
            </html>
            """;

        return new HtmlCopyContent(section, html);
    }

    private static string RenderSocialSection(
        DocumentSnapshot document,
        MarkdownExportStyle style,
        SocialCopyProfile profile)
    {
        var parsed = Markdig.Markdown.Parse(document.Markdown ?? string.Empty, Pipeline);
        EmbedLocalImages(parsed, document.FilePath);
        var body = RenderSocialBlocks(parsed, style, profile);
        if (profile.AppendJuejinSuffix)
        {
            body = string.IsNullOrWhiteSpace(body)
                ? JuejinSuffixHtml
                : $"{body}{Environment.NewLine}{JuejinSuffixHtml}";
        }

        return $$"""
            <section id="vex" data-tool="markdown编辑器" data-website="https://codewf.com" style="{{profile.RootStyle}}">
            {{body}}
            </section>
            """;
    }

    private static string RenderSocialBlocks(
        IEnumerable<Block> blocks,
        MarkdownExportStyle style,
        SocialCopyProfile profile)
    {
        var builder = new StringBuilder();
        foreach (var block in blocks)
        {
            var html = RenderSocialBlock(block, style, profile);
            if (!string.IsNullOrWhiteSpace(html))
            {
                builder.AppendLine(html);
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string RenderSocialBlock(
        Block block,
        MarkdownExportStyle style,
        SocialCopyProfile profile)
    {
        return block switch
        {
            HeadingBlock heading => RenderSocialHeading(heading, style, profile),
            ParagraphBlock paragraph => RenderSocialParagraph(paragraph, style, profile),
            ListBlock list => RenderSocialList(list, style, profile),
            QuoteBlock quote => RenderSocialQuote(quote, style, profile),
            CodeBlock codeBlock => RenderSocialCodeBlock(codeBlock, style),
            ThematicBreakBlock => $"""<hr style="height: 1px; border: none; border-top: 1px solid {profile.BorderColor}; margin: 24px 0;" />""",
            Table table => RenderSocialTable(table, style, profile),
            HtmlBlock htmlBlock => htmlBlock.Lines.ToString(),
            ContainerBlock container => RenderSocialBlocks(container, style, profile),
            _ => string.Empty
        };
    }

    private static string RenderSocialHeading(
        HeadingBlock heading,
        MarkdownExportStyle style,
        SocialCopyProfile profile)
    {
        var content = RenderSocialInlines(heading.Inline, style, profile);
        var level = Math.Clamp(heading.Level, 1, 6);
        if (level == 2)
        {
            return profile.Format switch
            {
                SocialCopyFormat.Mountain => $$"""
                    <h2 data-tool="markdown.com.cn编辑器" style="font-weight: bold; color: black; font-size: 22px; display: block; text-align: center; background-image: url(https://my-wechat.mdvex.com/mdvex/mountain_2_20191028221337.png); background-position: center center; background-repeat: no-repeat; background-attachment: initial; background-origin: initial; background-clip: initial; background-size: 63px; margin-top: 38px; margin-bottom: 10px;"><span class="prefix" style="display: none;"></span><span class="content" style="text-align: center; display: inline-block; height: 38px; line-height: 42px; color: rgb(60, 112, 198); background-position: left center; background-repeat: no-repeat; background-attachment: initial; background-origin: initial; background-clip: initial; background-size: 63px; margin-top: 38px; font-size: 18px; margin-bottom: 10px;">{{content}}</span><span class="suffix"></span></h2>
                    """,
                _ => $$"""
                    <h2 data-tool="markdown.com.cn编辑器" style="margin-top: 30px; font-weight: bold; font-size: 22px; border-bottom: 2px solid rgb(89,89,89); margin-bottom: 30px; color: rgb(89,89,89);"><span class="prefix" style="display: none;"></span><span class="content" style="font-size: 22px; display: inline-block; border-bottom: 2px solid rgb(89,89,89);">{{content}}</span><span class="suffix"></span></h2>
                    """
            };
        }

        var fontSize = level switch
        {
            1 => 28,
            3 => 20,
            4 => 18,
            5 => 16,
            _ => 15
        };
        return $$"""
            <h{{level}} data-tool="markdown.com.cn编辑器" style="margin-top: 26px; margin-bottom: 16px; font-weight: bold; font-size: {{fontSize}}px; line-height: 1.35; color: {{profile.HeadingColor}};">{{content}}</h{{level}}>
            """;
    }

    private static string RenderSocialParagraph(
        ParagraphBlock paragraph,
        MarkdownExportStyle style,
        SocialCopyProfile profile)
    {
        var content = RenderSocialInlines(paragraph.Inline, style, profile);
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        return $"""<p data-tool="markdown.com.cn编辑器" style="{profile.ParagraphStyle}">{content}</p>""";
    }

    private static string RenderSocialList(
        ListBlock list,
        MarkdownExportStyle style,
        SocialCopyProfile profile)
    {
        var tag = list.IsOrdered ? "ol" : "ul";
        var builder = new StringBuilder();
        builder.Append($"""<{tag} style="margin: 8px 0 16px; padding-left: 24px; color: {profile.BodyColor}; font-size: 16px; line-height: 1.75;">""");
        foreach (var item in list.OfType<ListItemBlock>())
        {
            builder.Append("<li style=\"margin: 4px 0; padding-left: 2px;\">");
            builder.Append(RenderSocialBlocks(item, style, profile));
            builder.Append("</li>");
        }

        builder.Append($"</{tag}>");
        return builder.ToString();
    }

    private static string RenderSocialQuote(
        QuoteBlock quote,
        MarkdownExportStyle style,
        SocialCopyProfile profile)
    {
        var body = RenderSocialBlocks(quote, style, profile);
        return $$"""
            <blockquote style="margin: 14px 0; padding: 8px 14px; border-left: 4px solid {{profile.BorderColor}}; background: {{style.InlineCodeBackgroundColor}}; color: {{profile.BodyColor}};">{{body}}</blockquote>
            """;
    }

    private static string RenderSocialCodeBlock(CodeBlock codeBlock, MarkdownExportStyle style)
    {
        var code = WebUtility.HtmlEncode(codeBlock.Lines.ToString().TrimEnd());
        return $$"""
            <pre style="margin: 14px 0; padding: 14px; border-radius: 6px; background: {{style.CodeBackgroundColor}}; color: {{style.CodeForegroundColor}}; overflow: auto; font-size: {{FormatCssNumber(style.CodeFontSize)}}px; line-height: 1.55;"><code style="font-family: {{FormatCssFontFamily(style.MonoFontFamily)}}; background: transparent; color: inherit; padding: 0;">{{code}}</code></pre>
            """;
    }

    private static string RenderSocialTable(
        Table table,
        MarkdownExportStyle style,
        SocialCopyProfile profile)
    {
        var builder = new StringBuilder();
        builder.Append($"""<table style="border-collapse: collapse; width: 100%; margin: 14px 0; font-size: 14px; color: {profile.BodyColor};">""");
        foreach (var row in table.OfType<TableRow>())
        {
            builder.Append("<tr>");
            foreach (var cell in row.OfType<TableCell>())
            {
                var tag = row.IsHeader ? "th" : "td";
                var background = row.IsHeader ? $" background: {style.TableHeaderBackgroundColor};" : string.Empty;
                builder.Append($"""<{tag} style="border: 1px solid {profile.BorderColor}; padding: 8px 10px; text-align: left;{background}">""");
                builder.Append(RenderSocialBlocks(cell, style, profile));
                builder.Append($"</{tag}>");
            }

            builder.Append("</tr>");
        }

        builder.Append("</table>");
        return builder.ToString();
    }

    private static string RenderSocialInlines(
        ContainerInline? container,
        MarkdownExportStyle style,
        SocialCopyProfile profile)
    {
        if (container is null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var inline = container.FirstChild;
        while (inline is not null)
        {
            builder.Append(RenderSocialInline(inline, style, profile));
            inline = inline.NextSibling;
        }

        return builder.ToString();
    }

    private static string RenderSocialInline(
        Inline inline,
        MarkdownExportStyle style,
        SocialCopyProfile profile)
    {
        return inline switch
        {
            LiteralInline literal => WebUtility.HtmlEncode(literal.Content.ToString()),
            LineBreakInline => "<br />",
            CodeInline code => $"""<code style="font-family: {FormatCssFontFamily(style.MonoFontFamily)}; font-size: {FormatCssNumber(style.CodeFontSize)}px; color: {style.InlineCodeForegroundColor}; background: {style.InlineCodeBackgroundColor}; padding: 2px 4px; border-radius: 4px;">{WebUtility.HtmlEncode(code.Content)}</code>""",
            EmphasisInline emphasis => RenderSocialEmphasis(emphasis, style, profile),
            LinkInline { IsImage: true } image => RenderSocialImage(image, style, profile),
            LinkInline link => RenderSocialLink(link, style, profile),
            TaskList taskList => taskList.Checked ? "[x] " : "[ ] ",
            HtmlInline htmlInline => htmlInline.Tag,
            ContainerInline nested => RenderSocialInlines(nested, style, profile),
            _ => WebUtility.HtmlEncode(inline.ToString() ?? string.Empty)
        };
    }

    private static string RenderSocialEmphasis(
        EmphasisInline emphasis,
        MarkdownExportStyle style,
        SocialCopyProfile profile)
    {
        var content = RenderSocialInlines(emphasis, style, profile);
        return emphasis.DelimiterCount >= 2
            ? $"<strong style=\"font-weight: bold;\">{content}</strong>"
            : $"<em style=\"font-style: italic;\">{content}</em>";
    }

    private static string RenderSocialLink(
        LinkInline link,
        MarkdownExportStyle style,
        SocialCopyProfile profile)
    {
        var content = RenderSocialInlines(link, style, profile);
        var url = WebUtility.HtmlEncode(link.Url ?? string.Empty);
        return $"""<a href="{url}" style="{profile.LinkStyle}">{content}</a>""";
    }

    private static string RenderSocialImage(
        LinkInline image,
        MarkdownExportStyle style,
        SocialCopyProfile profile)
    {
        var url = WebUtility.HtmlEncode(image.Url ?? string.Empty);
        var alt = WebUtility.HtmlEncode(RenderSocialInlines(image, style, profile));
        return $"""<img src="{url}" alt="{alt}" style="max-width: 100%; display: block; margin: 14px auto;" />""";
    }

    private MarkdownExportStyle ResolveExportStyle()
    {
        return MarkdownExportStyle.Resolve(_appearanceState.TypographyTheme, _appearanceState.TypographySize);
    }

    private static SocialCopyProfile? ResolveSocialCopyProfile(string? target)
    {
        return target?.Trim().ToLowerInvariant() switch
        {
            "wechat" or "weixin" => WechatCopyProfile,
            "zhihu" => ZhihuCopyProfile,
            "juejin" => JuejinCopyProfile,
            _ => null
        };
    }

    private static string FormatCssNumber(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string FormatCssFontFamily(string fontFamily)
    {
        var families = fontFamily.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(FormatCssFontName);
        return string.Join(", ", families);
    }

    private static string FormatCssFontName(string fontName)
    {
        if (fontName.Contains('\'') || fontName.Contains('"'))
        {
            return fontName;
        }

        return fontName.Any(char.IsWhiteSpace)
            ? $"'{fontName}'"
            : fontName;
    }

    private string BuildPrintPreviewToolbar()
    {
        var paperLabel = WebUtility.HtmlEncode(_localizer.Get(VexL.PrintPaper));
        var marginLabel = WebUtility.HtmlEncode(_localizer.Get(VexL.PrintMargin));
        var headerFooterLabel = WebUtility.HtmlEncode(_localizer.Get(VexL.PrintHeaderFooter));
        var normalMarginLabel = WebUtility.HtmlEncode(_localizer.Get(VexL.PrintMarginNormal));
        var narrowMarginLabel = WebUtility.HtmlEncode(_localizer.Get(VexL.PrintMarginNarrow));
        var wideMarginLabel = WebUtility.HtmlEncode(_localizer.Get(VexL.PrintMarginWide));
        var printLabel = WebUtility.HtmlEncode(_localizer.Get(VexL.Print));
        var closeLabel = WebUtility.HtmlEncode(_localizer.Get(VexL.Close));
        return $$"""
              <div class="vex-print-toolbar">
                <label>
                  <span>{{paperLabel}}</span>
                  <select id="vexPrintPaper" onchange="vexApplyPrintSettings()">
                    <option value="a4" selected>A4</option>
                    <option value="letter">Letter</option>
                  </select>
                </label>
                <label>
                  <span>{{marginLabel}}</span>
                  <select id="vexPrintMargin" onchange="vexApplyPrintSettings()">
                    <option value="normal" selected>{{normalMarginLabel}}</option>
                    <option value="narrow">{{narrowMarginLabel}}</option>
                    <option value="wide">{{wideMarginLabel}}</option>
                  </select>
                </label>
                <label class="vex-print-checkbox">
                  <input id="vexPrintMetadata" type="checkbox" checked onchange="vexApplyPrintSettings()">
                  <span>{{headerFooterLabel}}</span>
                </label>
                <button type="button" onclick="vexPrint()">{{printLabel}}</button>
                <button type="button" onclick="vexClose()">{{closeLabel}}</button>
              </div>
            """;
    }

    private string BuildPrintPreviewMetadata(DocumentSnapshot document)
    {
        var title = WebUtility.HtmlEncode(ResolvePrintTitle(document));
        var footer = WebUtility.HtmlEncode(ResolvePrintFooter(document));
        return $$"""
              <div class="vex-print-page-header">{{title}}</div>
              <div class="vex-print-page-footer">{{footer}}</div>
            """;
    }

    private string ResolvePrintTitle(DocumentSnapshot document)
    {
        return MarkdownHeadingScanner.FindFirstHeading(document.Markdown)
               ?? Path.GetFileNameWithoutExtension(document.FileName)
               ?? document.FileName
               ?? _localizer.Get(VexL.DocumentDefaultHeading);
    }

    private string ResolvePrintFooter(DocumentSnapshot document)
    {
        if (!string.IsNullOrWhiteSpace(document.FilePath))
        {
            return document.FilePath;
        }

        return string.IsNullOrWhiteSpace(document.FileName)
            ? _localizer.Get(VexL.DocumentDefaultFileName)
            : document.FileName;
    }

    private static string BuildPrintPreviewStyles(MarkdownExportStyle style, string cssBodyFont)
    {
        return $$"""
                .vex-print-toolbar {
                  position: sticky;
                  top: 0;
                  z-index: 20;
                  box-sizing: border-box;
                  display: flex;
                  flex-wrap: wrap;
                  justify-content: center;
                  align-items: center;
                  gap: 10px;
                  padding: 10px 16px;
                  border-bottom: 1px solid {{style.BorderColor}};
                  background: {{style.PageBackgroundColor}};
                  backdrop-filter: blur(8px);
                }
                .vex-print-toolbar label {
                  display: flex;
                  align-items: center;
                  gap: 6px;
                  min-height: 32px;
                  color: {{style.BodyColor}};
                  font: 13px/1.2 {{cssBodyFont}};
                }
                .vex-print-toolbar select {
                  min-height: 32px;
                  border: 1px solid {{style.BorderColor}};
                  border-radius: 6px;
                  color: {{style.BodyColor}};
                  background: {{style.InlineCodeBackgroundColor}};
                  font: 13px/1.2 {{cssBodyFont}};
                }
                .vex-print-checkbox input {
                  width: 16px;
                  height: 16px;
                  margin: 0;
                  accent-color: {{style.LinkColor}};
                }
                .vex-print-toolbar button {
                  min-width: 88px;
                  min-height: 32px;
                  border: 1px solid {{style.BorderColor}};
                  border-radius: 6px;
                  color: {{style.BodyColor}};
                  background: {{style.InlineCodeBackgroundColor}};
                  font: 13px/1.2 {{cssBodyFont}};
                  cursor: pointer;
                }
                .vex-print-toolbar button:hover { background: {{style.TableHeaderBackgroundColor}}; }
                .vex-print-page-header,
                .vex-print-page-footer {
                  display: none;
                }
                @page { size: A4 portrait; margin: 16mm 18mm; }
                @media print {
                  .vex-print-toolbar { display: none !important; }
                  .vex-print-page-header,
                  .vex-print-page-footer {
                    display: block;
                    position: fixed;
                    left: 0;
                    right: 0;
                    z-index: 10;
                    color: {{style.MutedColor}};
                    background: {{style.PageBackgroundColor}};
                    font: 9pt/1.3 {{cssBodyFont}};
                  }
                  .vex-print-page-header {
                    top: 0;
                    padding-bottom: 4mm;
                    border-bottom: 1px solid {{style.BorderColor}};
                  }
                  .vex-print-page-footer {
                    bottom: 0;
                    padding-top: 4mm;
                    border-top: 1px solid {{style.BorderColor}};
                    white-space: nowrap;
                    overflow: hidden;
                    text-overflow: ellipsis;
                  }
                  body.vex-print-metadata-off .vex-print-page-header,
                  body.vex-print-metadata-off .vex-print-page-footer { display: none !important; }
                  html, body { background: {{style.PageBackgroundColor}}; }
                  body { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
                  article {
                    max-width: none !important;
                    padding: 14mm 0 12mm !important;
                    color: {{style.BodyColor}} !important;
                    background: {{style.PageBackgroundColor}} !important;
                  }
                  body.vex-print-metadata-off article { padding: 0 !important; }
                  h1, h2, h3, h4, h5, h6 { break-after: avoid; page-break-after: avoid; }
                  pre, blockquote, figure, img, li { break-inside: avoid; page-break-inside: avoid; }
                  table, thead, tbody, tr { page-break-inside: avoid; }
                  table { page-break-inside: auto; }
                  thead { display: table-header-group; }
                  tr { break-inside: avoid; page-break-inside: avoid; }
                  a { color: {{style.LinkColor}}; text-decoration: underline; }
                }
            """;
    }

    private const string PrintPreviewScript = """
              <script>
                const vexPrintPapers = {
                  a4: "A4 portrait",
                  letter: "Letter portrait"
                };
                const vexPrintMargins = {
                  normal: "16mm 18mm",
                  narrow: "10mm 12mm",
                  wide: "24mm 26mm"
                };
                function vexApplyPrintSettings() {
                  const paper = document.getElementById("vexPrintPaper")?.value ?? "a4";
                  const margin = document.getElementById("vexPrintMargin")?.value ?? "normal";
                  const metadata = document.getElementById("vexPrintMetadata")?.checked ?? true;
                  let style = document.getElementById("vex-print-page-style");
                  if (!style) {
                    style = document.createElement("style");
                    style.id = "vex-print-page-style";
                    document.head.appendChild(style);
                  }

                  style.textContent = `@page { size: ${vexPrintPapers[paper] ?? vexPrintPapers.a4}; margin: ${vexPrintMargins[margin] ?? vexPrintMargins.normal}; }`;
                  document.body.classList.toggle("vex-print-metadata-off", !metadata);
                }
                function vexPrint() { window.print(); }
                function vexClose() { window.close(); }
                window.addEventListener("load", () => {
                  vexApplyPrintSettings();
                  window.setTimeout(vexPrint, 250);
                });
              </script>
    """;

    private static string RenderMarkdownHtml(DocumentSnapshot document)
    {
        var parsed = Markdig.Markdown.Parse(document.Markdown ?? string.Empty, Pipeline);
        EmbedLocalImages(parsed, document.FilePath);
        return Markdig.Markdown.ToHtml(parsed, Pipeline);
    }

    private static void EmbedLocalImages(ContainerBlock container, string? documentPath)
    {
        foreach (var block in container)
        {
            if (block is LeafBlock { Inline: { } inline })
            {
                EmbedLocalImages(inline, documentPath);
            }

            if (block is ContainerBlock childContainer)
            {
                EmbedLocalImages(childContainer, documentPath);
            }
        }
    }

    private static void EmbedLocalImages(ContainerInline container, string? documentPath)
    {
        foreach (var inline in container)
        {
            if (inline is LinkInline { IsImage: true } image
                && TryCreateImageDataUri(image.Url, documentPath, out var dataUri))
            {
                image.Url = dataUri;
            }

            if (inline is ContainerInline childContainer)
            {
                EmbedLocalImages(childContainer, documentPath);
            }
        }
    }

    private static bool TryCreateImageDataUri(string? url, string? documentPath, out string dataUri)
    {
        dataUri = string.Empty;
        if (!TryResolveLocalImagePath(url, documentPath, out var path))
        {
            return false;
        }

        try
        {
            var bytes = File.ReadAllBytes(path);
            dataUri = $"data:{ResolveImageMediaType(path)};base64,{Convert.ToBase64String(bytes)}";
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static bool TryResolveLocalImagePath(string? url, string? documentPath, out string path)
    {
        path = string.Empty;
        if (string.IsNullOrWhiteSpace(url)
            || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            if (!uri.IsFile)
            {
                return false;
            }

            path = uri.LocalPath;
            return File.Exists(path);
        }

        foreach (var candidate in EnumerateLocalImagePathCandidates(url))
        {
            if (Path.IsPathRooted(candidate))
            {
                path = candidate;
                if (File.Exists(path))
                {
                    return true;
                }
            }
        }

        var baseDirectory = string.IsNullOrWhiteSpace(documentPath) ? null : Path.GetDirectoryName(documentPath);
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            return false;
        }

        foreach (var candidate in EnumerateLocalImagePathCandidates(url))
        {
            path = Path.GetFullPath(Path.Combine(baseDirectory, candidate));
            if (File.Exists(path))
            {
                return true;
            }
        }

        path = string.Empty;
        return false;
    }

    private static IEnumerable<string> EnumerateLocalImagePathCandidates(string url)
    {
        var normalized = url.Replace('/', Path.DirectorySeparatorChar);
        yield return normalized;

        var decoded = DecodeLocalImageUrl(normalized);
        if (!string.Equals(decoded, normalized, StringComparison.Ordinal))
        {
            yield return decoded;
        }
    }

    private static string DecodeLocalImageUrl(string url)
    {
        try
        {
            return Uri.UnescapeDataString(url);
        }
        catch (UriFormatException)
        {
            return url;
        }
    }

    private static string ResolveImageMediaType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".svg" => "image/svg+xml",
            ".gif" => "image/gif",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".avif" => "image/avif",
            _ => "application/octet-stream"
        };
    }

    private static DataTransfer CreateHtmlClipboardData(string text, string html)
    {
        var item = new DataTransferItem();
        item.SetText(text);
        item.Set(HtmlMimeFormat, html);
        item.Set(MacHtmlFormat, html);
        item.Set(WindowsHtmlFormat, BuildWindowsClipboardHtml(html));

        var transfer = new DataTransfer();
        transfer.Add(item);
        return transfer;
    }

    private static string BuildWindowsClipboardHtml(string html)
    {
        const string StartMarker = "<!--StartFragment-->";
        const string EndMarker = "<!--EndFragment-->";
        const string HeaderFormat = "Version:1.0\r\nStartHTML:{0:0000000000}\r\nEndHTML:{1:0000000000}\r\nStartFragment:{2:0000000000}\r\nEndFragment:{3:0000000000}\r\n";

        var startMarkerIndex = html.IndexOf(StartMarker, StringComparison.Ordinal);
        var endMarkerIndex = html.IndexOf(EndMarker, StringComparison.Ordinal);
        if (startMarkerIndex < 0 || endMarkerIndex < 0 || endMarkerIndex < startMarkerIndex)
        {
            return html;
        }

        var blankHeader = string.Format(CultureInfo.InvariantCulture, HeaderFormat, 0, 0, 0, 0);
        var startHtml = Encoding.UTF8.GetByteCount(blankHeader);
        var endHtml = startHtml + Encoding.UTF8.GetByteCount(html);
        var startFragment = startHtml + Encoding.UTF8.GetByteCount(html[..(startMarkerIndex + StartMarker.Length)]);
        var endFragment = startHtml + Encoding.UTF8.GetByteCount(html[..endMarkerIndex]);
        var header = string.Format(CultureInfo.InvariantCulture, HeaderFormat, startHtml, endHtml, startFragment, endFragment);
        return header + html;
    }

    private const string SocialBodyFontFamily = "Optima-Regular, Optima, PingFangSC-light, PingFangTC-light, 'PingFang SC', Cambria, Cochin, Georgia, Times, 'Times New Roman', serif";

    private const string JuejinSuffixHtml = """
        <p id="vex-suffix-juejin-container" class="vex-suffix-juejin-container" data-tool="markdown.com.cn编辑器" style="font-size: 16px; padding-bottom: 8px; margin: 0; padding-top: 23px; color: rgb(74,74,74); line-height: 1.75em; margin-top: 20px !important;">本文使用 <a href="https://markdown.com.cn" style="word-wrap: break-word; font-weight: bold; color: rgb(60, 112, 198); text-decoration: none; border-bottom: 1px solid rgb(60, 112, 198);">markdown.com.cn</a> 排版</p>
        """;

    private static readonly SocialCopyProfile WechatCopyProfile = new(
        "wechat",
        SocialCopyFormat.Wechat,
        false,
        $"font-size: 16px; color: black; padding: 25px 30px; line-height: 1.6; word-spacing: 0px; letter-spacing: 0px; word-break: break-word; word-wrap: break-word; text-align: justify; font-family: {SocialBodyFontFamily}; margin-top: -10px;",
        "font-size: 16px; padding-top: 8px; padding-bottom: 8px; margin: 0; line-height: 26px; color: rgb(89,89,89);",
        "rgb(89,89,89)",
        "rgb(89,89,89)",
        "rgb(89,89,89)",
        "word-wrap: break-word; color: rgb(89,89,89); text-decoration: none; border-bottom: 1px solid rgb(89,89,89);");

    private static readonly SocialCopyProfile ZhihuCopyProfile = new(
        "zhihu",
        SocialCopyFormat.Mountain,
        false,
        $"padding: 25px 30px; word-spacing: 0px; word-wrap: break-word; text-align: justify; font-family: {SocialBodyFontFamily}; margin-top: -10px; line-height: 1.6; letter-spacing: .034em; color: rgb(63, 63, 63); font-size: 16px; word-break: all;",
        "font-size: 16px; padding-bottom: 8px; margin: 0; padding-top: 23px; color: rgb(74,74,74); line-height: 1.75em;",
        "rgb(74,74,74)",
        "rgb(60, 112, 198)",
        "rgb(60, 112, 198)",
        "word-wrap: break-word; font-weight: bold; color: rgb(60, 112, 198); text-decoration: none; border-bottom: 1px solid rgb(60, 112, 198);");

    private static readonly SocialCopyProfile JuejinCopyProfile = ZhihuCopyProfile with
    {
        TargetName = "juejin",
        AppendJuejinSuffix = true
    };

    private static SocialCopyLayout ResolveSocialLayout(string? target)
    {
        return target?.Trim().ToLowerInvariant() switch
        {
            "wechat" or "weixin" => new("wechat", 760, 40, 28, 64),
            "zhihu" => new("zhihu", 740, 40, 28, 64),
            "juejin" => new("juejin", 820, 44, 32, 68),
            _ => new("document", 860, 48, 32, 72)
        };
    }

    private sealed record HtmlCopyContent(string Text, string Html);

    private sealed record SocialCopyProfile(
        string TargetName,
        SocialCopyFormat Format,
        bool AppendJuejinSuffix,
        string RootStyle,
        string ParagraphStyle,
        string BodyColor,
        string HeadingColor,
        string BorderColor,
        string LinkStyle);

    private sealed record SocialCopyLayout(string TargetName, int MaxWidth, int PaddingTop, int PaddingX, int PaddingBottom);

    private enum SocialCopyFormat
    {
        Wechat,
        Mountain
    }

    private enum HtmlDocumentMode
    {
        Export,
        PrintPreview
    }

    private IReadOnlyList<FilePickerFileType> CreateHtmlFileTypes()
    {
        return
        [
            new(_localizer.Get(VexL.FileTypeHtml))
            {
                Patterns = ["*.html", "*.htm"]
            },
            FilePickerFileTypes.All
        ];
    }

    private IReadOnlyList<FilePickerFileType> CreatePdfFileTypes()
    {
        return
        [
            new(_localizer.Get(VexL.FileTypePdf))
            {
                Patterns = ["*.pdf"]
            },
            FilePickerFileTypes.All
        ];
    }

    private IReadOnlyList<FilePickerFileType> CreatePngFileTypes()
    {
        return
        [
            new(_localizer.Get(VexL.FileTypePng))
            {
                Patterns = ["*.png"]
            },
            FilePickerFileTypes.All
        ];
    }

    private static Window? GetMainWindow()
    {
        return Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
    }
}
