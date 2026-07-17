# AI context internals

The `AIContext/` folder contains Markdown that is compiled into the driver as embedded resources. `AerospikeAIContext` combines selected resources with live connection metadata to construct the prompt sent to LINQPad AI.

## Runtime files

| File | Role |
|---|---|
| `Header.md` | Opens the generated context and driver reference |
| `SystemInstruction.QuerySyntax.md` | System-level rules for query-syntax generation |
| `SystemInstruction.MethodSyntax.md` | System-level rules for method-syntax generation |
| `DriverGuide.QuerySyntax.md` | Driver usage rules and query-syntax preferences |
| `DriverGuide.MethodSyntax.md` | Driver usage rules and method-syntax preferences |
| `Examples.QuerySyntax.md` | Driver-mode query-syntax examples |
| `Examples.MethodSyntax.md` | Driver-mode method-syntax examples |
| `Examples.NativeClient.md` | Native Aerospike client examples |
| `Examples.DataOperations.md` | Mutation, import, export, and safety examples |
| `Examples.General.md` | Context inspection and submission examples |
| `AValues_Readme.md` | Detailed `AValue` behavior supplied to the model when requested |
| `Footer.md` | Final generation and validation guidance |

`AerospikeAIContext.cs` refers to these filenames directly. Rename a file only when the corresponding loader reference is changed and all context profiles are tested.

## Design documents

- `aerospike_linqpad_ai_context_phase_ii_plan.md` describes profile-driven context generation, ordering, truncation diagnostics, and validation strategy.
- `aerospike_linqpad_ai_context_copilot_handoff.md` is a detailed implementation handoff, source-of-truth guide, and code-generation checklist.
- `AIContextVersion.cs` tracks the context version exposed by the implementation.

## Context assembly order

The exact order depends on `AerospikeAIContextProfile` and options, but the full context generally prioritizes:

1. System instruction.
2. Header and driver rules.
3. Cluster and schema metadata.
4. Focused namespace/set metadata.
5. Examples.
6. Footer validation rules.
7. User request.

When the context exceeds `MaxChars`, the implementation should preserve rules and schema before optional examples when `PreferSchemaOverExamples` is enabled.

## Editing rules

- Keep driver-mode and native-mode examples clearly separated.
- Use generated properties in driver mode and raw bin names in expression/native mode.
- Keep query-syntax and method-syntax files semantically equivalent except for syntax preference.
- Update examples when APIs or null-handling rules change.
- Avoid stale set names or schemas that conflict with live metadata.
- Keep destructive examples explicit and preview-oriented.
- Validate the complete context, not only an individual Markdown file.

## Validation checklist

1. Build all context profiles.
2. Build both syntax preferences.
3. Build a full connection context and a set-focused context.
4. Verify all expected resource names load.
5. Check `WasTruncated`, warnings, included sections, and final length.
6. Run representative prompts for driver mode, native mode, expressions, joins, `AValue`, and data mutations.
7. Compile generated C# in LINQPad.
8. Confirm examples do not contradict the system instructions or driver guide.

## Packaging note

The project file embeds and packs `AIContext/*.md`. Adding a Markdown file to this folder changes the package even when the runtime loader does not use that file. Put general user documentation under `docs/` instead.

[Back to the documentation index](README.md)
