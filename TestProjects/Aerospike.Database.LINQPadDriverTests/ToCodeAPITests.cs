using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aerospike.Database.LINQPadDriver.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aerospike.Database.LINQPadDriver.Extensions.Tests
{
	[TestClass()]
	public class ToCodeAPITests
	{
		[TestMethod()]
		public void ToAPICodeTest()
		{
			var aRecord = ARecord.FromJson("tns", "tset", ARecordTests.jsonRecords);

			var records = new ARecord[] { aRecord };

			var codeBlocks = records.ToAPICode().ToList();

			Assert.AreEqual(2, codeBlocks.Count);

			var codeBlock1 = @"tns.tset.Get(new Byte[] { (byte) 0,(byte) 128,(byte) 162,(byte) 69,(byte) 250,(byte) 190,(byte) 87,(byte) 153,(byte) 151,(byte) 7,(byte) 220,(byte) 65,(byte) 206,(byte) 214,(byte) 14,(byte) 220,(byte) 74,(byte) 199,(byte) 172,(byte) 64 })";
			var codeBlock2 = @"tns.tset.Put(new Byte[] { (byte) 0,(byte) 128,(byte) 162,(byte) 69,(byte) 250,(byte) 190,(byte) 87,(byte) 153,(byte) 151,(byte) 7,(byte) 220,(byte) 65,(byte) 206,(byte) 214,(byte) 14,(byte) 220,(byte) 74,(byte) 199,(byte) 172,(byte) 64 }, 
new Dictionary<string,object>() {{""account_id"", 794875L},
{""transaction_count"", 6L},
{""bucket_start_date"", DateTime.Parse(""2018-12-27T05:00:00.0000"")},
{""bucket_end_date"", DateTime.Parse(""2016-09-06T00:00:00.0000"")},
{""transactions"", new List<Object>() { new Dictionary<Object,Object>() { {""date"",DateTime.Parse(""2011-12-28T00:00:00.0000"")},{""amount"",1197L},{""transaction_code"",""buy""},{""symbol"",""nvda""},{""price"",12.733002429934103361119923647M},{""total"",15241.403908631122D} },new Dictionary<Object,Object>() { {""date"",DateTime.Parse(""2016-06-13T00:00:00.0000"")},{""amount"",8797L},{""transaction_code"",""buy""},{""symbol"",""nvda""},{""price"",46.538731724063914896305504953M},{""total"",409401.2229765903D} },new Dictionary<Object,Object>() { {""date"",DateTime.Parse(""2016-08-31T00:00:00.0000"")},{""amount"",6146L},{""transaction_code"",""sell""},{""symbol"",""ebay""},{""price"",""32.11600884852845894101847079582512378692626953125""},{""total"",""197384.9903830559086514995215""} },new Dictionary<Object,Object>() { {""date"",DateTime.Parse(""2004-11-22T00:00:00.0000"")},{""amount"",253L},{""transaction_code"",""buy""},{""symbol"",""amzn""},{""price"",""37.77441226157566944721111212857067584991455078125""},{""total"",""9556.926302178644370144411369""} },new Dictionary<Object,Object>() { {""date"",DateTime.Parse(""2002-05-23T00:00:00.0000"")},{""amount"",4521L},{""transaction_code"",""buy""},{""symbol"",""nvda""},{""price"",""10.763069758141103449133879621513187885284423828125""},{""total"",""48659.83837655592869353426977""} },new Dictionary<Object,Object>() { {""date"",DateTime.Parse(""1999-09-01T00:00:00.0000"")},{""amount"",955L},{""transaction_code"",""buy""},{""symbol"",""csco""},{""price"",""27.992136535152877030441231909207999706268310546875""},{""total"",""26732.49039107099756407137647""} } }}})";

			Assert.AreEqual(codeBlock1, codeBlocks[0]);
			Assert.AreEqual(codeBlock2, codeBlocks[1]);

			codeBlocks = records.ToAPICode(useAerospikeAPI: true).ToList();

			Assert.AreEqual(2, codeBlocks.Count);

			codeBlock1 = @"ASClient.Get(null,
		new Key(""tns"",""tset"",new Byte[] { (byte) 0,(byte) 128,(byte) 162,(byte) 69,(byte) 250,(byte) 190,(byte) 87,(byte) 153,(byte) 151,(byte) 7,(byte) 220,(byte) 65,(byte) 206,(byte) 214,(byte) 14,(byte) 220,(byte) 74,(byte) 199,(byte) 172,(byte) 64 }))";
			codeBlock2 = @"ASClient.Put(null,
		new Key(""tns"",""tset"",new Byte[] { (byte) 0,(byte) 128,(byte) 162,(byte) 69,(byte) 250,(byte) 190,(byte) 87,(byte) 153,(byte) 151,(byte) 7,(byte) 220,(byte) 65,(byte) 206,(byte) 214,(byte) 14,(byte) 220,(byte) 74,(byte) 199,(byte) 172,(byte) 64 }), 
new Bin(""account_id"", Value.Get(794875L)),
new Bin(""transaction_count"", Value.Get(6L)),
new Bin(""bucket_start_date"", Value.Get(""2018-12-27T05:00:00.0000"")),
new Bin(""bucket_end_date"", Value.Get(""2016-09-06T00:00:00.0000"")),
new Bin(""transactions"", Value.Get(new List<Object>() { new Dictionary<Object,Object>() { {""date"",""2011-12-28T00:00:00.0000""},{""amount"",1197L},{""transaction_code"",""buy""},{""symbol"",""nvda""},{""price"",12.733002429934103D},{""total"",15241.403908631122D} },new Dictionary<Object,Object>() { {""date"",""2016-06-13T00:00:00.0000""},{""amount"",8797L},{""transaction_code"",""buy""},{""symbol"",""nvda""},{""price"",46.538731724063915D},{""total"",409401.2229765903D} },new Dictionary<Object,Object>() { {""date"",""2016-08-31T00:00:00.0000""},{""amount"",6146L},{""transaction_code"",""sell""},{""symbol"",""ebay""},{""price"",""32.11600884852845894101847079582512378692626953125""},{""total"",""197384.9903830559086514995215""} },new Dictionary<Object,Object>() { {""date"",""2004-11-22T00:00:00.0000""},{""amount"",253L},{""transaction_code"",""buy""},{""symbol"",""amzn""},{""price"",""37.77441226157566944721111212857067584991455078125""},{""total"",""9556.926302178644370144411369""} },new Dictionary<Object,Object>() { {""date"",""2002-05-23T00:00:00.0000""},{""amount"",4521L},{""transaction_code"",""buy""},{""symbol"",""nvda""},{""price"",""10.763069758141103449133879621513187885284423828125""},{""total"",""48659.83837655592869353426977""} },new Dictionary<Object,Object>() { {""date"",""1999-09-01T00:00:00.0000""},{""amount"",955L},{""transaction_code"",""buy""},{""symbol"",""csco""},{""price"",""27.992136535152877030441231909207999706268310546875""},{""total"",""26732.49039107099756407137647""} } })))";

			Assert.AreEqual(codeBlock1, codeBlocks[0]);
			Assert.AreEqual(codeBlock2, codeBlocks[1]);
		}
	}
}