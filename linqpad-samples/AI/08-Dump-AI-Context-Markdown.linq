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

// Dump the current AI context.
// Useful for verifying what the AI sees before asking a question.

var options = new AerospikeAIContextOptions
{
    IncludeDriverGuide = true,
    IncludeClusterSummary = true,
    IncludeNamespaces = true,
    IncludeSets = true,
    IncludeBins = true,
    IncludeSecondaryIndexes = true,
    IncludeUdfs = false,
    IncludeExamples = true,
    ForceRefreshMetadata = false,
    MaxNamespaces = 25,
    MaxSetsPerNamespace = 25,
    MaxBinsPerSet = 75,
    MaxChars = 40_000
};

var markdown = AIContext.ToMarkdown(options);

LINQPad.Util.Markdown(markdown).Dump("Aerospike AI Context");
