# Aerospike LINQPad AI features

The full AI guide has moved to [`docs/ai-features.md`](docs/ai-features.md).

## Quick example

```csharp
AIContext.SubmitRequestAndCreateQuery("""
Generate a read-only query against test.Customer.
Use LINQ query syntax and generated properties.
Limit to 100 rows and call Dump().
""");
```

The driver builds a prompt from the active Aerospike connection, discovered namespaces, sets, bins, indexes, Auto Value rules, and generation-mode guidance.

Read next:

- [AI feature guide](docs/ai-features.md)
- [AI samples](linqpad-samples/AI/README.md)
- [AI context internals](docs/ai-context-internals.md)
- [Auto Values](docs/auto-values/README.md)

Generated code must be reviewed before execution, particularly writes, deletes, imports, and native-client connection code.
