using CodeWF.Toolkit.Core.UpdateChecking;
using Netor.Markdor.Core.Services;

namespace Netor.Markdor.Modules.Help.Services;

/// <summary>检查、下载和校验复用 Toolkit；Windows 使用 Markdor 安装器的就绪握手。</summary>
public sealed class MarkdorUpdateService : IUpdateService, IDisposable
{
    private readonly UpdateService _updates = new(new UpdateServiceOptions
    {
        Owner = "sunyonghuan", Repository = "Vex", ProductName = "Markdor"
    });
    private readonly WindowsUpdateInstaller _windows;

    public MarkdorUpdateService(IAppLocalizer localizer) => _windows = new WindowsUpdateInstaller(localizer);
    public UpdateServiceOptions Options => _updates.Options;
    public Version CurrentVersion => _updates.CurrentVersion;
    public string CurrentVersionText => _updates.CurrentVersionText;
    public Task<UpdateCheckResult> CheckAsync(Version? current = null, CancellationToken cancellationToken = default)
        => _updates.CheckAsync(current, cancellationToken);
    public Task<UpdateDownloadResult> DownloadAsync(UpdateInfo update, IProgress<UpdateDownloadProgress>? progress = null, CancellationToken cancellationToken = default)
        => _updates.DownloadAsync(update, progress, cancellationToken);
    public Task<UpdateInstallResult> InstallAsync(UpdateDownloadResult download, CancellationToken cancellationToken = default)
        => OperatingSystem.IsWindows() ? _windows.InstallAsync(download.FilePath, cancellationToken) : _updates.InstallAsync(download, cancellationToken);
    public void Dispose() => _updates.Dispose();
}
