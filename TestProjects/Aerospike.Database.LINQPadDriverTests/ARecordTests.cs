using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aerospike.Database.LINQPadDriver.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Data;

namespace Aerospike.Database.LINQPadDriver.Extensions.Tests
{
    [TestClass()]
    public class ARecordTests
    {
        [TestMethod()]
        public void FromJsonTest()
        {
            var json = @"
    {
	""_id"": {""$oid"":""0080a245fabe57999707dc41ced60edc4ac7ac40""},	
    ""account_id"": 794875,
    ""transaction_count"": 6,
    ""bucket_start_date"": {""$date"": {
          ""$numberLong"": ""1545886800000""}},
    ""bucket_end_date"": {""$date"": 1473120000000},
    ""transactions"": [
      {
        ""date"": {""$date"": 1325030400000},
        ""amount"": 1197,
        ""transaction_code"": ""buy"",
        ""symbol"": ""nvda"",
        ""price"": {""$decimal"": ""12.7330024299341033611199236474931240081787109375""},
        ""total"": 15241.40390863112172326054861
      },
      {
         ""date"": {""$date"": 1465776000000},
         ""amount"": 8797,
         ""transaction_code"": ""buy"",
         ""symbol"": ""nvda"",
         ""price"": {""$decimal"": ""46.53873172406391489630550495348870754241943359375""},
         ""total"": {""$double"": 409401.2229765902593427995271}
      },
      {
         ""date"": {""$date"": 1472601600000},
         ""amount"": 6146,
         ""transaction_code"": ""sell"",
         ""symbol"": ""ebay"",
         ""price"": ""32.11600884852845894101847079582512378692626953125"",
         ""total"": ""197384.9903830559086514995215""
      },
      {
         ""date"": {""$date"": 1101081600000},
         ""amount"": 253,
         ""transaction_code"": ""buy"",
         ""symbol"": ""amzn"",
         ""price"": ""37.77441226157566944721111212857067584991455078125"",
         ""total"": ""9556.926302178644370144411369""
      },
      {
         ""date"": {""$date"": 1022112000000},
         ""amount"": 4521,
         ""transaction_code"": ""buy"",
         ""symbol"": ""nvda"",
         ""price"": ""10.763069758141103449133879621513187885284423828125"",
         ""total"": ""48659.83837655592869353426977""
      },
      {
         ""date"": {""$date"": 936144000000},
         ""amount"": 955,
         ""transaction_code"": ""buy"",
         ""symbol"": ""csco"",
         ""price"": ""27.992136535152877030441231909207999706268310546875"",
         ""total"": ""26732.49039107099756407137647""
      }
    ]
  }
";
            var aRecord = ARecord.FromJson("tns", "tset", json);

            Assert.AreEqual(5, aRecord.Aerospike.Count);
            Assert.IsNull(aRecord.Aerospike.PrimaryKey);
            Assert.IsNotNull(aRecord.Aerospike.Digest);
            Assert.AreEqual(20, aRecord.Aerospike.Digest.Length);

            Assert.AreEqual(794875l, aRecord["account_id"].Value);
            Assert.AreEqual(6l, aRecord["transaction_count"].Value);
            Assert.AreEqual(DateTime.Parse(@"December 27, 2018 5:00:00 AM"), aRecord["bucket_start_date"].Value);
            Assert.AreEqual(DateTime.Parse(@"September 6, 2016 12:00:00 AM"), aRecord["bucket_end_date"].Value);

            Assert.IsNotNull(aRecord["transactions"]?.Value);
            Assert.IsInstanceOfType<List<Object>>(aRecord["transactions"].Value);
            var trans = (IList<object>)aRecord["transactions"].Value;
            Assert.AreEqual(6, trans.Count);

            var transItem = trans[0];

            Assert.IsInstanceOfType<Dictionary<string,Object>>(transItem);
            var dictItems = (Dictionary<string, Object>)transItem;

            Assert.AreEqual(6, dictItems.Count);
            Assert.AreEqual(DateTime.Parse(@"December 28, 2011 12:00:00 AM"), dictItems["date"]);
            Assert.AreEqual(1197l, dictItems["amount"]);
            Assert.AreEqual("buy", dictItems["transaction_code"]);
            Assert.AreEqual("nvda", dictItems["symbol"]);
            Assert.AreEqual(12.7330024299341033611199236474931240081787109375M, dictItems["price"]);
            Assert.AreEqual(15241.40390863112172326054861d, dictItems["total"]);

            transItem = trans[1];

            Assert.IsInstanceOfType<Dictionary<string, Object>>(transItem);
            dictItems = (Dictionary<string, Object>)transItem;

            Assert.AreEqual(6, dictItems.Count);
            Assert.AreEqual(DateTime.Parse(@"June 13, 2016 12:00:00 AM"), dictItems["date"]);
            Assert.AreEqual(8797l, dictItems["amount"]);
            Assert.AreEqual("buy", dictItems["transaction_code"]);
            Assert.AreEqual("nvda", dictItems["symbol"]);
            Assert.AreEqual(46.53873172406391489630550495348870754241943359375M, dictItems["price"]);
            Assert.AreEqual(409401.2229765902593427995271d, dictItems["total"]);


           json = @"
    {	
    ""oid"": {""$oid"": ""$numeric""},	
    ""datetimeoff"": {""$datetimeoffset"":""2016-06-13T12:00:00+01:00""},
    ""_id"": {""$oid"": ""$guid""}
  }";

            aRecord = ARecord.FromJson("tns", "tset", json);

            Assert.AreEqual(2, aRecord.Aerospike.Count);
            Assert.IsNotNull(aRecord.Aerospike.PrimaryKey);
            Assert.IsNotNull(aRecord.Aerospike.Digest);
            Assert.IsInstanceOfType<string>(aRecord.Aerospike.PrimaryKey);
            Assert.AreEqual(36, ((string) aRecord.Aerospike.PrimaryKey).Length);

            Assert.AreEqual(1l, aRecord["oid"].Value);
            Assert.AreEqual(DateTimeOffset.Parse(@"2016-06-13T12:00:00+01:00"), aRecord["datetimeoff"].Value);

            json = @"
    {
	""myid"": {""$oid"": ""$guid""},	
    ""oid"": {""$oid"": ""$numeric""},	
    ""datetimeoff"": {""$datetimeoffset"":""2016-06-13T12:00:00+01:00""}
  }";

            aRecord = ARecord.FromJson("tns", "tset", json, pkPropertyName: "myid");

            Assert.AreEqual(2, aRecord.Aerospike.Count);
            Assert.IsNotNull(aRecord.Aerospike.PrimaryKey);
            Assert.IsNotNull(aRecord.Aerospike.Digest);
            Assert.IsInstanceOfType<string>(aRecord.Aerospike.PrimaryKey);
            Assert.AreEqual(36, ((string)aRecord.Aerospike.PrimaryKey).Length);

            Assert.AreEqual(1l, aRecord["oid"].Value);
            Assert.AreEqual(DateTimeOffset.Parse(@"2016-06-13T12:00:00+01:00"), aRecord["datetimeoff"].Value);


            json = @"
{
  ""$type"": ""Aerospike.Database.LINQPadDriver.Extensions.JsonExportStructure, Aerospike.Database.LINQPadDriver"",
  ""NameSpace"": ""Demo"",
  ""SetName"": ""Track"",
  ""Generation"": 1,
  ""Digest"": {
    ""$type"": ""System.Byte[], System.Private.CoreLib"",
    ""$value"": ""AOCnAVwIO+po+DkvrPm4OhziPkI=""
  },
  ""KeyValue"": 2984,
  ""Values"": {
    ""$type"": ""System.Collections.Generic.Dictionary`2[[System.String, System.Private.CoreLib],[System.Object, System.Private.CoreLib]], System.Private.CoreLib"",
    ""AlbumId"": 236,
    ""Bytes"": 10227333,
    ""Composer"": ""Bono, The Edge, Adam Clayton, and Larry Mullen"",
    ""GenreId"": 1,
    ""MediaTypeId"": 1,
    ""Milliseconds"": 315167,
    ""Name"": ""If You Wear That Velvet Dress"",
    ""UnitPrice"": 0.99
  }
}";
            aRecord = ARecord.FromJson("tns", "tset", json, pkPropertyName: "Digest");

            Assert.AreEqual(6, aRecord.Aerospike.Count);
            Assert.IsNull(aRecord.Aerospike.PrimaryKey);
            Assert.IsNotNull(aRecord.Aerospike.Digest);
            Assert.IsInstanceOfType<Dictionary<string, object>>(aRecord["Values"].Value);

            var valuesDict = (Dictionary<string, object>)aRecord["Values"].Value;

            Assert.AreEqual(8, valuesDict.Count);
            Assert.AreEqual(236l, valuesDict["AlbumId"]);

        }
    }
}