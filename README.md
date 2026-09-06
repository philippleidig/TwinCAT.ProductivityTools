# <img src="assets/images/twincat.png" width="50"> TwinCAT.ProductivityTools

[![CI](https://github.com/philippleidig/TwinCAT.ProductivityTools/actions/workflows/ci.yml/badge.svg)](https://github.com/philippleidig/TwinCAT.ProductivityTools/actions/workflows/ci.yml)
[![Release](https://github.com/philippleidig/TwinCAT.ProductivityTools/actions/workflows/release.yml/badge.svg)](https://github.com/philippleidig/TwinCAT.ProductivityTools/actions/workflows/release.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.MD)

A Visual Studio extension for TwinCAT 3.1 from Beckhoff. It adds the commands to TwinCAT XAE that
the engineering environment does not offer itself — project freezing, PLC housekeeping, I/O
mapping export and target system maintenance — right where you already are: in the context menu of
the item you selected.

![The Tools menu with the submenu open](docs/images/tools-menu.png)

## Compatibility

| TwinCAT | TcXaeShell (32 bit) | TcXaeShell 64 | Visual Studio 2017/2019 | Visual Studio 2022 | Visual Studio 2026 |
| --- | :---: | :---: | :---: | :---: | :---: |
| 3.1 build 4024 | yes | – | yes | yes | – |
| 3.1 build 4026 | – | yes | – | yes | yes |

## Installation

```powershell
tcpkg install TwinCAT.ProductivityTools
```

The packages live on GitHub Packages, so that feed has to be registered once before the command
above resolves — see [docs/installation.md](docs/installation.md#registering-the-feed).

…or download the installer from the
[latest release](https://github.com/philippleidig/TwinCAT.ProductivityTools/releases/latest) and
pick the environments to install into. Both routes are described in
[docs/installation.md](docs/installation.md).

## Features

| Feature | Where | Documentation |
| --- | --- | --- |
| **Freeze Project** | XAE project context menu | [freeze-project.md](docs/freeze-project.md) |
| **Use relative NetIds** | XAE project context menu | [project-commands.md](docs/project-commands.md#use-relative-netids) |
| **TwinCAT Logged Events** | XAE project context menu | [project-commands.md](docs/project-commands.md#twincat-logged-events) |
| **Delete build artifacts on clean** | XAE project context menu | [project-commands.md](docs/project-commands.md#delete-build-artifacts-on-clean) |
| **Remove all comments** | PLC object, folder or project | [plc-commands.md](docs/plc-commands.md#remove-all-comments) |
| **Remove all regions** | PLC object, folder or project | [plc-commands.md](docs/plc-commands.md#remove-all-regions) |
| **Open in Visual Studio Code** | PLC folder context menu | [plc-commands.md](docs/plc-commands.md#open-in-visual-studio-code) |
| **Open in File Explorer** | PLC folder context menu | [plc-commands.md](docs/plc-commands.md#open-in-file-explorer) |
| **Enable ADS Server** | EtherCAT master context menu | [io-commands.md](docs/io-commands.md#enable-ads-server) |
| **Show IO Mappings** | I/O mapping context menu | [io-commands.md](docs/io-commands.md#show-io-mappings) |
| **Device Info** | Tools ▸ TwinCAT Productivity Tools | [target-commands.md](docs/target-commands.md#device-info) |
| **Reboot** / **Shutdown** | Tools ▸ TwinCAT Productivity Tools | [target-commands.md](docs/target-commands.md#reboot) |
| **Connect to Target** | Tools ▸ TwinCAT Productivity Tools | [target-commands.md](docs/target-commands.md#connect-to-target) |
| **Open Device Manager** | Tools ▸ TwinCAT Productivity Tools | [target-commands.md](docs/target-commands.md#open-device-manager) |
| **Show Realtime Ethernet Compatible Devices** | Tools ▸ TwinCAT Productivity Tools | [target-commands.md](docs/target-commands.md#show-realtime-ethernet-compatible-devices) |
| **Windows Set Tick** | Tools ▸ TwinCAT Productivity Tools | [target-commands.md](docs/target-commands.md#windows-set-tick) |
| **Standard PLC Project Optimized Defaults** | Add ▸ New Item ▸ Plc Templates | [plc-templates.md](docs/plc-templates.md) |
| **Options** | Tools ▸ Options | [options.md](docs/options.md) |

The full documentation index is in [docs/README.md](docs/README.md).

## Contributing

Always excited to hear your ideas and improve this project together. If you have a feature in mind
that you would like to see implemented, please open a GitHub issue at any time. To make things
smoother, describe the feature and how it would enhance the project, and include examples, mockups
or references if you have them.

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/) and are the
only version input — a push to `main` builds, tags and publishes a release on its own. See
[CONTRIBUTING.md](CONTRIBUTING.md) for the workflow and [TESTING.md](TESTING.md) for the test
suites.

## License

[MIT](LICENSE.MD)
