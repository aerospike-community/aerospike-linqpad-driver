﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aerospike.Database.LINQPadDriver.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using Aerospike.Client;
using NuGet.Frameworks;
using Newtonsoft.Json.Linq;

namespace Aerospike.Database.LINQPadDriver.Extensions.Tests
{
    [TestClass()]
    public class AValueTests
    {
        [TestMethod]
        public void CompareToNativeType()
        {
            var expectedValue = 1;
            var checkValue = new AValue(expectedValue, "binInt", "fldInt");
            var checkValue1 = new AValue(expectedValue, "binInt", "fldInt");

            var expectedValuex10 = expectedValue * 10;
            var expectedValueNeg = expectedValue * -1;
            var checkValueX10 = new AValue(expectedValuex10, "binIntX10", "fldIntX10");
            var checkValueNeg = new AValue(expectedValueNeg, "binIntX-1", "fldIntX-1");

            Assert.AreEqual(expectedValue, checkValue);
            Assert.AreEqual(checkValue, checkValue);
            Assert.AreEqual(checkValue, checkValue1);

            //AValue and Int
            Assert.IsTrue(checkValue == expectedValue);
            Assert.IsFalse(checkValue != expectedValue);
            Assert.IsTrue(checkValue != expectedValuex10);
            Assert.IsTrue(checkValue >= expectedValue);
            Assert.IsFalse(checkValue >= expectedValuex10);
            Assert.IsTrue(checkValue >= expectedValueNeg);
            Assert.IsFalse(checkValue > expectedValue);
            Assert.IsFalse(checkValue > expectedValuex10);
            Assert.IsTrue(checkValue > expectedValueNeg);

            Assert.IsTrue(checkValue <= expectedValue);
            Assert.IsTrue(checkValue <= checkValueX10);
            Assert.IsFalse(checkValue <= expectedValueNeg);
            Assert.IsFalse(checkValue < expectedValue);
            Assert.IsTrue(checkValue < checkValueX10);
            Assert.IsFalse(checkValue < expectedValueNeg);

            //int and AValue
            Assert.IsTrue(expectedValue == checkValue);
            Assert.IsTrue(expectedValue >= checkValue);
            Assert.IsTrue(expectedValuex10 >= checkValue);
            Assert.IsTrue(expectedValueNeg >= checkValueNeg);
            Assert.IsFalse(expectedValue > checkValue);
            Assert.IsFalse(expectedValuex10 > checkValueX10);
            Assert.IsFalse(expectedValueNeg > checkValueNeg);

            Assert.IsTrue(expectedValue <= checkValue);
            Assert.IsFalse(checkValueX10 <= checkValue);
            Assert.IsTrue(expectedValueNeg <= checkValue);
            Assert.IsFalse(expectedValue < checkValue);
            Assert.IsFalse(checkValueX10 < checkValue);
            Assert.IsTrue(expectedValueNeg < checkValue);

            //AValue and AValue
            Assert.AreEqual(expectedValue, checkValue);
            Assert.IsTrue(checkValue >= checkValue1);
            Assert.IsFalse(checkValue >= checkValueX10);
            Assert.IsTrue(checkValue >= checkValueNeg);
            Assert.IsFalse(checkValue > checkValue1);
            Assert.IsFalse(checkValue > checkValueX10);
            Assert.IsTrue(checkValue > checkValueNeg);

            Assert.IsTrue(checkValue <= checkValue1);
            Assert.IsTrue(checkValue <= checkValueX10);
            Assert.IsFalse(checkValue <= checkValueNeg);
            Assert.IsFalse(checkValue < checkValue1);
            Assert.IsTrue(checkValue < checkValueX10);
            Assert.IsFalse(checkValue < checkValueNeg);

            //AValue and string            
            Assert.IsFalse(checkValue == "abc");
            Assert.IsTrue(checkValue != "abc");
            Assert.IsFalse(checkValue >= "abc");
            Assert.IsFalse(checkValue > "abc");
            Assert.IsTrue(checkValue <= "abc");
            Assert.IsTrue(checkValue < "abc");

            var aValueStr = new AValue("abc", "binStr", "fldStr");

            Assert.IsFalse(checkValue == aValueStr);
            Assert.IsTrue(checkValue != aValueStr);
            Assert.IsFalse(checkValue >= aValueStr);
            Assert.IsFalse(checkValue > aValueStr);
            Assert.IsTrue(checkValue <= aValueStr);
            Assert.IsTrue(checkValue < aValueStr);

            Assert.AreEqual(aValueStr, "abc");
            Assert.IsTrue(aValueStr == "abc");
            Assert.IsFalse(aValueStr != "abc");
            Assert.IsTrue(aValueStr >= "abc");
            Assert.IsFalse(aValueStr > "abc");
            Assert.IsTrue(aValueStr <= "abc");
            Assert.IsFalse(aValueStr < "abc");

            aValueStr = new AValue(expectedValuex10.ToString(), "binStrNum", "fldStrNum");

            Assert.AreEqual(aValueStr, expectedValuex10.ToString());
            Assert.AreEqual(aValueStr, expectedValuex10);
            Assert.IsFalse(checkValue == aValueStr);
            Assert.IsTrue(checkValue != aValueStr);
            Assert.IsFalse(checkValue >= aValueStr);
            Assert.IsFalse(checkValue > aValueStr);
            Assert.IsTrue(checkValue <= aValueStr);
            Assert.IsTrue(checkValue < aValueStr);

            Assert.IsFalse(aValueStr == expectedValuex10);
            Assert.IsTrue(aValueStr != expectedValuex10);
            Assert.IsTrue(aValueStr >= expectedValuex10);
            Assert.IsTrue(aValueStr > expectedValuex10);
            Assert.IsFalse(aValueStr <= expectedValuex10);
            Assert.IsFalse(aValueStr < expectedValuex10);
        }

        [TestMethod]
        public void ToTypes()
        {
            AValue aValue;

            var listTst = new List<string>() { "a", "b", "c" };
            aValue = listTst.ToAValue();

            Assert.IsTrue(Helpers.SequenceEquals(listTst, aValue.Value));
            Assert.IsTrue(aValue.IsList);
            Assert.IsTrue(aValue.IsCDT);
            Assert.IsFalse(aValue.IsMap);
            Assert.IsFalse(aValue.IsJson);
            Assert.AreEqual(listTst.Count, aValue.Count());
            Assert.IsTrue(Helpers.SequenceEquals(listTst, aValue.ToList()));
            Assert.IsTrue(Helpers.SequenceEquals(listTst, aValue.ToListItem()));
            Assert.IsTrue(aValue.ToDictionary().Count == 0);

            var jToken = aValue.ToJToken();

            Assert.IsInstanceOfType<JArray>(jToken);
            Assert.AreEqual(listTst.Count, ((JArray)jToken).Count);
            Assert.IsTrue(Helpers.SequenceEquals(listTst, jToken));

            aValue = jToken.ToAValue();

            Assert.IsTrue(Helpers.SequenceEquals(listTst, aValue.Value));
            Assert.IsTrue(aValue.IsList);
            Assert.IsTrue(aValue.IsCDT);
            Assert.IsFalse(aValue.IsMap);
            Assert.IsTrue(aValue.IsJson);
            Assert.AreEqual(listTst.Count, aValue.Count());
            Assert.IsTrue(Helpers.SequenceEquals(listTst, aValue.ToList()));
            Assert.IsTrue(Helpers.SequenceEquals(listTst, aValue.ToListItem()));
            Assert.IsTrue(aValue.ToDictionary().Count == 0);
            Assert.AreEqual(jToken, aValue.ToJToken());

            var dirTst = new Dictionary<string, string>() { { "pa", "a" }, { "pb", "b" }, { "pc", "c" } };

            aValue = dirTst.ToAValue();

            Assert.IsTrue(Helpers.SequenceEquals(dirTst, aValue.Value));
            Assert.IsFalse(aValue.IsList);
            Assert.IsTrue(aValue.IsCDT);
            Assert.IsTrue(aValue.IsMap);
            Assert.IsFalse(aValue.IsJson);
            Assert.AreEqual(dirTst.Count, aValue.Count());
            Assert.IsTrue(Helpers.SequenceEquals(dirTst.ToList(), aValue.ToList()));
            Assert.IsTrue(Helpers.SequenceEquals(dirTst.ToList(), aValue.ToListItem()));

            var newDict = aValue.ToDictionary();
            Assert.IsTrue(Helpers.SequenceEquals(dirTst.Keys, newDict.Keys));
            Assert.IsTrue(Helpers.SequenceEquals(dirTst.Values, newDict.Values));
            Assert.AreEqual(dirTst.Count, newDict.Count);

            jToken = aValue.ToJToken();

            Assert.IsInstanceOfType<JObject>(jToken);
            Assert.AreEqual(dirTst.Count, ((JObject)jToken).Count);
            Assert.IsTrue(Helpers.SequenceEquals(dirTst.Keys,
                                                    ((JObject)jToken).Properties().Select(j => j.Name)));
            Assert.IsTrue(Helpers.SequenceEquals(dirTst.Values,
                                                    ((JObject)jToken).Properties().Select(j => ((JValue)j.Value).Value)));

            aValue = jToken.ToAValue();

            Assert.IsInstanceOfType<JObject>(aValue.Value);
            Assert.IsTrue(Helpers.SequenceEquals(dirTst.Keys,
                                                    ((JObject)aValue.Value).Properties().Select(j => j.Name)));
            Assert.IsTrue(Helpers.SequenceEquals(dirTst.Values,
                                                    ((JObject)aValue.Value).Properties().Select(j => ((JValue)j.Value).Value)));

            Assert.IsTrue(aValue.IsList);
            Assert.IsTrue(aValue.IsCDT);
            Assert.IsTrue(aValue.IsMap);
            Assert.IsTrue(aValue.IsJson);
            Assert.AreEqual(dirTst.Count, aValue.Count());
            
            Assert.AreEqual(dirTst.ToList().Count, aValue.ToList().Count);
            Assert.AreEqual(dirTst.ToList().Count, aValue.ToListItem().Count);

            Assert.IsTrue(Helpers.SequenceEquals(dirTst.Keys, aValue.ToDictionary().Keys));
            Assert.IsTrue(Helpers.SequenceEquals(dirTst.Values, aValue.ToDictionary().Values));
            Assert.AreEqual(jToken, aValue.ToJToken());

        }
    }
}