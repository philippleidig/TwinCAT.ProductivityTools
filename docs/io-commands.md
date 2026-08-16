# I/O commands

Two commands work on the I/O tree of a TwinCAT project.

---

## Enable ADS Server

Publishes the process image of an EtherCAT master through an ADS server, so that the image can be
read by name from outside the PLC.

**Where** — context menu of an EtherCAT master ("Device n (EtherCAT)") below **I/O ▸ Devices**.

### Why

Without an ADS server the process image of a master is only reachable through the PLC that maps
it. Enabling the server gives every image a port of its own and creates the symbol information,
so a test bench, a HMI or an ADS script can read the raw image directly — no PLC variable, no
mapping, no rebuild of the PLC project.

Doing the same by hand means opening every process image below the master, switching to the ADS
tab, enabling the server, picking a port that is not in use yet and ticking "Create symbols".

### How to use it

1. Right-click the EtherCAT master in the Solution Explorer.
2. Choose **Enable ADS Server**.
3. The status bar reports for how many process images the server was enabled.

For every process image below the master the command enables the ADS server, switches symbol
creation on and assigns a port. The port is derived from the tree item id of the image starting at
**27904**, which is the range TwinCAT reserves for image servers, so two images never collide.

### Notes and limitations

* The change is a single undo step named "Enable ADS server".
* The command is only visible when the selected item really is an EtherCAT master.
* A master without a process image is not an error; the status bar says that none was found.
* The ports become active with the next activation of the configuration.

---

## Show IO Mappings

Opens the **IO Mappings** tool window, which lists the complete mapping of the active TwinCAT
project as a searchable tree.

**Where** — context menu of an I/O mapping in the Solution Explorer, and from the tool window
list once it has been opened.

### Why

TwinCAT shows a mapping one dialog at a time. Answering "where does this input end up" or "which
variables are not mapped at all" means clicking through the tree. The tool window reads the whole
mapping at once and lets you search it.

### How to use it

1. Right-click a mapping below **I/O ▸ Mappings** and choose **Show IO Mappings**.
2. Type into the search box to filter the tree. The filter matches anywhere in the name.
3. **Reload** re-reads the mapping from the project after you changed it.
4. **Copy** puts the selected entry on the clipboard.
5. **Export** writes the whole mapping to a CSV file, which is the format a review, a test
   specification or a spreadsheet can use.

### Notes and limitations

* The window shows the mapping of the **active** TwinCAT project. It is empty while no TwinCAT
  project is loaded.
* The tree is a snapshot. Changing a mapping in the project does not update the window — press
  **Reload**.
