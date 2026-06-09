<!-- AIContext-Version: 2026.06.08.13; Change: C# type declaration placement rule for generated LINQPad scripts. -->

## AI Query Guidance

- Prefer bounded queries.
- Use secondary indexes when available.
- Treat bin/type information as inferred because Aerospike is schemaless.
- Prefer generated record properties over string-indexer bin access.
{{AlwaysUseAValuesGuidance}}
- For AValue-backed properties, use `CanConvert<T>()`, `Convert<T>()`, `Apply<TValue, TResult>()`, and null-safe `TryApply<TValue, TResult>()` instead of unsafe CLR casts.
- Prefer `TryApply<TValue, bool>(...)` in filters when invoking type-specific methods on values that may be null, missing, mixed-type, or AValue-backed.
- Use AValue comparison operators for direct comparisons when supported; use `Apply` / `TryApply` for type-specific methods.
- Use type-inspection properties such as `IsString`, `IsNumeric`, `IsInt`, `IsList`, `IsMap`, `IsJson`, `IsGeoJson`, `IsEmpty`, and `UnderlyingType` before type-sensitive operations on mixed bins.
- Use `Contains(...)`, `ContainsKey(...)`, `FindAll(...)`, `TryGetValue(...)`, and `AValue.MatchOptions` for scalar/list/map/JSON/CDT searches.
- Use `AsEnumerable()`, `AsEnumerable<T>()`, `ToList()`, `ToListItem()`, `ToDictionary()`, `ToDictionary<K,V>()`, `ToCDT()`, and `ElementAtOrDefault(...)` for CDT exploration.
- Use `ToBin()` when turning an `AValue` back into an Aerospike bin for write operations.
- Use `ToExpBin()` and `ToExpVal()` only for Aerospike expression-building scenarios.
- Use Aerospike expressions with raw bin names when the user asks for server-side filtering; do not replace those with LINQ `where` clauses.
- Use `{{DefaultASPIKeyName}}` for the primary key when available; otherwise use `GetPK()`.
- Use `.AsEnumerable()` before collection-style LINQ operations on `SetRecords` instances.
{{LinqSyntaxGuidance}}
- Avoid destructive operations unless explicitly requested.

## Context-Bound Naming Validation

Do not treat sample names such as `test`, `Customer`, `CustInvsDoc`, `Track`, `Album`, `Artist`, `Invoices`, `Lines`, or `TrackId` as fixed rules. Apply rules generically to the actual namespace, set, bin, generated property, and primary-key names from the current AI context metadata or the user's request.

## Example Mode Validation

Before using an example, verify its declared mode. Do not copy LINQPad-driver examples into native Aerospike API mode, and do not copy native Aerospike API examples into LINQPad-driver mode.



## Final Cross-Mode Join Key Source Validation

When generated code joins, enriches, or correlates records across sets in either LINQPad-driver mode or native Aerospike C# client API mode, verify that it uses direct scalar bin values, generated scalar properties, or primary-key values as join keys when those are available in the current metadata and represent the same relationship.

Do not traverse CDT, JSON, map, list, or document bins to obtain a join/correlation key when an equivalent direct scalar bin/property exists.

Use CDT/JSON/map/list/document traversal for join/correlation keys only when the nested value is required by the user request, the relationship exists only in the nested structure, or nested filtering is needed before enrichment.

## Final Source-of-Truth Validation for Embedded Document Values

When generated code returns related-entity values from associated sets, verify that it does not treat embedded CDT, JSON, map, list, or document values as authoritative when the associated set is available and the relationship can be resolved.

Embedded document values may be denormalized snapshots and can be stale.

Use embedded values to locate, filter, or match parent records when needed, but prefer reading current related values from the associated set. Only return embedded values as authoritative when the user explicitly asks for embedded document content, the associated set is unavailable, the embedded value is the only available source, or the purpose is to inspect or compare the denormalized snapshot.

## Final AValue-keyed Map/CDT Validation

When generated code navigates CDT/map/list data whose keys may be `AValue`, prefer the non-throwing AValue-keyed `TryGetValue(...)` helpers. Do not generate manual `TryApply<IDictionary<...>>(...)` conversion or `ContainsKey(...) ? GetByKey(...) : AValue.Empty` when `TryGetValue(...)` can express the lookup directly.




## Final Cross-Mode Dictionary Lookup Helper Validation

When generated code uses a normal CLR dictionary-shaped source such as `Dictionary<TKey,TValue>`, `IReadOnlyDictionary<TKey,TValue>`, or `IDictionary<TKey,TValue>` inside LINQ query clauses in either LINQPad-driver mode or native Aerospike C# client API mode, reject and rewrite `TryGetValue(key, out var value)` patterns inside `let`, `where`, `orderby`, `join`, or `select` clauses.

Reject patterns such as:

```csharp
let hasValue = dict.TryGetValue(key, out var value)
where hasValue && value != null
select value
```

Generate a local helper such as `GetValueOrDefault(dictionary, key)` and use that helper inside LINQ query clauses.

The helper may be used with any key/value types, including CLR primitives, nullable values, tuples, records, reference types, `AValue`, or `APrimaryKey`, as long as the source is a normal CLR dictionary-shaped source.

Use standard `dict.TryGetValue(key, out var value)` only inside normal C# statement blocks, block lambdas, or local helper methods.

Do not apply this rule to AValue/CDT/map/document navigation targets such as `line.TryGetValue("TrackId", AValue.Empty)`. For those sources, continue using AValue-safe `TryGetValue(...)`, `AsEnumerable()`, `AValue.Empty`, `CanConvert<T>()`, and `Convert<T>()`.

## Final AValue Null-Normalization Validation

When generated code navigates a property or nested value as AValue/CDT data, do not generate CLR fallback containers such as `customer.Invoices ?? new List<System.Text.Json.JsonDocument>()`. Normalize with `customer.Invoices.ToAValue()` instead, then use `IsEmpty`, `AsEnumerable()`, `TryGetValue(...)`, `CanConvert<T>()`, and `Convert<T>()`.


## Generated Script Summary and Comments

- Start generated runnable C# scripts with a concise comment block summarizing the user's request and the chosen execution mode.
- Include the target set/namespace, key filter criteria, output shape, and whether server-side expressions or client-side traversal are used.
{{InlineCommentGuidance}}


## Native API Purity Validation

When the user asks for native Aerospike C# client API code, do not return any generated code that uses LINQPad-driver data access. Reject and rewrite code containing `test.CustInvsDoc`, `test.Track`, `test.Album`, `test.Artist`, `.AsEnumerable()` on generated sets, `.AerospikeClient` taken from a generated set, `SetRecords`, `AValue`, `APrimaryKey`, `PK`, `GetPK()`, or generated record properties for Aerospike data access.

Native mode must use an explicit `new AerospikeClient(...)` and native operations such as `client.Query(...)`, `client.ScanAll(...)`, `client.Get(...)`, and `record.GetValue("BinName")`. If native mode needs Track/Album/Artist enrichment, read those related sets through the native client too.


## Native Dictionary Lookup Validation

When the user asks for native Aerospike C# client API code, do not return LINQPad-driver/AValue helper dictionary lookup forms such as `dict.TryGetValue(key, null)`, `dict.TryGetValue(key, default(...))`, `dict.TryGetValue(key, defaultValue)`, `source.TryGetValue("KeyName")`, or `source.TryGetValue("KeyName", AValue.Empty)`.

Native mode must use ordinary C# dictionary APIs such as `TryGetValue(key, out var value)` inside a statement block, block lambda, or helper method where the `out var` scope is clear.

If native-mode code contains `let x = dict.TryGetValue(key, default(...))` or similar default-value helper syntax, rewrite it before returning.


## Native Disposable Object Validation

When the user asks for native Aerospike C# client API code, reject and rewrite `using var clientPolicy = new ClientPolicy();`. Use `var clientPolicy = new ClientPolicy();` instead.

Reserve `using var` for disposable native objects such as `AerospikeClient` and `RecordSet`. Do not use `using var` for `ClientPolicy`, `ScanPolicy`, `QueryPolicy`, `WritePolicy`, or similar policy/configuration objects.



## Native ScanAll Callback Validation

When generated native Aerospike C# client code uses `client.ScanAll(...)`, verify that it is not assigned to a variable, enumerated with `foreach`, or chained with LINQ methods.

Reject and rewrite patterns such as `var x = client.ScanAll(...)`, `client.ScanAll(...).ToList()`, or `foreach (...) in client.ScanAll(...)`.

`ScanAll(...)` must use the callback form and collect results inside the callback. Use `client.Query(...)` with `RecordSet` when the code needs an iterable native result set.


## Final LINQ `out var` Dictionary Lookup Chain Validation

Reject and rewrite LINQ query clauses that declare `out var` variables with `TryGetValue(...)`, especially chained dependent dictionary lookups such as:

```csharp
let hasAlbum = albumByTrackId.TryGetValue(trackId, out var albumId)
let hasArtist = hasAlbum && albumArtistByAlbumId.TryGetValue(albumId, out var artistId)
let hasArtistName = hasArtist && artistById.TryGetValue(artistId, out var artistName)
```

Use a local helper method, block lambda, or materialized intermediate result so each `out var` is scoped inside a normal C# statement block. Prefer correct C# scoping over forcing query syntax.

## C# Iterator Helper Validation

- If a generated helper method contains `yield return`, do not use `return someEnumerable;` or `return someValue;` in that method.
- To return all items from an enumerable inside an iterator helper, generate `foreach (var item in enumerable) yield return item;`.
- For helpers such as `AsObjectEnumerable(object value)`, if the `IEnumerable<object>` branch emits items, add `yield break;` before the broader `System.Collections.IEnumerable` branch to avoid duplicate enumeration.


## Mode-Specific API Reference and Reflection Fallback Validation

When generated code is in LINQPad-driver mode, use the Aerospike LINQPad driver repository as the API authority: `{{DriverRepositoryUrl}}`.

When generated code is in native Aerospike C# client API mode, use the official Aerospike C# client repository as the API authority: `{{NativeCSharpClientRepositoryUrl}}`.

Do not use LINQPad-driver-only APIs as the authority for native-mode code, and do not use native-client-only APIs as the authority for LINQPad-driver-only APIs.

If an API member, overload, enum, constructor, namespace, or disposable behavior is unclear, prefer reflection against the relevant loaded assembly before guessing. In native mode, inspect the loaded `Aerospike.Client` assembly. In LINQPad-driver mode, inspect the Aerospike LINQPad driver assembly and generated connection types. If reflection does not confirm the API surface, generate a simpler verified pattern or explain the limitation.



## Final C# Type Declaration Placement Validation

Before returning generated LINQPad C# Statements code, verify that helper `record`, `class`, `struct`, and `enum` declarations are placed at the end of the script after executable statements and helper methods.

Reject and rewrite scripts that interleave helper type declarations with query, scan, lookup, enrichment, or output logic.

This validation applies to both LINQPad-driver mode and native Aerospike C# client API mode.
