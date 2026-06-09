# Phase I Low-Risk Subset Change Manifest

Baseline used: newest uploaded AI Context MD files in `/mnt/data` as of this turn. No older generated bundles were used.

Version header applied to changed MD files:

```text
<!-- AIContext-Version: 2026.06.08.2; Change: Phase I low-risk cleanup: context-bound naming, example-mode labels, and footer validations. -->
```

## Files changed

- Header.md
- Footer.md
- Examples.MethodSyntax.md
- Examples.QuerySyntax.md
- Examples.NativeClient.md
- Examples.DataOperations.md

## Changes made

1. Added `Context-Bound Naming Rule` to `Header.md`.
2. Added/kept example naming notes in example files.
3. Added example-mode notes in example files.
4. Mode-labeled example headings as LINQPad-driver mode, native Aerospike API mode, LINQPad-driver server-side expression mode, shared/diagnostic, or data operation/mutation where applicable.
5. Added footer validations for context-bound naming, example-mode boundaries, and native dictionary lookup boundaries.

## Intentionally not changed

- No C# files were modified.
- No SystemInstruction files were modified.
- No DriverGuide files were modified.
- No AValues_Readme.md changes were made.
- No example code bodies were intentionally changed.
- No AValue/CDT lookup behavior was changed.
- No native expression examples were changed.
- No LINQ syntax preference rules were changed.

## Risk level

Low-to-medium. Most edits are metadata, notes, and headings. The only behavior-adjacent addition is footer validation that prevents mode leakage, especially native-mode dictionary lookup leakage.
