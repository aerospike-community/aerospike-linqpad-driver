# Auto Values (`AValue`)

The complete Auto Values documentation is now organized under [`docs/auto-values/`](docs/auto-values/README.md).

`AValue` and `APrimaryKey` make Aerospike's sparse and mixed-type records easier to explore from LINQPad. They provide missing-value handling, comparisons, conversion, collection and document traversal, search helpers, and Aerospike expression helpers.

## Common patterns

```csharp
!customer.Company.IsEmpty
```

```csharp
customer.FirstName.TryApply<string, bool>(name => name.StartsWith("J"))
```

```csharp
customer.Total.CanConvert<decimal>()
    && customer.Total.Convert<decimal>() > 10m
```

```csharp
customer.Profile.TryGetValue("email", AValue.Empty)
```

## Guide sections

- [Overview and decision guide](docs/auto-values/README.md)
- [Fundamentals and primary keys](docs/auto-values/fundamentals.md)
- [Conversion, comparison, date/time, and type-specific operations](docs/auto-values/conversion-and-comparison.md)
- [Collections, maps, JSON, CDTs, and search](docs/auto-values/collections-and-search.md)
- [LINQ and Aerospike expressions](docs/auto-values/expressions-and-querying.md)
- [Helper reference, common mistakes, and AI prompt guidance](docs/auto-values/reference.md)
