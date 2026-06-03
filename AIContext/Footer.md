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

