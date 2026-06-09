# Aerospike LINQPad Driver AI Context Project — Copilot Handoff Outline

## 1. Project Purpose

This project builds and maintains the AI context used by the Aerospike LINQPad Driver to generate correct LINQPad C# Statements code.

The AI context is not just documentation. It is a runtime prompt/context package that teaches an AI model how to generate LINQPad scripts against an active Aerospike connection.

The system must support two distinct generation modes:

1. **LINQPad-driver mode**
   - Uses generated LINQPad driver namespaces, sets, records, and properties.
   - Uses `SetRecords` / `SetRecords<T>`.
   - Uses `AValue` / `APrimaryKey` when Auto Values are enabled.
   - Uses generated set names such as `test.Customer`, `test.Track`, `test.CustInvsDoc`.
   - Uses generated properties such as `customer.FirstName`, `track.AlbumId`, `record.PK`.
   - Uses `Dump()` for output.

2. **Native Aerospike C# client API mode**
   - Uses only the native `Aerospike.Client` API.
   - Uses `AerospikeClient`, `ClientPolicy`, `ScanPolicy`, `QueryPolicy`, `Statement`, `RecordSet`, `Key`, `Record`, `Bin`, `Value`, `Exp`, `CDTExp`, `CTX`, `ListExp`, `MapExp`, etc.
   - Reads bins with `record.GetValue("BinName")`.
   - Uses raw namespace/set/bin names.
   - Must not use generated LINQPad-driver sets, `SetRecords`, `AValue`, `APrimaryKey`, `PK`, `GetPK()`, or generated record properties.

The purpose of the AI context is to help an AI model choose the correct mode, APIs, syntax style, null-handling, AValue operations, dictionary lookup patterns, and output shape.

---

## 2. Key Source Files and Their Roles

### 2.1 AIContextVersion.cs

This file contains the current AI context version.

It must be updated whenever AIContext MD files or AIContext runtime behavior changes.

Use the existing version style already present in the project, for example:

```csharp
public const string Current = "2026.06.08.18";
```

Do not arbitrarily reset the version. Always inspect the currently uploaded/current file first.

### 2.2 Header.md

This is the top of the generated AI context.

It explains that the context describes the current Aerospike LINQPad connection and that Aerospike is schemaless, so bin/type information is inferred from driver metadata.

It also lists driver reference placeholders such as:

```text
{{DriverRepositoryName}}
{{DriverRepositoryUrl}}
{{AValueReadmeFileName}}
{{AutoValuesBlogUrl}}
```

### 2.3 Footer.md

This is the final validation/checklist section.

Treat this as the last pre-output validation guidance for generated C#.

Footer guidance should contain high-priority “reject and rewrite before returning code” rules, such as:

- Reject ordinary null checks using `== null` or `!= null`.
- Use `is null` / `is not null` for ordinary C# null checks.
- Preserve AValue semantic checks such as `value.IsEmpty` / `!value.IsEmpty`.
- Reject invalid iterator helper patterns.
- Reject normal CLR dictionary lookup patterns that do not compile or conflict with LINQ query-syntax constraints.
- Preserve native-vs-driver mode boundaries.

### 2.4 SystemInstruction.MethodSyntax.md

This is the system instruction template when the configured LINQ style is MethodSyntax.

It should include the same core rules as the query syntax file, but it should prefer chained LINQ method syntax where appropriate:

```csharp
source
    .Where(...)
    .Select(...)
    .OrderBy(...)
```

Method syntax examples may use statement-block lambdas when necessary.

### 2.5 SystemInstruction.QuerySyntax.md

This is the system instruction template when the configured LINQ style is QuerySyntax.

It should prefer query syntax where practical:

```csharp
from record in Namespace.Set.AsEnumerable()
where ...
select ...
```

It should avoid method-chain `.Where(...)`, `.Select(...)`, `.Join(...)`, and `.GroupBy(...)` when query syntax can express the operation clearly.

Terminal operations such as `.Take(100)`, `.ToList()`, `.Any()`, `.Count()`, and `.Dump()` are still fine.

### 2.6 DriverGuide.MethodSyntax.md

This is the detailed driver guide for method syntax.

It should include:

- General LINQPad-driver rules.
- Generated property rule.
- AValue rule.
- APrimaryKey rule.
- Aerospike expression rule.
- SetRecords / `AsEnumerable()` rule.
- Native mode boundary rule if applicable.
- Normal CLR dictionary lookup rule.
- Null-check pattern rule.
- Iterator helper rule.

### 2.7 DriverGuide.QuerySyntax.md

This is the detailed driver guide for query syntax.

It should include the same rules as MethodSyntax but phrased for query syntax.

Special attention is needed for LINQ query clauses because C# query syntax does not allow all statement/block patterns directly. For example, `out var` in `let` clauses is risky and should be avoided.

### 2.8 Examples.MethodSyntax.md

This contains method-syntax examples.

Examples are powerful because models often copy them directly. Therefore, examples must never contain patterns that contradict current rules.

Do not leave stale examples that use:

- `dict.TryGetValue(key, null)` on normal CLR dictionaries.
- `where value != null`.
- `if (value == null)`.
- Native API examples inside LINQPad-driver sections.
- LINQPad-driver APIs inside native API examples.
- `AValue` in native API examples.
- `record == null` instead of `record is null`.

### 2.9 Examples.QuerySyntax.md

This contains query-syntax examples.

Because query syntax is heavily copied by AI models, this file must be kept especially clean.

For normal CLR dictionary lookup inside query clauses, prefer:

```csharp
let info = GetValueOrDefault(dictionary, key)
where info is not null
```

Do not use:

```csharp
let info = dictionary.TryGetValue(key, null)
where info != null
```

### 2.10 Examples.NativeClient.md

This contains native Aerospike C# client API examples.

Native examples must not use:

- `test.Customer`
- `test.Track`
- `test.CustInvsDoc`
- `SetRecords`
- `AValue`
- `APrimaryKey`
- `PK`
- `GetPK()`
- Generated record properties such as `customer.FirstName`

Native examples should use:

- `AerospikeClient`
- `ScanPolicy`
- `QueryPolicy`
- `Statement`
- `RecordSet`
- `record.GetValue("BinName")`
- `Exp.Build(...)` for native policy filter expressions
- Raw bin names

### 2.11 Examples.DataOperations.md

This contains examples for writes, deletes, exports, imports, or other data-changing behavior.

Default generated code should be safe, bounded, and read-only unless the user explicitly asks for mutation.

Destructive operations should generally preview records first and leave destructive calls commented unless explicitly requested.

### 2.12 Examples.General.md

This contains general usage examples, such as displaying AI context, asking LINQPad AI, and creating generated queries.

### 2.13 AValues_Readme.md

This is the detailed source for `AValue`, `APrimaryKey`, and Auto Values behavior.

It explains:

- Why `AValue` exists.
- How `AValue` handles missing and sparse bins.
- Direct AValue comparisons.
- `IsEmpty`.
- `CanConvert<T>()`.
- `Convert<T>()`.
- `Apply<TValue,TResult>()`.
- `TryApply<TValue,TResult>()`.
- `Contains`.
- `ContainsKey`.
- `FindAll`.
- `TryGetValue`.
- `AsEnumerable`.
- `ToAValue`.
- `ToDictionary`.
- `ToList`.
- `ToCDT`.
- `ToExpBin`.
- `ToExpVal`.
- APrimaryKey behavior.

This README should be treated as the human-readable authority for AValue behavior.

---

## 3. Core Generation Modes

### 3.1 LINQPad-driver Mode

Use this mode by default unless the user explicitly asks for native Aerospike C# client API code.

Allowed patterns:

```csharp
from customer in test.Customer.AsEnumerable()
where customer.State == "CA"
select customer
```

```csharp
test.Customer
    .AsEnumerable()
    .Where(customer => customer.State == "CA")
    .Take(100)
    .Dump();
```

Generated properties are preferred:

```csharp
customer.FirstName
customer.LastName
customer.PK
```

Avoid dynamic string-indexer access when a generated property exists:

```csharp
customer["FirstName"]
```

Use string-indexer access only when:

- No generated property exists.
- The bin name is not a valid C# identifier.
- Dynamic access is specifically required.

### 3.2 Native Aerospike C# Client API Mode

Use this only when the user asks for native API code or translation to native Aerospike C# client.

Allowed:

```csharp
using Aerospike.Client;

var clientPolicy = new ClientPolicy();
using var client = new AerospikeClient(clientPolicy, host, port);

var scanPolicy = new ScanPolicy();

client.ScanAll(
    scanPolicy,
    namespaceName,
    setName,
    (key, record) =>
    {
        if (record is null)
            return;

        var firstName = record.GetValue("FirstName") as string;
    });
```

Native server-side expression filtering must use:

```csharp
filterExp = Exp.Build(...)
```

inside `ScanPolicy` or `QueryPolicy`.

Do not use LINQPad-driver APIs in native mode.

---

## 4. AValue / Auto Values Rules

### 4.1 General AValue Guidance

When Auto Values are enabled, generated record properties may be `AValue`.

Do not assume an AValue-backed property is a plain CLR type unless metadata clearly says so.

Prefer AValue-aware operations:

```csharp
value.IsEmpty
value.CanConvert<long>()
value.Convert<long>()
value.TryApply<string, bool>(s => s.StartsWith("A"))
value.Apply<string, int>(s => s.Length)
value.AsEnumerable()
value.TryGetValue("Key", AValue.Empty)
```

### 4.2 AValue Missing/Empty Rule

Use:

```csharp
value.IsEmpty
!value.IsEmpty
```

for AValue semantic missing/empty/null checks.

`IsEmpty` is an extension method and may be valid even if the AValue variable itself is null.

Do not rewrite AValue semantic checks to ordinary null checks unless the intent is specifically to test the variable reference.

Correct:

```csharp
let invoices = customer.Invoices.ToAValue()
where !invoices.IsEmpty
```

### 4.3 AValue Conversion Rule

Do not use `System.Convert.*` directly on values that may be `AValue` or `APrimaryKey`.

Prefer:

```csharp
value.CanConvert<long>()
value.Convert<long>()
```

or available convenience methods such as:

```csharp
value.ToLong()
```

when those APIs exist in the driver.

### 4.4 AValue CDT / Document Navigation

Use `ToAValue()` before traversal when a value may be null or may already be AValue:

```csharp
let invoices = customer.Invoices.ToAValue()
where !invoices.IsEmpty
from invoice in invoices.AsEnumerable()
from line in invoice.TryGetValue("Lines", AValue.Empty).AsEnumerable()
let trackId = line.TryGetValue("TrackId", AValue.Empty)
where trackId.CanConvert<long>()
select trackId.Convert<long>()
```

Do not replace this with normal CLR dictionary lookup rules.

AValue/CDT `TryGetValue(...)` is separate from normal CLR dictionary `TryGetValue(...)`.

---

## 5. Normal CLR Dictionary Lookup Rule

This rule is important because it has caused repeated generation errors.

### 5.1 Source Shape Determines the Rule

The rule depends on the source shape.

If the source is a normal CLR dictionary-shaped object:

```csharp
Dictionary<TKey, TValue>
IReadOnlyDictionary<TKey, TValue>
IDictionary<TKey, TValue>
```

then it should use normal CLR dictionary lookup patterns.

This applies even if the key or value type is `AValue`, `APrimaryKey`, anonymous types, records, nullable types, or reference types.

### 5.2 Inside LINQ Query Clauses

Inside LINQ query syntax clauses, do not generate:

```csharp
let info = dictionary.TryGetValue(key, out var value)
```

Do not generate:

```csharp
let info = dictionary.TryGetValue(key, null)
```

Do not generate:

```csharp
let info = dictionary.TryGetValue(key, default)
```

Prefer a helper:

```csharp
let info = GetValueOrDefault(dictionary, key)
where info is not null
```

Helper:

```csharp
static TValue GetValueOrDefault<TKey, TValue>(
    IReadOnlyDictionary<TKey, TValue> source,
    TKey key,
    TValue defaultValue = default)
{
    return source.TryGetValue(key, out var value)
        ? value
        : defaultValue;
}
```

### 5.3 Inside Statement Blocks / Method Lambdas

Inside ordinary statement blocks, standard C# dictionary lookup is fine:

```csharp
if (!dictionary.TryGetValue(key, out var value))
    return;
```

or:

```csharp
if (dictionary.TryGetValue(key, out var value))
{
    ...
}
```

Do not use fake/default-value overloads on normal CLR dictionaries:

```csharp
dictionary.TryGetValue(key, null)
```

unless the project has a verified custom extension method for that exact source type. Do not assume such an extension exists for normal CLR dictionaries.

### 5.4 Do Not Confuse with AValue TryGetValue

AValue/CDT navigation remains valid:

```csharp
line.TryGetValue("TrackId", AValue.Empty)
profile.TryGetValue("email", "<missing>")
```

This is not the same as normal CLR dictionary lookup.

---

## 6. C# Null Check Pattern Rule

For ordinary C# reference/null checks, use:

```csharp
value is null
value is not null
```

Do not generate:

```csharp
value == null
value != null
```

Correct:

```csharp
if (record is null)
    return;

where info is not null
```

Incorrect:

```csharp
if (record == null)
    return;

where info != null
```

Exception: For AValue semantic missing/empty checks, use:

```csharp
value.IsEmpty
!value.IsEmpty
```

Do not replace AValue `IsEmpty` checks with `is null`.

---

## 7. LINQ Syntax Preference Rules

The AI context can be generated with either MethodSyntax or QuerySyntax preference.

### 7.1 MethodSyntax Preference

Prefer:

```csharp
var rows = test.Customer
    .AsEnumerable()
    .Where(customer => customer.State == "CA")
    .Select(customer => new
    {
        customer.PK,
        customer.FirstName,
        customer.LastName
    })
    .Take(100);

rows.Dump();
```

### 7.2 QuerySyntax Preference

Prefer:

```csharp
var rows =
    (from customer in test.Customer.AsEnumerable()
     where customer.State == "CA"
     select new
     {
         customer.PK,
         customer.FirstName,
         customer.LastName
     })
    .Take(100);

rows.Dump();
```

### 7.3 Terminal Operations

Terminal operations are allowed in both styles:

```csharp
.Take(100)
.ToList()
.Any()
.Count()
.Dump()
```

---

## 8. SetRecords / AsEnumerable Rule

Generated Aerospike set objects are `SetRecords` / `SetRecords<T>`.

For collection-style LINQ operations, call `.AsEnumerable()` first.

Use `.AsEnumerable()` before:

```csharp
Join
GroupJoin
OrderBy
ThenBy
GroupBy
SelectMany
Concat
Union
Distinct
Except
Intersect
ToDictionary
```

Correct:

```csharp
from customer in test.Customer.AsEnumerable()
join invoice in test.Invoice.AsEnumerable()
    on customer.PK equals invoice.CustomerId
select ...
```

Incorrect:

```csharp
test.Customer.Join(...)
```

Some set APIs may be available directly, such as:

```csharp
Where
First
FirstOrDefault
Skip
ToList
ToArray
```

Use direct set APIs only when the driver exposes them and the operation is appropriate.

---

## 9. Aerospike Expression Rules

### 9.1 LINQPad-driver Expression Filters

For LINQPad-driver mode, server-side expression filters are passed to `SetRecords.Query(...)`.

Use raw bin names in expressions:

```csharp
Client.Exp filterExpression = Exp.And(
    Exp.EQ(Exp.StringBin("State"), Exp.Val("CA")),
    Exp.BinExists("Company"));

var customers =
    from customer in test.Customer.Query(filterExpression)
    select customer;
```

Do not call `Exp.Build(...)` when passing a `Client.Exp` to LINQPad-driver `SetRecords.Query(...)`. The driver builds it into the policy.

### 9.2 Native API Expression Filters

In native API mode, build the expression into the policy:

```csharp
var policy = new QueryPolicy
{
    filterExp = Exp.Build(...)
};
```

Use native `client.Query(...)` or `client.ScanAll(...)`.

### 9.3 Raw Bin Names

In Aerospike expressions, use raw bin names:

```csharp
Exp.StringBin("FirstName")
Exp.IntBin("TrackId")
Exp.ListBin("Invoices")
Exp.MapBin("Profile")
```

Do not use generated record properties inside expression builders.

---

## 10. Native API CDT Expression Rules

For nested list/map/document paths in native server-side expressions, prefer:

```csharp
CDTExp.SelectByPath(...)
```

with `CTX` selectors.

Example:

```csharp
var trackIdsExpression =
    CDTExp.SelectByPath(
        Exp.Type.LIST,
        SelectFlag.VALUE,
        Exp.ListBin("Invoices"),
        CTX.AllChildren(),
        CTX.MapKey(Value.Get("Lines")),
        CTX.AllChildren(),
        CTX.MapKey(Value.Get("TrackId")));
```

Then test membership with:

```csharp
ListExp.GetByValue(
    ListReturnType.EXISTS,
    Exp.Val(targetTrackId),
    trackIdsExpression)
```

Do not generate speculative invalid expressions such as:

```csharp
Exp.Val(ListReturnType.VALUE)
ListExp.ValRange(...)
ListExp.ValRange(Value.Get("TrackId"))
Exp.Bin("Invoices")
Exp.RegexFlag.NONE
```

Use:

```csharp
RegexFlag.NONE
```

not:

```csharp
Exp.RegexFlag.NONE
```

---

## 11. Iterator Helper Rule

If a helper method uses `yield return`, it is an iterator block.

Do not mix `yield return` with:

```csharp
return someEnumerable;
return someValue;
return objectList;
```

Correct:

```csharp
IEnumerable<object> AsObjectEnumerable(object value)
{
    if (value is IEnumerable<object> objectList)
    {
        foreach (var item in objectList)
            yield return item;

        yield break;
    }

    if (value is System.Collections.IEnumerable enumerable && value is not string)
    {
        foreach (var item in enumerable)
            yield return item;
    }
}
```

---

## 12. Source-of-Truth Rules

### 12.1 Current Uploaded Files Are the Baseline

When patching, always use the currently uploaded/current repository files as the baseline.

Do not use an older generated bundle as baseline.

Do not use files from memory.

Do not assume a prior patch was applied unless the current uploaded files show it.

### 12.2 Examples Must Not Conflict with Rules

AI models copy examples aggressively.

If a rule says one thing and an example says another, the generated code may follow the example.

Therefore, when adding or changing a rule:

- Search all example files for conflicting patterns.
- Update or remove stale examples.
- Run string checks for known forbidden patterns.
- Ensure both MethodSyntax and QuerySyntax examples are aligned.

### 12.3 Stale Bundle Avoidance

Before creating a changed-only bundle:

1. Inspect the current baseline files.
2. Record the current `AIContextVersion.cs`.
3. Identify only the files that require changes.
4. Patch only those files.
5. Verify no unrelated changes were introduced.
6. Verify stale forbidden patterns are removed.
7. Include a README manifest.
8. Produce changed-only zip files.

---

## 13. Bundle Rules

The project uses split bundle ownership.

### 13.1 AIContext Bundle

Use this bundle for AI context resources and related version files, such as:

```text
AIContextVersion.cs
Header.md
Footer.md
DriverGuide.MethodSyntax.md
DriverGuide.QuerySyntax.md
SystemInstruction.MethodSyntax.md
SystemInstruction.QuerySyntax.md
Examples.MethodSyntax.md
Examples.QuerySyntax.md
Examples.NativeClient.md
Examples.DataOperations.md
Examples.General.md
AValues_Readme.md
README_*.md
```

### 13.2 Non-AIContext Bundle

Use this bundle for main-folder runtime/source files, such as:

```text
AerospikeAIContext.cs
AerospikeAIContextExtensions.cs
AerospikeAIContextOptions.cs
LINQPadAIGeneratedQuery.cs
AValue.cs
AValueHelper.cs
AValuePart.cs
APrimaryKey.cs
```

### 13.3 Split Bundles

If a patch changes both AIContext resources and runtime C# files, produce two bundles:

- One AIContext bundle.
- One non-AIContext bundle.

### 13.4 Changed-Only Bundles

Bundles must include only changed files plus a README/manifest.

Do not include full replacement sets unless explicitly requested.

---

## 14. README / Manifest Requirements

Every bundle should include a README that states:

```text
Patch name
Baseline version
New version
Bundle type
Files changed
Files intentionally not changed
Purpose
Detailed changes
Validation performed
Known risks
Rollback guidance
```

Example:

```text
Baseline version: 2026.06.08.18
New version: 2026.06.08.19
Bundle type: AIContext changed-only

Changed files:
- AIContextVersion.cs
- Examples.QuerySyntax.md
- Examples.MethodSyntax.md
- Footer.md

Purpose:
Remove stale normal-CLR-dictionary examples that used dict.TryGetValue(key, null).

Validation:
- Confirmed no occurrences of trackInfoById.TryGetValue(trackId, null).
- Confirmed no ordinary where x != null in touched examples.
- Confirmed AValue/CDT TryGetValue examples remain intact.
```

---

## 15. Validation Checklist Before Returning Generated C# Code

Before returning generated LINQPad C# code, validate:

1. Does the code use the correct mode?
   - LINQPad-driver mode vs native API mode.

2. If LINQPad-driver mode:
   - Uses generated sets/properties.
   - Uses `AsEnumerable()` before collection-style LINQ operations.
   - Uses AValue helpers for AValue-backed values.
   - Uses `SetRecords.Query(...)` correctly for driver expressions.
   - Does not use native-only APIs unless explicitly needed.

3. If native API mode:
   - Uses `AerospikeClient`.
   - Uses raw namespace/set/bin names.
   - Uses `record.GetValue("BinName")`.
   - Uses `Exp.Build(...)` in native policies.
   - Does not use `test.Customer`, `SetRecords`, `AValue`, `PK`, `GetPK()`, or generated properties.

4. Null checks:
   - Ordinary reference checks use `is null` / `is not null`.
   - AValue semantic checks use `IsEmpty` / `!IsEmpty`.

5. Dictionary lookups:
   - Normal CLR dictionaries inside LINQ clauses use `GetValueOrDefault(...)`.
   - Normal CLR dictionaries inside statement blocks use `TryGetValue(key, out var value)`.
   - Do not use `dict.TryGetValue(key, null)` unless verified as a custom project extension for that exact type.
   - Do not replace AValue/CDT `TryGetValue` with dictionary helpers.

6. AValue conversion:
   - Use `CanConvert<T>()` and `Convert<T>()`.
   - Do not use `System.Convert.*` directly on possible `AValue` or `APrimaryKey`.

7. Iterator helpers:
   - Do not mix `yield return` with `return someEnumerable`.

8. Output:
   - Use `Dump()`.
   - Keep queries bounded with `Take(...)`, indexes, or filters when appropriate.

---

## 16. Known High-Risk Areas

### 16.1 Stale Examples

Examples may lag behind rules. Always search examples when changing rules.

Known stale pattern to watch for:

```csharp
trackInfoById.TryGetValue(trackId, null)
where info != null
```

Correct pattern:

```csharp
let info = GetValueOrDefault(trackInfoById, trackId)
where info is not null
```

### 16.2 Stale LINQPadAIGeneratedQuery.cs

This file previously regressed when patched from an older baseline.

Preserve:

- `DumpAIResponse(...)`
- Markdown/RawHtml response formatting fallback behavior
- Generated query creation behavior
- AI context version comment behavior
- Native-vs-driver behavior

Never replace this file wholesale from an old bundle.

### 16.3 Version Drift

The MD files and `AIContextVersion.cs` may show different version headers if not synchronized.

Before patching:

- Inspect actual current files.
- Decide the version bump from the current `AIContextVersion.cs`.
- Do not infer current version from old generated bundles.

### 16.4 Context Truncation

If AI context is truncated, the user wants a visible LINQPad output warning.

Preferred behavior:

- Dump a clear warning before submitting the AI request.
- Show `WARNING` prominently, ideally bold/red via RawHtml if available.
- Preserve existing Markdown response rendering.

---

## 17. Recommended Workflow for Copilot

When asked to modify this project:

1. Ask or identify which files are the active baseline.
2. Inspect the active baseline files directly.
3. Do not use older generated ZIP bundles as baseline.
4. Locate the relevant MD/C# files.
5. Make the smallest possible patch.
6. Search for contradictory examples.
7. Search for stale forbidden patterns.
8. Update `AIContextVersion.cs`.
9. Add a README manifest.
10. Produce changed-only bundle(s).
11. Summarize:
    - Baseline version.
    - New version.
    - Bundle type.
    - Changed files.
    - What changed.
    - What was intentionally not changed.
    - Validation performed.

---

## 18. Recommended Files for Copilot to Read First

Read these first:

```text
AIContextVersion.cs
Header.md
Footer.md
SystemInstruction.MethodSyntax.md
SystemInstruction.QuerySyntax.md
DriverGuide.MethodSyntax.md
DriverGuide.QuerySyntax.md
AValues_Readme.md
Examples.MethodSyntax.md
Examples.QuerySyntax.md
Examples.NativeClient.md
Examples.DataOperations.md
Examples.General.md
AerospikeAIContext.cs
AerospikeAIContextOptions.cs
AerospikeAIContextExtensions.cs
LINQPadAIGeneratedQuery.cs
```

Then inspect any runtime helper files involved in the specific change, such as:

```text
AValue.cs
AValueHelper.cs
AValuePart.cs
APrimaryKey.cs
```

---

## 19. One-Sentence Project Summary

This project generates a schema-aware, rule-heavy AI context for the Aerospike LINQPad Driver so an AI model can produce runnable LINQPad C# Statements code in either LINQPad-driver mode or native Aerospike C# client API mode while respecting AValue semantics, generated driver APIs, C# syntax constraints, null handling, dictionary lookup safety, and mode-specific API boundaries.
