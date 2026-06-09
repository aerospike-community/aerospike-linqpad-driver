<!-- AIContext-Version: 2026.06.08.3; Change: runtime AI-context version source, LINQPad output display, and generated script provenance comments. -->

### Show this AI context as plain text

```csharp
AIContext.ToMarkdown().Dump("Aerospike AI Context Markdown");
```

### Show this AI context as rendered Markdown in LINQPad

```csharp
var markdown = AIContext.ToMarkdown();
LINQPad.Util.Markdown(markdown).Dump("Aerospike AI Context");
```

### Ask LINQPad AI using this context via API

```csharp
var prompt = AIContext.BuildPrompt("Show me 100 records from the most relevant set.");
var response = await LINQPad.Util.AI.Ask(prompt).GetResponseAsync();
response.Text.Dump("AI-generated LINQPad C#");
```

### Ask LINQPad AI using this context with hyperlinking

```csharp
AIContext.SubmitRequestAndCreateQuery(
"""
Show me 100 records from the most relevant set.
"""
);
```

