# Aerospike LINQPad Driver AI Context — Phase II Architecture and Runtime Enhancements

## Document Purpose

This document describes the planned **Phase II** work for the Aerospike LINQPad Driver AI Context project.

Phase II moves the project beyond rule cleanup and example hardening into the **architecture and runtime behavior** of AI context generation. The goal is to make the generated context more reliable, less fragile, easier to troubleshoot, and better aligned with the active Aerospike connection metadata.

This document is intended for project contributors, Copilot, and other AI-assisted development tools that need to understand why Phase II exists, what work it includes, what outcomes are expected, and how to avoid repeating previous sync and stale-baseline issues.

---

## 1. Background

The Aerospike LINQPad Driver AI Context provides structured guidance to an AI model so it can generate runnable LINQPad C# Statements code against an active Aerospike connection.

The context includes:

- Driver usage rules.
- LINQ syntax preferences.
- AValue and APrimaryKey behavior.
- Native Aerospike C# client API boundaries.
- Server-side Aerospike expression guidance.
- Set and bin metadata.
- Examples for LINQPad-driver mode, native client mode, query syntax, method syntax, and data operations.
- Footer validation rules.
- Runtime support code that builds, trims, displays, and submits the AI context.

Phase I primarily focused on **rule correctness and guardrails**. Phase II focuses on **context architecture, runtime resilience, profile-driven generation, truncation visibility, and source-of-truth discipline**.

---

## 2. Phase I Summary

Phase I was the low-risk stabilization phase.

It focused on correcting the model’s generated C# by adding or tightening rules around:

- LINQPad-driver mode versus native Aerospike C# client API mode.
- Generated property usage.
- `SetRecords` and `.AsEnumerable()` behavior.
- AValue and APrimaryKey handling.
- AValue CDT/document traversal.
- Native API expression filters.
- LINQPad-driver expression filters.
- C# null-check style: `is null` and `is not null`.
- AValue semantic checks: `IsEmpty` and `!IsEmpty`.
- Normal CLR dictionary lookup behavior.
- Avoiding `out var` in LINQ query clauses.
- Avoiding invalid iterator helper patterns.
- Avoiding stale examples that contradict rules.

Phase I also exposed a major process issue: **patches and generated bundles can easily drift from the active source baseline** if the current files are not inspected before every change.

---

## 3. Why Phase II Is Needed

Phase II is needed because the AI context is becoming large, multi-purpose, and sensitive to ordering, truncation, stale examples, and mode confusion.

The current approach has several problems.

### 3.1 Context Truncation Can Hide Critical Rules

The AI context can exceed the configured maximum size. When truncation occurs, important schema, rules, examples, or footer validation content can be lost.

This can cause the model to generate code using older or incorrect patterns even when the correct rules exist somewhere in the full context.

Phase II must make truncation visible and actionable.

### 3.2 Rules and Examples Can Conflict

AI models often copy examples more strongly than they obey abstract rules.

If a rule says:

```csharp
let info = GetValueOrDefault(trackInfoById, trackId)
where info is not null
```

but an example still says:

```csharp
let info = trackInfoById.TryGetValue(trackId, null)
where info != null
```

then the model may generate the stale example pattern.

Phase II must make context assembly more deliberate and testable so stale examples do not override newer rules.

### 3.3 Metadata Should Be Prioritized Over Examples

The generated context must help the AI use the **current active Aerospike connection metadata**.

That means namespace, set, bin, generated property, primary key, and AValue metadata should appear early enough and clearly enough that the model uses the actual current schema instead of copying sample names such as `test.Customer`, `Track`, `Album`, or `Artist`.

Phase II introduces schema-first ordering and profile-driven context generation.

### 3.4 Runtime Behavior Needs Better Diagnostics

The runtime currently builds and submits AI context, but contributors need better visibility into:

- Which profile was used.
- How large the context was before trimming.
- Whether it was truncated.
- Which sections were included or omitted.
- Whether examples were included.
- Whether full cluster details were included.
- Whether namespace/set/bin metadata was included.
- What version of the AI context was embedded into generated scripts.

Phase II should add an explicit build report.

### 3.5 Stale-Baseline Patching Has Created Regressions

Several patch attempts accidentally used stale files or older generated bundles as baselines. One notable regression replaced a current `LINQPadAIGeneratedQuery.cs` that contained Markdown response rendering with an older version that did not.

Phase II must introduce stricter source-of-truth and patching discipline.

---

## 4. Phase II Objectives

Phase II has the following objectives.

### 4.1 Make Context Generation Profile-Driven

Introduce or stabilize named context profiles such as:

```csharp
AerospikeAIContextProfile.Full
AerospikeAIContextProfile.RulesOnly
AerospikeAIContextProfile.SchemaOnly
AerospikeAIContextProfile.Debug
```

Each profile should have a clear purpose.

#### Full

The default profile for normal AI query generation.

Expected sections:

1. Header
2. Compact cluster summary
3. Namespace / set / bin metadata
4. UDF metadata if available
5. Driver rules
6. Examples
7. Footer validation
8. Build report if enabled

#### RulesOnly

A compact profile for rule review or testing.

Expected sections:

1. Header
2. Driver rules
3. System instructions
4. Footer validation
5. Minimal or no schema
6. Minimal or no examples

#### SchemaOnly

A compact profile focused on active connection metadata.

Expected sections:

1. Header
2. Compact cluster summary
3. Namespace / set / bin metadata
4. Primary key and generated property metadata
5. Minimal generation rules
6. No examples unless explicitly enabled

#### Debug

A full diagnostic profile.

Expected sections:

1. Header
2. Full cluster summary
3. Namespace configuration if enabled
4. Full metadata
5. Rules
6. Examples
7. Footer validation
8. Build report
9. Optional diagnostics useful for troubleshooting context assembly

---

## 5. Proposed Context Ordering

Phase II should make context ordering explicit and stable.

Recommended ordering for the default `Full` profile:

1. **Header**
   - Project identity.
   - Driver reference placeholders.
   - Current context version.
   - Current connection summary.

2. **Generation Mode Summary**
   - LINQPad-driver mode default.
   - Native API mode only when explicitly requested.
   - Strong mode boundary reminders.

3. **Compact Cluster Summary**
   - Contact hosts/IPs/ports.
   - Feature flags.
   - Node count and node identifiers/endpoints when available.
   - Record view setting.
   - Document API enabled setting.
   - Always use AValue setting.
   - Send user key setting.

4. **Schema / Metadata**
   - Namespaces.
   - Sets.
   - Bins.
   - Generated property names.
   - Primary key property/default key name.
   - Observed/inferred types.
   - AValue usage hints.
   - Indexes if available.

5. **UDFs / Secondary Features**
   - UDF metadata when available.
   - Secondary indexes if available.
   - Expression support hints.

6. **Core Driver Rules**
   - Generated property rule.
   - AValue rule.
   - APrimaryKey rule.
   - SetRecords / AsEnumerable rule.
   - Expression rule.
   - Dictionary lookup rule.
   - Null-check rule.
   - Iterator helper rule.

7. **Mode-Specific Rules**
   - LINQPad-driver mode rules.
   - Native Aerospike C# client API mode rules.
   - Server-side expression differences between driver and native mode.

8. **Examples**
   - Include only examples that match the current syntax preference and mode needs.
   - Clearly label method syntax, query syntax, native API, and data operation examples.
   - Avoid stale examples that contradict rules.

9. **Footer Validation**
   - Final reject/rewrite checklist.
   - Must remain late in the context so it acts like a final pre-output check.

10. **Build Report**
    - Profile used.
    - Sections included.
    - Original length.
    - Final length.
    - MaxChars.
    - Truncation status.
    - Warnings.

---

## 6. Runtime Enhancements

### 6.1 Context Build Result

Phase II should expose a richer build result rather than only returning a Markdown string.

Recommended shape:

```csharp
public sealed class AerospikeAIContextBuildResult
{
    public required string Markdown { get; init; }
    public required int OriginalLength { get; init; }
    public required int FinalLength { get; init; }
    public required bool WasTruncated { get; init; }
    public required int MaxChars { get; init; }
    public required AerospikeAIContextProfile Profile { get; init; }
    public IReadOnlyList<string> IncludedSections { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
```

Existing API compatibility should be preserved:

```csharp
public string ToMarkdown(AerospikeAIContextOptions? options = null)
    => BuildMarkdown(options).Markdown;
```

### 6.2 Truncation Warning in LINQPad Output

If the AI context is truncated, the user should see a clear warning in LINQPad output before the AI request is submitted.

Preferred warning:

```text
WARNING: AI context was truncated from 126,450 characters to 40,000 characters. Some schema, rules, examples, or validation guidance may be missing.
```

If possible, render `WARNING` in bold red using LINQPad RawHtml.

Fallback to plain text if RawHtml is unavailable.

Important: preserve existing `DumpAIResponse(...)` behavior and Markdown rendering fallback in `LINQPadAIGeneratedQuery.cs`.

### 6.3 Context Build Report

A build report should optionally be appended to the context or dumped to LINQPad.

Suggested option:

```csharp
public bool IncludeContextBuildReport { get; set; } = true;
```

The build report should include:

- Context version.
- Profile.
- Syntax preference.
- Original length.
- Final length.
- MaxChars.
- Was truncated.
- Included sections.
- Excluded sections.
- Important warnings.
- Current key options.

### 6.4 Compact Cluster Summary

The default context should avoid dumping full cluster/connection diagnostics unless debug mode is selected.

Recommended option:

```csharp
public bool IncludeFullClusterInfo { get; set; } = false;
```

Default compact cluster summary should include:

- Contact host(s).
- Port(s).
- Node count.
- Node endpoints or identifiers when safe.
- Feature flags.
- Record view.
- Document API enabled.
- Always use AValue.
- Send user key.

Avoid including by default:

- Passwords.
- Full connection strings.
- Excessive timeout/retry configuration.
- Large namespace configuration dumps.

### 6.5 Namespace Configuration Control

Recommended option:

```csharp
public bool IncludeNamespaceConfig { get; set; } = false;
```

Namespace configuration can be large and often low-value for code generation.

Only include it in Debug profile or when explicitly requested.

---

## 7. Options Model

Phase II should make the options model explicit and stable.

Potential options:

```csharp
public AerospikeAIContextProfile ContextProfile { get; set; } = AerospikeAIContextProfile.Full;
public bool PreferSchemaOverExamples { get; set; } = true;
public bool IncludeContextBuildReport { get; set; } = true;
public bool IncludeFullClusterInfo { get; set; } = false;
public bool IncludeNamespaceConfig { get; set; } = false;
public bool DumpTruncationWarning { get; set; } = true;
public int MaxChars { get; set; } = 40_000;
```

Existing options should be preserved unless intentionally deprecated.

Do not break existing user scripts that call:

```csharp
AIContext.ToMarkdown(options)
AIContext.BuildPrompt(userRequest, options)
AIContext.SubmitRequestAndCreateQueryAsync(userRequest, options)
```

---

## 8. Expected Results

After Phase II, the project should produce better AI-generated LINQPad C# in several ways.

### 8.1 Better Schema Awareness

The AI should use current metadata names rather than copying example names.

Expected improvement:

- Fewer hard-coded `test.Customer` assumptions.
- More accurate generated property names.
- Better primary key handling.
- Better set/bin selection.

### 8.2 Fewer Mode Mistakes

The AI should more reliably distinguish:

- LINQPad-driver mode.
- Native Aerospike C# client API mode.

Expected improvement:

- Native code does not use `test.Customer`.
- Driver code does not unnecessarily use `AerospikeClient`.
- Expression filters use the correct `Exp.Build(...)` behavior depending on mode.

### 8.3 More Reliable AValue Handling

The AI should better preserve AValue semantics.

Expected improvement:

- Uses `IsEmpty` for AValue missing/empty checks.
- Uses `CanConvert<T>()` / `Convert<T>()` for AValue conversion.
- Uses `TryGetValue(..., AValue.Empty)` for CDT/document traversal.
- Avoids unsafe casts and direct `System.Convert.*` on possible AValues.

### 8.4 More Reliable Dictionary Lookup Code

The AI should avoid invalid dictionary lookup patterns.

Expected improvement:

- Uses `GetValueOrDefault(dictionary, key)` inside LINQ query clauses.
- Uses `TryGetValue(key, out var value)` inside statement blocks.
- Does not generate `dict.TryGetValue(key, null)` for normal CLR dictionaries.
- Does not confuse normal CLR dictionaries with AValue/CDT `TryGetValue`.

### 8.5 Visible Truncation Diagnostics

Users should immediately know when the AI context was truncated.

Expected improvement:

- Clear warning in LINQPad output.
- Better troubleshooting when the AI misses rules or schema.
- Easier decision-making around increasing `MaxChars`, switching profiles, or reducing included sections.

### 8.6 Safer Patch Process

Contributors should avoid stale baseline regressions.

Expected improvement:

- Changed-only bundles are based on current files.
- README manifests document baseline and version.
- Examples are updated along with rules.
- Runtime source files such as `LINQPadAIGeneratedQuery.cs` are not overwritten from old bundles.

---

## 9. Validation Strategy

### 9.1 Static String Checks

Run string checks against patched files for known-bad patterns.

Examples:

```text
trackInfoById.TryGetValue(trackId, null)
TryGetValue(trackId, null)
where info != null
if (record == null)
Exp.RegexFlag.NONE
Exp.Build( passed to SetRecords.Query
test.Customer in native examples unless explicitly quoted as original driver code
AValue in native output examples
```

### 9.2 Example Consistency Checks

When a rule changes, search all example files for contradictions.

Example rule-to-example checks:

- Null-check rule:
  - Replace ordinary `== null` / `!= null` examples with `is null` / `is not null`.

- Dictionary lookup rule:
  - Replace normal CLR `dict.TryGetValue(key, null)` with `GetValueOrDefault(...)` or `TryGetValue(key, out var value)` depending on context.

- Native mode rule:
  - Ensure native examples do not use LINQPad-driver APIs.

### 9.3 Runtime Smoke Tests

Suggested smoke tests:

1. Generate context with default Full profile.
2. Generate context with RulesOnly profile.
3. Generate context with SchemaOnly profile.
4. Generate context with Debug profile.
5. Force low `MaxChars` and verify truncation warning appears.
6. Verify generated script includes AI context version comment.
7. Verify Markdown response rendering still works.
8. Verify `DumpAIResponse(...)` remains present and unchanged unless intentionally modified.

### 9.4 LINQPad Compile Checks

For generated examples, test representative scripts in LINQPad C# Statements mode.

Compile checks should include:

- Query syntax mode.
- Method syntax mode.
- LINQPad-driver mode with AValues.
- Native API mode.
- Server-side expression filter example.
- Nested CDT traversal example.
- Normal CLR dictionary enrichment example.

---

## 10. Phase II Deliverables

Recommended deliverables:

1. **Context Profiles**
   - Add or stabilize `AerospikeAIContextProfile`.
   - Implement Full, RulesOnly, SchemaOnly, Debug.

2. **Schema-First Context Assembly**
   - Ensure metadata appears before long examples.
   - Make section ordering deterministic.

3. **Compact Cluster Summary**
   - Default compact cluster summary.
   - Full cluster info only when requested/debug.

4. **Namespace Configuration Controls**
   - Default off.
   - Debug/on-demand inclusion.

5. **Context Build Result**
   - Add build metadata.
   - Preserve `ToMarkdown(...)`.

6. **Truncation Warning**
   - Dump visible warning to LINQPad output.
   - Preserve `DumpAIResponse(...)`.

7. **Context Build Report**
   - Include or dump build diagnostics.

8. **Example Cleanup**
   - Ensure examples do not contradict rules.
   - Update method/query/native examples.

9. **Bundle Process Discipline**
   - Changed-only bundles.
   - Split AIContext/non-AIContext bundles.
   - Manifest/README required.
   - Baseline version documented.

10. **Validation Checklist**
    - Static string checks.
    - Example consistency checks.
    - Runtime smoke tests.
    - LINQPad compile checks.

---

## 11. Risks and Mitigations

### Risk: Stale Baseline Regression

**Description:** A patch may accidentally use an older source file and remove newer behavior.

**Mitigation:**
- Always inspect current files before patching.
- Never use old generated bundles as baseline.
- Preserve high-risk files such as `LINQPadAIGeneratedQuery.cs` unless patching them surgically.
- Compare changed files before packaging.

### Risk: Rule/Example Conflict

**Description:** Examples may contradict newer rules and lead the model to generate stale code.

**Mitigation:**
- Search all examples whenever rules change.
- Treat examples as executable prompt influence, not passive documentation.
- Add validation checks for forbidden patterns.

### Risk: Context Becomes Too Large

**Description:** Important rules or schema may be truncated.

**Mitigation:**
- Schema-first ordering.
- Profiles.
- Compact cluster summary.
- Optional examples.
- Truncation warning.
- Build report.

### Risk: Native and Driver Modes Get Mixed

**Description:** Generated code may combine native API and LINQPad-driver APIs incorrectly.

**Mitigation:**
- Strong mode boundary rules.
- Separate native examples.
- Footer validation.
- Mode-specific checklist.

### Risk: Overfitting to Sample Names

**Description:** The AI may copy `test.Customer`, `Track`, `Album`, `Artist`, etc. from examples.

**Mitigation:**
- Add example naming note.
- Put current schema before examples.
- Use placeholders in examples where possible.
- Emphasize current metadata as source of truth.

---

## 12. Current Phase II Status

Phase II has started but should be considered **in progress**, not complete.

Completed or partially completed ideas include:

- Profile concept identified.
- Schema-first ordering identified.
- Compact cluster summary identified.
- Context build report identified.
- Truncation warning requirement identified.
- Need to preserve Markdown response rendering identified.
- Bundle/version discipline issues identified.

Before continuing Phase II, contributors should reconcile the active baseline and avoid mixing:

- rule/example cleanup work, and
- runtime architecture changes

without clear bundle boundaries.

---

## 13. Recommended Next Steps

1. Establish the current source baseline.
2. Confirm current `AIContextVersion.cs`.
3. Confirm whether Phase II profile options are already applied in code.
4. Confirm whether `LINQPadAIGeneratedQuery.cs` contains current `DumpAIResponse(...)`.
5. Implement context profiles if not already implemented.
6. Implement or verify `BuildMarkdown(...)` / build result.
7. Implement or verify truncation warning without replacing response formatting.
8. Reorder context assembly to schema-first.
9. Add build report.
10. Clean stale examples.
11. Run validation checks.
12. Create changed-only bundles with manifests.

---

## 14. Copilot Instructions

When Copilot works on this project:

- Do not assume old generated ZIP files are current.
- Use the currently checked-out repository files.
- Read the key MD files before changing rules.
- Read `AValues_Readme.md` before changing AValue behavior.
- Read `Examples.NativeClient.md` before changing native API behavior.
- Read `LINQPadAIGeneratedQuery.cs` before changing AI request/response behavior.
- Preserve existing behavior unless the change explicitly targets it.
- Make small, surgical patches.
- Update examples whenever rules change.
- Include validation notes.
- Use changed-only bundles if packaging output.

---

## 15. One-Sentence Summary

Phase II turns the Aerospike LINQPad Driver AI Context from a large rule-and-example document into a profile-driven, schema-first, diagnostics-aware runtime context system that preserves mode boundaries, exposes truncation, prioritizes active metadata, and reduces stale-example regressions.
