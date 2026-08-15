# Installation

## Which environments are supported

| TwinCAT | TcXaeShell (32 bit) | TcXaeShell 64 | Visual Studio 2017/2019 | Visual Studio 2022 | Visual Studio 2026 |
| --- | :---: | :---: | :---: | :---: | :---: |
| 3.1 build 4024 | yes | – | yes | yes | – |
| 3.1 build 4026 | – | yes | – | yes | yes |

The extension ships as two VSIX packages that share all of their code:

* `TwinCAT.ProductivityTools.15.vsix` targets the 32 bit shells — TcXaeShell and Visual Studio
  2017/2019.
* `TwinCAT.ProductivityTools.17.vsix` targets the 64 bit shells — TcXaeShell 64, Visual Studio
  2022 and Visual Studio 2026. Its installation target is `[17.0, 19.0)`, so a Visual Studio
  update to a new major version does not disable it.

The PLC project template is installed next to the extension. Its location differs between the
TwinCAT builds, see [PLC project template](plc-templates.md).

## Option 1: the installer

1. Download the latest `TwinCAT.ProductivityTools_<version>.exe` from the
   [releases page](https://github.com/philippleidig/TwinCAT.ProductivityTools/releases/latest).
2. Run it. The installer lists the engineering environments it found on the machine.
3. Select the ones the extension should be installed into and confirm.

Close every Visual Studio and TcXaeShell instance first. An IDE that is running while the
extension is replaced keeps the old files loaded until it is restarted.

## Option 2: the TwinCAT Package Manager

The extension is also published as a TcPkg workload, which is the more convenient route on a
machine that is provisioned with `tcpkg`.

```powershell
tcpkg install TwinCAT.ProductivityTools
```

That pulls in the whole set:

| Package | Contents |
| --- | --- |
| `TwinCAT.ProductivityTools` | The workload itself |
| `TwinCAT.ProductivityTools.Config` | PLC project template, matched to the installed TwinCAT build |
| `TwinCAT.ProductivityTools.XAE` | Meta package for the engineering integration |
| `TwinCAT.ProductivityTools.XAE.TcXaeShell` | TcXaeShell, 32 bit |
| `TwinCAT.ProductivityTools.XAE.TcXaeShell64` | TcXaeShell, 64 bit |
| `TwinCAT.ProductivityTools.XAE.VS2022` | Visual Studio 2022, any edition |
| `TwinCAT.ProductivityTools.XAE.VS2026` | Visual Studio 2026, any edition |

Every integration package resolves the IDE it belongs to at install time through `vswhere`, and
does nothing when that IDE is not on the machine. Installing a single environment works as well:

```powershell
tcpkg install TwinCAT.ProductivityTools.XAE.VS2022
```

Removing the workload removes the extension and the template again:

```powershell
tcpkg remove TwinCAT.ProductivityTools
```

## Option 3: the VSIX by hand

Download `TwinCAT.ProductivityTools.17.vsix` from the release and install it with the VSIX
installer of the target IDE. Copying the folder into `Extensions` is **not** enough — Visual
Studio only picks up an extension that went through the installer, because the installer is what
merges the command table.

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\VSIXInstaller.exe" `
    /quiet /instanceIds:<instanceId> TwinCAT.ProductivityTools.17.vsix
```

The instance identifiers of the installed IDEs come from `vswhere`:

```powershell
& "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
    -all -prerelease -products * -format json
```

## Verifying the installation

Open a solution that contains a TwinCAT XAE project. The **Tools** menu then holds a
**TwinCAT Productivity Tools** submenu:

![The Tools menu with the submenu open](images/tools-menu.png)

If the submenu is missing, the extension did not load. **Help ▸ About** lists it under
"TwinCAT 3.1 ProductivityTools"; when it is listed but the menu is missing, the command table was
not merged and the IDE has to be started once with `devenv.exe /setup`.
