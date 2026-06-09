<!-- AIContext-Version: 2026.06.08.20; Change: enforce normal CLR dictionary lookup pattern in LINQ clauses and prevent AValue TryGetValue-style misuse. -->

## Driver Usage Rules

- Generate C# statements intended to run inside LINQPad.
- Use `Dump()` to display results.
- Prefer generated namespace and set members when they are present.
- Keep scans and queries bounded with `Take(...)`, filters, or secondary indexes where possible.
- Do not assume every Aerospike record in a set has every bin.
- Do not assume a bin has only one type unless metadata clearly indicates it.
- Treat bin names as case-sensitive.
- Prefer read-only query/exploration code unless the user explicitly asks for writes.
- Ask before destructive deletes/truncates unless the user explicitly requested them.
- Use the native Aerospike client only when the high-level driver API does not cover the request.


### Important Generated Script Summary and Comment Rule

- For runnable generated C# scripts, start with a short comment block that summarizes the user's request before the first executable statement.
- The request summary should capture the target namespace/set, filter criteria, output shape, and any important mode choice such as LINQPad-driver mode, native API mode, server-side expression filtering, or client-side traversal.
- Keep the summary factual and concise; do not restate the entire prompt verbatim.
- The top request-summary comment is always allowed because it documents the generated script's intent.
{{InlineCommentGuidance}}


### Important C# Iterator Helper Rule

- When generating C# helper methods that use `yield return`, the method body is an iterator block.
- Do not generate `return someEnumerable;`, `return someValue;`, or `return objectList;` inside an iterator block. That does not compile when the method also contains `yield return`.
- To emit all items from an existing enumerable inside an iterator block, use `foreach` and `yield return` each item.
- When a first branch handles `IEnumerable<object>`, add `yield break;` after yielding those items so the broader `System.Collections.IEnumerable` branch does not emit the same items again.
- If the helper should directly return an enumerable, then do not use `yield return` anywhere in that helper; instead return `Enumerable.Empty<object>()`, `objectList`, or a projected enumerable consistently.

Preferred native/helper pattern:

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

Avoid this invalid iterator pattern:

```csharp
IEnumerable<object> AsObjectEnumerable(object value)
{
    if (value is IEnumerable<object> objectList)
        return objectList;

    if (value is System.Collections.IEnumerable enumerable && value is not string)
    {
        foreach (var item in enumerable)
            yield return item;
    }
}
```


### Important C# Null Check Pattern Rule

This rule applies to all generated LINQPad C# Statements code, including LINQPad-driver mode, native Aerospike C# client API mode, helper methods, projections, enrichment logic, and post-processing code.

When checking ordinary C# reference values for null, prefer C# pattern matching syntax:

```csharp
value is null
value is not null
```

Do not generate equality or inequality null checks for ordinary null checks:

```csharp
value == null
value != null
```

Preferred:

```csharp
if (record is null)
    return;

where albumInfo is not null
```

Avoid:

```csharp
if (record == null)
    return;

where albumInfo != null
```

For AValue values, prefer AValue semantic checks when the intent is "missing, empty, or null AValue":

```csharp
value.IsEmpty
!value.IsEmpty
```

`IsEmpty` is an AValue extension method and may be used even when the AValue variable itself is null.

Do not replace AValue semantic checks with ordinary null checks unless the intent is specifically to test the variable reference rather than the AValue/missing-bin semantics.

Preferred for AValue/CDT navigation:

```csharp
let invoices = customer.Invoices.ToAValue()
where !invoices.IsEmpty
```

Also valid when an AValue variable may be null:

```csharp
if (value.IsEmpty)
    return;
```

Use `is null` / `is not null` for ordinary reference checks, and use `IsEmpty` / `!IsEmpty` for AValue semantic emptiness checks.

### Important LINQ Syntax Preference

- Preferred LINQ style: `query syntax`.
- Use query syntax as the default form for query logic.
- For filters, projections, sorting, joins, and grouping, use `from`, `where`, `select`, `orderby`, `join`, and `group`.
- Do not use method-chain forms such as `.Where(...)`, `.Select(...)`, `.OrderBy(...)`, `.Join(...)`, or `.GroupBy(...)` when an equivalent query-syntax form is available.
- Method syntax is allowed only for terminal or non-query-expression operations such as `.Take(100)`, `.Skip(...)`, `.ToList()`, `.Count()`, `.FirstOrDefault()`, `.Any()`, `.Dump()`, or operations that cannot be expressed cleanly in query syntax.
- For joins, generate `from left in Namespace.LeftSet.AsEnumerable() join right in Namespace.RightSet.AsEnumerable() on left.Key equals right.Key select ...`.
- Do not generate `.Join(...)` when a query-syntax `join` clause can express the same logic.

### Important Record Property Rule

- When accessing Aerospike bin values from generated record objects, prefer generated C# properties when available.
- Example: use `customer.userid` instead of `customer["userid"]` when the `userid` property exists.
- Use `record["binName"]` only when no generated property exists, the bin name is not a valid C# identifier, or dynamic access is specifically required.
- Prefer property access in projections, filters, joins, sorts, and groups.
- The set-level bin metadata below lists the raw Aerospike bin name and, when available, the generated C# property name.

### Important AValue / AutoValue Rule

- The Aerospike LINQPad driver may expose bin values through `AValue` / AutoValue behavior.
- Current connection setting `Always use AValue`: `{{AlwaysUseAValues}}`.
- When `Always use AValue` is true, generated record properties may represent Aerospike values using the driver's `AValue` abstraction instead of plain CLR primitive types.
- Prefer generated record properties first, but write comparisons and projections in a way that respects the property's generated type.
- Do not assume an `AValue`-backed property is a raw `string`, `int`, `long`, `double`, `bool`, `DateTime`, list, or dictionary unless the context metadata clearly says so.
- Avoid unsafe casts from `AValue`-backed values to CLR primitive types.
- Use the driver's `AValue`-friendly comparison, conversion, or value-access patterns when needed.
- Simple equality comparisons such as `record.Status == "active"` may be valid when the generated property/operator supports it.
- Numeric comparisons such as `record.Amount > 100` should only be generated when the generated property type supports that comparison.
- When `Always use AValue` is false and metadata shows a concrete CLR type, generated properties can usually be used as normal typed C# properties.

### Important AValue Operations Rule

- Generated record properties may be `AValue` instances, especially when `Always use AValue` / AutoValue behavior is enabled.
- `AValue` makes schemaless, mixed-type, sparse Aerospike records feel natural in LINQPad without repeated null checks, casts, type guards, or raw `Aerospike.Client.Value` plumbing.
- Prefer generated record properties first, such as `customer.FirstName`, `invoice.Total`, or `record.Status`.
- When a generated property is `AValue`, use the driver's AValue-aware operations instead of unsafe casts.
- For simple equality, direct comparisons such as `customer.State == "CA"` or `record.Status == "active"` are preferred when the generated property/operator supports the comparison.

#### AValue type inspection

- Use type-inspection properties when the operation depends on the underlying type: `IsString`, `IsNumeric`, `IsInt`, `IsFloat`, `IsBool`, `IsList`, `IsMap`, `IsDictionary`, `IsCDT`, `IsJson`, `IsGeoJson`, `IsDateTime`, `IsDateTimeOffset`, `IsTimeSpan`, `IsKeyValuePair`, `IsEmpty`, and `UnderlyingType`.
- For mixed-type ordering, prefer type checks or `CanConvert<T>()` before numeric/date comparisons.

#### AValue conversion operations

- Use `value.CanConvert<T>()` to test whether an `AValue` can be converted to `T` without throwing.
- Use `value.Convert<T>()` to convert an `AValue` to `T` when conversion is expected to be valid.
- Use `value.Apply<TValue, TResult>(func)` to safely convert an existing non-null `AValue` to `TValue`, execute `func`, and return `TResult`; it returns `default` if conversion or execution fails.
- Use `value.TryApply<TValue, TResult>(func)` when the `AValue` itself may be null; this is the preferred null-safe option in query filters.

#### AValue comparison operations

- `AValue` supports equality and comparison operators such as `==`, `!=`, `<`, `>`, `<=`, and `>=`.
- `AValue` also supports `CompareTo(...)` for comparing against another `AValue`, an Aerospike `Value`, an Aerospike `Key`, or another object.
- Use simple comparison operators when the intent is direct value comparison and the generated property/operator supports it.
- Use `Apply` or `TryApply` when invoking type-specific methods such as `StartsWith`, `Contains`, `ToUpper`, date operations, or numeric calculations.

#### AValue collection, map, JSON, and CDT operations

- Use `Contains(...)`, `ContainsKey(...)`, `FindAll(...)`, `TryGetValue(...)`, and `AValue.MatchOptions` for scalar/list/map/JSON/CDT search scenarios.
- `AValue.MatchOptions` can control value matching, equality matching, dictionary key/value matching, substring matching, exact matching, and regex matching.
- Use `AsEnumerable()`, `AsEnumerable<T>()`, `ToList()`, `ToListItem()`, `ToDictionary()`, `ToDictionary<K,V>()`, `ToCDT()`, `ElementAt(...)`, and `ElementAtOrDefault(...)` for collection, map, JSON, GeoJSON, and CDT exploration.
- Use `Count()` for string or collection counts, noting that non-string/non-collection values may return `-1`.
- Use `ToBin()` when turning an `AValue` back into an Aerospike `Bin` for write operations.
- Use `DebugDump()` when debugging `AValue` metadata such as value, bin name, field name, and detected type.

#### AValue helper extension operations

- Use `ToAValue(...)` to create an `AValue` from a normal value, nullable value, Aerospike `Value`, or Aerospike `Bin`.
- Use `ToAPrimaryKey(...)` to create an `APrimaryKey` from a normal value or Aerospike `Key`.
- Use `ToAValueList()` to create a list of AValues from an `ARecord` or Aerospike `Record`.
- For `IEnumerable<AValue>`, use `OfType<T>()`, `Cast<T>()`, or `Convert<T>()` depending on whether exact type filtering, strict casting, or coercion is desired.

#### Preferred query usage

- In query filters, prefer `TryApply<TValue, bool>(...)` when the bin/property may be missing, null, mixed-type, or AValue-backed.
- Use `Apply<TValue, TResult>(...)` when the `AValue` is expected to exist but the underlying value still needs safe conversion.
- Avoid direct CLR casts such as `(string)customer.FirstName.Value` unless the context clearly says the value is present and has that exact type.
- Avoid calling type-specific CLR methods directly on an `AValue` unless the generated property type is known to be that CLR type.

### Important APrimaryKey / Digest Rule

- `APrimaryKey` is the primary-key companion to `AValue` and supports the same Auto-Value style comparisons and conversions.
- Prefer the generated/default primary-key property first, then `GetPK()` if the generated property is unavailable.
- `APrimaryKey` can represent a user key value, an Aerospike `Key`, a digest-backed key, a byte-array digest, or a hex digest string.
- Records written with send-key disabled may not expose the original user key, but can still be identified by digest.
- A digest identifies the record but is not the original user key value.
- The native Aerospike `Key` instance can be obtained through the `AerospikeKey` property when native API calls require it.
- Digest/user-key comparisons are supported, so primary-key filters can often be written naturally, such as `record.PK == "0x..."` or `record.PK == userKeyValue`.
- Do not treat the primary key as a normal bin unless the context explicitly says the primary key is stored as a bin.

### Important Aerospike Expression Rule

- AValue comparisons and LINQ `where` clauses are client-side after records are returned and materialized by the driver.
- Aerospike filter expressions are server-side and use raw Aerospike bin names plus Aerospike expression APIs.
- Use Aerospike expressions when the user asks for server-side filtering, expression filters, filter expressions, `Query(...)`, CDT/map/list expression filters, or reducing records at the server.
- Use raw bin names inside `Exp.StringBin(...)`, `Exp.IntBin(...)`, `Exp.FloatBin(...)`, `Exp.BoolBin(...)`, `Exp.Bin(...)`, `MapExp`, `ListExp`, and related expression builders.
- Do not use generated record properties inside server-side `Exp.*` expression builders.
- When using AValue expression helpers, use `value.ToExpBin(...)` for the bin reference side and `value.ToExpVal()` for the literal side.
- For straightforward server-side expressions, using raw bin names directly is usually simpler and clearer.
- Do not call `Exp.Build(...)` when passing a `Exp` filter expression to `SetRecords.Query(...)`; the driver builds it into the policy.
- Use operational expressions with `Operate(...)` and `ExpOperation.Read(...)` / `ExpOperation.Write(...)` only when the user asks for expression read/write operations.
- If `Exp` is referenced, it should include a C# `using` alias directive (e.g., `using Exp = Aerospike.Client.Exp;`) for theAerospike native driver's `Aerospike.Client.Exp` class or fully qualify the name as `Aerospike.Client.Exp`.

### Important Primary Key Rule

- When the Aerospike primary key value is required, prefer the generated/default primary-key property when available.
- Example: use `record.{{DefaultASPIKeyName}}` when the `{{DefaultASPIKeyName}}` property exists.
- If no generated/default primary-key property is available, use `record.GetPK()`.
- Do not access the primary key through `record["{{DefaultASPIKeyName}}"]` or another string-indexer expression unless the context explicitly says the primary key is stored as a normal bin.

### Important Normal CLR Dictionary Lookup Rule

- For normal CLR dictionaries (for example `Dictionary<TKey, TValue>`, `IReadOnlyDictionary<TKey, TValue>`, `IDictionary<TKey, TValue>`), do not generate pseudo-overload patterns such as `dictionary.TryGetValue(key, null)` or `dictionary.TryGetValue(key, default)`.
- In LINQ query clauses (`let`, `where`, `select`), avoid `out var` dictionary lookup patterns; prefer helper-based lookup such as `let value = GetValueOrDefault(dictionary, key)` followed by `where value is not null`.
- In statement blocks/lambdas, normal dictionary lookup should use `dictionary.TryGetValue(key, out var value)`.
- Do not confuse normal CLR dictionary lookup with AValue/CDT `TryGetValue(...)` APIs; AValue/CDT `TryGetValue` patterns remain valid only for AValue/document navigation contexts.

### Important LINQ Rule for SetRecords

- Generated Aerospike set objects are `SetRecords` / `SetRecords<T>` instances.
- When using LINQ extension methods that require `IEnumerable<T>` semantics, call `AsEnumerable()` on the set first.
- This applies to LINQ operations such as `Join`, `GroupJoin`, `OrderBy`, `OrderByDescending`, `ThenBy`, `ThenByDescending`, `GroupBy`, `SelectMany`, `Concat`, `Union`, `Distinct`, `Except`, `Intersect`, `ToDictionary`, and similar collection-style LINQ methods.
- The API has native First, FirstOrDefault, Skip, Where, ToList and ToArray functions for 'set' instances(i.e., SetRecords, SetRecords<T>) and those should be used directly. if possible, without using the AsEnumerable() pattern.
- With query syntax, use `from record in NamespaceName.SetName.AsEnumerable()`.
- With method syntax, use `NamespaceName.SetName.AsEnumerable()` as the LINQ source.
- Do not generate `NamespaceName.SetName.Join(...)`, `NamespaceName.SetName.OrderBy(...)`, or `NamespaceName.SetName.GroupBy(...)` directly.
- Instead generate query syntax such as `from record in NamespaceName.SetName.AsEnumerable()` when QuerySyntax is configured.
- Use method syntax such as `NamespaceName.SetName.AsEnumerable().Join(...)` only when MethodSyntax is configured or query syntax cannot express the operation cleanly.
