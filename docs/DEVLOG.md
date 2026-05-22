# Vex Development Log

## 0.1.0 - 2026-05-22

### zh-CN

- 完善项目身份信息：作者“沙漠尽头的狼”、出品“码坊 CodeWF”、网站 `https://codewf.com`。
- 创建 Vex Avalonia 桌面应用骨架，接入 Prism 8.x 模块目录。
- 创建 `Vex.Controls` 和 `Vex.Controls.Themes`，按 Semi.Avalonia 风格拆分控件和主题资源。
- 实现主窗口标题栏菜单、左中右三栏、状态栏和 Markdown 编辑/预览基础体验。
- 接入 CodeWF.EventBus，用于编辑器视图和 Shell ViewModel 之间传递编辑动作与 Markdown 内容变化。
- 接入 CodeWF.Markdown.Themes，提供排版主题和紧凑布局切换入口。
- 创建双语更新日志、快速开始和鸣谢文档。

### en-US

- Added project identity metadata for author, CodeWF, and `https://codewf.com`.
- Created the Vex Avalonia desktop application shell with Prism 8.x module catalog wiring.
- Created `Vex.Controls` and `Vex.Controls.Themes`, with controls and theme resources separated in a Semi.Avalonia-style layout.
- Implemented the title-bar menu, three-pane workspace, status bar, and the initial Markdown editing/preview experience.
- Wired CodeWF.EventBus for editor action and Markdown content communication between the editor view and shell ViewModel.
- Integrated CodeWF.Markdown.Themes entry points for typography themes and compact layout switching.
- Added bilingual changelog, quick start, and acknowledgements documents.
