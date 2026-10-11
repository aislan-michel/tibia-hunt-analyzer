; Inno Setup Script for Tibia Hunt Analyzer

#define MyAppName "Tibia Hunt Analyzer"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Aislan Michel"
#define MyAppURL "https://github.com/aislan-michel"
#define MyAppExeName "Tibia Hunt Analyzer.exe"
#define MyAppDirName "Tibia Hunt Analyzer"

[Setup]
AppId={{7F53E58B-67D0-4C63-8765-D34EF7F89E17}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
AppCopyright=MIT License
DefaultDirName={autopf}\{#MyAppDirName}
DefaultGroupName={#MyAppName}
OutputBaseFilename=TibiaHuntAnalyzer-Setup
OutputDir=..\artifacts\windows\installer
SourceDir=..\artifacts\windows\win-x64
SetupIconFile=..\src\TibiaHuntAnalyzer.Desktop\Assets\tibia-hunt-analyzer-icon.ico
LicenseFile=..\LICENSE
Compression=lzma
SolidCompression=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
CreateAppDir=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
UsePreviousAppDir=no
AllowNoIcons=no
AlwaysRestart=no
RestartIfNeededByRun=no
ShowLanguageDialog=no
CloseApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: checkedonce

[Files]
Source: "*.*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; WorkingDir: "{app}"; IconFilename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Tibia Hunt Analyzer"; Flags: nowait postinstall skipifsilent
