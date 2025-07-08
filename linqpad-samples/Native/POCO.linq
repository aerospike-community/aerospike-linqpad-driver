<Query Kind="Program">
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

#load ".\POCO-Classes"
/*
This program will read the CustInvsDoc (Customer-Invoice documents) set (create in "Create CustInvsDoc set" script) and cast the result into a .Net list of user defined classes by means of the driver’s ORM.
     
Note: this is not meant to be used in a production environment and there can be performance implications using this LinqPad driver!  
*/
void Main()
{
	//If true, the read record is re-written with a different PK to demo how PICO are written back to the DB.
	var reWriteAsDiffPK = true;
	
	var customerInvoices = test.CustInvsDoc
							.AsEnumerable()
							.Select(cid => cid.Cast<Customer>(cid.PK)).ToArray();

	customerInvoices.OrderBy(i => i.LastName)
					.ThenBy(i => i.LastName)
					.ThenBy(i => i.Id)
					.Dump("Customer Invoices class instances created from the DB", 0);
	
	
	if(reWriteAsDiffPK)
	{
		var newRecs = new List<long>();
		
		foreach (var element in customerInvoices)
		{
			var newPK = element.Id * 1000; //Change the PK
			
			//Create DB Records from the Customer instances
			test.CustInvsDoc.WriteObject(newPK, element);
			newRecs.Add(newPK); //Removed new record later...
		}
		
		//Note that bin "Fax" is present in the DB (and list as a known bin in the Set's Bin list pane) but not as a property in the Customer Class
		//	Also bin "Company" is present in the DB and wasn't detected by the Set's Bin list pane and isn't a property either. 
		//		As such, records that have defined "Company" bin, will have an ExpandoObject value indicating that records has additional bins.
		test.CustInvsDoc.AsEnumerable()
						.OrderBy(cid => cid.LastName)
						.ThenBy(cid => cid.FirstName)
						.ThenBy(cid => cid.PK)
						.Dump("Customer Invoices Docs set From DB (rewritten with new PKs)", 1);

		LINQPad.Util.ReadLine("Press <Enter> to continue and remove newly written records!".Dump());

		foreach (var removePK in newRecs)
		{
			test.CustInvsDoc.Delete(removePK);
		}
	}
}
