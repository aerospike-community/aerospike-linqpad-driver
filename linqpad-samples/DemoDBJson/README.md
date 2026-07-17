# Demo database JSON files

This folder contains the JSON source files used by the demo-data creation/export workflow. The files model customers, invoices, invoice lines, artists, albums, tracks, playlists, employees, genres, and media types.

`CreateDemoSetsExportt.linq` uses these files to create or export the sample sets. Review its configured paths, namespace, and set names before running it.

These files are sample data, not a schema contract. Aerospike records remain schemaless, and the generated LINQPad properties reflect the records discovered through the active connection.

[Back to the sample catalog](../README.md)
