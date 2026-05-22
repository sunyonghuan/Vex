using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CodeWF.EventBus;
using CodeWF.Markdown.Themes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lang.Avalonia;
using Vex.Core.Messaging;
using Vex.Core.Models;
using Vex.Core.Services;

namespace Vex.Modules.Shell.ViewModels;

public sealed class MainWindowViewModel : ObservableObject
{
    private readonly IDocumentService _documentService;
    private readonly IMarkdownOutlineService _outlineService;
    private readonly IMarkdownStatisticsService _statisticsService;
    private readonly IThemeService _themeService;
    private readonly IHelpService _helpService;
    private readonly IEventBus _eventBus;
    private DocumentSnapshot _document;
    private string _markdown = string.Empty;
    private string _statusText = "Ready";
    private int _caretLine = 1;
    private int _caretColumn = 1;
    private bool _isSidebarVisible = true;
    private bool _isStatusBarVisible = true;
    private bool _isPreviewVisible = true;
    private bool _isAlwaysOnTop;
    private bool _isFullScreen;
    private bool _isCompactLayout;
    private double _editorZoom = 1.0;
    private ThemeOption? _selectedTheme;
    private TypographyOption? _selectedTypography;
    private LanguageOption? _selectedLanguage;
    private MarkdownStatistics _statistics = new(0, 0, 1);
    private DocumentFile? _selectedDocumentFile;

    public MainWindowViewModel(
        IDocumentService documentService,
        IMarkdownOutlineService outlineService,
        IMarkdownStatisticsService statisticsService,
        IThemeService themeService,
        IHelpService helpService,
        IEventBus eventBus)
    {
        _documentService = documentService;
        _outlineService = outlineService;
        _statisticsService = statisticsService;
        _themeService = themeService;
        _helpService = helpService;
        _eventBus = eventBus;
        _eventBus.Subscribe<MarkdownTextChangedCommand>(OnMarkdownTextChanged);

        NewDocumentCommand = new RelayCommand(NewDocument);
        NewWindowCommand = new RelayCommand(NewWindow);
        OpenCommand = new AsyncRelayCommand(OpenAsync);
        OpenFolderCommand = new AsyncRelayCommand(OpenFolderAsync);
        QuickOpenCommand = new AsyncRelayCommand(OpenAsync);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        SaveAsCommand = new AsyncRelayCommand(SaveAsAsync);
        SaveAllCommand = new AsyncRelayCommand(SaveAsync);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => HasCurrentFile);
        OpenFileLocationCommand = new AsyncRelayCommand(OpenFileLocationAsync, () => HasCurrentFile);
        ShowPropertiesCommand = new RelayCommand(() => SetStatus(CurrentFilePath ?? "Untitled document"));
        ExportCommand = new RelayCommand<string?>(format => SetStatus($"Export {format ?? "document"} is queued for implementation."));
        PrintCommand = new RelayCommand(() => SetStatus("Print is queued for implementation."));
        CloseDocumentCommand = new RelayCommand(NewDocument);
        ReopenWithEncodingCommand = new AsyncRelayCommand<string?>(ReopenWithEncodingAsync);

        ToggleSidebarCommand = new RelayCommand(() => IsSidebarVisible = !IsSidebarVisible);
        ShowOutlineCommand = new RelayCommand(() => SelectedSideTabIndex = 1);
        ShowFilesCommand = new RelayCommand(() => SelectedSideTabIndex = 0);
        TogglePreviewCommand = new RelayCommand(() => IsPreviewVisible = !IsPreviewVisible);
        ToggleStatusBarCommand = new RelayCommand(() => IsStatusBarVisible = !IsStatusBarVisible);
        ToggleSourceModeCommand = new RelayCommand(() => IsPreviewVisible = !IsPreviewVisible);
        ToggleAlwaysOnTopCommand = new RelayCommand(() => IsAlwaysOnTop = !IsAlwaysOnTop);
        ToggleFullScreenCommand = new RelayCommand(() => IsFullScreen = !IsFullScreen);
        ActualSizeCommand = new RelayCommand(() => EditorZoom = 1.0);
        ZoomInCommand = new RelayCommand(() => EditorZoom = Math.Min(1.8, EditorZoom + 0.1));
        ZoomOutCommand = new RelayCommand(() => EditorZoom = Math.Max(0.7, EditorZoom - 0.1));
        WordCountCommand = new RelayCommand(() => SetStatus($"Words {Statistics.Words}, Characters {Statistics.Characters}, Lines {Statistics.Lines}."));

        SelectThemeCommand = new RelayCommand<ThemeOption?>(SelectTheme);
        SelectThemeByKeyCommand = new RelayCommand<string?>(SelectThemeByKey);
        SelectTypographyCommand = new RelayCommand<TypographyOption?>(SelectTypography);
        SelectTypographyByKeyCommand = new RelayCommand<string?>(SelectTypographyByKey);
        ToggleCompactLayoutCommand = new RelayCommand(() => IsCompactLayout = !IsCompactLayout);
        SelectLanguageCommand = new RelayCommand<LanguageOption?>(SelectLanguage);
        SelectLanguageByCultureCommand = new RelayCommand<string?>(SelectLanguageByCulture);

        HelpCommand = new RelayCommand<string?>(OpenHelpTopic);

        UndoCommand = new RelayCommand(() => PublishEditorAction(EditorActionKind.Undo));
        RedoCommand = new RelayCommand(() => PublishEditorAction(EditorActionKind.Redo));
        CutCommand = new RelayCommand(() => PublishEditorAction(EditorActionKind.Cut));
        CopyCommand = new RelayCommand(() => PublishEditorAction(EditorActionKind.Copy));
        PasteCommand = new RelayCommand(() => PublishEditorAction(EditorActionKind.Paste));
        SelectAllCommand = new RelayCommand(() => PublishEditorAction(EditorActionKind.SelectAll));
        FocusEditorCommand = new RelayCommand(() => PublishEditorAction(EditorActionKind.FocusEditor));
        InsertActionCommand = new RelayCommand<EditorActionKind?>(action =>
        {
            if (action is { } value)
            {
                PublishEditorAction(value);
            }
        });

        ThemeOptions = new ObservableCollection<ThemeOption>(_themeService.GetThemeOptions());
        TypographyOptions = new ObservableCollection<TypographyOption>(
            MarkdownTypographyThemes.All.Select(theme => new TypographyOption(theme.Name, theme.Key)));
        LanguageOptions = new ObservableCollection<LanguageOption>
        {
            new("zh-CN", "简体中文", "中文（简体）"),
            new("zh-Hant", "繁體中文", "中文（繁體）"),
            new("en-US", "English", "English"),
            new("ja-JP", "日本語", "日本語")
        };

        SelectedTheme = ThemeOptions.FirstOrDefault();
        SelectedTypography = TypographyOptions.FirstOrDefault(item => item.Key == MarkdownTypographyThemes.Simple)
                             ?? TypographyOptions.FirstOrDefault();
        SelectedLanguage = LanguageOptions.FirstOrDefault(item => item.CultureName == "zh-CN");

        _document = _documentService.CreateNew();
        Markdown = _document.Markdown;
    }

    public IEventBus EventBus => _eventBus;

    public ObservableCollection<DocumentFile> DocumentFiles { get; } = [];

    public ObservableCollection<OutlineItem> OutlineItems { get; } = [];

    public ObservableCollection<ThemeOption> ThemeOptions { get; }

    public ObservableCollection<TypographyOption> TypographyOptions { get; }

    public ObservableCollection<LanguageOption> LanguageOptions { get; }

    public IRelayCommand NewDocumentCommand { get; }

    public IRelayCommand NewWindowCommand { get; }

    public IAsyncRelayCommand OpenCommand { get; }

    public IAsyncRelayCommand OpenFolderCommand { get; }

    public IAsyncRelayCommand QuickOpenCommand { get; }

    public IAsyncRelayCommand SaveCommand { get; }

    public IAsyncRelayCommand SaveAsCommand { get; }

    public IAsyncRelayCommand SaveAllCommand { get; }

    public IAsyncRelayCommand DeleteCommand { get; }

    public IAsyncRelayCommand OpenFileLocationCommand { get; }

    public IRelayCommand ShowPropertiesCommand { get; }

    public IRelayCommand<string?> ExportCommand { get; }

    public IRelayCommand PrintCommand { get; }

    public IRelayCommand CloseDocumentCommand { get; }

    public IAsyncRelayCommand<string?> ReopenWithEncodingCommand { get; }

    public IRelayCommand ToggleSidebarCommand { get; }

    public IRelayCommand ShowOutlineCommand { get; }

    public IRelayCommand ShowFilesCommand { get; }

    public IRelayCommand TogglePreviewCommand { get; }

    public IRelayCommand ToggleStatusBarCommand { get; }

    public IRelayCommand ToggleSourceModeCommand { get; }

    public IRelayCommand ToggleAlwaysOnTopCommand { get; }

    public IRelayCommand ToggleFullScreenCommand { get; }

    public IRelayCommand ActualSizeCommand { get; }

    public IRelayCommand ZoomInCommand { get; }

    public IRelayCommand ZoomOutCommand { get; }

    public IRelayCommand WordCountCommand { get; }

    public IRelayCommand<ThemeOption?> SelectThemeCommand { get; }

    public IRelayCommand<string?> SelectThemeByKeyCommand { get; }

    public IRelayCommand<TypographyOption?> SelectTypographyCommand { get; }

    public IRelayCommand<string?> SelectTypographyByKeyCommand { get; }

    public IRelayCommand ToggleCompactLayoutCommand { get; }

    public IRelayCommand<LanguageOption?> SelectLanguageCommand { get; }

    public IRelayCommand<string?> SelectLanguageByCultureCommand { get; }

    public IRelayCommand<string?> HelpCommand { get; }

    public IRelayCommand UndoCommand { get; }

    public IRelayCommand RedoCommand { get; }

    public IRelayCommand CutCommand { get; }

    public IRelayCommand CopyCommand { get; }

    public IRelayCommand PasteCommand { get; }

    public IRelayCommand SelectAllCommand { get; }

    public IRelayCommand FocusEditorCommand { get; }

    public IRelayCommand<EditorActionKind?> InsertActionCommand { get; }

    public string Markdown
    {
        get => _markdown;
        set
        {
            if (SetProperty(ref _markdown, value ?? string.Empty))
            {
                _document = _document with { Markdown = _markdown };
                RefreshMarkdownDerivedState();
            }
        }
    }

    public string WindowTitle => $"{(_document.IsNew ? string.Empty : string.Empty)}{_document.FileName} - Vex";

    public string CurrentDocumentTitle => _document.FileName;

    public string? CurrentFilePath => _document.FilePath;

    public bool HasCurrentFile => !string.IsNullOrWhiteSpace(CurrentFilePath);

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public int CaretLine
    {
        get => _caretLine;
        set => SetProperty(ref _caretLine, value);
    }

    public int CaretColumn
    {
        get => _caretColumn;
        set => SetProperty(ref _caretColumn, value);
    }

    public int SelectedSideTabIndex
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsSidebarVisible
    {
        get => _isSidebarVisible;
        set
        {
            if (SetProperty(ref _isSidebarVisible, value))
            {
                OnPropertyChanged(nameof(SidebarColumnWidth));
                OnPropertyChanged(nameof(SidebarSplitterWidth));
            }
        }
    }

    public bool IsStatusBarVisible
    {
        get => _isStatusBarVisible;
        set => SetProperty(ref _isStatusBarVisible, value);
    }

    public bool IsPreviewVisible
    {
        get => _isPreviewVisible;
        set
        {
            if (SetProperty(ref _isPreviewVisible, value))
            {
                OnPropertyChanged(nameof(PreviewColumnWidth));
                OnPropertyChanged(nameof(PreviewSplitterWidth));
            }
        }
    }

    public GridLength SidebarColumnWidth => IsSidebarVisible ? new GridLength(320) : new GridLength(0);

    public GridLength SidebarSplitterWidth => IsSidebarVisible ? new GridLength(6) : new GridLength(0);

    public GridLength PreviewColumnWidth => IsPreviewVisible ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

    public GridLength PreviewSplitterWidth => IsPreviewVisible ? new GridLength(6) : new GridLength(0);

    public bool IsAlwaysOnTop
    {
        get => _isAlwaysOnTop;
        set => SetProperty(ref _isAlwaysOnTop, value);
    }

    public bool IsFullScreen
    {
        get => _isFullScreen;
        set => SetProperty(ref _isFullScreen, value);
    }

    public bool IsCompactLayout
    {
        get => _isCompactLayout;
        set
        {
            if (SetProperty(ref _isCompactLayout, value))
            {
                OnPropertyChanged(nameof(CurrentTypographySize));
            }
        }
    }

    public double EditorZoom
    {
        get => _editorZoom;
        set
        {
            if (SetProperty(ref _editorZoom, value))
            {
                OnPropertyChanged(nameof(EditorFontSize));
                OnPropertyChanged(nameof(ZoomText));
            }
        }
    }

    public double EditorFontSize => Math.Round(15 * EditorZoom, 1);

    public string ZoomText => $"{EditorZoom:P0}";

    public ThemeOption? SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetProperty(ref _selectedTheme, value) && value is not null)
            {
                _themeService.ApplyTheme(value);
            }
        }
    }

    public TypographyOption? SelectedTypography
    {
        get => _selectedTypography;
        set
        {
            if (SetProperty(ref _selectedTypography, value))
            {
                OnPropertyChanged(nameof(CurrentTypographyTheme));
            }
        }
    }

    public string? CurrentTypographyTheme => SelectedTypography?.Key;

    public string CurrentTypographySize => IsCompactLayout
        ? MarkdownTypographySizes.Small
        : MarkdownTypographySizes.Normal;

    public LanguageOption? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (SetProperty(ref _selectedLanguage, value) && value is not null)
            {
                I18nManager.Instance.Culture = new CultureInfo(value.CultureName);
                SetStatus($"Language switched to {value.DisplayName}.");
            }
        }
    }

    public MarkdownStatistics Statistics
    {
        get => _statistics;
        set
        {
            if (SetProperty(ref _statistics, value))
            {
                OnPropertyChanged(nameof(WordCountText));
                OnPropertyChanged(nameof(CharacterCountText));
                OnPropertyChanged(nameof(LineCountText));
            }
        }
    }

    public string WordCountText => $"{Statistics.Words} words";

    public string CharacterCountText => $"{Statistics.Characters} chars";

    public string LineCountText => $"{Statistics.Lines} lines";

    public DocumentFile? SelectedDocumentFile
    {
        get => _selectedDocumentFile;
        set
        {
            if (SetProperty(ref _selectedDocumentFile, value) && value is not null)
            {
                _ = OpenDocumentFileAsync(value);
            }
        }
    }

    private void NewDocument()
    {
        _document = _documentService.CreateNew();
        Markdown = _document.Markdown;
        SetStatus("New document created.");
        NotifyDocumentChanged();
        PublishEditorAction(EditorActionKind.FocusEditor);
    }

    private static void NewWindow()
    {
        if (!string.IsNullOrWhiteSpace(Environment.ProcessPath))
        {
            Process.Start(new ProcessStartInfo(Environment.ProcessPath) { UseShellExecute = true });
        }
    }

    private async Task OpenAsync()
    {
        var snapshot = await _documentService.OpenAsync();
        if (snapshot is not null)
        {
            ApplyDocument(snapshot);
        }
    }

    private async Task OpenFolderAsync()
    {
        var files = await _documentService.OpenFolderAsync();
        DocumentFiles.Clear();
        foreach (var file in files)
        {
            DocumentFiles.Add(file);
        }

        SetStatus(files.Count == 0 ? "No markdown files loaded." : $"Loaded {files.Count} markdown files.");
        if (files.Count > 0)
        {
            SelectedDocumentFile = files[0];
        }
    }

    private async Task OpenDocumentFileAsync(DocumentFile file)
    {
        var snapshot = await _documentService.OpenPathAsync(file.Path);
        ApplyDocument(snapshot);
    }

    private async Task SaveAsync()
    {
        var saved = await _documentService.SaveAsync(_document with { Markdown = Markdown });
        if (saved is not null)
        {
            ApplyDocument(saved, false);
            SetStatus($"Saved {saved.FileName}.");
        }
    }

    private async Task SaveAsAsync()
    {
        var saved = await _documentService.SaveAsAsync(_document with { Markdown = Markdown });
        if (saved is not null)
        {
            ApplyDocument(saved, false);
            SetStatus($"Saved as {saved.FileName}.");
        }
    }

    private async Task DeleteAsync()
    {
        if (CurrentFilePath is not { Length: > 0 } path)
        {
            return;
        }

        await _documentService.DeleteAsync(path);
        NewDocument();
        SetStatus("File deleted.");
    }

    private async Task OpenFileLocationAsync()
    {
        if (CurrentFilePath is { Length: > 0 } path)
        {
            await _documentService.OpenFileLocationAsync(path);
        }
    }

    private async Task ReopenWithEncodingAsync(string? encodingName)
    {
        if (CurrentFilePath is not { Length: > 0 } path || string.IsNullOrWhiteSpace(encodingName))
        {
            SetStatus("Open a file before choosing an encoding.");
            return;
        }

        ApplyDocument(await _documentService.OpenPathAsync(path, encodingName));
        SetStatus($"Reopened with {encodingName}.");
    }

    private void ApplyDocument(DocumentSnapshot snapshot, bool updateMarkdown = true)
    {
        _document = snapshot;
        if (updateMarkdown)
        {
            Markdown = snapshot.Markdown;
        }

        NotifyDocumentChanged();
        SetStatus($"Opened {snapshot.FileName}.");
        PublishEditorAction(EditorActionKind.FocusEditor);
    }

    private void NotifyDocumentChanged()
    {
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(CurrentDocumentTitle));
        OnPropertyChanged(nameof(CurrentFilePath));
        OnPropertyChanged(nameof(HasCurrentFile));
        DeleteCommand.NotifyCanExecuteChanged();
        OpenFileLocationCommand.NotifyCanExecuteChanged();
    }

    private void OnMarkdownTextChanged(MarkdownTextChangedCommand command)
    {
        CaretLine = command.CaretLine;
        CaretColumn = command.CaretColumn;
        if (Markdown != command.Markdown)
        {
            Markdown = command.Markdown;
        }
    }

    private void RefreshMarkdownDerivedState()
    {
        Statistics = _statisticsService.Count(Markdown);
        OutlineItems.Clear();
        foreach (var item in _outlineService.BuildOutline(Markdown))
        {
            OutlineItems.Add(item);
        }
    }

    private void PublishEditorAction(EditorActionKind action)
    {
        _eventBus.Publish(new EditorActionCommand(action));
    }

    private void SelectTheme(ThemeOption? theme)
    {
        if (theme is not null)
        {
            SelectedTheme = theme;
        }
    }

    private void SelectThemeByKey(string? key)
    {
        var theme = ThemeOptions.FirstOrDefault(item => item.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        SelectTheme(theme);
    }

    private void SelectTypography(TypographyOption? typography)
    {
        if (typography is not null)
        {
            SelectedTypography = typography;
        }
    }

    private void SelectTypographyByKey(string? key)
    {
        var typography = TypographyOptions.FirstOrDefault(item => item.Key?.Equals(key, StringComparison.OrdinalIgnoreCase) == true);
        SelectTypography(typography);
    }

    private void SelectLanguage(LanguageOption? language)
    {
        if (language is not null)
        {
            SelectedLanguage = language;
        }
    }

    private void SelectLanguageByCulture(string? cultureName)
    {
        var language = LanguageOptions.FirstOrDefault(item => item.CultureName.Equals(cultureName, StringComparison.OrdinalIgnoreCase));
        SelectLanguage(language);
    }

    private void OpenHelpTopic(string? topic)
    {
        switch (topic)
        {
            case "website":
                _ = _helpService.OpenWebsiteAsync();
                break;
            case "feedback":
                _ = _helpService.OpenFeedbackAsync();
                break;
            case "about":
                SetStatus("Vex 0.1.0 - 极简之力，妙笔成章.");
                break;
            default:
                SetStatus($"{topic ?? "Help"} is queued for implementation.");
                break;
        }
    }

    private void SetStatus(string message)
    {
        StatusText = message;
        _eventBus.Publish(new WorkspaceStatusChangedCommand(message));
    }
}
