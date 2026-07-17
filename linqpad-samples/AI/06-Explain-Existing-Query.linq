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

// Paste a query and ask AI to explain it in Aerospike LINQPad-driver terms.
// This demonstrates some of the LINQPad AI API...

var queryToExplain = LINQPad.Util.ReadLine(
"Paste the LINQPad/Aerospike query to explain:",
"""
from customer in test.Customer.AsEnumerable()
where customer.FirstName.TryApply<string, bool>(name => name.StartsWith("J"))
select customer
""");

if (string.IsNullOrWhiteSpace(queryToExplain))
    return;

var request = $"""
Explain this LINQPad Aerospike query.

Focus on:
- Whether it is client-side LINQ or server-side Aerospike expression logic.
- Why AsEnumerable() is used.
- How AValue/TryApply affects null, missing, and mixed-type values.
- Whether generated properties are used correctly.
- Any safety or performance considerations.

Query:
{queryToExplain}
""";

//Build Request with Aerospike AI Context (includes metadata about the connection, etc.)
var prompt = AIContext.BuildPrompt(request);

//Send request to AI and get response (async)
var response = await LINQPad.Util.AI.Ask(prompt).GetResponseAsync();

response.Text.Dump("AI Explanation");
