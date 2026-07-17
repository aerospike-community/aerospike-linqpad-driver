# AI-assisted Aerospike queries and code

The driver can build a connection-aware Markdown context for LINQPad AI. That context describes the active connection, namespaces, sets, observed bins, secondary indexes, Auto Value behavior, query-syntax preferences, and rules for driver or native-client code.

The result is still generated code: review it before execution.

## What the AI workflow can do

- Generate LINQPad C# Statements queries against generated sets.
- Generate query syntax or method syntax.
- Create `AValue`-safe filters and projections.
- Explain an existing Aerospike LINQPad query.
- Generate server-side Aerospike expressions.
- Translate driver-based queries to native Aerospike C# client code.
- Generate a new openable `.linq` file from a code response.
- Build or inspect the exact Markdown context sent to AI.

## Prerequisites

1. Configure a supported AI provider in LINQPad.
2. Connect the query to an Aerospike driver connection.
3. Confirm the connection metadata has been refreshed.
4. Use a bounded, read-only request for the first test.

The media folder includes a provider/settings walkthrough at `media/AI/LINQPadAISummary/AISettings.mp4`.

## Fastest path

```csharp
AIContext.SubmitRequestAndCreateQuery("""
Generate a read-only LINQPad C# Statements query.
Use test.Customer.AsEnumerable().
Show PK, FirstName, LastName, and Email.
Sort by LastName and FirstName.
Limit to 100 rows and call Dump().
""");
```

The helper submits the request, extracts generated C# when present, creates a `.linq` query with the current connection header, and provides a link to open it.

## Build and submit a prompt manually

```csharp
var request = """
Explain the safest way to filter test.Customer where FirstName starts with J.
Compare client-side LINQ with a server-side Aerospike expression.
""";

var prompt = AIContext.BuildPrompt(request);
var response = await LINQPad.Util.AI.Ask(prompt).GetResponseAsync();

response.Text.Dump("AI response");
```

Use `BuildSetPrompt(namespaceName, setName, userRequest, ...)` to focus metadata on one set.

## Driver mode and native mode

### LINQPad-driver mode

Driver mode uses generated sets, generated properties, `AValue`, `APrimaryKey`, and driver extension methods.

```csharp
var rows =
    from customer in test.Customer.AsEnumerable()
    where customer.FirstName.TryApply<string, bool>(
        name => name.StartsWith("J", StringComparison.OrdinalIgnoreCase))
    select customer;

rows.Take(100).Dump();
```

### Native Aerospike client mode

Native mode uses `AerospikeClient`, native policies, raw namespace/set/bin names, `Record.GetValue(...)`, and `Exp.Build(...)`.

```csharp
using Aerospike.Client;

var policy = new ScanPolicy
{
    filterExp = Exp.Build(
        Exp.RegexCompare("^J", RegexFlag.ICASE, Exp.StringBin("FirstName")))
};
```

Native code should not use generated sets, `AValue`, `APrimaryKey`, or generated record properties.

A request can explicitly select a mode with a mode override such as `mode:driver` or `mode:native`.

## Auto Value guidance for generated code

Good driver-mode prompts encourage these patterns:

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

See [Auto Values](auto-values/README.md) for the complete behavior.

## Context options and profiles

`AerospikeAIContextOptions` controls context size and content. Important options include:

| Option | Purpose |
|---|---|
| `ContextProfile` | Selects Full, RulesOnly, SchemaOnly, or Debug behavior |
| `LinqSyntaxPreference` | Chooses query syntax or method syntax guidance |
| `IncludeDriverGuide` | Includes driver usage rules |
| `IncludeClusterSummary` | Includes compact connection and cluster information |
| `IncludeNamespaces`, `IncludeSets`, `IncludeBins` | Controls generated schema metadata |
| `IncludeSecondaryIndexes`, `IncludeUdfs` | Includes additional discovered objects |
| `IncludeExamples` | Includes mode-appropriate examples |
| `PreferSchemaOverExamples` | Preserves metadata before examples when the context must be reduced |
| `MaxNamespaces`, `MaxSetsPerNamespace`, `MaxBinsPerSet` | Bounds schema expansion |
| `MaxChars` | Maximum generated context size |
| `DumpTruncationWarning` | Displays a warning when context is truncated |
| `IncludeContextBuildReport` | Adds diagnostic information about included sections |
| `NamespaceName`, `SetName` | Focuses the context on a specific source |

Inspect the build result when prompt quality is uncertain:

```csharp
var result = AIContext.BuildMarkdown(new AerospikeAIContextOptions
{
    ContextProfile = AerospikeAIContextProfile.Debug,
    IncludeContextBuildReport = true,
    MaxChars = 100_000
});

result.WasTruncated.Dump("Was truncated");
result.Warnings.Dump("Warnings");
LINQPad.Util.Markdown(result.Markdown).Dump("Context");
```

## Request-writing guidelines

A useful request identifies:

- Driver or native mode.
- Namespace and set names.
- Whether filtering must be server-side.
- Required fields and joins.
- Expected result limit.
- Query syntax or method syntax preference.
- Whether the result should only explain code or create an openable query.

Example:

```text
mode:driver
Generate C# Statements using LINQ query syntax.
Join test.Customer and test.Invoice on customer.PK and invoice.CustomerId.
Project customer name, invoice date, total, city, and country.
Limit to 100 rows and call Dump().
Do not perform writes.
```

Comment-only lines can be included in requests and are removed before submission.

## Safety

- Ask for read-only code by default.
- Bound exploratory output with `Take(...)`.
- Preview data before a delete or update.
- Review raw bin names in expressions.
- Confirm the current connection and namespace.
- Require explicit intent for destructive operations.
- Treat generated native client construction and credentials as code that must be adapted.

## Samples

Start with [the AI sample README](../linqpad-samples/AI/README.md). The scripts cover:

1. Active-connection questions.
2. Set-focused prompts.
3. Query-syntax joins.
4. `AValue` operations.
5. Server-side expression generation.
6. Query explanation.
7. Creating an openable `.linq` file.
8. Dumping the current context.

## Included media

| Path | Content |
|---|---|
| `media/AI/LINQPadAIOV.mp4` | AI feature overview video |
| `media/AI/aerospike_linqpad_ai_features_overview_server_expressions.pptx` | Overview presentation with server-side-expression examples |
| `media/AI/ExDataModel/` | Sample schema and data-model diagrams |
| `media/AI/ExCustwInvwArtwAlbwTrackPusQuery/` | Query-syntax purchase-path example |
| `media/AI/ExCustwInvwArtwAlbwTrackPusMethod/` | Method-syntax purchase-path example |
| `media/AI/ExCustwInvwArtwAlbwTrackPusNative/` | Native-client purchase-path example |
| `media/AI/ExQurtywExpressions/` | Driver query with expressions |
| `media/AI/ExNativewExpressions/` | Native-client expression example |
| `media/AI/ExExplainLinqQuery/` | Existing-query explanation example |
| `media/AI/ExTransformLINQQuerytoNative/` | Driver-to-native translation example |
| `media/AI/LINQPadAISummary/` | LINQPad AI provider and settings media |

## Implementation documentation

The Markdown in `AIContext/` is embedded into the driver assembly. It is not general user documentation; it supplies system instructions, rules, examples, and footer checks used to build prompts. See [AI context internals](ai-context-internals.md).

[Back to the documentation index](README.md)
