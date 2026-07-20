#define MyAppName "EyesGuard"
#define MyAppVersion "0.1.0"
#define MyAppPublisher "EyesGuard"
#define MyAppExeName "EyesGuard.App.exe"

[Setup]
AppId={{B8D9A2F5-8C12-4A58-A5E5-1B0C7E9F6E6D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\EyesGuard
DefaultGroupName=EyesGuard
OutputDir=..\.artifacts\installer
OutputBaseFilename=EyesGuard-Setup-{#MyAppVersion}
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\{#MyAppExeName}

[Files]
Source: "..\.artifacts\windows\win-x64\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\EyesGuard"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\EyesGuard"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加快捷方式："

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动 EyesGuard"; Flags: nowait postinstall skipifsilent
