using System.Globalization;
using System.Text;
using CodeWF.EventBus;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using ReactiveUI;
using Vex.Core.Messaging;
using Vex.Core.Services;

namespace Vex.Modules.Workspace.ViewModels;

public sealed class MarkdownPreviewViewModel : ReactiveObject
{
    private const string PreviewRefreshQueryName = "vex-preview-refresh";
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();
    private readonly IEditorAppearanceState _appearanceState;
    private string? _imageBasePath;
    private string _markdown;
    private string _sourceMarkdown;
    private int _previewSourceLine = 1;
    private double _previewScrollRatio;
    private long _refreshVersion;
    private string _typographySize;
    private string? _typographyTheme;

    public MarkdownPreviewViewModel(
        IWorkspaceDocumentState documentState,
        IEditorAppearanceState appearanceState)
    {
        _appearanceState = appearanceState;
        _imageBasePath = documentState.FilePath;
        _sourceMarkdown = documentState.Markdown;
        _markdown = BuildPreviewMarkdown(_sourceMarkdown, _refreshVersion);
        _typographySize = appearanceState.TypographySize;
        _typographyTheme = appearanceState.TypographyTheme;
        _appearanceState.Changed += OnAppearanceChanged;
        CodeWF.EventBus.EventBus.Default.Subscribe(this);
    }

    public string Markdown
    {
        get => _markdown;
        private set => this.RaiseAndSetIfChanged(ref _markdown, value);
    }

    public string? ImageBasePath
    {
        get => _imageBasePath;
        private set => this.RaiseAndSetIfChanged(ref _imageBasePath, value);
    }

    public double PreviewScrollRatio
    {
        get => _previewScrollRatio;
        private set => this.RaiseAndSetIfChanged(ref _previewScrollRatio, value);
    }

    public int PreviewSourceLine
    {
        get => _previewSourceLine;
        private set => this.RaiseAndSetIfChanged(ref _previewSourceLine, value);
    }

    public string TypographySize
    {
        get => _typographySize;
        private set => this.RaiseAndSetIfChanged(ref _typographySize, value);
    }

    public string? TypographyTheme
    {
        get => _typographyTheme;
        private set => this.RaiseAndSetIfChanged(ref _typographyTheme, value);
    }

    [EventHandler]
    public void ApplyMarkdownDocumentChanged(MarkdownDocumentChangedCommand command)
    {
        if (!string.Equals(_sourceMarkdown, command.Markdown ?? string.Empty, StringComparison.Ordinal)
            || !string.Equals(_imageBasePath, command.FilePath, StringComparison.OrdinalIgnoreCase))
        {
            _refreshVersion = 0;
        }

        SetDocument(command.Markdown, command.FilePath, forceRefresh: false);
    }

    [EventHandler]
    public void ApplyMarkdownPreviewRefresh(MarkdownPreviewRefreshCommand command)
    {
        _refreshVersion = command.RefreshVersion;
        SetDocument(command.Markdown, command.FilePath, forceRefresh: true);
    }

    [EventHandler]
    public void ApplyMarkdownTextChanged(MarkdownTextChangedCommand command)
    {
        PreviewSourceLine = command.CaretLine;
        PreviewScrollRatio = CalculatePreviewScrollRatio(command.CaretLine, command.LineCount);
    }

    private void OnAppearanceChanged(object? sender, EventArgs e)
    {
        TypographySize = _appearanceState.TypographySize;
        TypographyTheme = _appearanceState.TypographyTheme;
    }

    private void SetDocument(string? markdown, string? filePath, bool forceRefresh)
    {
        _sourceMarkdown = markdown ?? string.Empty;
        var previewMarkdown = BuildPreviewMarkdown(_sourceMarkdown, _refreshVersion);

        ImageBasePath = filePath;
        if (forceRefresh && string.Equals(_markdown, previewMarkdown, StringComparison.Ordinal))
        {
            this.RaiseAndSetIfChanged(ref _markdown, string.Empty, nameof(Markdown));
        }

        Markdown = previewMarkdown;
    }

    private static string BuildPreviewMarkdown(string markdown, long refreshVersion)
    {
        if (refreshVersion <= 0 || string.IsNullOrWhiteSpace(markdown))
        {
            return markdown;
        }

        var replacements = CollectRemoteImageUrlReplacements(markdown, refreshVersion);
        if (replacements.Count == 0)
        {
            return markdown;
        }

        var builder = new StringBuilder(markdown);
        foreach (var replacement in replacements.OrderByDescending(item => item.Start))
        {
            builder.Remove(replacement.Start, replacement.Length);
            builder.Insert(replacement.Start, replacement.Value);
        }

        return builder.ToString();
    }

    private static IReadOnlyList<ImageUrlReplacement> CollectRemoteImageUrlReplacements(string markdown, long refreshVersion)
    {
        var parsed = Markdig.Markdown.Parse(markdown, Pipeline);
        var replacements = new Dictionary<int, ImageUrlReplacement>();
        CollectRemoteImageUrlReplacements(parsed, markdown, refreshVersion, replacements);
        return replacements.Values.ToArray();
    }

    private static void CollectRemoteImageUrlReplacements(
        ContainerBlock container,
        string markdown,
        long refreshVersion,
        IDictionary<int, ImageUrlReplacement> replacements)
    {
        foreach (var block in container)
        {
            if (block is LeafBlock { Inline: { } inline })
            {
                CollectRemoteImageUrlReplacements(inline, markdown, refreshVersion, replacements);
            }

            if (block is ContainerBlock childContainer)
            {
                CollectRemoteImageUrlReplacements(childContainer, markdown, refreshVersion, replacements);
            }
        }
    }

    private static void CollectRemoteImageUrlReplacements(
        ContainerInline container,
        string markdown,
        long refreshVersion,
        IDictionary<int, ImageUrlReplacement> replacements)
    {
        foreach (var inline in container)
        {
            if (inline is LinkInline { IsImage: true } image
                && IsRefreshableRemoteUrl(image.Url)
                && TryGetUrlSpan(image, markdown, out var start, out var length))
            {
                replacements[start] = new ImageUrlReplacement(
                    start,
                    length,
                    AddRefreshQuery(image.Url!, refreshVersion));
            }

            if (inline is ContainerInline childContainer)
            {
                CollectRemoteImageUrlReplacements(childContainer, markdown, refreshVersion, replacements);
            }
        }
    }

    private static bool TryGetUrlSpan(LinkInline image, string markdown, out int start, out int length)
    {
        start = image.UrlSpan.Start;
        length = image.UrlSpan.End - image.UrlSpan.Start + 1;
        if (start < 0 || length <= 0 || start + length > markdown.Length)
        {
            return false;
        }

        return string.Equals(markdown.Substring(start, length), image.Url, StringComparison.Ordinal);
    }

    private static bool IsRefreshableRemoteUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    private static string AddRefreshQuery(string url, long refreshVersion)
    {
        var fragmentStart = url.IndexOf('#', StringComparison.Ordinal);
        var urlWithoutFragment = fragmentStart >= 0 ? url[..fragmentStart] : url;
        var fragment = fragmentStart >= 0 ? url[fragmentStart..] : string.Empty;
        var separator = urlWithoutFragment.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        var version = refreshVersion.ToString(CultureInfo.InvariantCulture);
        return $"{urlWithoutFragment}{separator}{PreviewRefreshQueryName}={version}{fragment}";
    }

    private static double CalculatePreviewScrollRatio(int caretLine, int lineCount)
    {
        if (lineCount <= 1)
        {
            return 0d;
        }

        var lineIndex = Math.Clamp(caretLine, 1, lineCount) - 1;
        return lineIndex / (double)(lineCount - 1);
    }

    private readonly record struct ImageUrlReplacement(int Start, int Length, string Value);
}
