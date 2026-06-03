### Show this AI context as plain text

```csharp
AIContext.ToMarkdown().Dump("Aerospike AI Context Markdown");
```

### Show this AI context as rendered Markdown in LINQPad

```csharp
var markdown = AIContext.ToMarkdown();
Util.Markdown(markdown).Dump("Aerospike AI Context");
```

### Ask LINQPad AI using this context

```csharp
var prompt = AIContext.BuildPrompt("Show me 100 records from the most relevant set.");
var response = await Util.AI.Ask(prompt).GetResponseAsync();
response.Text.Dump("AI-generated LINQPad C#");
```

