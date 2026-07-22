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
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

// Ask AI for AValue-safe examples using TryApply, Apply, CanConvert, and Convert.
// Use short-form request.

AIContext.SubmitRequestAndCreateQuery("""
Generate LINQPad C# Statements examples for AValue-backed properties.

Use query syntax where practical.
Use test.Customer.AsEnumerable().
Show:
1. A filter using FirstName.TryApply<string,bool>(name => name.StartsWith("M")).
2. A projection using FirstName.Apply<string,int>(name => name.Length).
3. A numeric conversion example using CanConvert<decimal>() and Convert<decimal>().
4. Use generated properties, not string-indexer access.
Limit each example to 100 rows and use Dump().
""");