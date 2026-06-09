<!-- AIContext-Version: 2026.06.08.4; Change: native dictionary lookup boundary to prevent LINQPad-driver TryGetValue helper leakage into native mode. -->

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

## Final AValue-keyed Map/CDT Validation

When generated code navigates CDT/map/list data whose keys may be `AValue`, prefer the non-throwing AValue-keyed `TryGetValue(...)` helpers. Do not generate manual `TryApply<IDictionary<...>>(...)` conversion or `ContainsKey(...) ? GetByKey(...) : AValue.Empty` when `TryGetValue(...)` can express the lookup directly.




## Final Dictionary Lookup Validation

When generated LINQPad-driver code enriches query results from a dictionary inside LINQ query clauses, prefer the non-throwing default-value lookup helper. Do not generate `let x = dict.ContainsKey(key) ? dict[key] : null`; rewrite it as `let x = dict.TryGetValue(key, null)` followed by `where x != null` when missing lookup rows should be ignored. Use `TryGetValue(key, out var value)` only outside LINQ query clauses or inside block lambdas/local helpers where the `out var` scope is clear.

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


## C# Iterator Helper Validation

- If a generated helper method contains `yield return`, do not use `return someEnumerable;` or `return someValue;` in that method.
- To return all items from an enumerable inside an iterator helper, generate `foreach (var item in enumerable) yield return item;`.
- For helpers such as `AsObjectEnumerable(object value)`, if the `IEnumerable<object>` branch emits items, add `yield break;` before the broader `System.Collections.IEnumerable` branch to avoid duplicate enumeration.
