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
  </Connection>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

// Ask AI to generate a server-side Aerospike filter-expression query.
// This should use SetRecords.Query(...) and raw Aerospike bin names inside Exp.*Bin(...).

AIContext.SubmitRequestAndCreateQuery("""
Generate a LINQPad C# Statements query using an Aerospike server-side filter expression.

Use test.Customer.Query(filterExpression).
Filter where State == "CA" and Company bin exists.
Use Aerospike.Client.Exp APIs.
Use raw bin names inside Exp.StringBin and Exp.BinExists.
Do not use a LINQ where clause for the State/Company filtering.
After Query(...), project PK, FirstName, LastName, Company, State.
Limit to 100 rows and Dump().
""");
