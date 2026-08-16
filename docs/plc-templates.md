# PLC project template

The extension installs an additional PLC project template called
**Standard PLC Project Optimized Defaults**.

**Where** — right-click the TwinCAT XAE project ▸ **Add ▸ New Item ▸ Plc Templates**.

## Why

Every new PLC project starts with the same round of housekeeping: turn off the settings that only
exist for backward compatibility, sort the objects by name so that a diff is readable, and add the
folders the project is going to need anyway. The template is that round, done once.

## What is different from "Standard PLC Project"

| Setting | Beckhoff default | This template | Reason |
| --- | --- | --- | --- |
| `SubObjectsSortedByName` | off | **on** | The order of the objects in the `.plcproj` follows their name instead of the order they were created in. Without it every commit reorders the file. |
| `GenerateTpy` | on | **off** | The `.tpy` file is only needed by the legacy ADS symbol interface. Generating it slows the build down and adds a binary to the repository. |
| `WriteProductVersion` | on | **off** | Keeps the product version out of the compiled application, so two builds of the same sources are identical. |
| `CombineIds` | off | **on** | Keeps the object identifiers stable across a rename. |
| Folders | none | `POUs`, `DUTs`, `GVLs`, `VISUs` | The structure almost every project ends up with. |

`MAIN` and a `PlcTask` task configuration are part of the template, with `Tc2_Standard`,
`Tc2_System` and the other standard placeholder references already resolved.

## Where it is installed

TwinCAT changed the template layout between the builds, so the template ships with two descriptors
and the installer picks the matching one.

| TwinCAT | Location |
| --- | --- |
| 3.1 build 4024 (`PlcTemplates` 1.0.0.0) | `C:\TwinCAT\3.1\Components\Plc\PlcTemplates\1.0.0.0\Plc Templates\TwinCAT.ProductivityTools.Templates\` |
| 3.1 build 4026 (`PlcTemplates` 1.1.0.x) | `C:\ProgramData\Beckhoff\TwinCAT\PlcEngineering\PlcTemplates\<version>\`, with the descriptor in the shared `TemplatesDir` folder |

Under 4024 all templates live in one flat folder described by a single `.vsdir`. The extension
installs into a sub folder of its own so that it never touches the Beckhoff files. Under 4026
every template has a folder and its own `.vsdir` in `TemplatesDir`, which is why the descriptor
refers to the payload with a relative `..\` path.

When several `PlcTemplates` versions are installed, the highest one wins — that is the one the PLC
engineering uses.

## Notes and limitations

* The template is deployed by the installer and by the `TwinCAT.ProductivityTools.Config` TcPkg
  package. Installing only the VSIX by hand does not deploy it.
* A TwinCAT installation that is upgraded to a newer `PlcTemplates` version does not carry the
  template over. Repair the installation or reinstall the package afterwards.
