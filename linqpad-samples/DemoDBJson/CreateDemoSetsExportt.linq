<Query Kind="Program">
  <Connection>
    <ID>973104d1-5fc3-4e74-a869-59441d5e370d</ID>
    <NamingServiceVersion>2</NamingServiceVersion>
    <Driver Assembly="Aerospike.Database.LINQPadDriver" PublicKeyToken="no-strong-name">Aerospike.Database.LINQPadDriver.DynamicDriver</Driver>
    <Server>localhost</Server>
    <Persist>true</Persist>
    <DisplayName>Aerospike Cluster (Local)</DisplayName>
    <DriverData>
      <UseExternalIP>false</UseExternalIP>
      <Debug>false</Debug>
      <RecordView>Record</RecordView>
      <DocumentAPI>true</DocumentAPI>
      <AlwaysUseAValues>false</AlwaysUseAValues>
    </DriverData>
  </Connection>
  <NuGetReference>Common.Functions</NuGetReference>
  <NuGetReference>Common.Path</NuGetReference>
  <Namespace>Common</Namespace>
</Query>

void Main()
{
	var path = new Common.File.FilePathAbsolute(@".\DemoDBJson\*.*");
	var files = path.GetWildCardMatches();
	
	Demo.Truncate();
	
	foreach (var file in files)
	{
		if(file.Name == "aerospike") continue;
		
		Demo.FromJson(file.Name, ((Common.IFilePath) file).ReadAllText()).Dump(file.Name);
	}
	
	Demo.Export(@"\DemoDBJson\aerospike.json");
}
