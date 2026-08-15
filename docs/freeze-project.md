# Freeze project

Pins a TwinCAT solution to the toolchain it was engineered with, so that opening it on a machine
with a newer TwinCAT installation does not silently migrate it.

**Where** — context menu of the TwinCAT XAE project in the Solution Explorer.

## Why

A TwinCAT solution normally adapts itself to the engineering station that opens it. A newer
TwinCAT build updates the project format, the PLC compiler version follows the installed
PLC engineering, and the visualization and UML profiles are updated on load. That is convenient
while a project is being written and a problem as soon as it is delivered: the binary that a
colleague builds is not the binary that was tested, and the diff of the commit that follows is
full of changes nobody made on purpose.

Freezing writes the current versions into the project files. From then on TwinCAT keeps them.

## What it changes

| File | Change |
| --- | --- |
| `*.tsproj` | `TcVersionFixed="true"` on the `TcSmProject` root — the solution project keeps its TwinCAT version |
| `*.plcproj` | The compiler version is written as a fixed value instead of being resolved on load |
| `*.plcproj` | `SecureOnlineMode`, `AutoUpdateVisuProfile` and `AutoUpdateUmlProfile` are removed, so the visualization and UML profiles are no longer updated automatically |

Every PLC project of the solution is frozen, not just the active one.

## How to use it

1. Build the solution once and make sure it is the state you want to preserve.
2. Right-click the TwinCAT XAE project in the Solution Explorer.
3. Choose **Freeze Project**.
4. The status bar reports `The TwinCAT project was frozen.` and the solution is saved.
5. Commit the changed `.tsproj` and `.plcproj` files.

## Notes and limitations

* The operation is **idempotent**. Freezing an already frozen project changes nothing and reports
  the same message, so it is safe to run it from a script or after every release.
* Freezing does not pin library *versions*. Use the library repository and the placeholder
  resolution of TwinCAT for that; a placeholder that resolves to "newest" still resolves to
  newest after freezing.
* To undo it, delete the `TcVersionFixed` attribute from the `.tsproj` again. There is no
  "unfreeze" command, because reverting the commit is the operation you actually want.
* The command is only visible while the solution has an active TwinCAT XAE project.
