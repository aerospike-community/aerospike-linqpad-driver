# Documentation index

This documentation is organized by task. Start with the short guide that matches what you are trying to do, then follow the links to detailed references and runnable LINQPad samples.

For a visual introduction to the project, begin with the [feature overview](../FEATURES.md).

## New users

1. [Getting started](getting-started.md)
2. [Connection configuration](connection-configuration.md)
3. [Sample catalog](../linqpad-samples/README.md)
4. [Querying and records](querying-and-records.md)

## Core guides

| Guide | Covers |
|---|---|
| [Querying and records](querying-and-records.md) | Generated sets, records, keys, LINQ, server-side expressions, secondary indexes, null sets |
| [Auto Values](auto-values/README.md) | `AValue`, `APrimaryKey`, conversion, comparison, maps, lists, JSON/CDT traversal, expression helpers |
| [Data mapping and documents](data-mapping-and-documents.md) | POCO serialization, constructor and bin attributes, JSON, document APIs |
| [Data operations](data-operations.md) | Put, delete, batch, operate, import/export, UDFs, multi-record transactions |
| [Advanced features](advanced-features.md) | Native client access, code generation, metadata, display views, policy overrides |
| [AI features](ai-features.md) | Connection-aware prompts, generated queries, mode selection, safety, samples and media |

## Reference and contributor information

| Document | Purpose |
|---|---|
| [AI context internals](ai-context-internals.md) | Explains the embedded Markdown in `AIContext/` and its runtime role |
| [Building and packaging](development.md) | Build, pack, package layout, and documentation packaging notes |
| [Media catalog](../media/README.md) | Screenshots, AI demonstrations, diagrams, presentations, and video assets |
| [Hosted API documentation](https://aerospike-community.github.io/aerospike-linqpad-driver/) | Generated class and member reference |
| [`AIContext/aerospike_linqpad_ai_context_phase_ii_plan.md`](../AIContext/aerospike_linqpad_ai_context_phase_ii_plan.md) | AI-context architecture plan |
| [`AIContext/aerospike_linqpad_ai_context_copilot_handoff.md`](../AIContext/aerospike_linqpad_ai_context_copilot_handoff.md) | Detailed implementation handoff and validation rules |

## Source folders

- `docs/` contains user-facing and contributor documentation.
- `linqpad-samples/` contains runnable `.linq` files and demo data.
- `media/` contains screenshots, videos, diagrams, and presentation assets.
- `AIContext/` contains Markdown embedded into the driver assembly and used to construct AI prompts.

[Back to the project README](../README.md)
