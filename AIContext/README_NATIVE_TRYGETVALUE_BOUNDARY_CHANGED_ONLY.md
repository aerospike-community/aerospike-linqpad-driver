# Native TryGetValue Boundary Patch - Changed Files Only

Version: 2026.06.08.4

## Bundle policy

This ZIP follows the updated packaging rule:

- Include only files changed by this patch.
- Separate AIContext-owned files from main-folder / non-AIContext files.
- This patch has no changed main-folder / non-AIContext files, so only an AIContext bundle is produced.

## Baseline

Based on the newest uploaded AI Context MD/CS baseline, plus the already-approved runtime version mechanism. This package is a repackaging of the native TryGetValue boundary patch, not a new behavior change.

## Changed files in this AIContext bundle

- AIContextVersion.cs
- SystemInstruction.MethodSyntax.md
- SystemInstruction.QuerySyntax.md
- DriverGuide.MethodSyntax.md
- DriverGuide.QuerySyntax.md
- Footer.md

## Change summary

- Bumped AIContextVersion.Current to 2026.06.08.4.
- Added / tightened native-mode dictionary lookup boundary rules.
- Clarified that LINQPad-driver default-value TryGetValue helper patterns do not apply in native Aerospike C# API mode.
- Added footer validation rejecting native-mode code such as dict.TryGetValue(key, null), dict.TryGetValue(key, default(...)), and source.TryGetValue("KeyName").

## Non-AIContext bundle

No non-AIContext files changed in this patch, so no non-AIContext ZIP was generated.

## Intentionally not included

- Unchanged example files
- Unchanged Header.md
- Unchanged AValues_Readme.md
- Unchanged main-folder CS files such as LINQPadAIGeneratedQuery.cs, AerospikeAIContext.cs, AerospikeAIContextExtensions.cs, and AerospikeAIContextOptions.cs
