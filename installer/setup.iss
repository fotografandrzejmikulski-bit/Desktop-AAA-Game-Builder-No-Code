; =====================================================================
;  Inno Setup Script – Desktop AAA Game Builder – No Code
;  Version 1.0.0
;
;  Ręczna kompilacja (po wykonaniu dotnet publish):
;    iscc installer\setup.iss
;
;  CI kompiluje automatycznie – patrz .github/workflows/build-installer.yml
;  Wymagania: Inno Setup 6 (https://jrsoftware.org/isinfo.php)
; =====================================================================

#define MyAppName      "AAA Game Builder"
#define MyAppFullName  "Desktop AAA Game Builder – No Code"
#define MyAppVersion   "1.0.0"
#define MyAppPublisher "fotografandrzejmikulski-bit"
#define MyAppURL       "https://github.com/fotografandrzejmikulski-bit/Desktop-AAA-Game-Builder-No-Code"
#define MyAppExeName   "AAAGameBuilder.exe"
#define MyAppIconFile  "..\src\AAA.App\Assets\icon.ico"
#define MyAppId        "{{B7E4C2A1-3F5D-4E8B-9A0C-1D2E3F4A5B6C}"
; OutputBaseFilename can be overridden via /D flag from the CI workflow
#ifndef OutputBaseFilename
  #define OutputBaseFilename "AAAGameBuilder-Setup-v" + MyAppVersion
#endif

[Setup]
AppId={#MyAppId}
AppName={#MyAppFullName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
; Ikona okna instalatora i pozycji Dodaj/Usuń programy
SetupIconFile={#MyAppIconFile}
OutputDir=..\artifacts
OutputBaseFilename={#OutputBaseFilename}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequiredOverridesAllowed=dialog
MinVersion=10.0
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=force
RestartIfNeededByRun=no
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppFullName}
VersionInfoVersion={#MyAppVersion}
VersionInfoProductVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppFullName}
VersionInfoCopyright=Copyright (C) 2026 {#MyAppPublisher}

[Languages]
Name: "polish";   MessagesFile: "compiler:Languages\Polish.isl"
Name: "english";  MessagesFile: "compiler:Default.isl"

[CustomMessages]
polish.AppDescription=No-code game builder napędzany konwersacją z AI.%nOpis gry po polsku → wygenerowany, zwalidowany i zatwierdzony GDD → gotowy projekt gry.
english.AppDescription=AI-powered no-code game builder.%nDescribe your game in Polish → AI generates, validates and approves a GDD → ready game project.

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Self-contained publish output – brak wymagania .NET Runtime u użytkownika
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}";                          Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}";    Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";                    Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
