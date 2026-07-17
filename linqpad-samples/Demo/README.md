# Core driver samples

These scripts demonstrate the non-AI driver APIs. Run `ReadMeFirst.linq` before samples that depend on the demo sets.

## Setup and fundamentals

| Sample | Purpose | Changes data? |
|---|---|---|
| `ReadMeFirst.linq` | Introduces the samples and prepares the demonstration environment | Yes |
| `Basic Data Types.linq` | Creates mixed-type records and demonstrates bins, keys, conversions, and generated properties | Yes |
| `Basic Data Types 2.linq` | Adds list/map variants and demonstrates `AValue` search and type helpers | Yes |
| `Record Display View.linq` | Compares Record, Dynamic, and detailed output behavior | May create/read demo data |
| `Using NullSet.linq` | Shows access to records that do not have a set name | Read-oriented |

## Querying and expressions

| Sample | Purpose |
|---|---|
| `Linq Join Customer and Invoice.linq` | Client-side LINQ join across two generated sets |
| `LinqWhere-AerospikePK.linq` | Primary-key filtering and native/driver key operations |
| `LinqWhere-AerospikeExpressions.linq` | LINQ predicates versus server-side Aerospike expressions |

## Objects, JSON, and documents

| Sample | Purpose | Changes data? |
|---|---|---|
| `POCO.linq` | Reads and writes mapped C# object graphs | Yes |
| `POCO-Classes.linq` | Supporting class definitions and mapping examples | Depends on use |
| `CDT-Json-Docs.linq` | Lists, maps, JSON, documents, and nested expression operations | Yes |
| `Create CustInvsDoc set.linq` | Creates the customer/invoice document-style set | Yes |

## Writes and code generation

| Sample | Purpose | Changes data? |
|---|---|---|
| `Put-Aerospike.linq` | Insert and update patterns through the driver and client APIs | Yes |
| `Generate Code.linq` | Generates driver or native API code from records/result sets | Normally read-only until generated code is run |
| `MRT.linq` | Multi-record transaction creation, operations, commit, abort, and state handling | Yes |

## Dependency notes

- `Basic Data Types 2.linq` expects `Basic Data Types.linq` to have created the `DataTypes` set.
- Join and POCO samples expect the demo customer/invoice/music sets.
- Document examples expect the appropriate document-style demo set.
- A connection refresh may be required after setup so the generated set and bin properties compile.

## Safety

Use a non-production namespace. Several scripts call `Truncate`, `Put`, `WriteObject`, `Delete`, or transaction commit APIs.

[Back to the sample catalog](../README.md)
