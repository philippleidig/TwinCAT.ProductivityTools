# XAE project commands

Three commands sit in the context menu of the TwinCAT XAE project, next to
[Freeze Project](freeze-project.md).

---

## Use relative NetIds

Switches the project to relative AmsNetIDs.

**Where** — context menu of the TwinCAT XAE project.

### Why

By default a TwinCAT project stores the AmsNetID of the engineering station in every route and in
every ADS symbol reference. That AmsNetID belongs to one machine. As soon as the project is
cloned, built on a build server or handed to a colleague, the stored identifier is wrong, and the
first thing the new station does is change it — which shows up as noise in every diff.

With relative AmsNetIDs the project refers to "the target of this project" instead of naming a
concrete machine.

### How to use it

1. Right-click the TwinCAT XAE project.
2. Choose **Use relative NetIds**.
3. The status bar reports `Relative AmsNetIDs are now enabled.` and the solution is saved.

The extension also offers this from an info bar at the top of the editor whenever a project
without relative AmsNetIDs is opened. The **Use relative NetIds** button on the info bar does
exactly the same thing as the menu entry.

### Notes and limitations

* The command disappears once the setting is on — there is nothing left for it to do. That is the
  quickest way to check the current state.
* The setting is a property of the TwinCAT project, not of the extension, so it stays with the
  project and does not have to be repeated per machine.

---

## TwinCAT Logged Events

Opens the **TwinCAT Logged Events** window.

**Where** — context menu of the TwinCAT XAE project.

### Why

The event window belongs to TwinCAT XAE Base and is otherwise buried in the TwinCAT menu, several
levels deep. Since it is the first place to look when a target misbehaves, it is offered where the
project is.

### Notes and limitations

* The window is part of TwinCAT XAE Base, not of this extension. In a Visual Studio without the
  TwinCAT integration the command reports that the window is not available instead of failing
  silently.
* The command is only visible while a TwinCAT project is loaded.

---

## Delete build artifacts on clean

A toggle that makes **Build ▸ Clean** actually delete the build output of a TwinCAT project.

**Where** — context menu of the TwinCAT XAE project. The entry carries a check mark while the
option is on.

### Why

Cleaning a TwinCAT solution leaves the `_Boot` folder and the compiler output on disk. A stale
boot folder is deployed to the target on the next activation, and a stale compiler output makes
the next build reuse code that no longer matches the sources. Both failure modes are hard to see
and easy to avoid.

### How to use it

1. Right-click the TwinCAT XAE project.
2. Choose **Delete build artifacts on clean**. The check mark shows the new state.
3. Run **Build ▸ Clean Solution**. The boot folder and the compiler output are removed.

The same switch is available under
**Tools ▸ Options ▸ TwinCAT 3.1 ProductivityTools ▸ Build**, see [Options](options.md).

### Notes and limitations

* The option is **off** by default. Deleting files from disk is destructive enough that it has to
  be opted into.
* The setting is per user, not per solution — it is a Visual Studio option, so it applies to every
  TwinCAT solution you open.
* Only the output of TwinCAT projects is removed. A .NET or C++ project in the same solution is
  cleaned by its own build system as usual.
