# LINQPad sample catalog

The sample folder contains runnable LINQPad queries and the demo data used by many examples.

## Before running samples

- Create or choose an Aerospike connection in LINQPad.
- The scripts use a namespace named `test` by default.
- Reattach a sample to your connection when its embedded connection ID does not exist on your computer.
- Review every mutating script before execution.
- Refresh the connection after a script creates sets or changes the observed bin structure.

## Recommended order

1. `Demo/ReadMeFirst.linq`
2. `Demo/Basic Data Types.linq`
3. `Demo/Basic Data Types 2.linq`
4. `Demo/Record Display View.linq`
5. Query, expression, object-mapping, JSON/document, code-generation, and MRT samples as needed
6. `AI/00-ReadMe.linq`, followed by the numbered AI samples

## Folders

| Folder | Purpose |
|---|---|
| [`Demo/`](Demo/README.md) | Core driver, querying, data types, writes, POCO, document, code-generation, and MRT examples |
| [`AI/`](AI/README.md) | Connection-aware LINQPad AI prompt and generated-query examples |
| `DemoDBJson/` | JSON files and a query used to create/export the sample data model |
| `aerospike-mrt-patterns.json` | Additional MRT-related sample data/patterns |

## Important behavior

Many samples create, modify, or remove records. They are demonstration scripts, not production migration tools. Read the top comment and the `Main` method before running a sample.

[Back to the project README](../README.md)
