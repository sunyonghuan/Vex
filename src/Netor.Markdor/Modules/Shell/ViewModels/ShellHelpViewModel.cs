using Netor.Markdor.Core.Services;
using Netor.Markdor.Modules.Shell.Services;

namespace Netor.Markdor.Modules.Shell.ViewModels;

// 处理帮助菜单入口；Shell 只协调菜单动作，文档和网站打开细节收口到帮助服务。
public sealed class ShellHelpViewModel
{
    private readonly IHelpService _helpService;
    private readonly ShellDialogsViewModel _dialogs;
    private readonly IShellStatusPublisher _statusPublisher;
    private readonly IAppLocalizer _localizer;

    public ShellHelpViewModel(
        IHelpService helpService,
        ShellDialogsViewModel dialogs,
        IShellStatusPublisher statusPublisher,
        IAppLocalizer localizer)
    {
        _helpService = helpService;
        _dialogs = dialogs;
        _statusPublisher = statusPublisher;
        _localizer = localizer;
    }

    public async Task OpenHelpTopic(string? topic)
    {
        try
        {
            switch (topic)
            {
                case "samples":
                    await _helpService.OpenSamplesAsync();
                    break;
                case "changelog":
                    await _helpService.OpenDocumentAsync("更新日志.md");
                    _statusPublisher.PublishResource(MarkdorL.StatusOpenedChangelog);
                    break;
                case "thanks":
                    await _helpService.OpenDocumentAsync("鸣谢.md");
                    _statusPublisher.PublishResource(MarkdorL.StatusOpenedAcknowledgements);
                    break;
                case "website":
                    await _helpService.OpenWebsiteAsync();
                    break;
                case "feedback":
                    await _helpService.OpenFeedbackAsync();
                    break;
                case "about":
                    await _helpService.ShowAboutWindowAsync();
                    _statusPublisher.PublishResource(MarkdorL.StatusAboutMarkdor);
                    break;
                case "update":
                    await _helpService.ShowUpdatesAsync();
                    break;
                default:
                    _statusPublisher.PublishResourceFormat(MarkdorL.StatusHelpQueuedFormat, GetHelpTopicDisplayName(topic));
                    break;
            }
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(MarkdorL.ErrorMessageCannotOpenHelpFormat, exception, GetHelpTopicDisplayName(topic));
        }
    }

    private string GetHelpTopicDisplayName(string? topic)
    {
        return topic switch
        {
            "samples" => _localizer.Get(MarkdorL.MenuSamples),
            "changelog" => _localizer.Get(MarkdorL.Changelog),
            "thanks" => _localizer.Get(MarkdorL.Thanks),
            "website" => _localizer.Get(MarkdorL.Website),
            "feedback" => _localizer.Get(MarkdorL.Feedback),
            "about" => _localizer.Get(MarkdorL.About),
            "update" => _localizer.Get(MarkdorL.AboutCheckUpdate),
            _ => string.IsNullOrWhiteSpace(topic) ? _localizer.Get(MarkdorL.MenuHelp) : topic
        };
    }
}
