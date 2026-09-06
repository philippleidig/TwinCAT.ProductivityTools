# Target commands

Seven commands act on the **target system** of the active TwinCAT project — the machine the project
is configured to run on, not the engineering station.

**Where** — **Tools ▸ TwinCAT Productivity Tools**. Shutdown, Reboot, Connect to Target and
Device Manager are also on the **TwinCAT Productivity Tools** toolbar.

![The Tools menu with the submenu open](images/tools-menu.png)

## Shared behaviour

Every one of these commands resolves the target the same way:

1. The active TwinCAT XAE project is asked for its target AmsNetID.
2. The AmsNetID is parsed. An invalid one is reported instead of being passed on to ADS.
3. Destructive commands ask for confirmation, naming the target.
4. The result goes to the status bar; a failure goes to the output window with its exception.

All seven are hidden while the solution has no active TwinCAT project or the project has no valid
target AmsNetID. That is deliberate: a target command without a target is a command that can only
fail.

> The target is whatever the project points at. Check the target in the TwinCAT toolbar before
> using **Shutdown** or **Reboot** — "Local" means your own machine.

---

## Device Info

Shows hardware, image and TwinCAT version information of the target, plus the list of TwinCAT
functions that are installed on it.

| Tab | Contents |
| --- | --- |
| Device | Target type, hardware model, serial number, CPU architecture, hardware date, CPU version, TwinCAT version |
| OS | Image device, image version, image level, operating system name and version |
| Functions | The TwinCAT functions installed on the target, with their versions |

### Notes and limitations

* The values come from the target itself. A Beckhoff IPC or embedded PC fills all of them; a plain
  PC without Beckhoff hardware reports zeros for model, serial number and hardware date, as in the
  screenshot above. That is the target answering, not a failure of the command.
* Reading the information goes over ADS and needs a working route to the target.

---

## Reboot

Reboots the target system.

Asks `Reboot the target <name>?` first and reports `Reboot triggered on target <name>.` afterwards.

---

## Shutdown

Shuts the target system down.

Asks `Shut down the target <name>?` first and reports `Shutdown triggered on target <name>.`
afterwards.

> A shut down target cannot be woken up over ADS. Only use it on a machine you can reach
> physically or through a managed power supply.

---

## Connect to Target

Opens an interactive session on the target, choosing how by the operating system the target runs.

| Operating system of the target | Session |
| --- | --- |
| Windows (10 IoT, Embedded Standard, …) | Remote desktop, through `mstsc.exe` |
| TwinCAT/BSD, TwinCAT/Linux | SSH in a new console window, through the Windows OpenSSH client |
| Windows CE | Neither — CE serves no remote session. Use [Open Device Manager](#open-device-manager). |
| Could not be determined | The command asks which of the two to use |

The address is resolved from the route of the target, so the command works with the name the
project uses and does not need the address to be typed. When no address can be determined — an
unrouted target, or a route with neither an address nor a name — the command says so instead of
opening an empty session.

The SSH user name is a setting, see [Options](options.md#general). It starts out as
`Administrator`, the account every Beckhoff image ships, and the extension connects as
`<user>@<address>`.

### Notes and limitations

* The operating system is read from the target over ADS. An offline target cannot answer, which is
  why the command asks instead of guessing — guessing Windows would open a remote desktop session
  on a TwinCAT/BSD machine that has no server to answer it.
* SSH uses the OpenSSH client that comes with Windows. When it is missing, install it under
  **Settings ▸ Apps ▸ Optional features ▸ OpenSSH Client**.
* The address is never derived from the AmsNetID. An AmsNetID that TwinCAT generated itself looks
  like `5.24.13.37.1.1` and its first four octets come from a MAC address, not from an address of
  the target. Earlier versions used them anyway, which sent the session to a public address on the
  internet that belongs to somebody else. A target without a route now gets an error message.

---

## Open Device Manager

Opens the Beckhoff Device Manager of the target, `https://<address>/config`, in the default
browser.

The Device Manager is the web interface of a Beckhoff image. It configures what is not part of the
TwinCAT project — network, users, the write filter, the software watchdog — and on a Windows CE
target it is the only way to configure the machine remotely.

### Notes and limitations

* The address is resolved the same way as for [Connect to Target](#connect-to-target).
* The Device Manager uses a self signed certificate, so the browser warns before it shows the page.
  That warning is expected.
* Not every image ships the Device Manager. A plain PC without a Beckhoff image does not answer.

---

## Show Realtime Ethernet Compatible Devices

Lists the network adapters of the target that are compatible with the TwinCAT real time driver and
installs the driver on the ones you pick.

### Why

The same job is otherwise done with `TcRteInstall.exe`, which has to be started **on** the target.
On an embedded PC without a screen that means a remote desktop session first. This command talks
to the target over ADS instead.

### How to use it

1. Choose **Tools ▸ TwinCAT Productivity Tools ▸ Show Realtime Ethernet Compatible Devices**.
2. The dialog lists the adapters of the target and marks the ones that already have the driver.
3. Select an adapter and install or uninstall the driver.

### Notes and limitations

* Installing the driver on the adapter that carries the ADS route cuts the connection to the
  target. Use a second adapter for engineering.
* The change takes effect after a reboot of the target.

---

## Windows Set Tick

Runs `win8settick.bat` on the target, which sets the Windows timer resolution to 1 ms.

### Why

Without it the Windows timer runs at about 15.6 ms, which is a common cause of jitter on an IPC
that otherwise looks correctly configured. The script is part of TwinCAT, but it lives on the
target and normally has to be started there.

### Notes and limitations

* Asks for confirmation, naming the target.
* The script is in a different directory on TwinCAT 4024 and on 4026. The command reads the
  TwinCAT version of the target and picks the matching path; when the version cannot be read it
  falls back to the 4024 layout.
* The change takes effect after a reboot of the target.
