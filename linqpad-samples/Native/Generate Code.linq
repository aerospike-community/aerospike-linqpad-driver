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

Demo.Track.Take(3).ToAPICode().Dump("Generated Get and Put Extended API for three rows");
Demo.Track.Take(3).ToAPICode(useAerospikeAPI: true).Dump("Generated Get and Put Aerospike API for three rows");
Demo.Track.Take(3).ToAPICodeBatch().Dump("Generated Batch Extended APIfor three rows");
Demo.Track.Take(3).ToAPICodeBatch(useAerospikeAPI: true).Dump("Generated Batch Aerospike API for three rows");