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

// Ask AI about a specific namespace/set.
// Change namespaceName and setName to match your connection.
// This uses some fo the AI API... 

var namespaceName = "test";
var setName = "Customer";

var request = LINQPad.Util.ReadLine(
    $"Ask AI about {namespaceName}.{setName}:",
    "Generate a safe read-only query that filters, sorts, and shows 100 useful records from this set.");

if (string.IsNullOrWhiteSpace(request))
    return;

var prompt = AIContext.BuildSetPrompt(
    namespaceName: namespaceName,
    setName: setName,
    userRequest: request);

var response = await LINQPad.Util.AI.Ask(prompt).GetResponseAsync();

response.Text.Dump("AI Response");
