<Query Kind="Statements">
  <Namespace>System</Namespace>
  <Namespace>System.IO</Namespace>
</Query>

/*
Aerospike LINQPad AI Sample Scripts
===================================

These sample '.linq' scripts demonstrate how to use the Aerospike LINQPad 9 driver's
AIContext helpers with LINQPad AI.

The samples are designed to show how natural-language requests can produce useful
Aerospike LINQPad code, including:

- LINQ query-syntax scripts against generated LINQPad driver sets.
- AValue-aware filters that handle null, missing, and mixed-type values.
- Explanations of existing LINQPad Aerospike queries.
- Native Aerospike C# client API translations.
- Server-side Aerospike expression filters.
- Complex joins across Customer, Invoice, InvoiceLine, Track, Album, and Artist.
- Nested CDT/list/map traversal, including invoice-line TrackId searches.

Overview
--------

The Aerospike LINQPad 9 AI workflow is intended to help a user move from a natural
language request to runnable LINQPad C# Statements code.

Typical workflow:

1. Ask a natural-language question.
2. AIContext supplies connection metadata, driver rules, AValue guidance, and examples.
3. LINQPad AI generates C# Statements code.
4. The helper script can display the response, create a new .linq query, and preserve
the current connection header.
5. The user reviews and runs the generated query in LINQPad.

Key capabilities demonstrated by these samples:

- Query generation from natural-language requests.
- Query explanation and code review.
- LINQPad 9 driver mode using generated sets and generated properties.
- Native Aerospike C# client API mode.
- AValue-safe filtering and projection.
- Server-side expression filtering.
- Complex enrichment joins.
- Nested document/CDT traversal.

Existing Samples
----------------

1. '01-Ask-AI-About-Connection.linq'
Prompts for a general AI request about the current Aerospike connection.

2. '02-Ask-AI-About-Specific-Set.linq'
Prompts for an AI request scoped to a namespace/set.

3. '03-Generate-Query-Syntax-Join.linq'
Strongly requests query-syntax LINQ for a Customer/Invoice join.

4. '04-AValue-TryApply-Examples.linq'
Requests examples using 'TryApply', 'Apply', 'CanConvert', and 'Convert'.

5. '05-Ask-AI-For-Filter-Expression.linq'
Requests a server-side Aerospike filter-expression query using 'SetRecords.Query(...)'.

6. '06-Explain-Existing-Query.linq'
Prompts AI to explain an existing LINQPad/Aerospike query.

7. '07-Generate-Openable-CSharp-Statements-Query.linq'
Detects generated C# code and creates a new '.linq' file with the same connection header.

8. '08-Dump-AI-Context-Markdown.linq'
Renders the current 'AIContext' as Markdown in LINQPad.

9. '09-CDT-Nested-Query.linq'
Demonstrates how to generate a query where the data is nested within multiple maps (documents).

Recommended Additional Samples
------------------------------

The following sample scripts are recommended additions based on the LINQPad AI
overview deck and demo workflow.

10. '10-Generate-Customer-Invoice-Artist-Purchase-Query.linq'

Purpose:
Generate a LINQ query-syntax script that returns customers, their invoices, and
the artists associated with the tracks they purchased.

Recommended request:

AIContext.SubmitRequestAndCreateQuery(
"""
I would like a list of customers with invoices and the associated artists whose
tracks they purchased.

Please use LINQ query syntax.
"""
);

Expected AI behavior:
- Use LINQPad-driver mode.
- Use generated sets such as Customer, Invoice, InvoiceLine, Track, Album, and Artist
when those sets are available in the current connection metadata.
- Use generated properties instead of string-indexer access when possible.
- Use '.AsEnumerable()' before collection-style LINQ joins on SetRecords.
- Generate a bounded result set with 'Take(...)'.
- Use 'Dump()' for output.

11. '11-Explain-AValue-TryApply-Customer-Query.linq'

Purpose:
Ask AI to explain an existing LINQPad Aerospike query and identify whether the
query runs client-side or server-side.

Recommended request:

AIContext.SubmitRequestAndCreateQuery(
"""
Explain this LINQPad Aerospike query.

Query:

from customer in test.Customer.AsEnumerable()
where customer.FirstName.TryApply<string, bool>
  (name => name.StartsWith("J"))
  select customer

  Focus on:
  - Whether this is client-side LINQ or server-side Aerospike expression logic.
  - Why AsEnumerable() is used.
  - How AValue and TryApply affect null, missing, and mixed-type values.
  - Whether generated properties are used correctly.
  - Any safety or performance considerations.
"""
);

Expected AI behavior:
  - Explain that this is client-side LINQ after records are materialized by the
  LINQPad driver.
  - Explain that 'AsEnumerable()' switches to normal LINQ-to-Objects behavior for
  collection-style operations.
  - Explain that 'TryApply<string, bool>
    (...)' safely converts the AValue-backed
    property to string before calling 'StartsWith'.
    - Explain that missing, null, or mixed-type values simply do not match instead
    of throwing.
    - Explain that 'customer.FirstName' is a generated property and is preferred over
    string-indexer access when available.
    - Call out that server-side filtering may be more efficient for large sets.

12. '12-Translate-TryApply-Query-To-Native-Server-Expression.linq'

Purpose:
  Ask AI to translate a LINQPad-driver client-side AValue/TryApply query into
  native Aerospike C# client API code using server-side expressions.

Recommended request:

AIContext.SubmitRequestAndCreateQuery(
"""
  Query:

  from customer in test.Customer.AsEnumerable()
  where customer.FirstName.TryApply<string, bool>
    (name => name.StartsWith("J"))
    select customer

    I want to translate this to use the Aerospike native API with server-side expressions.
    """
    );

    Expected AI behavior:
    - Use native Aerospike C# client API mode.
    - Use 'AerospikeClient', 'ClientPolicy', 'ScanPolicy' or 'QueryPolicy', and raw
    namespace/set/bin names.
    - Use 'record.GetValue("FirstName")' when reading returned records.
    - Use a server-side expression such as 'Exp.RegexCompare(...)' or equivalent
    expression logic for "starts with J".
    - Assign the built expression with 'Exp.Build(...)' to the native policy.
    - Do not use LINQPad-driver APIs such as 'test.Customer', 'SetRecords',
    'AValue', 'PK', or generated record properties in the native implementation.
    

    Notes
    -----

    - These samples use 'Query Kind="Statements"' by default.
    - The existing samples use Aerospike connection 'Aerospike Cluster (Demo)'.
      This connection is created using default properties (if it doesn't already exist). You may need to point this connection to the proper DB location.
      You can, also, select different Aerospike connection.
    - Ensure the sample scripts are pointed to the current Aerospike connection.
    - These samples use the Demo Database which can be installed by running the "ReadMeFirst.linq" script in the samples "Demo" folder.
    - Review generated scripts before running, especially if the request involves writes,
    deletes, truncates, or long-running scans.

    Comment Lines
    --------
    You can add comment lines to your AI request. These lines will be removed/ignored.
    Any line that starts with any of the following can be used for comments:

    - '//'
    - '#'
    - '--'

    Example:

    """
    // working note: test with server-side Exp
    # customer query variant
    -- do not send this line

    from customer in test.Customer.AsEnumerable()
    where customer.FirstName.TryApply<string, bool="">(name => name.StartsWith("J"))
  select customer
"""
  
In this example, the three comment lines will be stripped before the request is
processed

*/

