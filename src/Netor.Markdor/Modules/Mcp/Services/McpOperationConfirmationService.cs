using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Netor.Markdor.Core.Services;
using Netor.Markdor.Modules.Mcp.Views;
using Netor.Markdor.Modules.Shell.Services;

namespace Netor.Markdor.Modules.Mcp.Services;

public sealed class McpOperationConfirmationService : IMcpOperationConfirmationService
{
    private readonly Lock _syncRoot = new();
    private readonly HashSet<string> _rememberedAllowed = new(StringComparer.Ordinal);
    private readonly HashSet<string> _rememberedRejected = new(StringComparer.Ordinal);
    private readonly IAppLocalizer _localizer;
    private readonly IShellStatusPublisher _statusPublisher;

    public McpOperationConfirmationService(IAppLocalizer localizer, IShellStatusPublisher statusPublisher)
    {
        _localizer = localizer;
        _statusPublisher = statusPublisher;
    }

    public async Task<bool> ConfirmAsync(string toolName, string target, string summary)
    {
        lock (_syncRoot)
        {
            if (_rememberedAllowed.Contains(toolName))
            {
                return true;
            }

            if (_rememberedRejected.Contains(toolName))
            {
                _statusPublisher.PublishResource(MarkdorL.McpOperationRejected);
                return false;
            }
        }

        var owner = GetMainWindow();
        var window = new McpOperationConfirmationWindow(
            _localizer.Get(MarkdorL.McpOperationConfirmationTitle),
            _localizer.Get(MarkdorL.McpOperationConfirmationMessage),
            toolName,
            target,
            summary,
            _localizer.Get(MarkdorL.Cancel),
            _localizer.Get(MarkdorL.McpOperationConfirm),
            _localizer.Get(MarkdorL.McpOperationRememberChoice));

        if (owner is null)
        {
            window.Show();
            _statusPublisher.PublishResource(MarkdorL.McpOperationRejected);
            return false;
        }

        await window.ShowDialog(owner);
        var confirmed = window.Confirmed;
        if (window.RememberChoice)
        {
            lock (_syncRoot)
            {
                var set = confirmed ? _rememberedAllowed : _rememberedRejected;
                set.Add(toolName);
            }
        }

        if (!confirmed)
        {
            _statusPublisher.PublishResource(MarkdorL.McpOperationRejected);
        }

        return confirmed;
    }

    public void ResetRememberedChoices()
    {
        lock (_syncRoot)
        {
            _rememberedAllowed.Clear();
            _rememberedRejected.Clear();
        }
    }

    private static Window? GetMainWindow()
    {
        return Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } mainWindow }
            ? mainWindow
            : null;
    }
}
