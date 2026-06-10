<!-- AIContext-Version: 2026.06.10.01; Change: add native mode precedence validation for inferred connection values versus explicit requested values. -->

## Generated Script Summary and Comments

- Start generated runnable C# scripts with a concise comment block summarizing the user's request and the chosen execution mode.
- Include the target set/namespace, key filter criteria, output shape, and whether server-side expressions or client-side traversal are used.
{{InlineCommentGuidance}}



## C# Iterator Helper Validation

- If a generated helper method contains `yield return`, do not use `return someEnumerable;` or `return someValue;` in that method.
- To return all items from an enumerable inside an iterator helper, generate `foreach (var item in enumerable) yield return item;`.
- For helpers such as `AsObjectEnumerable(object value)`, if the `IEnumerable<object>` branch emits items, add `yield break;` before the broader `System.Collections.IEnumerable` branch to avoid duplicate enumeration.

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
- In native Aerospike C# client mode, apply connection-value precedence in this order: explicit user request values > explicit values already present in generated code > inferred connection defaults.
- In native mode, infer host/port/TLS/auth/policy defaults only for missing values and do not overwrite explicit requested values.
- In native mode, preserve explicit `namespaceName` and `setName` values already present in generated code unless explicitly asked to change them.
- Avoid destructive operations unless explicitly requested.



## Final C# Null Check Pattern Validation

Before returning generated LINQPad C# Statements code, reject and rewrite ordinary null checks that use `== null` or `!= null`.

Use `is null` and `is not null` for ordinary C# reference/null checks.

This applies to both LINQPad-driver mode and native Aerospike C# client API mode.

Do not rewrite AValue semantic checks such as `value.IsEmpty` or `!value.IsEmpty`; those are preferred for AValue missing/empty/null semantics and may be valid even when the AValue variable itself is null.

Before returning generated code, reject and rewrite normal CLR dictionary lookup patterns that misuse `TryGetValue` in LINQ clauses (for example, `dictionary.TryGetValue(key, null)` or `dictionary.TryGetValue(key, default)`).
Use helper-based lookup in query clauses (for example, `GetValueOrDefault(...)`) and use `TryGetValue(key, out var value)` only in statement/lambda blocks.
Do not apply this rewrite to AValue/CDT `TryGetValue(...)` navigation patterns.
