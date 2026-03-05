; QCactus Inno Setup Script
; Produces: installer/Output/QCactus_Setup.exe
;
; Prerequisites:
;   1. Run build_installer.bat (or dotnet publish manually) to populate the publish/ folder
;   2. Inno Setup 6 installed: https://jrsoftware.org/isinfo.php

#define AppName    "QCactus"
#define AppVersion "1.0"
#define AppPublisher "Cedars-Sinai Precision Biomarker Laboratories"
#define AppURL     "https://www.cs-pbl.com/"
#define AppExeName "ThermoDust.exe"

[Setup]
AppId={{A3F7B2D1-9C4E-4E8A-B6F2-1D3E5C7A9B0F}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
OutputDir=Output
OutputBaseFilename=QCactus_Setup
SetupIconFile=..\ThermoDust\images\cactus.ico
Compression=lzma2/ultra64
SolidCompression=yes
; Windows 10 or later recommended for .NET 6
MinVersion=10.0
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
; Self-contained .NET 6 app (from dotnet publish)
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; MSFragger tools (JAR, FASTA database, params)
; Note: MSFragger-3.8.jar requires Java — see Java note below
Source: "..\msfragger\*"; DestDir: "{app}\msfragger"; Flags: ignoreversion recursesubdirs createallsubdirs

; Thermo Fisher + Bruker external DLLs
Source: "..\DLLS\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Images
Source: "..\ThermoDust\images\*"; DestDir: "{app}\images"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\{#AppName}";                Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}";      Filename: "{uninstallexe}"
Name: "{commondesktop}\{#AppName}";        Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
; After install: auto-detect paths (the app handles this on first launch)
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Clean up user output folder on uninstall (optional — comment out to preserve data)
; Type: filesandordirs; Name: "{localappdata}\QCactus"

[Code]
// Check for Java on PATH before completing install
function JavaFound(): Boolean;
var
  ResultCode: Integer;
begin
  Result := Exec('java', '-version', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if not JavaFound() then
    begin
      MsgBox(
        'Java was not found on your PATH.' + #13#10 + #13#10 +
        'QCactus requires Java to run MSFragger (the peptide identification engine).' + #13#10 +
        'Please install OpenJDK from https://openjdk.org/ and add it to your PATH,' + #13#10 +
        'then re-open QCactus and use Tools > Settings to configure the java.exe path.',
        mbInformation, MB_OK);
    end;
  end;
end;
