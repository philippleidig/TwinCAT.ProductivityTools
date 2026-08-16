# Testing

Three suites cover the extension, in increasing order of what they need from the machine.

| Suite | Needs | Runs in CI | Count |
| --- | --- | :---: | ---: |
| `TwinCAT.ProductivityTools.Core.Tests` | nothing | yes | 254 |
| `TwinCAT.ProductivityTools.Integration.Tests` | nothing | yes | 55 |
| `TwinCAT.ProductivityTools.E2E.Tests` | an installed IDE, TwinCAT for part of it | no, opt in | 6 per IDE |

```powershell
dotnet test tests\TwinCAT.ProductivityTools.Core.Tests\TwinCAT.ProductivityTools.Core.Tests.csproj
dotnet test tests\TwinCAT.ProductivityTools.Integration.Tests\TwinCAT.ProductivityTools.Integration.Tests.csproj
```

The stack is xUnit, FluentAssertions and NSubstitute throughout.

## Unit tests — `Core.Tests`

Cover everything in `TwinCAT.ProductivityTools.Core`: the Structured Text comment and region
removers, the freeze transformations on `.tsproj` and `.plcproj` documents, the route reader and
AmsNetID parser, the gitignore style file filter, the device info parser, the binary event message
parser, line ending handling and the target path resolution.

One of them is an **architecture test**: it fails as soon as `Core.dll` gains a reference to
`Microsoft.VisualStudio.*`, `EnvDTE` or `TCatSysManagerLib`. Keeping the core free of them is what
makes it testable at all, so the rule is enforced instead of documented.

`Core` is signed and compiled with `LangVersion 7.3`. A member that a test needs has to be
`public`; `InternalsVisibleTo` is not an option for a signed assembly without a key for the test
project.

## Integration tests — `Integration.Tests`

Run against the mocked Visual Studio shell of `Microsoft.VisualStudio.Sdk.TestFramework`, so they
need no IDE and run headless in CI. They cover the parts that only exist inside the shell:

* the package: registered services, option pages, tool window and auto load rules,
* the command table: every command id in `Commands.vsct` has exactly one handler, and every
  handler has an id — a mismatch is the classic reason for a command that does nothing,
* the option models: display name, description and category on every setting, defaults, and that
  every setting has a type the settings store can round trip,
* the view models of the dialogs and of the I/O mapping window.

The SDK test framework is pinned to `17.11.66`, which in turn pins `xunit` to `2.9.3`.

## End to end tests — `E2E.Tests`

Start a real IDE, drive it through `EnvDTE` and the TwinCAT automation interface, and assert on
what ends up on disk. They are excluded from CI and from a plain `dotnet test` run over the
solution by the trait `Category=E2E`.

```powershell
$env:TCPT_E2E_IDE = 'VS2022'
dotnet test tests\TwinCAT.ProductivityTools.E2E.Tests\TwinCAT.ProductivityTools.E2E.Tests.csproj `
    --filter "Category=E2E"
```

### Environment variables

| Variable | Meaning |
| --- | --- |
| `TCPT_E2E_IDE` | Which IDE to drive: `VS2022`, `VS2026`, `TcXaeShell`, `TcXaeShell64`. Defaults to `VS2022`. |
| `TCPT_E2E_PLC` | Set to `1` to also run the tests that create a PLC project. Off by default. |
| `TCPT_E2E_VISIBLE` | Set to `1` to watch the IDE instead of running it hidden. Useful while debugging a test. |
| `TCPT_E2E_WORKDIR` | Directory for the generated solutions. Defaults to a folder under the temporary directory. |

### The extension has to be installed first

The tests assert against the IDE as the user has it, so the extension must be deployed into the
instance under test **before** the run. Copying the folder into `Extensions` is not enough —
Visual Studio only merges the command table for an extension that went through the installer:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\VSIXInstaller.exe" `
    /quiet /instanceIds:<instanceId> src\TwinCAT.ProductivityTools.17\bin\Release\TwinCAT.ProductivityTools.17.vsix
```

Get the instance identifiers with
`vswhere -all -prerelease -products * -format json`.

> **Uninstall first when the version did not change.** `VSIXInstaller.exe` compares the version in
> `source.extension.vsixmanifest` against the installed one and **exits with code 0 without doing
> anything** when they are equal. A rebuilt VSIX that still carries the same version is therefore
> silently ignored, and the tests then assert against the previously installed build. Force the
> replacement:
>
> ```powershell
> $vsixInstaller = "C:\Program Files\Microsoft Visual Studio\2022\Professional\Common7\IDE\VSIXInstaller.exe"
> & $vsixInstaller /quiet /uninstall:1e6f317c-4b46-4f08-96dc-4ab7dc8a1032 /instanceIds:<instanceId>
> & $vsixInstaller /quiet /instanceIds:<instanceId> src\TwinCAT.ProductivityTools.17\bin\Release\TwinCAT.ProductivityTools.17.vsix
> ```
>
> Verify that the deployment really is the current one — the timestamp of the deployed assembly has
> to match the build output:
>
> ```powershell
> Get-ChildItem "$env:LOCALAPPDATA\Microsoft\VisualStudio\17.0_<instanceId>\Extensions" `
>     -Recurse -Filter TwinCAT.ProductivityTools.17.dll | Select-Object FullName, LastWriteTime
> ```

A test whose prerequisite is missing **skips** instead of failing: no such IDE installed, the
extension not deployed into it, or `TCPT_E2E_PLC` not set. A run against an environment that
cannot host the extension therefore reports six skipped tests, not six failures.

### Known environment limitations

* Creating a PLC project through the automation interface fails on some TwinCAT 4026
  installations — the created node contains no sources and the PLC control reports
  `Value cannot be null. Parameter name: stText`. That is a PLC engineering problem, not an
  extension defect, which is why those tests are behind `TCPT_E2E_PLC`.
* TcXaeShell 64 cannot host the extension on every machine; its own `VSIXInstaller.exe` can fail
  with a type initializer exception. The tests detect this and skip.

### In CI

[`e2e.yml`](.github/workflows/e2e.yml) runs the suite on a self hosted runner labelled
`windows, twincat`, started manually with the IDE as an input. A GitHub hosted runner has neither
an IDE with TwinCAT nor a desktop session.

## Coverage

```powershell
dotnet test tests\TwinCAT.ProductivityTools.Core.Tests\TwinCAT.ProductivityTools.Core.Tests.csproj `
    --collect:"Code Coverage;Format=Cobertura"
```

CI collects coverage for `Core` and `Integration` and writes a summary into the job summary.

Use the profiler based collector shown above rather than coverlet's `XPlat Code Coverage`.
Coverlet rewrites the IL of the assemblies it instruments, which invalidates the strong name of
`TwinCAT.ProductivityTools.Core` — it is signed with `Key.snk` because the VSIX assemblies are
signed and a signed assembly cannot reference an unsigned one. On .NET Framework the runtime
verifies that signature on load, so every test touching `Core` then fails with
`FileLoadException: Strong name signature could not be verified`.
