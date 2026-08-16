
#define ApplicationName "TwinCAT.ProductivityTools"

; The version is injected by the release pipeline (GitVersion). When building locally without the
; environment variable set it falls back to the version of the freshly built VS2022 assembly.
#define ApplicationVersion GetEnv('PRODUCTIVITYTOOLS_VERSION')
#if ApplicationVersion == ""
  #define ApplicationVersion GetFileVersion('..\src\TwinCAT.ProductivityTools.17\bin\Release\TwinCAT.ProductivityTools.17.dll')
#endif

#define ApplicationPublisher "Philipp Leidig"
#define ApplicationURL "https://github.com/philippleidig/TwinCAT.ProductivityTools"

#define TcXaeShellExtensionsFolder15 "C:\Program Files (x86)\Beckhoff\TcXaeShell\Common7\IDE\Extensions\"
#define TcXaeShellExtensionsFolder17 "C:\Program Files\Beckhoff\TcXaeShell\Common7\IDE\Extensions\"
#define PackageVsixGuid "1e6f317c-4b46-4f08-96dc-4ab7dc8a1032"

[Setup]
AppId={{31584b8a-6a43-4c09-b713-bd8448d8b545}
AppName={#ApplicationName}
AppVersion={#ApplicationVersion}
AppVerName={#ApplicationName} {#ApplicationVersion}
AppPublisher={#ApplicationPublisher}
AppPublisherURL={#ApplicationURL}
AppSupportURL={#ApplicationURL}
AppUpdatesURL={#ApplicationURL}
CreateAppDir=no
LicenseFile=..\LICENSE.MD
OutputDir=..\dist
OutputBaseFilename={#ApplicationName}_{#ApplicationVersion}
SetupIconFile=..\assets\images\twincat.ico
Compression=lzma
SolidCompression=yes
VersionInfoCompany={#ApplicationPublisher}
VersionInfoProductName={#ApplicationName}
VersionInfoVersion={#ApplicationVersion}
CloseApplications=force
RestartApplications=True
WizardSmallImageFile=..\assets\images\twincat.bmp
SetupLogging=yes

[Files]
; --- PLC project templates -------------------------------------------------------------------
; The payload is identical for every TwinCAT version, only the .vsdir descriptor and the target
; layout differ between 4024 and 4026. See templates\README.md.
Source: "..\templates\Standard PLC Project Optimized Defaults\*"; \
  DestDir: "{code:GetTemplatePayloadDir}\Standard PLC Project Optimized Defaults"; \
  Flags: ignoreversion recursesubdirs createallsubdirs; Check: InstallTemplates;
Source: "..\templates\vsdir\4024\*"; DestDir: "{code:GetTemplateVsDirDir}"; \
  Flags: ignoreversion; Check: InstallTemplatesForTc4024;
Source: "..\templates\vsdir\4026\*"; DestDir: "{code:GetTemplateVsDirDir}"; \
  Flags: ignoreversion; Check: InstallTemplatesForTc4026;

; --- TcXaeShell (extensions are deployed by copying, the shell has no VSIXInstaller) -----------
Source: "..\src\TwinCAT.ProductivityTools.15\bin\Release\Package\*"; \
  DestDir: "{#TcXaeShellExtensionsFolder15}TwinCAT.ProductivityTools"; \
  Flags: ignoreversion recursesubdirs createallsubdirs; Check: InstallVsixInTcXaeShell15;
Source: "..\src\TwinCAT.ProductivityTools.17\bin\Release\Package\*"; \
  DestDir: "{#TcXaeShellExtensionsFolder17}TwinCAT.ProductivityTools"; \
  Flags: ignoreversion recursesubdirs createallsubdirs; Check: InstallVsixInTcXaeShell17;

; --- Visual Studio (installed via VSIXInstaller, see CurStepChanged) ---------------------------
Source: "..\src\TwinCAT.ProductivityTools.15\bin\Release\TwinCAT.ProductivityTools.15.vsix"; DestDir: "{tmp}"; Flags: deleteafterinstall;
Source: "..\src\TwinCAT.ProductivityTools.17\bin\Release\TwinCAT.ProductivityTools.17.vsix"; DestDir: "{tmp}"; Flags: deleteafterinstall;
Source: "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"; DestDir: "{tmp}"; Flags: ignoreversion deleteafterinstall;
; Keep a copy of vswhere so the uninstaller can still locate the Visual Studio instances.
Source: "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"; DestDir: "{commonpf}\{#ApplicationPublisher}\Utils"; Flags: ignoreversion uninsneveruninstall;

[InstallDelete]
Type: filesandordirs; Name: "{#TcXaeShellExtensionsFolder15}TwinCAT.ProductivityTools\*"
Type: filesandordirs; Name: "{#TcXaeShellExtensionsFolder17}TwinCAT.ProductivityTools\*"

[UninstallDelete]
Type: filesandordirs; Name: "{#TcXaeShellExtensionsFolder15}TwinCAT.ProductivityTools"
Type: filesandordirs; Name: "{#TcXaeShellExtensionsFolder17}TwinCAT.ProductivityTools"

[Code]
const
  TcXaeShell15Exe = 'C:\Program Files (x86)\Beckhoff\TcXaeShell\Common7\IDE\TcXaeShell.exe';
  TcXaeShell17Exe = 'C:\Program Files\Beckhoff\TcXaeShell\Common7\IDE\TcXaeShell.exe';
  Tc4024TemplatesRoot = 'C:\TwinCAT\3.1\Components\Plc\PlcTemplates\1.0.0.0\Plc Templates';
  { Index of the fixed entries on the option page. }
  IdxTcXaeShell15 = 0;
  IdxTcXaeShell17 = 1;
  IdxTemplates = 2;
  FirstVsIndex = 3;

var
  VisualStudioOptionsPage: TInputOptionWizardPage;

  ErrorCode: Integer;
  VsWhereOutput15: string;
  VsWhereOutput17: string;
  DisplayNames15: TStringList;
  DisplayNames17: TStringList;
  InstallationPaths15: TStringList;
  InstallationPaths17: TStringList;

  PackageVsixGuid: string;
  CachedTemplateRoot: string;
  CachedTemplateRootResolved: Boolean;

  UninstallFirstPage: TNewNotebookPage;
  UninstallButton: TNewButton;

function VsWhereValue(ParameterName: string; OutputData: string): TStringList;
var
  Lines: TStringList;
  Line: string;
  i: Integer;
begin
  Result := TStringList.Create;
  Lines := TStringList.Create;
  try
    Lines.Text := OutputData;
    for i := 0 to Lines.Count - 1 do
    begin
      Line := Lines[i];
      if Pos(ParameterName + ':', Line) > 0 then
      begin
        Result.Add(Trim(Copy(Line, Pos(ParameterName + ':', Line) + Length(ParameterName) + 2, MaxInt)));
      end;
    end;
  finally
    Lines.Free;
  end;
end;

// Exec with output stored in result.
// ResultString will only be altered if True is returned.
function ExecWithResult(Filename, Params, WorkingDir: String; ShowCmd: Integer; Wait: TExecWait; var ResultCode: Integer; var ResultString: String): Boolean;
var
  TempFilename: String;
  Command: String;
  ResultStringAnsi: AnsiString;
begin
  TempFilename := ExpandConstant('{tmp}\~execwithresult.txt');
  Command := Format('"%s" /S /C ""%s" %s > "%s""', [ExpandConstant('{cmd}'), Filename, Params, TempFilename]);
  Result := Exec(ExpandConstant('{cmd}'), Command, WorkingDir, ShowCmd, Wait, ResultCode);
  if not Result then
    Exit;
  LoadStringFromFile(TempFilename, ResultStringAnsi);
  ResultString := ResultStringAnsi;
  DeleteFile(TempFilename);
  // Remove new-line at the end
  if (Length(ResultString) >= 2) and (ResultString[Length(ResultString) - 1] = #13) and (ResultString[Length(ResultString)] = #10) then
    Delete(ResultString, Length(ResultString) - 1, 2);
end;

{ ---------------------------------------------------------------------------------------------
  TwinCAT detection
  --------------------------------------------------------------------------------------------- }

function GetTwinCATBuild(): Cardinal;
var
  Build: Cardinal;
begin
  Result := 0;
  if RegQueryDWordValue(HKLM32, 'SOFTWARE\Beckhoff\TwinCAT3\System', 'Build', Build) then
    Result := Build;
end;

function IsTwinCAT4026(): Boolean;
begin
  Result := GetTwinCATBuild() >= 4026;
end;

{ Returns the PlcTemplates root folder of the highest installed template package version.
  TwinCAT 4026 keeps the templates under ProgramData and versions the folder. }
function FindLatestTc4026TemplateRoot(): string;
var
  Base: string;
  FindRec: TFindRec;
  BestName: string;
  BestVersion: Int64;
  CurrentVersion: Int64;
begin
  Result := '';
  BestName := '';
  BestVersion := 0;
  Base := ExpandConstant('{commonappdata}') + '\Beckhoff\TwinCAT\PlcEngineering\PlcTemplates';

  if not DirExists(Base) then
    Exit;

  if FindFirst(Base + '\*', FindRec) then
  begin
    try
      repeat
        if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY <> 0) and
           (FindRec.Name <> '.') and (FindRec.Name <> '..') then
        begin
          if StrToVersion(FindRec.Name, CurrentVersion) then
          begin
            if (BestName = '') or (ComparePackedVersion(CurrentVersion, BestVersion) > 0) then
            begin
              BestName := FindRec.Name;
              BestVersion := CurrentVersion;
            end;
          end;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;

  if BestName <> '' then
    Result := Base + '\' + BestName;
end;

{ Root folder the templates are deployed into, depending on the installed TwinCAT version. }
function GetTemplateRoot(): string;
begin
  if not CachedTemplateRootResolved then
  begin
    if IsTwinCAT4026() then
      CachedTemplateRoot := FindLatestTc4026TemplateRoot()
    else if DirExists(Tc4024TemplatesRoot) then
      CachedTemplateRoot := Tc4024TemplatesRoot
    else
      CachedTemplateRoot := '';

    CachedTemplateRootResolved := True;
  end;

  Result := CachedTemplateRoot;
end;

function HasPlcTemplates(): Boolean;
begin
  Result := GetTemplateRoot() <> '';
end;

{ 4024: everything lives in one sub folder next to the Beckhoff templates.
  4026: one folder per template directly below the versioned root. }
function GetTemplatePayloadDir(Param: string): string;
begin
  if IsTwinCAT4026() then
    Result := GetTemplateRoot()
  else
    Result := GetTemplateRoot() + '\TwinCAT.ProductivityTools.Templates';
end;

{ 4024: the .vsdir sits next to the template folder.
  4026: all descriptors are collected in a shared TemplatesDir folder. }
function GetTemplateVsDirDir(Param: string): string;
begin
  if IsTwinCAT4026() then
    Result := GetTemplateRoot() + '\TemplatesDir'
  else
    Result := GetTemplateRoot() + '\TwinCAT.ProductivityTools.Templates';
end;

{ ---------------------------------------------------------------------------------------------
  Wizard
  --------------------------------------------------------------------------------------------- }

procedure InitializeWizard;
var
  i: Integer;
begin
  ExtractTemporaryFile('vswhere.exe');
  ExecWithResult(ExpandConstant('{tmp}\vswhere.exe'), '-all -prerelease -products * -requiresAny -requires Microsoft.VisualStudio.Product.Community Microsoft.VisualStudio.Product.Professional Microsoft.VisualStudio.Product.Enterprise -version [15.0,17.0)', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode, VsWhereOutput15);
  ExecWithResult(ExpandConstant('{tmp}\vswhere.exe'), '-all -prerelease -products * -requiresAny -requires Microsoft.VisualStudio.Product.Community Microsoft.VisualStudio.Product.Professional Microsoft.VisualStudio.Product.Enterprise -version [17.0,19.0)', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode, VsWhereOutput17);

  PackageVsixGuid := '{#PackageVsixGuid}';

  VisualStudioOptionsPage := CreateInputOptionPage(wpWelcome,
    'Install options', 'TwinCAT ProductivityTools are compatible with multiple IDEs',
    'Please choose the Visual Studio versions that TwinCAT ProductivityTools are installed for.',
    False, False);

  { IdxTcXaeShell15 }
  VisualStudioOptionsPage.Add('TcXaeShell (32-bit)');
  if FileExists(TcXaeShell15Exe) then
    VisualStudioOptionsPage.CheckListBox.Checked[IdxTcXaeShell15] := True
  else
    VisualStudioOptionsPage.CheckListBox.ItemEnabled[IdxTcXaeShell15] := False;

  { IdxTcXaeShell17 }
  VisualStudioOptionsPage.Add('TcXaeShell (64-bit)');
  if FileExists(TcXaeShell17Exe) then
    VisualStudioOptionsPage.CheckListBox.Checked[IdxTcXaeShell17] := True
  else
    VisualStudioOptionsPage.CheckListBox.ItemEnabled[IdxTcXaeShell17] := False;

  { IdxTemplates }
  VisualStudioOptionsPage.Add('PLC project templates');
  if HasPlcTemplates() then
    VisualStudioOptionsPage.CheckListBox.Checked[IdxTemplates] := True
  else
    VisualStudioOptionsPage.CheckListBox.ItemEnabled[IdxTemplates] := False;

  DisplayNames15 := VsWhereValue('displayName', VsWhereOutput15);
  InstallationPaths15 := VsWhereValue('installationPath', VsWhereOutput15);
  DisplayNames17 := VsWhereValue('displayName', VsWhereOutput17);
  InstallationPaths17 := VsWhereValue('installationPath', VsWhereOutput17);

  { Visual Studio entries start at FirstVsIndex: first all 15.x/16.x, then all 17.x/18.x. }
  for i := 0 to DisplayNames15.Count - 1 do
    VisualStudioOptionsPage.Add(DisplayNames15[i]);

  for i := 0 to DisplayNames17.Count - 1 do
    VisualStudioOptionsPage.Add(DisplayNames17[i]);
end;

function InstallVsixInTcXaeShell15(): Boolean;
begin
  Result := VisualStudioOptionsPage.CheckListBox.Checked[IdxTcXaeShell15];
end;

function InstallVsixInTcXaeShell17(): Boolean;
begin
  Result := VisualStudioOptionsPage.CheckListBox.Checked[IdxTcXaeShell17];
end;

function InstallTemplates(): Boolean;
begin
  Result := HasPlcTemplates() and VisualStudioOptionsPage.CheckListBox.Checked[IdxTemplates];
end;

function InstallTemplatesForTc4024(): Boolean;
begin
  Result := InstallTemplates() and (not IsTwinCAT4026());
end;

function InstallTemplatesForTc4026(): Boolean;
begin
  Result := InstallTemplates() and IsTwinCAT4026();
end;

procedure InstallVsix(InstallationPath, VsixFile: string);
var
  ReturnCode: Integer;
  Installer: string;
begin
  Installer := InstallationPath + '\Common7\IDE\VSIXInstaller.exe';
  if not FileExists(Installer) then
    Exit;

  ShellExec('', Installer, '/u:' + PackageVsixGuid + ' /quiet', '', SW_HIDE, ewWaitUntilTerminated, ReturnCode);
  ShellExec('', Installer, '/force /quiet "' + VsixFile + '"', '', SW_HIDE, ewWaitUntilTerminated, ReturnCode);
end;

procedure RegisterTcXaeShellExtension(ExePath: string);
var
  ReturnCode: Integer;
begin
  if FileExists(ExePath) then
    ShellExec('', ExePath, '/setup', '', SW_HIDE, ewWaitUntilTerminated, ReturnCode);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  i: Integer;
begin
  if CurStep <> ssPostInstall then
    Exit;

  ExtractTemporaryFile('TwinCAT.ProductivityTools.15.vsix');
  ExtractTemporaryFile('TwinCAT.ProductivityTools.17.vsix');

  for i := 0 to DisplayNames15.Count - 1 do
  begin
    if VisualStudioOptionsPage.CheckListBox.Checked[FirstVsIndex + i] then
      InstallVsix(InstallationPaths15[i], ExpandConstant('{tmp}\TwinCAT.ProductivityTools.15.vsix'));
  end;

  for i := 0 to DisplayNames17.Count - 1 do
  begin
    if VisualStudioOptionsPage.CheckListBox.Checked[FirstVsIndex + DisplayNames15.Count + i] then
      InstallVsix(InstallationPaths17[i], ExpandConstant('{tmp}\TwinCAT.ProductivityTools.17.vsix'));
  end;

  { The shells do not use VSIXInstaller, they need to rebuild their extension cache. }
  if InstallVsixInTcXaeShell15() then
    RegisterTcXaeShellExtension(TcXaeShell15Exe);
  if InstallVsixInTcXaeShell17() then
    RegisterTcXaeShellExtension(TcXaeShell17Exe);
end;

{ ---------------------------------------------------------------------------------------------
  Uninstall
  --------------------------------------------------------------------------------------------- }

function InitializeUninstall(): Boolean;
var
  VsWhere: string;
begin
  VsWhere := ExpandConstant('{commonpf}\{#ApplicationPublisher}\Utils\vswhere.exe');

  ExecWithResult(VsWhere, '-all -prerelease -products * -requiresAny -requires Microsoft.VisualStudio.Product.Community Microsoft.VisualStudio.Product.Professional Microsoft.VisualStudio.Product.Enterprise -version [15.0,17.0)', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode, VsWhereOutput15);
  ExecWithResult(VsWhere, '-all -prerelease -products * -requiresAny -requires Microsoft.VisualStudio.Product.Community Microsoft.VisualStudio.Product.Professional Microsoft.VisualStudio.Product.Enterprise -version [17.0,19.0)', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode, VsWhereOutput17);

  DisplayNames15 := VsWhereValue('displayName', VsWhereOutput15);
  InstallationPaths15 := VsWhereValue('installationPath', VsWhereOutput15);
  DisplayNames17 := VsWhereValue('displayName', VsWhereOutput17);
  InstallationPaths17 := VsWhereValue('installationPath', VsWhereOutput17);

  PackageVsixGuid := '{#PackageVsixGuid}';

  Result := True;
end;

procedure UninstallVsix(InstallationPath: string);
var
  ReturnCode: Integer;
  Installer: string;
begin
  Installer := InstallationPath + '\Common7\IDE\VSIXInstaller.exe';
  if FileExists(Installer) then
    ShellExec('', Installer, '/u:' + PackageVsixGuid + ' /quiet', '', SW_HIDE, ewWaitUntilTerminated, ReturnCode);
end;

procedure RemoveTemplates();
var
  Root: string;
begin
  Root := GetTemplateRoot();
  if Root = '' then
    Exit;

  if IsTwinCAT4026() then
  begin
    DelTree(Root + '\Standard PLC Project Optimized Defaults', True, True, True);
    DeleteFile(Root + '\TemplatesDir\Standard PLC Project Optimized Defaults.vsdir');
  end
  else
    DelTree(Root + '\TwinCAT.ProductivityTools.Templates', True, True, True);
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  i: Integer;
begin
  if CurUninstallStep <> usUninstall then
    Exit;

  for i := 0 to DisplayNames15.Count - 1 do
    UninstallVsix(InstallationPaths15[i]);

  for i := 0 to DisplayNames17.Count - 1 do
    UninstallVsix(InstallationPaths17[i]);

  RemoveTemplates();

  RegisterTcXaeShellExtension(TcXaeShell15Exe);
  RegisterTcXaeShellExtension(TcXaeShell17Exe);
end;

// --------------------------------------------------------------------------------------------------------------------------
// Uninstall Behavior
// --------------------------------------------------------------------------------------------------------------------------
procedure UpdateUninstallWizard;
begin
  UninstallButton.Caption := 'Uninstall';
  // Make the "Uninstall" button break the ShowModal loop
  UninstallButton.ModalResult := mrOK;
end;

procedure UninstallButtonClick(Sender: TObject);
begin
  UninstallButton.Visible := False;
  UpdateUninstallWizard;
end;

procedure InitializeUninstallProgressForm();
var
  UninstallText: TNewStaticText;
  PageNameLabel: string;
  PageDescriptionLabel: string;
  CancelButtonEnabled: Boolean;
  CancelButtonModalResult: Integer;
begin
  if not UninstallSilent then
  begin
    // Create the first page and make it active
    UninstallFirstPage := TNewNotebookPage.Create(UninstallProgressForm);
    UninstallFirstPage.Notebook := UninstallProgressForm.InnerNotebook;
    UninstallFirstPage.Parent := UninstallProgressForm.InnerNotebook;
    UninstallFirstPage.Align := alClient;

    UninstallText := TNewStaticText.Create(UninstallProgressForm);
    UninstallText.Parent := UninstallFirstPage;
    UninstallText.Top := UninstallProgressForm.StatusLabel.Top;
    UninstallText.Left := UninstallProgressForm.StatusLabel.Left;
    UninstallText.Width := UninstallProgressForm.StatusLabel.Width;
    UninstallText.Height := 300;
    UninstallText.AutoSize := False;
    UninstallText.ShowAccelChar := False;
    UninstallText.Caption := 'It was nice having you here!' #13 #10
                        'Thanks for using TwinCAT ProductivityTools, please leave some Feedback on:' #13 #10
                        '{#ApplicationURL}';

    UninstallProgressForm.InnerNotebook.ActivePage := UninstallFirstPage;

    PageNameLabel := UninstallProgressForm.PageNameLabel.Caption;
    PageDescriptionLabel := UninstallProgressForm.PageDescriptionLabel.Caption;

    UninstallButton := TNewButton.Create(UninstallProgressForm);
    UninstallButton.Parent := UninstallProgressForm;
    UninstallButton.Left := UninstallProgressForm.CancelButton.Left - UninstallProgressForm.CancelButton.Width - ScaleX(10);
    UninstallButton.Top := UninstallProgressForm.CancelButton.Top;
    UninstallButton.Width := UninstallProgressForm.CancelButton.Width;
    UninstallButton.Height := UninstallProgressForm.CancelButton.Height;
    UninstallButton.OnClick := @UninstallButtonClick;
    UninstallButton.TabOrder := UninstallButton.TabOrder + 1;

    UninstallProgressForm.CancelButton.TabOrder := UninstallButton.TabOrder + 1;

    // Run our wizard pages
    UpdateUninstallWizard;
    CancelButtonEnabled := UninstallProgressForm.CancelButton.Enabled;
    UninstallProgressForm.CancelButton.Enabled := True;
    CancelButtonModalResult := UninstallProgressForm.CancelButton.ModalResult;
    UninstallProgressForm.CancelButton.ModalResult := mrCancel;

    if UninstallProgressForm.ShowModal = mrCancel then Abort;

    // Restore the standard page layout
    UninstallProgressForm.CancelButton.Enabled := CancelButtonEnabled;
    UninstallProgressForm.CancelButton.ModalResult := CancelButtonModalResult;

    UninstallProgressForm.PageNameLabel.Caption := PageNameLabel;
    UninstallProgressForm.PageDescriptionLabel.Caption := PageDescriptionLabel;

    UninstallProgressForm.InnerNotebook.ActivePage := UninstallProgressForm.InstallingPage;
  end;
end;
