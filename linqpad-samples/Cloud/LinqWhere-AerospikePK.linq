<Query Kind="Statements">
  <Connection>
    <ID>973104d1-5fc3-4e74-a869-59441d5e370d</ID>
    <NamingServiceVersion>2</NamingServiceVersion>
    <Driver Assembly="Aerospike.Database.LINQPadDriver" PublicKeyToken="no-strong-name">Aerospike.Database.LINQPadDriver.DynamicDriver</Driver>
    <Persist>false</Persist>    
    <DisplayName>Aerospike Cloud (Demo)</DisplayName>
    <DriverData>
      <DBType>Cloud</DBType>
      <Port>4000</Port>
      <TLSOnlyLogin>true</TLSOnlyLogin>
      <SetNamesCloud>PlaylistTrack Track InvoiceLine Album Invoice Artist Playlist CustInvsDoc Customer Genre MediaType Employee DataTypes</SetNamesCloud>
    </DriverData>
  </Connection>
</Query>

/*
This example compares the use of a Linq Where clause and Aerospike Get function to obtain a record based on the Primary Key.

Note: this is not meant to be used in a production environment and there can be performance implications using either this LinqPad driver and expresions! 
*/
aerospike_cloud.Customer.Where(c => c.PK == 20).Dump("Using Linq");
aerospike_cloud.Customer.Get(20).Dump("Using Aerospike Get");