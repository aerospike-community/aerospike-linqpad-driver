<Query Kind="Statements">
  <Connection>
    <ID>973104d1-5fc3-4e74-a869-59441d5e370d</ID>
    <NamingServiceVersion>2</NamingServiceVersion>
    <Driver Assembly="Aerospike.Database.LINQPadDriver" PublicKeyToken="no-strong-name">Aerospike.Database.LINQPadDriver.DynamicDriver</Driver>
    <Server>localhost</Server>
    <DisplayName>Aerospike Cluster (Demo)</DisplayName>
    <DriverData>
      <UseExternalIP>false</UseExternalIP>
      <Debug>false</Debug>
      <RecordView>Record</RecordView>
      <DocumentAPI>true</DocumentAPI>
    </DriverData>
  </Connection>
</Query>

// Ask AI for C# Statements code. If the response contains C#,
// create a new .linq file with this query's connection header and dump a link to open it.
// Uses short form request.

AIContext.SubmitRequestAndCreateQuery("""
Generate a query-syntax join between test.Customer and test.Invoice, limit to 100 rows, and Dump the results.
""");


