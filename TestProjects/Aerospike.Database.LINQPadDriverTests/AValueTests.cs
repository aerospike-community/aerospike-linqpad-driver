using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aerospike.Database.LINQPadDriver.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using Aerospike.Client;
using NuGet.Frameworks;
using Newtonsoft.Json.Linq;
using System.Printing;

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

            var testEqual = new { a = "a" };
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
            Assert.IsFalse(aValue.Equals(testEqual));
            Assert.IsFalse(aValue.Equals(1));
            Assert.IsFalse(aValue.Equals("a"));

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
            Assert.IsFalse(aValue.Equals(testEqual));
            Assert.IsFalse(aValue.Equals(1));
            Assert.IsFalse(aValue.Equals("a"));

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
            Assert.IsFalse(aValue.Equals(testEqual));
            Assert.IsFalse(aValue.Equals(1));
            Assert.IsFalse(aValue.Equals("a"));

            Assert.IsTrue(aValue.ContainsKey("pa"));
            Assert.IsTrue(aValue.ContainsKey("pb"));
            Assert.IsTrue(aValue.ContainsKey("pc"));
            Assert.IsFalse(aValue.ContainsKey("a"));
            Assert.IsTrue(aValue.Contains("b", AValue.MatchOptions.Any));
            Assert.IsFalse(aValue.Contains("x"));

            Assert.IsTrue(aValue.Contains("pa", "a"));
            Assert.IsFalse(aValue.Contains("pa", "x"));
            Assert.IsFalse(aValue.Contains("px", "a"));

            Assert.AreEqual(Enumerable.Empty<AValue>(), aValue.AsEnumerable());

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

            var strTest = "abcdefg";

            aValue = strTest.ToAValue();

            Assert.IsInstanceOfType<string>(aValue.Value);
            Assert.IsFalse(aValue.IsList);
            Assert.IsFalse(aValue.IsCDT);
            Assert.IsFalse(aValue.IsMap);
            Assert.IsFalse(aValue.IsJson);
            Assert.IsTrue(aValue.IsString);
            Assert.AreEqual(strTest.Length, aValue.Count());
            Assert.AreEqual(strTest.Length, aValue.ToList().Count);
            Assert.AreEqual(1, aValue.ToListItem().Count);
            Assert.IsFalse(aValue.Contains("abc"));
            Assert.IsTrue(aValue.Contains("abc", AValue.MatchOptions.SubString));
            Assert.IsFalse(aValue.Contains("def"));
            Assert.IsTrue(aValue.Contains("def", AValue.MatchOptions.SubString));
            Assert.IsFalse(aValue.Contains("fg"));
            Assert.IsTrue(aValue.Contains("fg", AValue.MatchOptions.SubString));
            Assert.IsFalse(aValue.Contains("dzf"));
            Assert.IsFalse(aValue.Equals(testEqual));
            Assert.IsFalse(aValue.Equals(1));
            Assert.IsFalse(aValue.Equals("a"));
            Assert.IsTrue(aValue.Equals(strTest));

            var geoTest = new GeoJSON.Net.Geometry.Point(new GeoJSON.Net.Geometry.Position(51.899523, -2.124156));
            aValue = geoTest.ToAValue();

            Assert.IsInstanceOfType<GeoJSON.Net.Geometry.Point>(aValue.Value);
            Assert.IsFalse(aValue.IsList);
            Assert.IsFalse(aValue.IsCDT);
            Assert.IsFalse(aValue.IsMap);
            Assert.IsFalse(aValue.IsJson);
            Assert.IsFalse(aValue.IsString);
            Assert.IsTrue(aValue.IsGeoJson);
            Assert.AreEqual(geoTest, aValue.Value);
            Assert.IsTrue(aValue.Equals(geoTest));
            Assert.IsFalse(aValue.Equals(123));
            Assert.IsFalse(aValue.Contains("dzf"));
            Assert.IsFalse(aValue.Equals(testEqual));
            Assert.IsFalse(aValue.Equals(1));
            Assert.IsFalse(aValue.Equals("a"));

            {
                var dirObj = new Dictionary<string, object>() { { "pa", "a" }, { "pb", "b" }, { "pc", "c" } };
                aValue = dirObj.ToAValue();

                var aValueEnum = aValue.AsEnumerable();

                Assert.AreEqual(dirObj.Count, aValueEnum.Count());

                int idx = 0;
                foreach (var kvpValue in aValueEnum)
                {
                    Assert.IsTrue(kvpValue.IsKeyValuePair);
                    var kvp = dirObj.ElementAt(idx++);
                    Assert.AreEqual(kvp.Key, ((KeyValuePair<AValue,AValue>) kvpValue.Value).Key.Value);
                    Assert.AreEqual(kvp.Value, ((KeyValuePair<AValue, AValue>)kvpValue.Value).Value.Value);
                }
            }
        }


        [TestMethod]
        public void ToStringTest()
        {
            var tnValue = 123456;
            AValue aValue = tnValue.ToAValue();
            
            Assert.AreEqual(tnValue, aValue);
            Assert.AreEqual("123456", aValue.ToString());
            Assert.AreEqual("123,456", aValue.ToString("###,###"));
            Assert.AreEqual("$123,456.00", aValue.ToString("c"));

            var tjValue = JToken.FromObject(tnValue);
            aValue = tjValue.ToAValue();

            Assert.AreEqual(tnValue, aValue);
            Assert.AreEqual(tjValue, aValue);
            Assert.AreEqual("123456", aValue.ToString());
            Assert.AreEqual("123,456", aValue.ToString("###,###"));
            
        }

        [TestMethod]
        public void EmptyTest()
        {
            Assert.IsTrue(AValue.Empty.IsEmpty);
            Assert.IsFalse(AValue.Empty.IsBool);
            Assert.IsFalse(AValue.Empty.IsInt);
            Assert.IsFalse(AValue.Empty.IsCDT);
            Assert.IsFalse(AValue.Empty.IsJson);
            Assert.IsFalse(AValue.Empty.IsDateTime);
            Assert.IsFalse(AValue.Empty.IsDateTimeOffset);
            Assert.IsFalse(AValue.Empty.IsDictionary);
            Assert.IsFalse(AValue.Empty.IsFloat);
            Assert.IsFalse(AValue.Empty.IsGeoJson);
            Assert.IsFalse(AValue.Empty.IsKeyValuePair);
            Assert.IsFalse(AValue.Empty.IsList);
            Assert.IsFalse(AValue.Empty.IsMap);
            Assert.IsFalse(AValue.Empty.IsNumeric);
            Assert.IsFalse(AValue.Empty.IsString);
            Assert.IsFalse(AValue.Empty.IsTimeSpan);

            Assert.IsNull(AValue.Empty.TryGetValue("abv"));
            Assert.IsNull(AValue.Empty.TryGetValue(123));
            Assert.AreEqual(AValue.Empty, AValue.Empty.TryGetValue(345, returnEmptyAValue: true ));

            Assert.IsFalse(AValue.Empty.Contains("122"));
            Assert.IsFalse(AValue.Empty.Contains("122", 123));
            Assert.IsFalse(AValue.Empty.ContainsKey("123"));

            Assert.IsNull(AValue.Empty.Convert<object>());
            Assert.IsNull(AValue.Empty.Convert<string>());
            Assert.IsNull(AValue.Empty.Convert<bool?>());
            Assert.IsNotNull(AValue.Empty.ToAerospikeExpression());
            Assert.AreEqual(Client.Value.NullValue.Instance, AValue.Empty.ToAerospikeValue());

        }

        [TestMethod]
        public void ContainsMethodTest()
        {
            object checkValue = "123";
            AValue aValue = checkValue.ToAValue();

            Assert.IsTrue(aValue.Equals(checkValue));
            Assert.IsTrue(aValue.Contains(checkValue));
            Assert.IsFalse(aValue.Contains(checkValue, string.Empty));
            Assert.IsFalse(aValue.Contains(string.Empty, checkValue));
            Assert.IsFalse(aValue.ContainsKey(checkValue));
            Assert.AreEqual(aValue, aValue.TryGetValue(checkValue));

            checkValue = true;
            aValue = checkValue.ToAValue();

            Assert.IsTrue(aValue.Equals(checkValue));
            Assert.IsTrue(aValue.Contains(checkValue));
            Assert.IsFalse(aValue.Contains(checkValue, string.Empty));
            Assert.IsFalse(aValue.Contains(string.Empty, checkValue));
            Assert.IsFalse(aValue.ContainsKey(checkValue));
            Assert.AreEqual(aValue, aValue.TryGetValue(checkValue));

            checkValue = 123;
            aValue = checkValue.ToAValue();

            Assert.IsTrue(aValue.Equals(checkValue));
            Assert.IsTrue(aValue.Contains(checkValue));
            Assert.IsFalse(aValue.Contains(checkValue, string.Empty));
            Assert.IsFalse(aValue.Contains(string.Empty, checkValue));
            Assert.IsFalse(aValue.ContainsKey(checkValue));
            Assert.AreEqual(aValue, aValue.TryGetValue(checkValue));


            checkValue = 123.45M;
            aValue = checkValue.ToAValue();

            Assert.IsTrue(aValue.Equals(checkValue));
            Assert.IsTrue(aValue.Contains(checkValue));
            Assert.IsFalse(aValue.Contains(checkValue, string.Empty));
            Assert.IsFalse(aValue.Contains(string.Empty, checkValue));
            Assert.IsFalse(aValue.ContainsKey(checkValue));
            Assert.AreEqual(aValue, aValue.TryGetValue(checkValue));

            checkValue = 123.567D;
            aValue = checkValue.ToAValue();

            Assert.IsTrue(aValue.Equals(checkValue));
            Assert.IsTrue(aValue.Contains(checkValue));
            Assert.IsFalse(aValue.Contains(checkValue, string.Empty));
            Assert.IsFalse(aValue.Contains(string.Empty, checkValue));
            Assert.IsFalse(aValue.ContainsKey(checkValue));
            Assert.AreEqual(aValue, aValue.TryGetValue(checkValue));

            checkValue = new List<string>() { "a", "b", "c", "d", "abcde" };
            aValue = checkValue.ToAValue();
            object matchValue = "c";
            AValue matchAValue = matchValue.ToAValue();

            Assert.IsTrue(aValue.Equals(checkValue));
            Assert.IsTrue(aValue.Contains(matchValue));
            Assert.IsTrue(aValue.Contains(checkValue, AValue.MatchOptions.Exact));
            Assert.IsTrue(aValue.Contains(((List<string>)checkValue).ToList(), AValue.MatchOptions.Exact));
            Assert.IsTrue(aValue.Contains("cd", AValue.MatchOptions.SubString));
            Assert.IsFalse(aValue.Contains("cd"));
            Assert.IsFalse(aValue.Contains(matchValue, string.Empty));
            Assert.IsFalse(aValue.Contains(string.Empty, matchValue));
            Assert.IsFalse(aValue.ContainsKey(matchValue));
            Assert.AreEqual(matchAValue, aValue.TryGetValue(matchValue));
            Assert.IsNull(aValue.TryGetValue("z"));

            checkValue = new List<int>() { 1, 2, 3, 4 };
            aValue = checkValue.ToAValue();
            matchValue = 4;
            matchAValue = matchValue.ToAValue();

            Assert.IsTrue(aValue.Equals(checkValue));
            Assert.IsTrue(aValue.Contains(matchValue));
            Assert.IsFalse(aValue.Contains(matchValue, string.Empty));
            Assert.IsFalse(aValue.Contains(string.Empty, matchValue));
            Assert.IsFalse(aValue.ContainsKey(matchValue));
            Assert.AreEqual(matchAValue, aValue.TryGetValue(matchValue));
            Assert.IsNull(aValue.TryGetValue(10));

            checkValue = new Dictionary<string,object>() { { "a", 1 }, { "b", 2 }, { "c", 3 }, { "d", 4 }, { "e", "abcde" } };
            aValue = checkValue.ToAValue();
            matchValue = "c";
            matchAValue = 3.ToAValue();

            Assert.IsTrue(aValue.Equals(checkValue));
            Assert.IsFalse(aValue.Contains(3));
            Assert.IsTrue(aValue.Contains(3, AValue.MatchOptions.Any));
            Assert.IsTrue(aValue.Contains(matchValue));
            Assert.IsTrue(aValue.Contains(matchValue, AValue.MatchOptions.Any));
            Assert.IsTrue(aValue.Contains("cd", AValue.MatchOptions.SubString | AValue.MatchOptions.Any));
            Assert.IsTrue(aValue.Contains(matchValue, AValue.MatchOptions.Any));
            Assert.IsFalse(aValue.Contains(matchValue, AValue.MatchOptions.Exact));
            Assert.IsTrue(aValue.Contains(checkValue, AValue.MatchOptions.Exact));
            Assert.IsTrue(aValue.Contains(((Dictionary<string, object>)checkValue)
                                                .ToDictionary(k => k.Key, v => v.Value),
                                                AValue.MatchOptions.Exact));
            Assert.IsFalse(aValue.Contains(checkValue));
            Assert.IsTrue(aValue.ContainsKey(matchValue));
            Assert.AreEqual(matchAValue, aValue.TryGetValue(matchValue));
            
            checkValue = new Dictionary<string, int>() { { "a", 1 }, { "b", 2 }, { "c", 3 }, { "d", 4 }, {"abcde", 5 } };
            aValue = checkValue.ToAValue();
            matchValue = "c";
            
            Assert.IsTrue(aValue.Equals(checkValue));
            Assert.IsFalse(aValue.Contains(3));
            Assert.IsFalse(aValue.Contains(3, AValue.MatchOptions.Exact));
            Assert.IsTrue(aValue.Contains(3, AValue.MatchOptions.Any));
            Assert.IsTrue(aValue.Contains("cd", AValue.MatchOptions.SubString));
            Assert.IsFalse(aValue.Contains("cd"));
            Assert.IsFalse(aValue.Contains("cd", AValue.MatchOptions.Any));
            Assert.IsTrue(aValue.Contains(matchValue, 3));
            Assert.IsTrue(aValue.ContainsKey(matchValue));
            Assert.IsNull(aValue.TryGetValue(matchValue));


            checkValue = new Dictionary<object, object>() { { "a", 1 }, { "b", 2 }, { "c", 3 }, { "d", 4 } };
            aValue = checkValue.ToAValue();
            matchValue = "c";
            matchAValue = 3.ToAValue();

            Assert.IsTrue(aValue.Equals(checkValue));
            Assert.IsFalse(aValue.Contains(3));
            Assert.IsFalse(aValue.Contains("z"));
            Assert.IsTrue(aValue.Contains(matchValue));
            Assert.IsTrue(aValue.ContainsKey(matchValue));
            Assert.AreEqual(matchAValue, aValue.TryGetValue(matchValue));
            Assert.IsNull(aValue.TryGetValue("z"));

            checkValue = "123";
            aValue = checkValue.ToAValue();

            Assert.AreEqual(checkValue, aValue.TryGetValue<string>(checkValue));

            checkValue = true;
            aValue = checkValue.ToAValue();

            Assert.AreEqual(checkValue, aValue.TryGetValue<bool>(checkValue));

            checkValue = 123;
            aValue = checkValue.ToAValue();

            Assert.AreEqual(checkValue, aValue.TryGetValue<int>(checkValue));


            checkValue = 123.45M;
            aValue = checkValue.ToAValue();

            Assert.AreEqual(aValue, aValue.TryGetValue<decimal>(checkValue));

            checkValue = 123.567D;
            aValue = checkValue.ToAValue();

            Assert.AreEqual(aValue, aValue.TryGetValue<double>(checkValue));

            checkValue = new List<string>() { "a", "b", "c", "d" };
            aValue = checkValue.ToAValue();
            matchValue = "c";
            
            Assert.AreEqual(matchValue, aValue.TryGetValue<string>(matchValue));

            checkValue = new List<int>() { 1, 2, 3, 4 };
            aValue = checkValue.ToAValue();
            matchValue = 4;
            
            Assert.AreEqual(matchValue, aValue.TryGetValue<int>(matchValue));

            checkValue = new Dictionary<string, object>() { { "a", 1 }, { "b", 2 }, { "c", 3 }, { "d", 4 } };
            aValue = checkValue.ToAValue();
            matchValue = "c";
            
            Assert.AreEqual(3, aValue.TryGetValue<int>(matchValue));

            checkValue = new Dictionary<string, int>() { { "a", 1 }, { "b", 2 }, { "c", 3 }, { "d", 4 } };
            aValue = checkValue.ToAValue();
            matchValue = "c";
            
            Assert.AreEqual(0, aValue.TryGetValue<int>(matchValue));
            
            checkValue = new Dictionary<object, object>() { { "a", 1 }, { "b", 2 }, { "c", 3 }, { "d", 4 } };
            aValue = checkValue.ToAValue();
            matchValue = "c";
            
            Assert.AreEqual(3, aValue.TryGetValue<int>(matchValue));
            Assert.AreEqual(0, aValue.TryGetValue<int>("z"));
        }

        [TestMethod]
        public void AsEnumerableMethodTest()
        {
            object checkValue = "123";
            AValue aValue = checkValue.ToAValue();

            Assert.AreEqual(Enumerable.Empty<AValue>(), aValue.AsEnumerable());

            checkValue = true;
            aValue = checkValue.ToAValue();

            Assert.AreEqual(Enumerable.Empty<AValue>(), aValue.AsEnumerable());

            checkValue = 123;
            aValue = checkValue.ToAValue();

            Assert.AreEqual(Enumerable.Empty<AValue>(), aValue.AsEnumerable());

            checkValue = 123.45M;
            aValue = checkValue.ToAValue();

            Assert.AreEqual(Enumerable.Empty<AValue>(), aValue.AsEnumerable());

            checkValue = 123.567D;
            aValue = checkValue.ToAValue();

            Assert.AreEqual(Enumerable.Empty<AValue>(), aValue.AsEnumerable());

            checkValue = new List<object>() { "a", "b", "c", "d" };
            aValue = checkValue.ToAValue();
            var collection = aValue.AsEnumerable();

            Assert.AreEqual(((List<object>)checkValue).Count, collection.Count());
            foreach(var item in (List<object>) checkValue)
            {
                Assert.IsTrue(collection.Any(i => i.Equals(item)));
            }

            checkValue = new List<object>() { 1, 2, 3, 4 };
            aValue = checkValue.ToAValue();
            collection = aValue.AsEnumerable();

            Assert.AreEqual(((List<object>)checkValue).Count, collection.Count());
            foreach (var item in (List<object>)checkValue)
            {
                Assert.IsTrue(collection.Any(i => i.Equals(item)));
            }

            checkValue = new Dictionary<string, object>() { { "a", 1 }, { "b", 2 }, { "c", 3 }, { "d", 4 } };
            aValue = checkValue.ToAValue();

            collection = aValue.AsEnumerable();

            Assert.AreEqual(((Dictionary<string, object>)checkValue).Count, collection.Count());
            foreach (var item in ((Dictionary<string, object>)checkValue))
            {
                Assert.IsTrue(collection.Any(i => i.TryGetValue<object>(item.Key)?.Equals(item.Value) ?? false));
            }
        }

        [TestMethod]
        public void RecordTest()
        {
            string jsonRecords = @"
[
  {
    ""_id"": ""10.01"",
    ""BinA"": 10.01,
    ""BinB"": ""1001"",
    ""BinC"": 123
  },
  {
    ""_id"": ""Map4"",
    ""BinB"": {
      ""Map10"": ""Key3"",
      ""Map11"": ""Key3"",
      ""Map9"": ""Map2Bin"",
      ""key12"": ""BinA456""
    }
  },
  {
    ""_id"": ""Map3"",
    ""BinB"": {
      ""Map7"": ""Map2Bin"",
      ""Map8"": ""ABKey3CD"",
      ""key1"": ""BinA456""
    }
  },
  {
    ""_id"": 10.01,
    ""BinA"": ""10.01"",
    ""BinB"": 1001,
    ""BinC"": ""14:42:40""
  },
  {
    ""_id"": {
      ""$oid"": ""c363ecde6a39ae0611c69ee2c7bd8a3b6930337b""
    },
    ""BinA"": 10.02,
    ""BinB"": ""1002"",
    ""BinC"": 456
  },
  {
    ""_id"": ""MyPK"",
    ""BinA"": 456,
    ""BinB"": 456,
    ""BinC"": ""5/9/2023 2:42:40 PM -07:00""
  },
  {
    ""_id"": ""Map2"",
    ""BinB"": {
      ""Key3"": 7,
      ""Map4"": ""Map2Bin"",
      ""Map5"": ""ABKey3CD"",
      ""key1"": ""BinA123""
    }
  },
  {
    ""_id"": 7.89,
    ""BinA"": 7.89,
    ""BinB"": 789,
    ""BinC"": 1683668560000000000
  },
  {
    ""_id"": ""List1"",
    ""BinA"": [
      ""BinA123"",
      456,
      ""List1Bin""
    ]
  },
  {
    ""_id"": ""List3"",
    ""BinB"": [
      ""BinB123"",
      89,
      ""List3Bin"",
      ""Key3""
    ]
  },
  {
    ""_id"": ""Map1"",
    ""BinB"": {
      ""Key2"": ""BinB123"",
      ""Key3"": 456,
      ""Key4"": ""Map1Bin""
    }
  },
  {
    ""_id"": ""List2"",
    ""BinA"": [
      ""BinA123"",
      7.89,
      ""List2Bin""
    ]
  },
  {
    ""_id"": ""Key3"",
    ""BinA"": ""BinA123"",
    ""BinB"": ""Key3"",
    ""BinC"": ""BinCKey3""
  },
  {
    ""_id"": 123,
    ""BinA"": ""BinA123"",
    ""BinB"": 123,
    ""BinC"": ""5/9/2023 2:42:40 PM""
  }
]";
            var expectedDateTimeOffset = DateTimeOffset.Parse("5/9/2023 2:42:40 PM -07:00");
            var ns = new ANamespaceAccess("testit");

            var records = new List<ARecord>();

            ns.FromJson("DataTypes", jsonRecords, insertIntoList: records);

            Assert.AreEqual(14, records.Count);

            //**************************************
            var testRecords = records.Where(x => x["PK"] == "MyPK");

            Assert.AreEqual(1, testRecords.Count());
            Assert.IsNotNull(testRecords.First().Aerospike.PrimaryKey);
            Assert.AreEqual("MyPK", testRecords.First().Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecords.First().Aerospike.GetValues());
            Assert.AreEqual(3, testRecords.First().Aerospike.GetValues().Count);
            Assert.AreEqual(456L,
                                testRecords.First().GetValue("BinA").Value);
            Assert.AreEqual(456L,
                                testRecords.First().GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.ToString(),
                                testRecords.First().GetValue("BinC").Value);

            var testValue = records.Where(x => x["PK"] == 123)
                                .First() //Get first (only record)  BTW, could wrote as "Demo[demoSet].First(x => x["PK"] == 123)"
                                .GetValue("BinC"); //Get Bin "BinC"'s value
            Assert.IsNotNull(testValue);
            Assert.IsInstanceOfType<AValue>(testValue);
            Assert.AreEqual(@expectedDateTimeOffset.DateTime.ToString(),
                                testValue.Value);

            //**************************************
            testRecords = records.Where(x => x["PK"] == 10.01M);
            Assert.AreEqual(1, testRecords.Count());
            var testRecord = testRecords.First();
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual(10.01D, testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("10.01",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(1001L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.TimeOfDay.ToString(),
                                testRecord.GetValue("BinC").Value);

            //**************************************
            testRecords = records.Where(x => x["PK"] == "10.01");
            Assert.AreEqual(1, testRecords.Count());
            testRecord = testRecords.First();
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("10.01", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual(10.01D,
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual("1001",
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(123L,
                                testRecord.GetValue("BinC").Value);

            //**************************************
            testRecords = records.Where(x => x["PK"] == "NoPKValueSaved");
            Assert.AreEqual(1, testRecords.Count());
            testRecord = testRecords.First();
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsFalse(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            //Assert.AreEqual("10.01", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual(10.02D,
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual("1002",
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(456L,
                                testRecord.GetValue("BinC").Value);

            //**************************************
            testRecords = records.Where(x => x["BinC"] == expectedDateTimeOffset.DateTime);
            Assert.AreEqual(2, testRecords.Count());

            testRecord = testRecords.First();
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("MyPK", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual(456L,
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(456L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.ToString(),
                                testRecord.GetValue("BinC").Value);

            testRecord = testRecords.ElementAt(1);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual(123L, testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("BinA123",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(123L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.DateTime.ToString(),
                                testRecord.GetValue("BinC").Value);

            testRecords = records.Where(x => x["BinC"] == expectedDateTimeOffset.DateTime.ToUniversalTime());
            Assert.AreEqual(1, testRecords.Count());
            testRecord = testRecords.First();
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual(7.89D, testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual(7.89D,
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(789L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual((long) (expectedDateTimeOffset.ToUnixTimeMilliseconds() * 1000000),
                                testRecord.GetValue("BinC").Value);

            testRecords = records.Where(x => x["BinC"] == expectedDateTimeOffset);
            Assert.AreEqual(3, testRecords.Count());

            testRecord = testRecords.First();
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("MyPK", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual(456L,
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(456L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.ToString(),
                                testRecord.GetValue("BinC").Value);

            testRecord = testRecords.ElementAt(1);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual(7.89D, testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual(7.89D,
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(789L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual((long)(expectedDateTimeOffset.ToUnixTimeMilliseconds() * 1000000),
                                testRecord.GetValue("BinC").Value);

            testRecord = testRecords.ElementAt(2);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual(123L, testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("BinA123",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(123L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.DateTime.ToString(),
                                testRecord.GetValue("BinC").Value);

            //**************************************
            testRecords = records.Where(x => x["BinC"] == 1683668560000000000);
            Assert.AreEqual(1, testRecords.Count());
            testRecord = testRecords.First();
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual(7.89D, testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual(7.89D,
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(789L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual((long)(expectedDateTimeOffset.ToUnixTimeMilliseconds() * 1000000),
                                testRecord.GetValue("BinC").Value);

            //**************************************
            testRecords = records.Where(x => x["PK"] < 11);
            Assert.AreEqual(3, testRecords.Count());

            testRecord = testRecords.First();
            Assert.AreEqual(10.01D, testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("10.01",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(1001L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.TimeOfDay.ToString(),
                                testRecord.GetValue("BinC").Value);

            testRecord = testRecords.ElementAt(1);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("MyPK", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual(456L,
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(456L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.ToString(),
                                testRecord.GetValue("BinC").Value);

            testRecord = testRecords.ElementAt(2);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual(7.89D, testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual(7.89D,
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(789L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual((long)(expectedDateTimeOffset.ToUnixTimeMilliseconds() * 1000000),
                                testRecord.GetValue("BinC").Value);

            //**************************************
            testRecords = records.Where(x => x["BinB"] < 800);
            Assert.AreEqual(7, testRecords.Count());

            testRecord = testRecords.First();
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("10.01", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual(10.01D,
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual("1001",
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(123L,
                                testRecord.GetValue("BinC").Value);

            testRecord = testRecords.ElementAt(1);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsFalse(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            //Assert.AreEqual("10.01", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual(10.02D,
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual("1002",
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(456L,
                                testRecord.GetValue("BinC").Value);

            testRecord = testRecords.ElementAt(2);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("MyPK", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual(456L,
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(456L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.ToString(),
                                testRecord.GetValue("BinC").Value);

            testRecord = testRecords.ElementAt(3);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual(7.89D, testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual(7.89D,
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(789L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual((long)(expectedDateTimeOffset.ToUnixTimeMilliseconds() * 1000000),
                                testRecord.GetValue("BinC").Value);

            testRecord = testRecords.ElementAt(4);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("List1", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(1, testRecord.Aerospike.GetValues().Count);
            Assert.IsTrue(testRecord.GetValue("BinA").IsList);
            CollectionAssert.AreEqual(new object[] { "BinA123", 456L, "List1Bin" },
                                        (List<object>) testRecord.GetValue("BinA").Value);            
            
            testRecord = testRecords.ElementAt(5);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("List2", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(1, testRecord.Aerospike.GetValues().Count);
            Assert.IsTrue(testRecord.GetValue("BinA").IsList);
            CollectionAssert.AreEqual(new object[] { "BinA123", 7.89D, "List2Bin" },
                                        (List<object>)testRecord.GetValue("BinA").Value);

            testRecord = testRecords.Last();
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual(123L, testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("BinA123",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(123L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.DateTime.ToString(),
                                testRecord.GetValue("BinC").Value);

            //**************************************
            testRecords = records.Where(x => x.BinExists("BinB") && x["BinB"].IsInt && x["BinB"] < 800).ToList();
            Assert.AreEqual(3, testRecords.Count());

            testRecord = testRecords.First();
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("MyPK", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual(456L,
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(456L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.ToString(),
                                testRecord.GetValue("BinC").Value);

            testRecord = testRecords.ElementAt(1);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual(7.89D, testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual(7.89D,
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(789L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual((long)(expectedDateTimeOffset.ToUnixTimeMilliseconds() * 1000000),
                                testRecord.GetValue("BinC").Value);

            testRecord = testRecords.ElementAt(2);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual(123L, testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("BinA123",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(123L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.DateTime.ToString(),
                                testRecord.GetValue("BinC").Value);

            //**************************************
            testRecords = records.Where(x => x["BinA"] == "10.01" || x["BinB"] == "1001");
            Assert.AreEqual(2, testRecords.Count());

            testRecord = testRecords.First();
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("10.01", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual(10.01D,
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual("1001",
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(123L,
                                testRecord.GetValue("BinC").Value);

            testRecord = testRecords.ElementAt(1);
            Assert.AreEqual(10.01D, testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("10.01",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(1001L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.TimeOfDay.ToString(),
                                testRecord.GetValue("BinC").Value);

            //**************************************
            testRecords = records.Where(dt => dt.GetValue("BinA")?.Contains("BinA123") ?? false);
            //.Dump("All Records with value \"BinA123\"");
            Assert.AreEqual(4, testRecords.Count());

            testRecord = testRecords.ElementAt(0);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("List1", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(1, testRecord.Aerospike.GetValues().Count);
            Assert.IsTrue(testRecord.GetValue("BinA").IsList);
            CollectionAssert.AreEqual(new object[] { "BinA123", 456L, "List1Bin" },
                                        (List<object>)testRecord.GetValue("BinA").Value);

            testRecord = testRecords.ElementAt(1);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("List2", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(1, testRecord.Aerospike.GetValues().Count);
            Assert.IsTrue(testRecord.GetValue("BinA").IsList);
            CollectionAssert.AreEqual(new object[] { "BinA123", 7.89D, "List2Bin" },
                                        (List<object>)testRecord.GetValue("BinA").Value);

            testRecord = testRecords.ElementAt(2);
            Assert.AreEqual("Key3", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("BinA123",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual("Key3",
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual("BinCKey3",
                                testRecord.GetValue("BinC").Value);

            testRecord = testRecords.ElementAt(3);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual(123L, testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("BinA123",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(123L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.DateTime.ToString(),
                                testRecord.GetValue("BinC").Value);

            //**************************************
            testRecords = records.Where(dt => dt.BinExists("BinA") &&  dt.GetValue("BinA").IsList && dt.GetValue("BinA").Contains("BinA123"));
            //        .Dump("Records with value \"BinA123\" within a list");
            Assert.AreEqual(2, testRecords.Count());

            testRecord = testRecords.ElementAt(0);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("List1", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(1, testRecord.Aerospike.GetValues().Count);
            Assert.IsTrue(testRecord.GetValue("BinA").IsList);
            CollectionAssert.AreEqual(new object[] { "BinA123", 456L, "List1Bin" },
                                        (List<object>)testRecord.GetValue("BinA").Value);

            testRecord = testRecords.ElementAt(1);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("List2", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(1, testRecord.Aerospike.GetValues().Count);
            Assert.IsTrue(testRecord.GetValue("BinA").IsList);
            CollectionAssert.AreEqual(new object[] { "BinA123", 7.89D, "List2Bin" },
                                        (List<object>)testRecord.GetValue("BinA").Value);

            //**************************************
            testRecords = records.Where(dt => dt.BinExists("BinA") && dt.GetValue("BinA").IsString && dt.GetValue("BinA").Contains("BinA123"));
            //        .Dump("Records with value \"BinA123\" is a string");
            Assert.AreEqual(2, testRecords.Count());

            testRecord = testRecords.ElementAt(0);
            Assert.AreEqual("Key3", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("BinA123",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual("Key3",
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual("BinCKey3",
                                testRecord.GetValue("BinC").Value);

            testRecord = testRecords.ElementAt(1);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual(123L, testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("BinA123",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(123L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.DateTime.ToString(),
                                testRecord.GetValue("BinC").Value);

            //**************************************
            testRecords = records.Where(dt => dt.BinExists("BinA") && dt.BinExists("BinB") && dt.GetValue("BinA").Contains("BinA123"));
            //        .Dump("Records with value \"BinA123\" and BinB exists in the record");
            Assert.AreEqual(2, testRecords.Count());

            testRecord = testRecords.ElementAt(0);
            Assert.AreEqual("Key3", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("BinA123",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual("Key3",
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual("BinCKey3",
                                testRecord.GetValue("BinC").Value);

            testRecord = testRecords.ElementAt(1);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual(123L, testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("BinA123",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(123L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.DateTime.ToString(),
                                testRecord.GetValue("BinC").Value);

            //**************************************
            testRecords = records.Where(dt => dt.BinExists("BinA") && dt.GetValue("BinA").Contains("BinA123"));
            //       .Dump("Records with value \"BinA123\" in All record");
            Assert.AreEqual(4, testRecords.Count());
            testRecord = testRecords.ElementAt(0);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("List1", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(1, testRecord.Aerospike.GetValues().Count);
            Assert.IsTrue(testRecord.GetValue("BinA").IsList);
            CollectionAssert.AreEqual(new object[] { "BinA123", 456L, "List1Bin" },
                                        (List<object>)testRecord.GetValue("BinA").Value);

            testRecord = testRecords.ElementAt(1);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("List2", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(1, testRecord.Aerospike.GetValues().Count);
            Assert.IsTrue(testRecord.GetValue("BinA").IsList);
            CollectionAssert.AreEqual(new object[] { "BinA123", 7.89D, "List2Bin" },
                                        (List<object>)testRecord.GetValue("BinA").Value);

            testRecord = testRecords.ElementAt(2);
            Assert.AreEqual("Key3", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("BinA123",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual("Key3",
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual("BinCKey3",
                                testRecord.GetValue("BinC").Value);

            testRecord = testRecords.ElementAt(3);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual(123L, testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("BinA123",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(123L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.DateTime.ToString(),
                                testRecord.GetValue("BinC").Value);

            //**************************************
            testRecords = records.Where(dt => dt.GetValue("BinA") == "BinA123");
            //       .Dump("Records with value \"BinA123\" using ==");
            Assert.AreEqual(2, testRecords.Count());

            testRecord = testRecords.ElementAt(0);
            Assert.AreEqual("Key3", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("BinA123",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual("Key3",
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual("BinCKey3",
                                testRecord.GetValue("BinC").Value);

            testRecord = testRecords.ElementAt(1);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual(123L, testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("BinA123",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(123L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.DateTime.ToString(),
                                testRecord.GetValue("BinC").Value);


            //**************************************
            testRecords = records.Where(dt => dt.GetValue("BinB") == 1001);
            //        .Dump("Records with value 1001 in BinB");
            Assert.AreEqual(1, testRecords.Count());

            testRecord = testRecords.ElementAt(0);
            Assert.AreEqual(10.01D, testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("10.01",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(1001L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.TimeOfDay.ToString(),
                                testRecord.GetValue("BinC").Value);

            //**************************************
            testRecords = records.Where(dt => dt.GetValue("BinC") == expectedDateTimeOffset.ToString());
            //        .Dump("Records using DateTime string");
            Assert.AreEqual(1, testRecords.Count());

            testRecord = testRecords.First();
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("MyPK", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual(456L,
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(456L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.ToString(),
                                testRecord.GetValue("BinC").Value);

            //**************************************
            testRecords = records.Where(dt => dt.GetValue("BinC") == expectedDateTimeOffset);
            //        .Dump("Records using DateTimeOffset object");
            Assert.AreEqual(3, testRecords.Count());

            testRecord = testRecords.First();
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("MyPK", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual(456L,
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(456L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.ToString(),
                                testRecord.GetValue("BinC").Value);

            testRecord = testRecords.ElementAt(1);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual(7.89D, testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual(7.89D,
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(789L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual((long)(expectedDateTimeOffset.ToUnixTimeMilliseconds() * 1000000),
                                testRecord.GetValue("BinC").Value);

            testRecord = testRecords.ElementAt(2);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual(123L, testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("BinA123",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(123L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.DateTime.ToString(),
                                testRecord.GetValue("BinC").Value);

            //**************************************
            testRecords = records.Where(dt => dt.GetValue("BinC") == expectedDateTimeOffset.DateTime);
            Assert.AreEqual(2, testRecords.Count());

            testRecord = testRecords.First();
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("MyPK", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual(456L,
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(456L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.ToString(),
                                testRecord.GetValue("BinC").Value);
            
            testRecord = testRecords.ElementAt(1);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual(123L, testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("BinA123",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual(123L,
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual(expectedDateTimeOffset.DateTime.ToString(),
                                testRecord.GetValue("BinC").Value);

            //**************************************
            testRecords = records.Where(dt => dt.GetValue("BinB")?.Contains("Key3") ?? false);
            //        .Dump("Records where BinB has value \"Key3\" as a value or within a collection");
            Assert.AreEqual(4, testRecords.Count());

            testRecord = testRecords.First();
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("Map2", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(1, testRecord.Aerospike.GetValues().Count);
            var aBinValue = testRecord.GetValue("BinB");
            Assert.IsTrue(aBinValue.IsCDT);
            Assert.IsTrue(aBinValue.IsMap);
            Assert.IsTrue(aBinValue.IsDictionary);
            Assert.IsFalse(aBinValue.IsBool);
            Assert.IsFalse(aBinValue.IsKeyValuePair);
            Assert.IsTrue((((IDictionary<string,object>) aBinValue.Value).First().ToAValue().IsKeyValuePair));
            Assert.IsFalse(aBinValue.IsEmpty);
            Assert.IsFalse(aBinValue.IsFloat);
            Assert.IsFalse(aBinValue.IsGeoJson);
            Assert.IsFalse(aBinValue.IsInt);
            Assert.IsFalse(aBinValue.IsJson);
            Assert.IsFalse(aBinValue.IsList);
            Assert.IsFalse(aBinValue.IsNumeric);
            Assert.IsFalse(aBinValue.IsString);
            Assert.IsFalse(aBinValue.IsTimeSpan);
            Assert.IsFalse(aBinValue.IsDateTime);
            Assert.IsFalse(aBinValue.IsDateTimeOffset);
            Assert.AreEqual(4, ((IDictionary<string, object>)aBinValue.Value).Count);
            var dictStrObj = new Dictionary<string, object>() { { "Key3", 7L }, { "Map4", "Map2Bin" }, { "Map5", "ABKey3CD" }, { "key1", "BinA123" } };
            CollectionAssert.AreEqual(dictStrObj.Keys,
                                        ((Dictionary<string, object>) aBinValue.Value).Keys);
            CollectionAssert.AreEqual(dictStrObj.Values,
                                        ((Dictionary<string, object>)aBinValue.Value).Values);

            testRecord = testRecords.ElementAt(1);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("List3", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(1, testRecord.Aerospike.GetValues().Count);
            aBinValue = testRecord.GetValue("BinB");
            Assert.IsTrue(aBinValue.IsCDT);
            Assert.IsFalse(aBinValue.IsMap);
            Assert.IsFalse(aBinValue.IsDictionary);
            Assert.IsFalse(aBinValue.IsBool);
            Assert.IsFalse(aBinValue.IsKeyValuePair);
            Assert.IsFalse(aBinValue.IsEmpty);
            Assert.IsFalse(aBinValue.IsFloat);
            Assert.IsFalse(aBinValue.IsGeoJson);
            Assert.IsFalse(aBinValue.IsInt);
            Assert.IsFalse(aBinValue.IsJson);
            Assert.IsTrue(aBinValue.IsList);
            Assert.IsFalse(aBinValue.IsNumeric);
            Assert.IsFalse(aBinValue.IsString);
            Assert.IsFalse(aBinValue.IsTimeSpan);
            Assert.IsFalse(aBinValue.IsDateTime);
            Assert.IsFalse(aBinValue.IsDateTimeOffset);
            Assert.AreEqual(4, ((IList<object>)aBinValue.Value).Count);
            var listObj = new List<object>() { "BinB123", 89L, "List3Bin", "Key3" };
            CollectionAssert.AreEqual(listObj,
                                        ((List<object>)aBinValue.Value));

            testRecord = testRecords.ElementAt(2);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("Map1", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(1, testRecord.Aerospike.GetValues().Count);
            aBinValue = testRecord.GetValue("BinB");
            Assert.IsTrue(aBinValue.IsCDT);
            Assert.IsTrue(aBinValue.IsMap);
            Assert.IsTrue(aBinValue.IsDictionary);
            Assert.IsFalse(aBinValue.IsBool);
            Assert.IsFalse(aBinValue.IsKeyValuePair);
            Assert.IsTrue((((IDictionary<string, object>)aBinValue.Value).First().ToAValue().IsKeyValuePair));
            Assert.IsFalse(aBinValue.IsEmpty);
            Assert.IsFalse(aBinValue.IsFloat);
            Assert.IsFalse(aBinValue.IsGeoJson);
            Assert.IsFalse(aBinValue.IsInt);
            Assert.IsFalse(aBinValue.IsJson);
            Assert.IsFalse(aBinValue.IsList);
            Assert.IsFalse(aBinValue.IsNumeric);
            Assert.IsFalse(aBinValue.IsString);
            Assert.IsFalse(aBinValue.IsTimeSpan);
            Assert.IsFalse(aBinValue.IsDateTime);
            Assert.IsFalse(aBinValue.IsDateTimeOffset);
            Assert.AreEqual(3, ((IDictionary<string, object>)aBinValue.Value).Count);
            dictStrObj = new Dictionary<string, object>() { { "Key2", "BinB123" }, { "Key3", 456L }, { "Key4", "Map1Bin" } };
            CollectionAssert.AreEqual(dictStrObj.Keys,
                                        ((Dictionary<string, object>)aBinValue.Value).Keys);
            CollectionAssert.AreEqual(dictStrObj.Values,
                                        ((Dictionary<string, object>)aBinValue.Value).Values);

            testRecord = testRecords.ElementAt(3);
            Assert.AreEqual("Key3", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("BinA123",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual("Key3",
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual("BinCKey3",
                                testRecord.GetValue("BinC").Value);

            //**************************************
            var aValues  = records
                            .Select(i => i.GetValue("BinB"))
                            .Where(i => i is not null)
                            .FindAll("Key3");
            //        .Dump("Values in BinB with value \"Key3\" as a value or within a collection");
            Assert.AreEqual(4, testRecords.Count());

            aBinValue = aValues.First();
            Assert.IsFalse(aBinValue.IsCDT);
            Assert.IsFalse(aBinValue.IsMap);
            Assert.IsFalse(aBinValue.IsDictionary);
            Assert.IsFalse(aBinValue.IsBool);
            Assert.IsTrue(aBinValue.IsKeyValuePair);
            Assert.IsFalse(aBinValue.IsEmpty);
            Assert.IsFalse(aBinValue.IsFloat);
            Assert.IsFalse(aBinValue.IsGeoJson);
            Assert.IsFalse(aBinValue.IsInt);
            Assert.IsFalse(aBinValue.IsJson);
            Assert.IsFalse(aBinValue.IsList);
            Assert.IsFalse(aBinValue.IsNumeric);
            Assert.IsFalse(aBinValue.IsString);
            Assert.IsFalse(aBinValue.IsTimeSpan);
            Assert.IsFalse(aBinValue.IsDateTime);
            Assert.IsFalse(aBinValue.IsDateTimeOffset);
            Assert.AreEqual("Key3", ((KeyValuePair<string, object>)aBinValue.Value).Key);
            Assert.AreEqual(7L, ((KeyValuePair<string, object>)aBinValue.Value).Value);

            aBinValue = aValues.ElementAt(1);
            Assert.IsFalse(aBinValue.IsCDT);
            Assert.IsFalse(aBinValue.IsMap);
            Assert.IsFalse(aBinValue.IsDictionary);
            Assert.IsFalse(aBinValue.IsBool);
            Assert.IsFalse(aBinValue.IsKeyValuePair);
            Assert.IsFalse(aBinValue.IsEmpty);
            Assert.IsFalse(aBinValue.IsFloat);
            Assert.IsFalse(aBinValue.IsGeoJson);
            Assert.IsFalse(aBinValue.IsInt);
            Assert.IsFalse(aBinValue.IsJson);
            Assert.IsFalse(aBinValue.IsList);
            Assert.IsFalse(aBinValue.IsNumeric);
            Assert.IsTrue(aBinValue.IsString);
            Assert.IsFalse(aBinValue.IsTimeSpan);
            Assert.IsFalse(aBinValue.IsDateTime);
            Assert.IsFalse(aBinValue.IsDateTimeOffset);
            Assert.AreEqual("Key3", aBinValue.Value);

            aBinValue = aValues.ElementAt(2);
            Assert.IsFalse(aBinValue.IsCDT);
            Assert.IsFalse(aBinValue.IsMap);
            Assert.IsFalse(aBinValue.IsDictionary);
            Assert.IsFalse(aBinValue.IsBool);
            Assert.IsTrue(aBinValue.IsKeyValuePair);
            Assert.IsFalse(aBinValue.IsEmpty);
            Assert.IsFalse(aBinValue.IsFloat);
            Assert.IsFalse(aBinValue.IsGeoJson);
            Assert.IsFalse(aBinValue.IsInt);
            Assert.IsFalse(aBinValue.IsJson);
            Assert.IsFalse(aBinValue.IsList);
            Assert.IsFalse(aBinValue.IsNumeric);
            Assert.IsFalse(aBinValue.IsString);
            Assert.IsFalse(aBinValue.IsTimeSpan);
            Assert.IsFalse(aBinValue.IsDateTime);
            Assert.IsFalse(aBinValue.IsDateTimeOffset);
            Assert.AreEqual("Key3", ((KeyValuePair<string, object>)aBinValue.Value).Key);
            Assert.AreEqual(456L, ((KeyValuePair<string, object>)aBinValue.Value).Value);

            aBinValue = aValues.ElementAt(3);
            Assert.IsFalse(aBinValue.IsCDT);
            Assert.IsFalse(aBinValue.IsMap);
            Assert.IsFalse(aBinValue.IsDictionary);
            Assert.IsFalse(aBinValue.IsBool);
            Assert.IsFalse(aBinValue.IsKeyValuePair);
            Assert.IsFalse(aBinValue.IsEmpty);
            Assert.IsFalse(aBinValue.IsFloat);
            Assert.IsFalse(aBinValue.IsGeoJson);
            Assert.IsFalse(aBinValue.IsInt);
            Assert.IsFalse(aBinValue.IsJson);
            Assert.IsFalse(aBinValue.IsList);
            Assert.IsFalse(aBinValue.IsNumeric);
            Assert.IsTrue(aBinValue.IsString);
            Assert.IsFalse(aBinValue.IsTimeSpan);
            Assert.IsFalse(aBinValue.IsDateTime);
            Assert.IsFalse(aBinValue.IsDateTimeOffset);
            Assert.AreEqual("Key3", aBinValue.Value);

            //**************************************
            testRecords = records.Where(dt => dt.GetValue("BinB")?.Contains("Key3", AValue.MatchOptions.Any) ?? false);
            //        .Dump("Records where BinB has value \"Key3\" as a value or anywhere (canbe an element, Key, or Value) within collection");
            Assert.AreEqual(5, testRecords.Count());

            testRecord = testRecords.First();
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("Map4", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(1, testRecord.Aerospike.GetValues().Count);
            aBinValue = testRecord.GetValue("BinB");
            Assert.IsTrue(aBinValue.IsCDT);
            Assert.IsTrue(aBinValue.IsMap);
            Assert.IsTrue(aBinValue.IsDictionary);
            Assert.IsFalse(aBinValue.IsBool);
            Assert.IsFalse(aBinValue.IsKeyValuePair);
            Assert.IsTrue((((IDictionary<string, object>)aBinValue.Value).First().ToAValue().IsKeyValuePair));
            Assert.IsFalse(aBinValue.IsEmpty);
            Assert.IsFalse(aBinValue.IsFloat);
            Assert.IsFalse(aBinValue.IsGeoJson);
            Assert.IsFalse(aBinValue.IsInt);
            Assert.IsFalse(aBinValue.IsJson);
            Assert.IsFalse(aBinValue.IsList);
            Assert.IsFalse(aBinValue.IsNumeric);
            Assert.IsFalse(aBinValue.IsString);
            Assert.IsFalse(aBinValue.IsTimeSpan);
            Assert.IsFalse(aBinValue.IsDateTime);
            Assert.IsFalse(aBinValue.IsDateTimeOffset);
            Assert.AreEqual(4, ((IDictionary<string, object>)aBinValue.Value).Count);
            dictStrObj = new Dictionary<string, object>() { { "Map10", "Key3" }, { "Map11", "Key3" }, { "Map9", "Map2Bin" }, { "key12", "BinA456" } };
            CollectionAssert.AreEqual(dictStrObj.Keys,
                                        ((Dictionary<string, object>)aBinValue.Value).Keys);
            CollectionAssert.AreEqual(dictStrObj.Values,
                                        ((Dictionary<string, object>)aBinValue.Value).Values);


            testRecord = testRecords.ElementAt(1);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("Map2", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(1, testRecord.Aerospike.GetValues().Count);
            aBinValue = testRecord.GetValue("BinB");
            Assert.IsTrue(aBinValue.IsCDT);
            Assert.IsTrue(aBinValue.IsMap);
            Assert.IsTrue(aBinValue.IsDictionary);
            Assert.IsFalse(aBinValue.IsBool);
            Assert.IsFalse(aBinValue.IsKeyValuePair);
            Assert.IsTrue((((IDictionary<string, object>)aBinValue.Value).First().ToAValue().IsKeyValuePair));
            Assert.IsFalse(aBinValue.IsEmpty);
            Assert.IsFalse(aBinValue.IsFloat);
            Assert.IsFalse(aBinValue.IsGeoJson);
            Assert.IsFalse(aBinValue.IsInt);
            Assert.IsFalse(aBinValue.IsJson);
            Assert.IsFalse(aBinValue.IsList);
            Assert.IsFalse(aBinValue.IsNumeric);
            Assert.IsFalse(aBinValue.IsString);
            Assert.IsFalse(aBinValue.IsTimeSpan);
            Assert.IsFalse(aBinValue.IsDateTime);
            Assert.IsFalse(aBinValue.IsDateTimeOffset);
            Assert.AreEqual(4, ((IDictionary<string, object>)aBinValue.Value).Count);
            dictStrObj = new Dictionary<string, object>() { { "Key3", 7L }, { "Map4", "Map2Bin" }, { "Map5", "ABKey3CD" }, { "key1", "BinA123" } };
            CollectionAssert.AreEqual(dictStrObj.Keys,
                                        ((Dictionary<string, object>)aBinValue.Value).Keys);
            CollectionAssert.AreEqual(dictStrObj.Values,
                                        ((Dictionary<string, object>)aBinValue.Value).Values);

            testRecord = testRecords.ElementAt(2);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("List3", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(1, testRecord.Aerospike.GetValues().Count);
            aBinValue = testRecord.GetValue("BinB");
            Assert.IsTrue(aBinValue.IsCDT);
            Assert.IsFalse(aBinValue.IsMap);
            Assert.IsFalse(aBinValue.IsDictionary);
            Assert.IsFalse(aBinValue.IsBool);
            Assert.IsFalse(aBinValue.IsKeyValuePair);
            Assert.IsFalse(aBinValue.IsEmpty);
            Assert.IsFalse(aBinValue.IsFloat);
            Assert.IsFalse(aBinValue.IsGeoJson);
            Assert.IsFalse(aBinValue.IsInt);
            Assert.IsFalse(aBinValue.IsJson);
            Assert.IsTrue(aBinValue.IsList);
            Assert.IsFalse(aBinValue.IsNumeric);
            Assert.IsFalse(aBinValue.IsString);
            Assert.IsFalse(aBinValue.IsTimeSpan);
            Assert.IsFalse(aBinValue.IsDateTime);
            Assert.IsFalse(aBinValue.IsDateTimeOffset);
            Assert.AreEqual(4, ((IList<object>)aBinValue.Value).Count);
            listObj = new List<object>() { "BinB123", 89L, "List3Bin", "Key3" };
            CollectionAssert.AreEqual(listObj,
                                        ((List<object>)aBinValue.Value));

            testRecord = testRecords.ElementAt(3);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("Map1", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(1, testRecord.Aerospike.GetValues().Count);
            aBinValue = testRecord.GetValue("BinB");
            Assert.IsTrue(aBinValue.IsCDT);
            Assert.IsTrue(aBinValue.IsMap);
            Assert.IsTrue(aBinValue.IsDictionary);
            Assert.IsFalse(aBinValue.IsBool);
            Assert.IsFalse(aBinValue.IsKeyValuePair);
            Assert.IsTrue((((IDictionary<string, object>)aBinValue.Value).First().ToAValue().IsKeyValuePair));
            Assert.IsFalse(aBinValue.IsEmpty);
            Assert.IsFalse(aBinValue.IsFloat);
            Assert.IsFalse(aBinValue.IsGeoJson);
            Assert.IsFalse(aBinValue.IsInt);
            Assert.IsFalse(aBinValue.IsJson);
            Assert.IsFalse(aBinValue.IsList);
            Assert.IsFalse(aBinValue.IsNumeric);
            Assert.IsFalse(aBinValue.IsString);
            Assert.IsFalse(aBinValue.IsTimeSpan);
            Assert.IsFalse(aBinValue.IsDateTime);
            Assert.IsFalse(aBinValue.IsDateTimeOffset);
            Assert.AreEqual(3, ((IDictionary<string, object>)aBinValue.Value).Count);
            dictStrObj = new Dictionary<string, object>() { { "Key2", "BinB123" }, { "Key3", 456L }, { "Key4", "Map1Bin" } };
            CollectionAssert.AreEqual(dictStrObj.Keys,
                                        ((Dictionary<string, object>)aBinValue.Value).Keys);
            CollectionAssert.AreEqual(dictStrObj.Values,
                                        ((Dictionary<string, object>)aBinValue.Value).Values);

            testRecord = testRecords.ElementAt(4);
            Assert.AreEqual("Key3", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("BinA123",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual("Key3",
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual("BinCKey3",
                                testRecord.GetValue("BinC").Value);

            //**************************************
            aValues = records
                            .Select(i => i.GetValue("BinB"))
                            .Where(i => i is not null)
                            .FindAll("Key3", AValue.MatchOptions.Any);
            //        .Dump("Values in BinB with value \"Key3\" as a value or anywhere within a collection");
            Assert.AreEqual(6, aValues.Count());

            aBinValue = aValues.ElementAt(0);
            Assert.IsFalse(aBinValue.IsCDT);
            Assert.IsFalse(aBinValue.IsMap);
            Assert.IsFalse(aBinValue.IsDictionary);
            Assert.IsFalse(aBinValue.IsBool);
            Assert.IsTrue(aBinValue.IsKeyValuePair);
            Assert.IsFalse(aBinValue.IsEmpty);
            Assert.IsFalse(aBinValue.IsFloat);
            Assert.IsFalse(aBinValue.IsGeoJson);
            Assert.IsFalse(aBinValue.IsInt);
            Assert.IsFalse(aBinValue.IsJson);
            Assert.IsFalse(aBinValue.IsList);
            Assert.IsFalse(aBinValue.IsNumeric);
            Assert.IsFalse(aBinValue.IsString);
            Assert.IsFalse(aBinValue.IsTimeSpan);
            Assert.IsFalse(aBinValue.IsDateTime);
            Assert.IsFalse(aBinValue.IsDateTimeOffset);
            Assert.AreEqual("Map10",
                            ((KeyValuePair<string,object>)aBinValue.Value).Key);
            Assert.AreEqual("Key3",
                            ((KeyValuePair<string, object>)aBinValue.Value).Value);

            aBinValue = aValues.ElementAt(1);
            Assert.IsFalse(aBinValue.IsCDT);
            Assert.IsFalse(aBinValue.IsMap);
            Assert.IsFalse(aBinValue.IsDictionary);
            Assert.IsFalse(aBinValue.IsBool);
            Assert.IsTrue(aBinValue.IsKeyValuePair);
            Assert.IsFalse(aBinValue.IsEmpty);
            Assert.IsFalse(aBinValue.IsFloat);
            Assert.IsFalse(aBinValue.IsGeoJson);
            Assert.IsFalse(aBinValue.IsInt);
            Assert.IsFalse(aBinValue.IsJson);
            Assert.IsFalse(aBinValue.IsList);
            Assert.IsFalse(aBinValue.IsNumeric);
            Assert.IsFalse(aBinValue.IsString);
            Assert.IsFalse(aBinValue.IsTimeSpan);
            Assert.IsFalse(aBinValue.IsDateTime);
            Assert.IsFalse(aBinValue.IsDateTimeOffset);
            Assert.AreEqual("Map11",
                            ((KeyValuePair<string, object>)aBinValue.Value).Key);
            Assert.AreEqual("Key3",
                            ((KeyValuePair<string, object>)aBinValue.Value).Value);

            aBinValue = aValues.ElementAt(2);
            Assert.IsFalse(aBinValue.IsCDT);
            Assert.IsFalse(aBinValue.IsMap);
            Assert.IsFalse(aBinValue.IsDictionary);
            Assert.IsFalse(aBinValue.IsBool);
            Assert.IsTrue(aBinValue.IsKeyValuePair);
            Assert.IsFalse(aBinValue.IsEmpty);
            Assert.IsFalse(aBinValue.IsFloat);
            Assert.IsFalse(aBinValue.IsGeoJson);
            Assert.IsFalse(aBinValue.IsInt);
            Assert.IsFalse(aBinValue.IsJson);
            Assert.IsFalse(aBinValue.IsList);
            Assert.IsFalse(aBinValue.IsNumeric);
            Assert.IsFalse(aBinValue.IsString);
            Assert.IsFalse(aBinValue.IsTimeSpan);
            Assert.IsFalse(aBinValue.IsDateTime);
            Assert.IsFalse(aBinValue.IsDateTimeOffset);
            Assert.AreEqual("Key3", ((KeyValuePair<string, object>)aBinValue.Value).Key);
            Assert.AreEqual(7L, ((KeyValuePair<string, object>)aBinValue.Value).Value);

            aBinValue = aValues.ElementAt(3);
            Assert.IsFalse(aBinValue.IsCDT);
            Assert.IsFalse(aBinValue.IsMap);
            Assert.IsFalse(aBinValue.IsDictionary);
            Assert.IsFalse(aBinValue.IsBool);
            Assert.IsFalse(aBinValue.IsKeyValuePair);
            Assert.IsFalse(aBinValue.IsEmpty);
            Assert.IsFalse(aBinValue.IsFloat);
            Assert.IsFalse(aBinValue.IsGeoJson);
            Assert.IsFalse(aBinValue.IsInt);
            Assert.IsFalse(aBinValue.IsJson);
            Assert.IsFalse(aBinValue.IsList);
            Assert.IsFalse(aBinValue.IsNumeric);
            Assert.IsTrue(aBinValue.IsString);
            Assert.IsFalse(aBinValue.IsTimeSpan);
            Assert.IsFalse(aBinValue.IsDateTime);
            Assert.IsFalse(aBinValue.IsDateTimeOffset);
            Assert.AreEqual("Key3", aBinValue.Value);

            aBinValue = aValues.ElementAt(4);
            Assert.IsFalse(aBinValue.IsCDT);
            Assert.IsFalse(aBinValue.IsMap);
            Assert.IsFalse(aBinValue.IsDictionary);
            Assert.IsFalse(aBinValue.IsBool);
            Assert.IsTrue(aBinValue.IsKeyValuePair);
            Assert.IsFalse(aBinValue.IsEmpty);
            Assert.IsFalse(aBinValue.IsFloat);
            Assert.IsFalse(aBinValue.IsGeoJson);
            Assert.IsFalse(aBinValue.IsInt);
            Assert.IsFalse(aBinValue.IsJson);
            Assert.IsFalse(aBinValue.IsList);
            Assert.IsFalse(aBinValue.IsNumeric);
            Assert.IsFalse(aBinValue.IsString);
            Assert.IsFalse(aBinValue.IsTimeSpan);
            Assert.IsFalse(aBinValue.IsDateTime);
            Assert.IsFalse(aBinValue.IsDateTimeOffset);
            Assert.AreEqual("Key3", ((KeyValuePair<string, object>)aBinValue.Value).Key);
            Assert.AreEqual(456L, ((KeyValuePair<string, object>)aBinValue.Value).Value);

            aBinValue = aValues.ElementAt(5);
            Assert.IsFalse(aBinValue.IsCDT);
            Assert.IsFalse(aBinValue.IsMap);
            Assert.IsFalse(aBinValue.IsDictionary);
            Assert.IsFalse(aBinValue.IsBool);
            Assert.IsFalse(aBinValue.IsKeyValuePair);
            Assert.IsFalse(aBinValue.IsEmpty);
            Assert.IsFalse(aBinValue.IsFloat);
            Assert.IsFalse(aBinValue.IsGeoJson);
            Assert.IsFalse(aBinValue.IsInt);
            Assert.IsFalse(aBinValue.IsJson);
            Assert.IsFalse(aBinValue.IsList);
            Assert.IsFalse(aBinValue.IsNumeric);
            Assert.IsTrue(aBinValue.IsString);
            Assert.IsFalse(aBinValue.IsTimeSpan);
            Assert.IsFalse(aBinValue.IsDateTime);
            Assert.IsFalse(aBinValue.IsDateTimeOffset);
            Assert.AreEqual("Key3", aBinValue.Value);


            //**************************************
            testRecords = records.Where(dt => dt.GetValue("BinB")?.Contains("Key3", AValue.MatchOptions.Any | AValue.MatchOptions.SubString) ?? false);
            //        .Dump("Records where BinB has value \"Key3\" as a substring within a value or anywhere (canbe an element, Key, or Value) within collection");
            Assert.AreEqual(6, testRecords.Count());

            testRecord = testRecords.First();
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("Map4", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(1, testRecord.Aerospike.GetValues().Count);
            aBinValue = testRecord.GetValue("BinB");
            Assert.IsTrue(aBinValue.IsCDT);
            Assert.IsTrue(aBinValue.IsMap);
            Assert.IsTrue(aBinValue.IsDictionary);
            Assert.IsFalse(aBinValue.IsBool);
            Assert.IsFalse(aBinValue.IsKeyValuePair);
            Assert.IsTrue((((IDictionary<string, object>)aBinValue.Value).First().ToAValue().IsKeyValuePair));
            Assert.IsFalse(aBinValue.IsEmpty);
            Assert.IsFalse(aBinValue.IsFloat);
            Assert.IsFalse(aBinValue.IsGeoJson);
            Assert.IsFalse(aBinValue.IsInt);
            Assert.IsFalse(aBinValue.IsJson);
            Assert.IsFalse(aBinValue.IsList);
            Assert.IsFalse(aBinValue.IsNumeric);
            Assert.IsFalse(aBinValue.IsString);
            Assert.IsFalse(aBinValue.IsTimeSpan);
            Assert.IsFalse(aBinValue.IsDateTime);
            Assert.IsFalse(aBinValue.IsDateTimeOffset);
            Assert.AreEqual(4, ((IDictionary<string, object>)aBinValue.Value).Count);
            dictStrObj = new Dictionary<string, object>() { { "Map10", "Key3" }, { "Map11", "Key3" }, { "Map9", "Map2Bin" }, { "key12", "BinA456" } };
            CollectionAssert.AreEqual(dictStrObj.Keys,
                                        ((Dictionary<string, object>)aBinValue.Value).Keys);
            CollectionAssert.AreEqual(dictStrObj.Values,
                                        ((Dictionary<string, object>)aBinValue.Value).Values);

            testRecord = testRecords.ElementAt(1);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("Map3", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(1, testRecord.Aerospike.GetValues().Count);
            aBinValue = testRecord.GetValue("BinB");
            Assert.IsTrue(aBinValue.IsCDT);
            Assert.IsTrue(aBinValue.IsMap);
            Assert.IsTrue(aBinValue.IsDictionary);
            Assert.IsFalse(aBinValue.IsBool);
            Assert.IsFalse(aBinValue.IsKeyValuePair);
            Assert.IsTrue((((IDictionary<string, object>)aBinValue.Value).First().ToAValue().IsKeyValuePair));
            Assert.IsFalse(aBinValue.IsEmpty);
            Assert.IsFalse(aBinValue.IsFloat);
            Assert.IsFalse(aBinValue.IsGeoJson);
            Assert.IsFalse(aBinValue.IsInt);
            Assert.IsFalse(aBinValue.IsJson);
            Assert.IsFalse(aBinValue.IsList);
            Assert.IsFalse(aBinValue.IsNumeric);
            Assert.IsFalse(aBinValue.IsString);
            Assert.IsFalse(aBinValue.IsTimeSpan);
            Assert.IsFalse(aBinValue.IsDateTime);
            Assert.IsFalse(aBinValue.IsDateTimeOffset);
            Assert.AreEqual(3, ((IDictionary<string, object>)aBinValue.Value).Count);
            dictStrObj = new Dictionary<string, object>() { { "Map7", "Map2Bin" }, { "Map8", "ABKey3CD" }, { "key1", "BinA456" }};
            CollectionAssert.AreEqual(dictStrObj.Keys,
                                        ((Dictionary<string, object>)aBinValue.Value).Keys);
            CollectionAssert.AreEqual(dictStrObj.Values,
                                        ((Dictionary<string, object>)aBinValue.Value).Values);

            testRecord = testRecords.ElementAt(2);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("Map2", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(1, testRecord.Aerospike.GetValues().Count);
            aBinValue = testRecord.GetValue("BinB");
            Assert.IsTrue(aBinValue.IsCDT);
            Assert.IsTrue(aBinValue.IsMap);
            Assert.IsTrue(aBinValue.IsDictionary);
            Assert.IsFalse(aBinValue.IsBool);
            Assert.IsFalse(aBinValue.IsKeyValuePair);
            Assert.IsTrue((((IDictionary<string, object>)aBinValue.Value).First().ToAValue().IsKeyValuePair));
            Assert.IsFalse(aBinValue.IsEmpty);
            Assert.IsFalse(aBinValue.IsFloat);
            Assert.IsFalse(aBinValue.IsGeoJson);
            Assert.IsFalse(aBinValue.IsInt);
            Assert.IsFalse(aBinValue.IsJson);
            Assert.IsFalse(aBinValue.IsList);
            Assert.IsFalse(aBinValue.IsNumeric);
            Assert.IsFalse(aBinValue.IsString);
            Assert.IsFalse(aBinValue.IsTimeSpan);
            Assert.IsFalse(aBinValue.IsDateTime);
            Assert.IsFalse(aBinValue.IsDateTimeOffset);
            Assert.AreEqual(4, ((IDictionary<string, object>)aBinValue.Value).Count);
            dictStrObj = new Dictionary<string, object>() { { "Key3", 7L }, { "Map4", "Map2Bin" }, { "Map5", "ABKey3CD" }, { "key1", "BinA123" } };
            CollectionAssert.AreEqual(dictStrObj.Keys,
                                        ((Dictionary<string, object>)aBinValue.Value).Keys);
            CollectionAssert.AreEqual(dictStrObj.Values,
                                        ((Dictionary<string, object>)aBinValue.Value).Values);

            testRecord = testRecords.ElementAt(3);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("List3", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(1, testRecord.Aerospike.GetValues().Count);
            aBinValue = testRecord.GetValue("BinB");
            Assert.IsTrue(aBinValue.IsCDT);
            Assert.IsFalse(aBinValue.IsMap);
            Assert.IsFalse(aBinValue.IsDictionary);
            Assert.IsFalse(aBinValue.IsBool);
            Assert.IsFalse(aBinValue.IsKeyValuePair);
            Assert.IsFalse(aBinValue.IsEmpty);
            Assert.IsFalse(aBinValue.IsFloat);
            Assert.IsFalse(aBinValue.IsGeoJson);
            Assert.IsFalse(aBinValue.IsInt);
            Assert.IsFalse(aBinValue.IsJson);
            Assert.IsTrue(aBinValue.IsList);
            Assert.IsFalse(aBinValue.IsNumeric);
            Assert.IsFalse(aBinValue.IsString);
            Assert.IsFalse(aBinValue.IsTimeSpan);
            Assert.IsFalse(aBinValue.IsDateTime);
            Assert.IsFalse(aBinValue.IsDateTimeOffset);
            Assert.AreEqual(4, ((IList<object>)aBinValue.Value).Count);
            listObj = new List<object>() { "BinB123", 89L, "List3Bin", "Key3" };
            CollectionAssert.AreEqual(listObj,
                                        ((List<object>)aBinValue.Value));

            testRecord = testRecords.ElementAt(4);
            Assert.IsNotNull(testRecord.Aerospike.PrimaryKey);
            Assert.IsTrue(testRecord.Aerospike.PrimaryKey.HasKeyValue);
            Assert.AreEqual("Map1", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(1, testRecord.Aerospike.GetValues().Count);
            aBinValue = testRecord.GetValue("BinB");
            Assert.IsTrue(aBinValue.IsCDT);
            Assert.IsTrue(aBinValue.IsMap);
            Assert.IsTrue(aBinValue.IsDictionary);
            Assert.IsFalse(aBinValue.IsBool);
            Assert.IsFalse(aBinValue.IsKeyValuePair);
            Assert.IsTrue((aBinValue.ToDictionary().First().ToAValue().IsKeyValuePair));
            Assert.IsFalse(aBinValue.IsEmpty);
            Assert.IsFalse(aBinValue.IsFloat);
            Assert.IsFalse(aBinValue.IsGeoJson);
            Assert.IsFalse(aBinValue.IsInt);
            Assert.IsFalse(aBinValue.IsJson);
            Assert.IsFalse(aBinValue.IsList);
            Assert.IsFalse(aBinValue.IsNumeric);
            Assert.IsFalse(aBinValue.IsString);
            Assert.IsFalse(aBinValue.IsTimeSpan);
            Assert.IsFalse(aBinValue.IsDateTime);
            Assert.IsFalse(aBinValue.IsDateTimeOffset);
            Assert.AreEqual(3, ((IDictionary<string, object>)aBinValue.Value).Count);
            dictStrObj = new Dictionary<string, object>() { { "Key2", "BinB123" }, { "Key3", 456L }, { "Key4", "Map1Bin" } };
            CollectionAssert.AreEqual(dictStrObj.Keys,
                                        ((Dictionary<string, object>)aBinValue.Value).Keys);
            CollectionAssert.AreEqual(dictStrObj.Values,
                                        ((Dictionary<string, object>)aBinValue.Value).Values);

            {
                var jDoc = aBinValue.ToJObject();
                Assert.IsInstanceOfType<JObject>(jDoc);
                Assert.AreEqual(3, jDoc.Count);
                var dictObj = aBinValue.ToDictionary();
                Assert.AreEqual(3, dictObj.Count);
            }

            testRecord = testRecords.ElementAt(5);
            Assert.AreEqual("Key3", testRecord.Aerospike.PrimaryKey.Value);
            Assert.IsNotNull(testRecord.Aerospike.GetValues());
            Assert.AreEqual(3, testRecord.Aerospike.GetValues().Count);
            Assert.AreEqual("BinA123",
                                testRecord.GetValue("BinA").Value);
            Assert.AreEqual("Key3",
                                testRecord.GetValue("BinB").Value);
            Assert.AreEqual("BinCKey3",
                                testRecord.GetValue("BinC").Value);

        }

    }
}