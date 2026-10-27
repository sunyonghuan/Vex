; Markdor Windows 安装包。
; 从仓库根目录使用 Inno Setup 6 编译，并传入 /DAppVersion=x.y.z。

#ifndef AppVersion
#define AppVersion "0.0.0"
#endif

#ifndef SourceDir
#define SourceDir "..\artifacts\publish\win-x64\Markdor"
#endif

#ifndef OutputDir
#define OutputDir "..\artifacts\release"
#endif

[Setup]
LicenseFile=..\LICENSE
AppId={{8F8111FF-79BD-4B8A-91F3-C23FAF6906EE}
AppName=Markdor
AppVersion={#AppVersion}
AppPublisher=CodeWF
SetupIconFile=..\assets\logo.ico
AppSupportURL=https://github.com/sunyonghuan/Vex/issues
DefaultDirName={autopf}\Markdor
DefaultGroupName=Markdor
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=Markdor-v{#AppVersion}-win-x64-setup
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; 按用户安装：应用以普通权限运行，可持久化自身设置（配置文件在安装目录内）。
PrivilegesRequired=lowest
ChangesAssociations=yes
CloseApplications=yes
RestartApplications=yes
CloseApplicationsFilter=Markdor.exe
UninstallDisplayIcon={app}\Markdor.exe
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimplified"; MessagesFile: "Languages\ChineseSimplified.isl"

[CustomMessages]
english.OpenWithMarkdor=Open with Markdor
chinesesimplified.OpenWithMarkdor=用 Markdor 打开
english.MarkdownAssociation=Register Markdor as an opening option for Markdown files
chinesesimplified.MarkdownAssociation=注册 Markdor 为 Markdown 文件的打开方式候选
english.FileAssociations=File associations:
chinesesimplified.FileAssociations=文件关联：

[Registry]
; 注册候选、图标与右键入口，默认打开程序由用户在系统设置中选择。
Root: HKCU; Subkey: "Software\Classes\Markdor.Markdown"; ValueType: string; ValueName: ""; ValueData: "Markdown"; Flags: uninsdeletekey; Tasks: markdownassociation
Root: HKCU; Subkey: "Software\Classes\Markdor.Markdown\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: """{app}\Markdor.exe"",0"; Tasks: markdownassociation
Root: HKCU; Subkey: "Software\Classes\Markdor.Markdown\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Markdor.exe"" ""%1"""; Tasks: markdownassociation
Root: HKCU; Subkey: "Software\Classes\Applications\Markdor.exe"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "Markdor"; Flags: uninsdeletekey; Tasks: markdownassociation
Root: HKCU; Subkey: "Software\Classes\Applications\Markdor.exe\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: """{app}\Markdor.exe"",0"; Tasks: markdownassociation
Root: HKCU; Subkey: "Software\Classes\Applications\Markdor.exe\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Markdor.exe"" ""%1"""; Tasks: markdownassociation
#sub RegisterMarkdownExtension
Root: HKCU; Subkey: "Software\Classes\Applications\Markdor.exe\SupportedTypes"; ValueType: string; ValueName: "{#MarkdownExtension}"; ValueData: ""; Tasks: markdownassociation
Root: HKCU; Subkey: "Software\Classes\{#MarkdownExtension}\OpenWithProgids"; ValueType: none; ValueName: "Markdor.Markdown"; Flags: uninsdeletevalue; Tasks: markdownassociation
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\{#MarkdownExtension}\shell\OpenWithMarkdor"; ValueType: string; ValueName: ""; ValueData: "{cm:OpenWithMarkdor}"; Flags: uninsdeletekey; Tasks: markdownassociation
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\{#MarkdownExtension}\shell\OpenWithMarkdor"; ValueType: string; ValueName: "Icon"; ValueData: """{app}\Markdor.exe"",0"; Tasks: markdownassociation
Root: HKCU; Subkey: "Software\Classes\SystemFileAssociations\{#MarkdownExtension}\shell\OpenWithMarkdor\command"; ValueType: string; ValueName: ""; ValueData: """{app}\Markdor.exe"" ""%1"""; Tasks: markdownassociation
#endsub
#define MarkdownExtension ".md"
#emit RegisterMarkdownExtension
#define MarkdownExtension ".markdown"
#emit RegisterMarkdownExtension
#define MarkdownExtension ".mdown"
#emit RegisterMarkdownExtension

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "markdownassociation"; Description: "{cm:MarkdownAssociation}"; GroupDescription: "{cm:FileAssociations}"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Excludes: "*.pdb,Markdor.dll.config,Markdor.exe.config"; Flags: ignoreversion recursesubdirs createallsubdirs
; 设置不随升级覆盖，也不随卸载删除；迁移配置在复制载荷前恢复。
Source: "{#SourceDir}\Markdor.dll.config"; DestDir: "{app}"; Flags: onlyifdoesntexist uninsneveruninstall skipifsourcedoesntexist

[Icons]
Name: "{autoprograms}\Markdor"; Filename: "{app}\Markdor.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Markdor"; Filename: "{app}\Markdor.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\Markdor.exe"; Description: "{cm:LaunchProgram,Markdor}"; Flags: nowait postinstall skipifsilent

#include "installer\Markdor.Setup.iss"
