using Avalonia.Controls.Notifications;
using Netor.Markdor.Core.Models;
using Netor.Markdor.Core.Services;

namespace Netor.Markdor.Modules.Shell.Services;

// 主窗口文档流程的状态栏和确认框文案集中在这里，避免 MainWindowViewModel 因多语言格式化继续膨胀。
// 保存/导出/复制等完成型事件走 Toast 反馈，进度与提示类事件留在状态栏。
public sealed class ShellDocumentWorkflowText : IShellDocumentWorkflowText
{
    private readonly IAppLocalizer _localizer;
    private readonly IShellStatusPublisher _statusPublisher;
    private readonly IShellToastPublisher _toastPublisher;

    public ShellDocumentWorkflowText(
        IAppLocalizer localizer,
        IShellStatusPublisher statusPublisher,
        IShellToastPublisher toastPublisher)
    {
        _localizer = localizer;
        _statusPublisher = statusPublisher;
        _toastPublisher = toastPublisher;
    }

    public string UnsavedDocumentFallback => Text(MarkdorL.UnsavedDocument);

    public string TitleSaveChanges => Text(MarkdorL.UnsavedTitleSaveChanges);

    public string TitleBeforeOpeningFolder => Text(MarkdorL.UnsavedTitleBeforeOpeningFolder);

    public string TitleBeforeOpening => Text(MarkdorL.UnsavedTitleBeforeOpening);

    public string TitleBeforeSwitchingFiles => Text(MarkdorL.UnsavedTitleBeforeSwitchingFiles);

    public string TitleBeforeDeleting => Text(MarkdorL.UnsavedTitleBeforeDeleting);

    public string TitleBeforeReopening => Text(MarkdorL.UnsavedTitleBeforeReopening);

    public string TitleBeforeOpeningRecent => Text(MarkdorL.UnsavedTitleBeforeOpeningRecent);

    public string TitleBeforeClosingMarkdor => Text(MarkdorL.UnsavedTitleBeforeClosingMarkdor);

    public string BeforeOpeningDroppedFolder(string documentName) =>
        Format(MarkdorL.UnsavedMessageBeforeOpeningDroppedFolderFormat, documentName);

    public string BeforeOpeningFile(string documentName, string fileName) =>
        Format(MarkdorL.UnsavedMessageBeforeOpeningFileFormat, documentName, fileName);

    public string BeforeNewDocument(string documentName) =>
        Format(MarkdorL.UnsavedMessageBeforeNewDocumentFormat, documentName);

    public string BeforeClosingDocument(string documentName) =>
        Format(MarkdorL.UnsavedMessageBeforeClosingDocumentFormat, documentName);

    public string BeforeOpeningAnotherFile(string documentName) =>
        Format(MarkdorL.UnsavedMessageBeforeOpeningAnotherFileFormat, documentName);

    public string BeforeOpeningFolder(string documentName) =>
        Format(MarkdorL.UnsavedMessageBeforeOpeningFolderFormat, documentName);

    public string BeforeSwitchingFile(string documentName, string fileName) =>
        Format(MarkdorL.UnsavedMessageBeforeSwitchingFileFormat, documentName, fileName);

    public string BeforeDeleting(string documentName) =>
        Format(MarkdorL.UnsavedMessageBeforeDeletingFormat, documentName);

    public string BeforeReopeningWithEncoding(string documentName, string encodingName) =>
        Format(MarkdorL.UnsavedMessageBeforeReopeningWithEncodingFormat, documentName, encodingName);

    public string BeforeOpeningRecent(string documentName, string recentName) =>
        Format(MarkdorL.UnsavedMessageBeforeOpeningRecentFormat, documentName, recentName);

    public string BeforeClosingMarkdor(string documentName) =>
        Format(MarkdorL.UnsavedMessageBeforeClosingMarkdorFormat, documentName);

    public void PublishDroppedItemUnavailable() => Publish(MarkdorL.StatusDroppedItemUnavailable);

    public void PublishDropMarkdownOrTextFile() => Publish(MarkdorL.StatusDropMarkdownOrTextFile);

    public void PublishNewDocumentCreated() => Publish(MarkdorL.StatusNewDocumentCreated);

    public void PublishDocumentClosed() => Publish(MarkdorL.StatusDocumentClosed);

    public void PublishChooseDocumentFromLoadedFolder() => Publish(MarkdorL.StatusChooseDocumentFromLoadedFolder);

    public void PublishLoadedMarkdownFiles(int count)
    {
        if (count == 0)
        {
            Publish(MarkdorL.StatusNoMarkdownFilesLoaded);
            return;
        }

        PublishFormat(MarkdorL.StatusLoadedMarkdownFilesFormat, count);
    }

    public void PublishSaved(string fileName) => PublishToast(MarkdorL.StatusSavedFileFormat, fileName);

    public void PublishSavedAs(string fileName) => PublishToast(MarkdorL.StatusSavedAsFileFormat, fileName);

    public void PublishSaveAllResult(bool isStillModified)
    {
        Publish(isStillModified ? MarkdorL.StatusSaveAllCanceledModified : MarkdorL.StatusSavedCurrentDocumentNoMultiSave);
    }

    public void PublishFileDeleted() => Publish(MarkdorL.StatusFileDeleted);

    public void PublishRenamedFile(string fileName) => PublishToast(MarkdorL.StatusRenamedFileFormat, fileName);

    public void PublishOpenFileBeforeEncoding() => Publish(MarkdorL.StatusOpenFileBeforeEncoding);

    public void PublishReopenedWithEncoding(string encodingName) =>
        PublishFormat(MarkdorL.StatusReopenedWithEncodingFormat, encodingName);

    public void PublishOpened(string fileName) => PublishFormat(MarkdorL.StatusOpenedFileFormat, fileName);

    public void PublishExternalFileReloaded(string fileName) =>
        PublishToast(MarkdorL.StatusExternalFileReloadedFormat, NotificationType.Information, fileName);

    public void PublishPropertiesSummary(
        string title,
        string state,
        string encoding,
        string size,
        string location)
    {
        PublishFormat(MarkdorL.StatusPropertiesSummaryFormat, title, state, encoding, size, location);
    }

    public void PublishHtmlExportCanceled() => Publish(MarkdorL.StatusHtmlExportCanceled);

    public void PublishExportedHtmlTo(string fileName) => PublishToast(MarkdorL.StatusExportedHtmlToFormat, fileName);

    public void PublishExportedMarkdownTo(string fileName) => PublishToast(MarkdorL.StatusExportedMarkdownToFormat, fileName);

    public void PublishPdfExportCanceled() => Publish(MarkdorL.StatusPdfExportCanceled);

    public void PublishExportedPdfTo(string fileName) => PublishToast(MarkdorL.StatusExportedPdfToFormat, fileName);

    public void PublishPngExportCanceled() => Publish(MarkdorL.StatusPngExportCanceled);

    public void PublishExportedPngTo(string fileName) => PublishToast(MarkdorL.StatusExportedPngToFormat, fileName);

    public void PublishExportedWordTo(string fileName) => PublishToast(MarkdorL.StatusExportedWordToFormat, fileName);

    public void PublishExportNotImplemented(string? format)
    {
        var displayFormat = ExportFormatName(format);
        PublishFormat(MarkdorL.StatusExportNotImplementedFormat, displayFormat);
    }

    public string CopyTargetName(string? target) => Text(CopyTargetKey(target));

    public void PublishCopiedHtmlToPlatform(string? target) =>
        PublishToast(MarkdorL.StatusCopiedHtmlToPlatformFormat, CopyTargetName(target));

    public void PublishCopyHtmlUnavailable() => Publish(MarkdorL.StatusCopyHtmlUnavailable);

    public void PublishPrintPreviewResult(bool isCanceled)
    {
        Publish(isCanceled ? MarkdorL.StatusPrintPreviewCanceled : MarkdorL.StatusOpenedHtmlPrintPreview);
    }

    public void PublishStatisticsSummary(MarkdownStatistics statistics)
    {
        PublishFormat(
            MarkdorL.StatusStatisticsSummaryFormat,
            statistics.Words,
            statistics.Characters,
            statistics.Lines,
            statistics.ReadingMinutes);
    }

    public void PublishRecentFileUnavailable() => Publish(MarkdorL.StatusRecentFileUnavailable);

    public void PublishRecentFileRemovedMissing() => Publish(MarkdorL.StatusRecentFileRemovedMissing);

    public void PublishSaveCanceledActionIncomplete() => Publish(MarkdorL.StatusSaveCanceledActionIncomplete);

    private string Text(string key) => _localizer.Get(key);

    private string Format(string key, params object?[] args) => _localizer.Format(key, args);

    private void Publish(string key) => _statusPublisher.PublishResource(key);

    private void PublishFormat(string key, params object?[] args) => _statusPublisher.PublishResourceFormat(key, args);

    private void PublishToast(string key, params object?[] args) =>
        _toastPublisher.Publish(_localizer.Format(key, args));

    private void PublishToast(string key, NotificationType type, params object?[] args) =>
        _toastPublisher.Publish(_localizer.Format(key, args), type);

    private string ExportFormatName(string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return Text(MarkdorL.ExportFormatDocument);
        }

        return format.Trim().ToLowerInvariant() switch
        {
            "word" or "doc" or "docx" => Text(MarkdorL.ExportWord),
            _ => format.ToUpperInvariant()
        };
    }

    private static string CopyTargetKey(string? target)
    {
        return target?.Trim().ToLowerInvariant() switch
        {
            "wechat" or "weixin" => MarkdorL.CopyPlatformWechat,
            "zhihu" => MarkdorL.CopyPlatformZhihu,
            "juejin" => MarkdorL.CopyPlatformJuejin,
            _ => MarkdorL.ExportFormatDocument
        };
    }
}
