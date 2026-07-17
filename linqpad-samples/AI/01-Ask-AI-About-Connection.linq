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

// Ask LINQPad AI a general question about the current Aerospike connection.
// This uses AIContext.BuildPrompt(...) and the driver's current metadata/context.
// This demonstrates some of the LINQPad AI API...

var request = LINQPad.Util.ReadLine(
"Ask AI about this Aerospike connection:",
"Generate a safe read-only query that explores the most useful sets and shows 100 rows.");

if (string.IsNullOrWhiteSpace(request))
return;

//Build Request with Aerospike AI Context (includes metadata about the connection, etc.)
var prompt = AIContext.BuildPrompt(request);

//Send request to AI and get response (async)
var response = await LINQPad.Util.AI.Ask(prompt).GetResponseAsync();

response.Text.Dump("AI Response");
