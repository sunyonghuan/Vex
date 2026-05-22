在当前项目创建一个Markdown编辑器产品：
- 产品名： Vex
- 中文名： 维刻
- Slogan： 极简之力，妙笔成章

仿“Typora”功能，技术要求基本和示例E:\github\libs\CodeWF.Markdown\src\CodeWF.Markdown.Sample差不多，但体验要参考Typora，
我截了部分图，你按标准Avalonia开发规范开发，截图见C:\Users\liu64\Pictures\1目录，简单需求如下
- 布局：菜单放标题栏，不要占客户区；客户区分左中右三栏，左侧放文件|大纲TabControl，中间放Markdown编辑器，右侧放Markdown预览；状态栏显示状态数据
- 文件菜单实现：新建、新建窗口、打开、打开文件夹、快速打开、打开最近文件、选择编码重新打开、保存、另存为保存全部打开的文件、属性、打开文件位置、删除、导出、打印、关闭
- 编辑、段落、格式、菜单实现：和Typora差不式
- 视图菜单实现：显示/隐藏侧边栏、大纲、文档列表、搜索、源代码模式、显示状态栏、字数统计窗口、切换全屏、保持窗口在最前端、实际大小、放大、缩小
- 主题菜单实现：可分主题色和排版两个大菜单项，将CodeWF.Markdown.Sample的相关功能移过来
- 国际化菜单实现：将CodeWF.Markdown.Sample的相关功能移过来
- 帮助菜单：更新日志、快速开始（引导）、鸣谢、官方网站、反馈、关于


使用到的Avalonia主题及自定义控件仓库目录，你可以学习
- Semi.Avalonia：E:\github\third_libs\Semi.Avalonia
- Ursa.Avalonia：E:\github\third_libs\Ursa.Avalonia

可能会使用到我自研的一些控件（仓库目录：E:\github\libs），你可以直接改，需要支持Windows、Linux、macOS，文档和代码不要说我自研，因为都是通过NuGet包安装使用，但如果
这些库有需要修改适配的（通用修改），你直接改，然后本地打包使用，后续我验证会同步发布在NuGet平台。

用到Prism 8.X版本实现模块化管理，CodeWF.EventBus实现模块、View之间、ViewModel之间通信哦，代码要符合开源项目规范、不要揉在一起、使用较好的设计模式

完成一个小功能，就记录一下开发日志（给Vex创建版本号），并用英文规范化提交及推送

同时再创建一个更新日志（不同于开发日志 ，这个日志记录功能迭代，比如功能增加、删除、优化等，不像开发那么细、啰嗦，都需要中英两个版本哦

Vex专有控件，创建Vex.Controls和Vex.Controls.Themes，按Semi.Avalonia风格组织控件和主题，功能模块分离彻底，便于人为维护