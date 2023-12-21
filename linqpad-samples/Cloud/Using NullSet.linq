<Query Kind="Statements">
  <Connection>
    <ID>973104d1-5fc3-4e74-a869-59441d5e370d</ID>
    <NamingServiceVersion>2</NamingServiceVersion>
    <Driver Assembly="Aerospike.Database.LINQPadDriver" PublicKeyToken="no-strong-name">Aerospike.Database.LINQPadDriver.DynamicDriver</Driver>
    <DisplayName>Aerospike Cloud (Demo)</DisplayName>
    <DisplayName>Aerospike Cloud (Demo)</DisplayName>
    <DriverData>
      <DBType>Cloud</DBType>
      <Port>4000</Port>
      <TLSOnlyLogin>true</TLSOnlyLogin>
      <SetNamesCloud>PlaylistTrack Track InvoiceLine Album Invoice Artist Playlist CustInvsDoc Customer Genre MediaType Employee DataTypes</SetNamesCloud>
      <ConnectionTimeout>5000</ConnectionTimeout>
      <TotalTimeout>5000</TotalTimeout>
    </DriverData>
  </Connection>
</Query>

aerospike_cloud.NullSet.Where(ns => ns.Aerospike.SetName == "Artist")
	.Dump("Recorset for Artist Set using the NullSet (record view is Dynamic)");
aerospike_cloud.Artist
	.Dump("Recorset for Artist Set using the Set Directly (record view is Record)");