[Setup]
AppName=FluentMigratorWrapper
AppVersion=1.0.0
DefaultDirName={autopf}\FluentMigratorWrapper
DefaultGroupName=FluentMigratorWrapper
UninstallDisplayIcon={app}\FluentMigratorWrapper.exe
Compression=lzma
SolidCompression=yes
OutputDir=.\publish
OutputBaseFilename=FluentMigratorWrapper_Setup
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: "{#SourcePath}\publish\fm-wrapper.exe"; DestDir: "{app}"; Flags: ignoreversion

[Registry]
Root: HKLM; Subkey: "SYSTEM\CurrentControlSet\Control\Session Manager\Environment"; \
    ValueType: expandsz; ValueName: "Path"; ValueData: "{olddata};{app}"; \
    Check: NeedsAddPath('{app}')

[Code]
function NeedsAddPath(Param: string): boolean;
var
  OrigPath: string;
begin
  if not RegQueryStringValue(HKEY_LOCAL_MACHINE,
    'SYSTEM\CurrentControlSet\Control\Session Manager\Environment',
    'Path', OrigPath) then begin
    Result := True;
    exit;
  end;
  { Verifica se a pasta já não está no PATH para não duplicar }
  Result := Pos(Uppercase(Param), Uppercase(OrigPath)) = 0;
end;