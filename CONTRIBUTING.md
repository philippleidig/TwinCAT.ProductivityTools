# Contributing

## Getting the sources to build

You need Visual Studio 2022 or 2026 with the **Visual Studio extension development** workload, and
the .NET Framework 4.8 targeting pack. TwinCAT itself is **not** required to build — the solution
compiles against the interop assemblies that come with the repository.

```powershell
dotnet tool restore
msbuild TwinCAT.ProductivityTools.sln -restore -p:Configuration=Release -p:DeployExtension=false
```

## Layout

```
src/
  TwinCAT.ProductivityTools.Core/     class library, net48, no Visual Studio dependency
  TwinCAT.ProductivityTools.Shared/   shared project, all of the IDE integration
  TwinCAT.ProductivityTools.15/       VSIX for the 32 bit shells
  TwinCAT.ProductivityTools.17/       VSIX for the 64 bit shells
  SharedFiles/                        Commands.vsct and the generated Commands.cs
tests/
  TwinCAT.ProductivityTools.Core.Tests/          unit tests
  TwinCAT.ProductivityTools.Integration.Tests/   against a mocked Visual Studio shell
  TwinCAT.ProductivityTools.E2E.Tests/           against a real IDE, opt in
tcpkg/                                TwinCAT Package Manager packages
templates/                            the PLC project template
build/                                version stamping and packaging scripts
docs/                                 user documentation
```

`Core` holds everything that can be tested without an IDE: text rewriting, XML transformation,
route parsing, path resolution. It must not reference `Microsoft.VisualStudio.*`, `EnvDTE` or
`TCatSysManagerLib` — an architecture test in `Core.Tests` fails when it does. Put new logic there
and let the command in `Shared` do nothing but wire it up.

Both VSIX projects include the same shared project. A new file in
`TwinCAT.ProductivityTools.Shared` has to be listed in `TwinCAT.ProductivityTools.Shared.projitems`
or it is silently left out of the build.

## Commit messages

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/) and are
enforced by a Husky hook. They are also the **only** version input:

| Prefix | Effect on the version |
| --- | --- |
| `feat!:`, or a body with `BREAKING CHANGE:` | major |
| `feat:` | minor |
| `fix:`, `perf:`, `refactor:`, `docs:`, `test:`, `build:`, `ci:`, `chore:`, `style:`, `revert:` | patch |
| `none:`, `skip:` | no bump |

Write the subject in the imperative and describe the effect, not the mechanics:
`fix: keep the package alive outside TwinCAT solutions`, not `fix: change SetSite`.

## Formatting

The repository is formatted with [CSharpier](https://csharpier.com/). CI fails on unformatted code.

```powershell
dotnet csharpier .
```

A pre-commit hook runs it on the staged files.

## Tests

See [TESTING.md](TESTING.md). Unit and integration tests run in CI on every push and pull request;
the end to end tests need a machine with TwinCAT and are opt in.

## Releasing

There is nothing to do. Every push to `main` runs
[`release.yml`](.github/workflows/release.yml), which

1. derives the version with GitVersion from the commits since the last tag,
2. stamps it into both VSIX manifests and every `.nuspec`,
3. builds, tests and packs,
4. produces the Inno Setup installer and the TcPkg packages,
5. pushes the tag `v<version>`,
6. creates the GitHub release with the installer, both `.vsix` files and all `.nupkg` files, with
   release notes generated from the commit messages,
7. publishes the packages to GitHub Packages.

Run the workflow manually with **Run workflow** and `dry-run` enabled to build everything without
tagging or publishing.

Never edit a version number by hand. `Vsix.Version` and the `.nuspec` versions in the repository
are placeholders that the pipeline overwrites.
