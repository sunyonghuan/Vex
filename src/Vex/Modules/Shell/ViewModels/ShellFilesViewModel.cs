using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using CodeWF.EventBus;
using ReactiveUI;
using Vex.Core.Messaging;
using Vex.Core.Models;
using Vex.Core.Regions;

namespace Vex.Modules.Shell.ViewModels;

public sealed class ShellFilesViewModel : ReactiveObject, IRegionTabItem
{
    private readonly IEventBus _eventBus;
    private DocumentFile? _selectedDocumentFile;
    private bool _suppressSelectedFileOpen;

    public ShellFilesViewModel(IEventBus eventBus)
    {
        _eventBus = eventBus;
        eventBus.Subscribe(this);
    }

    public string? TitleKey { get; } = VexL.SidebarFiles;

    public ObservableCollection<DocumentFile> DocumentFiles { get; } = [];

    public bool HasDocumentFiles => DocumentFiles.Count > 0;

    public bool IsDocumentFilesEmpty => !HasDocumentFiles;

    public DocumentFile? SelectedDocumentFile
    {
        get => _selectedDocumentFile;
        set
        {
            var previousSelection = _selectedDocumentFile;
            if (SetProperty(ref _selectedDocumentFile, value))
            {
                OnPropertyChanged(nameof(HasSelectedDocumentFile));
                if (value is not null && !_suppressSelectedFileOpen)
                {
                    _eventBus.Publish(new DocumentFileOpenRequestedCommand(value, previousSelection));
                }
            }
        }
    }

    public bool HasSelectedDocumentFile => SelectedDocumentFile is not null;

    public void RenameSelectedFile()
    {
        if (SelectedDocumentFile is not { } file)
        {
            return;
        }

        _eventBus.Publish(new DocumentFileRenameRequestedCommand(file));
    }

    public void SelectDocumentFileForContextMenu(DocumentFile documentFile)
    {
        _suppressSelectedFileOpen = true;
        try
        {
            SelectedDocumentFile = documentFile;
        }
        finally
        {
            _suppressSelectedFileOpen = false;
        }
    }

    [EventHandler]
    public void ApplyDocumentFilesChanged(DocumentFilesChangedCommand command)
    {
        // 文件列表只接收 Shell 发布的快照，不直接调用主 ViewModel 的文件打开流程。
        DocumentFiles.Clear();
        foreach (var file in command.Files)
        {
            DocumentFiles.Add(file);
        }

        SelectDocumentFileSilently(command.SelectedFile);
        NotifyDocumentFilesChanged();
    }

    [EventHandler]
    public void ApplyDocumentFileSelectionChanged(DocumentFileSelectionChangedCommand command)
    {
        // 未保存确认被取消时静默恢复旧选择，避免再次触发打开文件请求。
        SelectDocumentFileSilently(command.SelectedFile);
    }

    private void SelectDocumentFileSilently(DocumentFile? documentFile)
    {
        if (SetProperty(ref _selectedDocumentFile, documentFile, nameof(SelectedDocumentFile)))
        {
            OnPropertyChanged(nameof(HasSelectedDocumentFile));
        }
    }

    private void NotifyDocumentFilesChanged()
    {
        OnPropertyChanged(nameof(HasDocumentFiles));
        OnPropertyChanged(nameof(IsDocumentFilesEmpty));
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        this.RaiseAndSetIfChanged(ref storage, value, propertyName);
        return true;
    }

    private void OnPropertyChanged(string propertyName)
    {
        this.RaisePropertyChanged(propertyName);
    }
}
