# AI Context Runtime Version Patch

Version: `2026.06.08.3`

## Baseline

Based only on the newest uploaded AI Context MD and CS files in this conversation.

## Files changed

- `AIContextVersion.cs` — new public static version source file.
- `LINQPadAIGeneratedQuery.cs` — minimal wiring to display the version and stamp generated scripts.
- AI Context `.md` files — version header comments updated/added to `2026.06.08.3`.

## Runtime behavior added

- Before submitting a generated-query AI request, LINQPad output now shows:

```csharp
new { Version = AIContextVersion.Current }.Dump("AI Context");
```

- Generated `.linq` scripts now include this line in the leading summary/comment block:

```csharp
// - AI Context Version: 2026.06.08.3
```

- If a generated script does not already start with a comment block, a minimal request-summary comment block is inserted.

## Intentionally not changed

- No AI-context rules were refactored.
- No examples were rewritten.
- No native-vs-driver mode logic was changed.
- No connection-copy behavior was changed.
- No AValue/CDT behavior was changed.
- No `AerospikeAIContext.cs`, `AerospikeAIContextExtensions.cs`, or `AerospikeAIContextOptions.cs` behavior was changed.

## Version update rule

Increment `AIContextVersion.Current` whenever AI-context rules, examples, final validation guidance, AValue README guidance, or generated-script AI behavior changes.

Use `YYYY.MM.DD.N`:

- Same day: increment `N`.
- New day: reset `N` to `1`.

The C# constant is the runtime source of truth. Per-file MD headers are traceability metadata.
