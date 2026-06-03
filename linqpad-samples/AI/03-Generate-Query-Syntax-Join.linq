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

// Strongly asks AI to generate query-syntax LINQ, not method-chain Join(...).

var request = """
Generate a LINQPad C# Statements query using C# query syntax.

Join test.Customer and test.Invoice.
Use test.Customer.AsEnumerable() and test.Invoice.AsEnumerable().
Join customer.PK to invoice.CustomerId.
Project customer name/email and invoice date/total/city/country.
Limit to 100 rows.
Use Dump().
Do not use .Join(...). Use a query-syntax join clause.
""";

var prompt = AIContext.BuildPrompt(request);

var response = await LINQPad.Util.AI.Ask(prompt).GetResponseAsync();

response.Text.Dump("AI-generated query-syntax join");
