using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Vex.Core.Services;
using Vex.Modules.Help.Views;

namespace Vex.Modules.Help.Services;

public sealed class HelpService : IHelpService
{
    private static readonly string DocumentsFolder = Path.Combine(AppContext.BaseDirectory, "docs");
    private readonly IEditorAppearanceState _appearanceState;
    private readonly IAppLocalizer _localizer;

    public HelpService(IEditorAppearanceState appearanceState, IAppLocalizer localizer)
    {
        _appearanceState = appearanceState;
        _localizer = localizer;
    }

    public Task OpenWebsiteAsync()
    {
        Open("https://codewf.com");
        return Task.CompletedTask;
    }

    public Task OpenFeedbackAsync()
    {
        Open("https://github.com/dotnet9/Vex/issues");
        return Task.CompletedTask;
    }

    public Task OpenDocumentAsync(string fileName)
    {
        var path = Path.Combine(DocumentsFolder, fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(_localizer.Get(VexL.HelpDetailDocumentNotFound), path);
        }

        Open(path);
        return Task.CompletedTask;
    }

    public Task ShowDocumentWindowAsync(string title, string fileName)
    {
        var path = GetDocumentPath(fileName);
        var markdown = File.ReadAllText(path);
        ShowWindow(new MarkdownDocumentWindow(
            title,
            markdown,
            path,
            _appearanceState.TypographyTheme,
            _appearanceState.TypographySize));
        return Task.CompletedTask;
    }

    public Task ShowAboutWindowAsync()
    {
        ShowWindow(new AboutWindow());
        return Task.CompletedTask;
    }

    private static void Open(string uri)
    {
        Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
    }

    private string GetDocumentPath(string fileName)
    {
        var path = Path.Combine(DocumentsFolder, fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(_localizer.Get(VexL.HelpDetailDocumentNotFound), path);
        }

        return path;
    }

    private static void ShowWindow(Window window)
    {
        if (GetMainWindow() is { } owner)
        {
            window.Show(owner);
            return;
        }

        window.Show();
    }

    private static Window? GetMainWindow()
    {
        return Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } mainWindow }
            ? mainWindow
            : null;
    }

}
