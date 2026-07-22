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

// This sample demonstrates how to translate a LINQ query into Aerospike native API calls using server expressions.
//

var userRequest =
"""
Can you translate this LINQ query into native API code, use filter expressions on invoice set:

  test.Customer.AsEnumerable()
        .GroupJoin(test.Invoice.AsEnumerable(),
                    c => c.PK,
                    i => i.CustomerId,
                    (cust, invoices) => new {Customer = cust, Invoices=invoices})
""";

await AIContext.SubmitRequestAndCreateQueryAsync(userRequest);
