# LINQPad AI samples

These numbered samples show how the Aerospike driver supplies connection and schema context to LINQPad AI.

## Requirements

- LINQPad AI is configured with a provider.
- The query is attached to an Aerospike driver connection.
- Demo sets exist when a prompt refers to `test.Customer`, `test.Invoice`, or other sample sets.
- The current connection metadata is refreshed.

Run `00-ReadMe.linq` first.

## Samples

| Sample | Demonstrates |
|---|---|
| `00-ReadMe.linq` | Overview, prerequisites, and sample order |
| `01-Ask-AI-About-Connection.linq` | Builds a prompt using the active connection and asks a general question |
| `02-Ask-AI-About-Specific-Set.linq` | Uses `BuildSetPrompt(...)` to focus on one namespace and set |
| `03-Generate-Query-Syntax-Join.linq` | Requests a query-syntax join and explicitly avoids method-chain `Join(...)` |
| `04-AValue-TryApply-Examples.linq` | Requests safe `AValue` patterns using `TryApply`, `Apply`, `CanConvert`, and `Convert` |
| `05-Ask-AI-For-Filter-Expression.linq` | Requests a server-side Aerospike expression with raw bin names |
| `06-Explain-Existing-Query.linq` | Asks AI to explain execution location, `AsEnumerable()`, and `AValue` behavior |
| `07-Generate-Openable-CSharp-Statements-Query.linq` | Submits a request and creates an openable `.linq` query from returned C# |
| `08-Dump-AI-Context-Markdown.linq` | Builds and renders the current AI context for diagnostics |

## Two API styles

### Build and submit manually

```csharp
var prompt = AIContext.BuildPrompt(request);
var response = await LINQPad.Util.AI.Ask(prompt).GetResponseAsync();
response.Text.Dump();
```

Use this when you need to inspect the prompt or response.

### Submit and create a query

```csharp
AIContext.SubmitRequestAndCreateQuery(request);
```

Use this when the expected response is C# Statements code that should be opened as a new LINQPad query.

## Adapting the samples

- Change `test` to the generated property for your namespace.
- Change set names to sets that exist in the active connection.
- Keep raw Aerospike bin names inside `Exp.*Bin(...)` calls.
- Keep generated property names in driver-mode LINQ.
- Add `mode:native` when the result must use the native C# client.
- Keep initial requests read-only and bounded.

## Related documentation

- [AI feature guide](../../docs/ai-features.md)
- [Auto Values](../../docs/auto-values/README.md)
- [AI context internals](../../docs/ai-context-internals.md)

[Back to the sample catalog](../README.md)
