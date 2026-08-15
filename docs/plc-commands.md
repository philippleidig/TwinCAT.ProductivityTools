# PLC commands

Four commands are added to the context menus inside a PLC project.

| Command | Available on |
| --- | --- |
| Remove all comments | a PLC object, a folder, the PLC project |
| Remove all regions | a PLC object, a folder, the PLC project |
| Open in Visual Studio Code | a folder of a PLC project |
| Open in File Explorer | a folder of a PLC project |

---

## Remove all comments

Strips every comment from the Structured Text of the selection.

**Where** — context menu of a PLC object (POU, GVL, DUT, interface …), of a folder, or of the PLC
project itself.

### Why

Generated code, imported third party libraries and code that is about to be shipped as a
compiled library often carry comments that should not travel with it. Doing that by hand across a
project is tedious and error prone; doing it with a text editor breaks as soon as a comment
marker appears inside a string literal.

### How to use it

1. Select an object, a folder or the PLC project in the Solution Explorer.
2. Right-click and choose **Remove all comments**.
3. The status bar reports how many objects were rewritten.

Everything **below** the selection is rewritten, which includes the methods, actions, properties
and transitions of a POU — they are child items of the object, not part of its own text.

### What is removed

* `// line comments`, up to the end of the line.
* `(* block comments *)`, including **nested** block comments.

### What is kept

* Comment markers inside string literals. `sText := 'http://beckhoff.com';` survives unchanged,
  including the `$` escape sequences of Structured Text.
* The line structure. Removing a comment does not join two lines, and the line endings of the
  document are preserved as they were.

### Notes and limitations

* The whole operation is a single undo step: one **Ctrl+Z** puts every object back.
* The command only appears on items that actually carry Structured Text.
* Attributes such as `{attribute 'qualified_only'}` are pragmas, not comments, and are kept.

---

## Remove all regions

Strips the `{region}` and `{endregion}` folding markers from the selection.

**Where** — the same places as **Remove all comments**.

### Why

Regions are a reading aid in the editor. In generated code and in code that is reviewed as text
they are noise, and a mismatched pair of markers makes the editor fold the wrong block.

### How to use it

Same as above: select an object, a folder or the PLC project, right-click, choose
**Remove all regions**.

### Notes and limitations

* Only the markers are removed. The code inside a region is kept exactly where it was.
* Nested regions are handled — every marker is removed, at any depth.
* One undo step for the whole operation.

---

## Open in Visual Studio Code

Opens the directory of the selected PLC folder in Visual Studio Code.

**Where** — context menu of a folder inside a PLC project.

### Why

A PLC project is a directory of XML files on disk. Searching across them, comparing two of them or
fixing something in a file that TwinCAT refuses to open is far easier in a text editor than in the
Solution Explorer.

### How to use it

1. Right-click a folder of the PLC project.
2. Choose **Open in Visual Studio Code**.

### Notes and limitations

* The path to `Code.exe` is empty by default and the command detects the installation itself: it
  reads the registry key of the "Open with Code" shell integration, then searches `PATH`, then the
  per user installation directory. What it found is stored under
  **Tools ▸ Options ▸ TwinCAT 3.1 ProductivityTools ▸ General**, see [Options](options.md).
* If Visual Studio Code cannot be found, the command offers a file dialog once and remembers the
  answer.
* Changing a file behind the back of TwinCAT means the editor has to reload it. Close the object
  in TwinCAT before editing it elsewhere.

---

## Open in File Explorer

Opens the directory of the selected PLC folder in the Windows file explorer.

**Where** — context menu of a folder inside a PLC project.

### Notes and limitations

* The command opens the folder that belongs to the selected tree item, not the project root.
* It is only visible on a folder of a PLC project, because that is the only tree item with a
  directory of its own.
