# Aerospike LINQPad AI Sample Scripts

These sample `.linq` scripts demonstrate how to use the Aerospike LINQPad driver's `AIContext` helpers with LINQPad AI.

## AI Prompt and Join (Advance)

This allows you to enter a request or use the example. The example performs a three-way join and provides a link to run the join in LINQPad.

This also loads the demo's cluster data if not already loaded.

## Samples

1. `01-Ask-AI-About-Connection.linq` - Prompts for a general AI request about the current Aerospike connection.
2. `02-Ask-AI-About-Specific-Set.linq` - Prompts for an AI request scoped to a namespace/set.
3. `03-Generate-Query-Syntax-Join.linq` - Strongly requests query-syntax LINQ for a Customer/Invoice join.
4. `04-AValue-TryApply-Examples.linq` - Requests examples using `TryApply`, `Apply`, `CanConvert`, and `Convert`.
5. `05-Ask-AI-For-Filter-Expression.linq` - Requests a server-side Aerospike filter-expression query using `SetRecords.Query(...)`.
6. `06-Explain-Existing-Query.linq` - Prompts AI to explain an existing LINQPad/Aerospike query.
7. `07-Generate-Openable-CSharp-Statements-Query.linq` - Detects generated C# code and creates a new `.linq` file with the same connection header.
8. `08-Dump-AI-Context-Markdown.linq` - Renders the current `AIContext` as Markdown in LINQPad.

## Notes

- These samples use `Query Kind="Statements"` by default.
- Set-specific examples use placeholder names such as `test`, `Customer`, and `Invoice`.
- Update namespace/set/bin names to match your Aerospike connection.
- The generated-query sample must be saved before running so it can copy the current `.linq` connection header.
