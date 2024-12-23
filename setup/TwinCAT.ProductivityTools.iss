
#define ApplicationName "TwinCAT.ProductivityTools"
#define ApplicationVersion GetFileVersion('..\src\TwinCAT.ProductivityTools.15\bin\Release\TwinCAT.ProductivityTools.15.dll')
#define ApplicationPublisher "Philipp Leidig"
#define ApplicationURL "https://github.com/philippleidig/TwinCAT.ProductivityTools"
#define ApplicationExeName "TwinCAT.ProductivityTools.exe"

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
CloseApplications=force
RestartApplications=True
WizardSmallImageFile=..\assets\images\twincat.bmp
SetupLogging=yes

[Files]
Source: "..\src\TwinCAT.ProductivityTools.15\bin\Release\Package\*"; DestDir: "{#TcXaeShellExtensionsFolder15}TwinCAT.ProductivityTools"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: InstallVsixInTcXaeShell15;
Source: "..\src\TwinCAT.ProductivityTools.17\bin\Release\Package\*"; DestDir: "{#TcXaeShellExtensionsFolder17}TwinCAT.ProductivityTools"; Flags: ignoreversion recursesubdirs createallsubdirs; Check: InstallVsixInTcXaeShell17;
Source: "..\src\TwinCAT.ProductivityTools.15\bin\Release\TwinCAT.ProductivityTools.15.vsix"; DestDir: "{tmp}"; Flags: deleteafterinstall;
Source: "..\src\TwinCAT.ProductivityTools.17\bin\Release\TwinCAT.ProductivityTools.17.vsix"; DestDir: "{tmp}"; Flags: deleteafterinstall;
Source: "C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"; DestDir: "{tmp}"; Flags: ignoreversion recursesubdirs deleteafterinstall;

[Dirs]
Name: "{commonpf}\Beckhoff\TcXaeShell\Common7\IDE\Extensions\TwinCAT.ProductivityTools"
Name: "C:\Program Files (x86)\Beckhoff\TcXaeShell\Common7\IDE\Extensions\TwinCAT.ProductivityTools"

[InstallDelete]
Type: filesandordirs; Name: "{#TcXaeShellExtensionsFolder15}TwinCAT.ProductivityTools\*"
Type: filesandordirs; Name: "{#TcXaeShellExtensionsFolder17}TwinCAT.ProductivityTools\*"


[Code]
function VsWhereValue(ParameterName: string; OutputData: string): TStringList;
var
  Lines: TStringList;
  Line: string;
  i: integer;
  begin
    Result := TStringList.Create;
    Lines := TStringList.Create;
    try
      Lines.Text := OutputData;
      for i := 0 TO Lines.Count - 1 do
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


var
  VisualStudioOptionsPage: TInputOptionWizardPage;

  OutputFile: string;
  OutputData: AnsiString;
  ErrorCode: integer;
  i : integer; 
  VsWhereOutput15 : string;
  VsWhereOutput17 : string; 
  DisplayNames15: TStringList;
  DisplayNames17: TStringList; 

  PackageVsixGuid: string;
  InstallationPaths15: TStringList;
  InstallationPaths17: TStringList;  
  UninstallFirstPage: TNewNotebookPage;
  UninstallButton: TNewButton; 

procedure InitializeWizard;
begin
  ExtractTemporaryFile('vswhere.exe');
  ExecWithResult(ExpandConstant('{tmp}\\vswhere.exe'), '-all -products * -requiresAny -requires Microsoft.VisualStudio.Product.Community Microsoft.VisualStudio.Product.Professional Microsoft.VisualStudio.Product.Enterprise -version [15.0,17.0)', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode, VsWhereOutput15);
  ExecWithResult(ExpandConstant('{tmp}\\vswhere.exe'), '-all -products * -requiresAny -requires Microsoft.VisualStudio.Product.Community Microsoft.VisualStudio.Product.Professional Microsoft.VisualStudio.Product.Enterprise -version [17.0,18.0)', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode, VsWhereOutput17);
 
  PackageVsixGuid := '1e6f317c-4b46-4f08-96dc-4ab7dc8a1032';

  { Create the pages }
  
  { VisualStudioOptionsPage } 
	VisualStudioOptionsPage := CreateInputOptionPage(wpWelcome,
	  'Install options', 'TwinCAT ProductivityTools are compatible with multiple IDEs',
	  'Please choose the Visual Studio versions that TwinCAT ProductivityTools are installed for.',
	  False, False);

	// Add items
  VisualStudioOptionsPage.Add('TcXawShell (32-bit)');
  if(FileExists('C:\Program Files (x86)\Beckhoff\TcXaeShell\Common7\IDE\TcXaeShell.exe')) then
    VisualStudioOptionsPage.CheckListBox.Checked[0] := true
  else
    VisualStudioOptionsPage.CheckListBox.ItemEnabled[0] := false;

  VisualStudioOptionsPage.Add('TcXaeShell (64-bit)');
  if(FileExists('C:\Program Files\Beckhoff\TcXaeShell\Common7\IDE\TcXaeShell.exe')) then
    VisualStudioOptionsPage.CheckListBox.Checked[1] := true
  else
    VisualStudioOptionsPage.CheckListBox.ItemEnabled[1] := false;

  DisplayNames15 := VsWhereValue('displayName', VsWhereOutput15);
  InstallationPaths15 := VsWhereValue('installationPath', VsWhereOutput15); 

  for i:= 0 to DisplayNames15.Count-1 do
  begin
    if(FileExists(InstallationPaths15[i] + '\Common7\IDE\VSIXInstaller.exe')) then
      VisualStudioOptionsPage.Add(DisplayNames15[i]);
  end;
  
  DisplayNames17 := VsWhereValue('displayName', VsWhereOutput17);
  InstallationPaths17 := VsWhereValue('installationPath', VsWhereOutput17); 

  for i:= 0 to DisplayNames17.Count-1 do
  begin
    if(FileExists(InstallationPaths17[i] + '\Common7\IDE\VSIXInstaller.exe')) then
      VisualStudioOptionsPage.Add(DisplayNames17[i]);
  end;  
end;

function InstallVsixInTcXaeShell15(): Boolean;
begin
  Result := VisualStudioOptionsPage.CheckListBox.Checked[0] = True;
end;

function InstallVsixInTcXaeShell17(): Boolean;
begin
  Result := VisualStudioOptionsPage.CheckListBox.Checked[1] = True;
end;

procedure CurStepChanged (CurStep: TSetupStep);
var
  WorkingDir:   String;
  ReturnCode:   Integer;
  i:            Integer;
begin  
  if (ssInstall = CurStep) then
  begin
    ExtractTemporaryFile('TwinCAT.ProductivityTools.15.vsix');
    ExtractTemporaryFile('TwinCAT.ProductivityTools.17.vsix');
	
    for i := 0 to DisplayNames15.Count-1 do
    begin
      if(VisualStudioOptionsPage.CheckListBox.Checked[i+3]) then
      begin
        ShellExec('', InstallationPaths15[i] + '\Common7\IDE\VSIXInstaller.exe', '/u:' + PackageVsixGuid + ' /quiet', '', SW_HIDE, ewWaitUntilTerminated, ReturnCode);
        ShellExec('', InstallationPaths15[i] + '\Common7\IDE\VSIXInstaller.exe', '/force /quiet ' + ExpandConstant('{tmp}\TwinCAT.ProductivityTools.15.vsix'), '', SW_HIDE, ewWaitUntilTerminated, ReturnCode);
      end;
    end;
	
    for i := 0 to DisplayNames17.Count-1 do
    begin
      if(VisualStudioOptionsPage.CheckListBox.Checked[i+2+DisplayNames15.Count]) then
      begin
        ShellExec('', InstallationPaths17[i] + '\Common7\IDE\VSIXInstaller.exe', '/u:' + PackageVsixGuid + ' /quiet', '', SW_HIDE, ewWaitUntilTerminated, ReturnCode);
        ShellExec('', InstallationPaths17[i] + '\Common7\IDE\VSIXInstaller.exe', '/force /quiet ' + ExpandConstant('{tmp}\TwinCAT.ProductivityTools.17.vsix'), '', SW_HIDE, ewWaitUntilTerminated, ReturnCode);
      end;
    end;	
  end;
end;

function InitializeUninstall(): Boolean;
begin
  ExecWithResult(ExpandConstant('{commonpf}\\{#ApplicationPublisher}\\Utils\\vswhere.exe'), '-all -products * -requiresAny -requires Microsoft.VisualStudio.Product.Community Microsoft.VisualStudio.Product.Professional Microsoft.VisualStudio.Product.Enterprise -version [15.0,17.0)', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode, VsWhereOutput15);
  ExecWithResult(ExpandConstant('{commonpf}\\{#ApplicationPublisher}\\Utils\\vswhere.exe'), '-all -products * -requiresAny -requires Microsoft.VisualStudio.Product.Community Microsoft.VisualStudio.Product.Professional Microsoft.VisualStudio.Product.Enterprise -version [17.0,18.0)', '', SW_HIDE, ewWaitUntilTerminated, ErrorCode, VsWhereOutput17);

  DisplayNames15 := VsWhereValue('displayName', VsWhereOutput15);
  InstallationPaths15 := VsWhereValue('installationPath', VsWhereOutput15);   
  DisplayNames17 := VsWhereValue('displayName', VsWhereOutput17);
  InstallationPaths17 := VsWhereValue('installationPath', VsWhereOutput17);  
  
  Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ReturnCode : Integer;
begin
  case CurUninstallStep of
    usUninstall:
      begin
        for i:= 0 to DisplayNames15.Count-1 do
        begin
          ShellExec('', InstallationPaths15[i] + '\Common7\IDE\VSIXInstaller.exe', '/u:' + PackageVsixGuid + ' /quiet', '', SW_HIDE, ewWaitUntilTerminated, ReturnCode);  
        end;        

        for i:= 0 to DisplayNames17.Count-1 do
        begin
          ShellExec('', InstallationPaths17[i] + '\Common7\IDE\VSIXInstaller.exe', '/u:' + PackageVsixGuid + ' /quiet', '', SW_HIDE, ewWaitUntilTerminated, ReturnCode);  
        end;        
      end;	  
    usPostUninstall:
      begin
        // ...insert code to perform post-uninstall tasks here...
      end;
  end;
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
  UserPageOpenDiscussionText: TNewStaticText;
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
    UninstallText.Height := 300; //UninstallProgressForm.StatusLabel.Height;
    UninstallText.AutoSize := False;
    UninstallText.ShowAccelChar := False;
    UninstallText.Caption := 'It was nice having you here!' #13 #10 
                        'Thanks for using TwinCAT ProductivityTools, please leave some Feedback on:';
    
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
    CancelButtonEnabled := UninstallProgressForm.CancelButton.Enabled
    UninstallProgressForm.CancelButton.Enabled := True;
    CancelButtonModalResult := UninstallProgressForm.CancelButton.ModalResult;
    UninstallProgressForm.CancelButton.ModalResult := mrCancel;

    if UninstallProgressForm.ShowModal = mrCancel then Abort;

    // Restore the standard page payout
    UninstallProgressForm.CancelButton.Enabled := CancelButtonEnabled;
    UninstallProgressForm.CancelButton.ModalResult := CancelButtonModalResult;

    UninstallProgressForm.PageNameLabel.Caption := PageNameLabel;
    UninstallProgressForm.PageDescriptionLabel.Caption := PageDescriptionLabel;

    UninstallProgressForm.InnerNotebook.ActivePage := UninstallProgressForm.InstallingPage;
  end;
end;