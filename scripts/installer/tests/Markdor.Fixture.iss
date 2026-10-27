; 安装界面夹具：只复制 LICENSE，不注册、不建快捷方式、不启动应用。
#define AppVersion "1.0.0"
#define CodeWFSetupFixture
#define MarkdorLicensePath "..\..\..\LICENSE"
[Setup]
AppId=Markdor.Installer.Verification.Only
AppName=Markdor Installer Fixture
AppVersion={#AppVersion}
DefaultDirName={tmp}\Markdor.Installer.Fixture
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
Uninstallable=no
CreateUninstallRegKey=no
OutputBaseFilename=Markdor.Installer.Fixture
WizardStyle=modern
LicenseFile=..\..\..\LICENSE
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimplified"; MessagesFile: "..\..\Languages\ChineseSimplified.isl"
[Files]
Source: "..\..\..\LICENSE"; DestDir: "{app}"
[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked
Name: "markdownassociation"; Description: "{cm:VAssociation}"
#include "..\Markdor.Setup.iss"
#include "Fixture.Progress.iss"
[Code]
<event('CurPageChanged')>
procedure VFixtureLaunchVisual(PageID: Integer);
begin
  if PageID<>wpFinished then exit;
  { 只添加无执行项的视觉选项；夹具没有 [Run] 声明，绝不启动应用。 }
  WizardForm.RunList.AddCheckBox(CustomMessage('VOpen'),'',0,False,True,False,False,nil);
  VLayout;
end;

<event('CurStepChanged')>
procedure VFixtureVerifyTasks(Step: TSetupStep);
begin
  if Step<>ssInstall then exit;
  if WizardIsTaskSelected('desktopicon') then Log('MARKDOR FIXTURE desktop=1') else Log('MARKDOR FIXTURE desktop=0');
  if WizardIsTaskSelected('markdownassociation') then Log('MARKDOR FIXTURE association=1') else Log('MARKDOR FIXTURE association=0');
end;

#ifdef MarkdorFixtureMaintenance
<event('InitializeWizard')>
procedure VFixtureExistingInstallation;
begin
  { 安装注册仅为内存样例，CodeWFSetupFixture 已在编译期移除真实注册探测与卸载启动。 }
  CWMRegistrationCount:=1;
  CWMRepairPath:=ExpandConstant('{tmp}\维刻 maintenance with spaces');
  #ifndef MarkdorFixtureMissingExecutable
    { 仅用于验证已安装主程序存在性；生产启动路径在夹具中已移除。 }
    ForceDirectories(CWMRepairPath);
    SaveStringToFile(AddBackslash(CWMRepairPath)+'Markdor.exe','Markdor visual fixture - not an executable',False);
  #endif
  CWMRegistrations[0].ScopeName:=CustomMessage('CWMCurrentUser');
  CWMRegistrations[0].AppPath:=CWMRepairPath;
  VFixtureVersions[0]:='0.9.0';
  CWMRegistrations[0].Uninstaller:=AddBackslash(CWMRepairPath)+'unins000.exe';
  #ifdef MarkdorFixtureUnknownLocation
    CWMRepairPath:=''; CWMRegistrations[0].AppPath:=''; CWMRegistrations[0].Uninstaller:='';
    VFixtureVersions[0]:='';
    CWMRegistrations[0].ScopeName:=CustomMessage('CWMAllUsers');
  #endif
  #ifdef MarkdorFixtureMultiple
    CWMRegistrationCount:=2;
    CWMRegistrations[1].ScopeName:=CustomMessage('CWMAllUsers');
    CWMRegistrations[1].AppPath:='C:\Program Files\Markdor';
    VFixtureVersions[1]:='0.8.0';
    CWMRegistrations[1].Uninstaller:='C:\Program Files\Markdor\unins000.exe';
  #endif
  #ifdef MarkdorFixtureMigration
    VMigrationNeeded:=True;
  #endif
  CWMCreatePage;
  CWMPage.CheckListBox.OnClickCheck:=@VMaintenanceSelection;
end;
#endif
