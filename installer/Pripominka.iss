; Instalátor Připomínky. Kompiluje ho scripts\build.ps1 přes ISCC.

#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

[Setup]
AppId={{D53CE325-8922-47AD-B905-B43674067090}
AppName=Připomínka
AppVersion={#AppVersion}
DefaultDirName={autopf}\Pripominka
DefaultGroupName=Připomínka
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\dist
OutputBaseFilename=Pripominka-Setup-{#AppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\Pripominka.exe
CloseApplications=yes

[Languages]
Name: "czech"; MessagesFile: "compiler:Languages\Czech.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked
Name: "autostart"; Description: "Spouštět s Windows"; Flags: unchecked

[Files]
Source: "..\publish\Pripominka.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Připomínka"; Filename: "{app}\Pripominka.exe"
Name: "{autodesktop}\Připomínka"; Filename: "{app}\Pripominka.exe"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Pripominka"; ValueData: """{app}\Pripominka.exe"""; Flags: uninsdeletevalue; Tasks: autostart

[Run]
Filename: "{app}\Pripominka.exe"; Description: "Spustit Připomínku"; Flags: nowait postinstall skipifsilent
