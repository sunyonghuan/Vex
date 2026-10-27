using Irihi.Avalonia.Shared.Contracts;
using Netor.Markdor.Core.Services;

namespace Netor.Markdor.Modules.Shell.ViewModels;

public sealed class ShellPreferencesViewModel(EditorPreferences preferences, ShellAppearanceViewModel appearance,
    ShellEditorDisplayViewModel display, ShellWindowLayoutViewModel layout, McpSettingsViewModel mcp,
    Markdor.Modules.Mcp.Services.IMcpOperationAuditService audit) : IDialogContext
{
    public EditorPreferences Preferences { get; } = preferences;
    public ShellAppearanceViewModel Appearance { get; } = appearance;
    public ShellEditorDisplayViewModel Display { get; } = display;
    public ShellWindowLayoutViewModel Layout { get; } = layout;
    public McpSettingsViewModel Mcp { get; } = mcp;
    public Markdor.Modules.Mcp.Services.IMcpOperationAuditService Audit { get; } = audit;
    public event EventHandler<object?>? RequestClose;
    public void Close() => RequestClose?.Invoke(this, null);
}
