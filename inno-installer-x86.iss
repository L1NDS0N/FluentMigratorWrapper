[Setup]
AppName=FluentMigratorWrapper (x86)
AppVersion=1.0.0
DefaultDirName={autopf}\FluentMigratorWrapper
DefaultGroupName=FluentMigratorWrapper
OutputDir=.\publish
OutputBaseFilename=FluentMigratorWrapper_x86_Setup
Compression=lzma
SolidCompression=yes
ChangesEnvironment=yes

[Files]
Source: "{#SourcePath}\publish\x86\fm-wrapper.exe"; DestDir: "{app}"; Flags: ignoreversion

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
  Result := Pos(Uppercase(Param), Uppercase(OrigPath)) = 0;
end;