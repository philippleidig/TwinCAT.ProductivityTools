# PLC project templates

This folder contains the custom PLC project templates that ship with TwinCAT ProductivityTools.

## Layout

```
templates/
  Standard PLC Project Optimized Defaults/   # template payload (identical for all TwinCAT versions)
  vsdir/
    4024/                                    # .vsdir for the TwinCAT 3.1.4024 template layout
    4026/                                    # .vsdir for the TwinCAT 3.1.4026 template layout
```

The template payload is version independent. Only the `.vsdir` descriptor differs, because
TwinCAT changed the PLC template layout between 4024 and 4026.

## TwinCAT 3.1.4024 (`PlcTemplates` 1.0.0.0)

Root: `C:\TwinCAT\3.1\Components\Plc\PlcTemplates\1.0.0.0\Plc Templates\`

Templates live in a flat folder and a single `.vsdir` describes all of them. ProductivityTools
installs into a dedicated sub folder so it never touches the Beckhoff files:

```
...\Plc Templates\TwinCAT.ProductivityTools.Templates\
    TwinCAT.ProductivityTools.Templates.vsdir
    Standard PLC Project Optimized Defaults\...
```

The `.vsdir` therefore uses the relative path `Standard PLC Project Optimized Defaults\....plcproj`.

## TwinCAT 3.1.4026 (`PlcTemplates` 1.1.0.0 and newer)

Root: `C:\ProgramData\Beckhoff\TwinCAT\PlcEngineering\PlcTemplates\<version>\`

Every template gets its own folder and all descriptors are collected in a shared `TemplatesDir`
folder, one `.vsdir` per template:

```
...\<version>\
    Standard PLC Project Optimized Defaults\...
    TemplatesDir\Standard PLC Project Optimized Defaults.vsdir
```

Because the descriptor now lives one level deeper, the `.vsdir` uses the relative path
`..\Standard PLC Project Optimized Defaults\....plcproj`.

The installer and the TcPkg package detect the installed TwinCAT build and pick the matching
layout automatically.
