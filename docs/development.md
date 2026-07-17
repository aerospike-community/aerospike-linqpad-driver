# Building and packaging

## Requirements

- .NET 8 SDK
- Windows build environment with WPF targeting support
- The strong-name key expected by the project configuration

The project targets `net8.0-windows` and enables WPF. The NuGet package deliberately places the complete driver runtime under `lib/net8.0` so LINQPad can load the same package on Windows and, through LINQPad XPF, macOS.

## Build

From the repository root:

```powershell
dotnet restore .\Aerospike.Database.LINQPadDriver.sln
dotnet build .\Aerospike.Database.LINQPadDriver.sln -c Release
```

## Pack

```powershell
dotnet pack .\Aerospike.Database.LINQPadDriver.csproj -c Release
```

The package is written under:

```text
bin/Release/
```

Do not infer the package location from the target-framework output directory; `PackageOutputPath` is explicitly configured.

## Package contents

The package is self-contained for LINQPad and includes:

- The driver assembly and runtime dependencies.
- Connection images and package icon.
- Demo and AI `.linq` samples.
- Selected sample data.
- Embedded and packaged AI-context Markdown.
- The package README and license.

The project suppresses the normal NuGet dependency graph because runtime dependencies are copied into the driver package by the custom pack target.

## Documentation changes

When adding or moving documentation:

- Keep the root `README.md`; it is the NuGet package readme.
- Put user-facing guides in `docs/`.
- Put runnable examples and sample-specific READMEs in `linqpad-samples/`.
- Keep runtime AI prompt resources in `AIContext/`.
- Update relative links and the documentation index.
- Add sample READMEs to the project package when users need them after NuGet installation.
- Run the documentation validation script described below.

## Suggested validation

```powershell
dotnet build .\Aerospike.Database.LINQPadDriver.sln -c Release
dotnet pack .\Aerospike.Database.LINQPadDriver.csproj -c Release
```

Then inspect the `.nupkg` as a ZIP and verify:

- `README.md` and `LICENSE` are at the package root.
- The runtime is under `lib/net8.0`.
- AI-context resources and sample files are present.
- No build-only LINQPad reference assembly is bundled.
- Relative links in repository Markdown resolve.

[Back to the documentation index](README.md)
