using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aerospike.Database.LINQPadDriver.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

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
    }
}