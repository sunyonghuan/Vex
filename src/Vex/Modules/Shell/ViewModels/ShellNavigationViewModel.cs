using System.Runtime.CompilerServices;
using CodeWF.EventBus;
using ReactiveUI;
using Vex.Core.Messaging;

namespace Vex.Modules.Shell.ViewModels;

public sealed class ShellNavigationViewModel : ReactiveObject
{
    public ShellNavigationViewModel(IEventBus eventBus)
    {
        eventBus.Subscribe(this);
    }

    public int SelectedSideTabIndex
    {
        get;
        set => SetProperty(ref field, value);
    }

    [EventHandler]
    public void ApplyShellSidebarTabSelected(ShellSidebarTabSelectedCommand command)
    {
        // 侧边栏页签选择统一走事件总线，避免布局菜单直接引用文件/大纲 ViewModel。
        SelectedSideTabIndex = command.SelectedIndex;
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
}
