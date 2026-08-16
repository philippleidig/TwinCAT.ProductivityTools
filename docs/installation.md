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

### Registering the feed

The packages are published to GitHub Packages, not to one of the Beckhoff feeds, so the source
has to be registered once. The feed is a NuGet v3 service index:

```
https://nuget.pkg.github.com/philippleidig/index.json
```

GitHub Packages requires authentication on its NuGet registry **even for public packages** — an
anonymous request answers `401`. Create a classic personal access token with the `read:packages`
scope and register the source from an elevated PowerShell, because `tcpkg` writes to the machine
wide configuration:

```powershell
'<your-token>' | tcpkg source add `
    --name GitHub `
    --source https://nuget.pkg.github.com/philippleidig/index.json `
    --user <your-github-username> `
    --password-stdin `
    --priority 7 `
    --take 100
```

The Beckhoff feeds occupy priorities 1 to 6, so 7 keeps this source below them and leaves the
resolution of TwinCAT's own packages untouched. Verify it with `tcpkg source list`.

#### `--take 100` is not optional

Leaving it out makes the command fail:

```
Error: Failed to retrieve metadata from source
'https://nuget.pkg.github.com/<owner>/query?q=&skip=0&take=500&prerelease=false&packageTypeFilter=Disclaimer&semVerLevel=2.0.0'
```

`tcpkg` asks every new source for its packages, and the page size it uses comes from `DefaultTake`
in `C:\ProgramData\Beckhoff\TcPkg\appsettings.json`, which ships as `500`. **GitHub Packages caps
the page size of its search endpoint at 100** and answers anything above it with `400 Bad Request`.
Measured against the live registry:

| `take` | Response |
| --- | --- |
| 20, 50, 100 | `200 OK` |
| 101 and above | `400 Bad Request` |

The Beckhoff feeds accept 500, which is why the default goes unnoticed until a GitHub source is
added. `--take` is stored per source, so this only affects the GitHub entry.

Two things this error is *not*, despite what it looks like: it is not the `packageTypeFilter`
parameter, which GitHub accepts and ignores, and it is not an authentication problem. A missing
`read:packages` scope produces `401`/`403` instead, with an explicit message about the token.

### Installing

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
