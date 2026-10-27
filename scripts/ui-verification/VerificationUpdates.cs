using CodeWF.Toolkit.Core.UpdateChecking;

public sealed class VerificationUpdates : IUpdateService
{
    private readonly TaskCompletionSource<UpdateDownloadResult> _download = new();
    public int Checks { get; private set; }
    public int Downloads { get; private set; }
    public UpdateServiceOptions Options { get; } = new() { Repository = "Markdor" };
    public Version CurrentVersion => new(1, 6, 0);
    public string CurrentVersionText => "1.6.0";
    public Task<UpdateCheckResult> CheckAsync(Version? current = null, CancellationToken cancellationToken = default)
    {
        Checks++;
        return Task.FromResult(new UpdateCheckResult(new UpdateInfo(new Version(9, 0), "v9.0.0", "Markdor", "Update notes / 更新说明",
            "https://example.test/releases", "https://example.test/Markdor-setup.exe", "Markdor-setup.exe", "https://example.test/Markdor-setup.exe.sha256"), true, null));
    }
    public Task<UpdateDownloadResult> DownloadAsync(UpdateInfo update, IProgress<UpdateDownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        Downloads++;
        progress?.Report(new UpdateDownloadProgress(50, 100));
        return _download.Task.WaitAsync(cancellationToken);
    }
    public void CompleteDownload() => _download.SetResult(new UpdateDownloadResult(Path.Combine(Path.GetTempPath(), "Markdor 更新", "Markdor-setup.exe"), "Markdor-setup.exe", 100));
    public Task<UpdateInstallResult> InstallAsync(UpdateDownloadResult download, CancellationToken cancellationToken = default)
        => Task.FromResult(new UpdateInstallResult(UpdateInstallKind.Manual, "Installer not ready / 安装向导尚未就绪") { ShouldExitApplication = false });
}
