using Aerospike.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Aerospike.Database.LINQPadDriver;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Aerospike.Database.LINQPadDriver.Extensions;
using LPEDC = LINQPad.Extensibility.DataContext;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.FileIO;
using static Aerospike.Client.Value;
using System.Collections.Specialized;
using System.Reflection.Metadata.Ecma335;

namespace Aerospike.Client
{
    /// <summary>
    /// Instructs the Aerospike LinqPad Driver to use this name for the bin instead of the field/property name.
    /// </summary>
    /// <seealso cref="ConstructorAttribute"/>
    /// <seealso cref="BinIgnoreAttribute"/>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class BinNameAttribute : Attribute
    {
        public BinNameAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }

        public string PropertyName { get => this.Name; set => this.Name = value; }
    }

    /// <summary>
    /// Instructs the Serializer to use the specified constructor when deserializing that object.
    /// </summary>
    /// <seealso cref="BinNameAttribute"/>
    /// <seealso cref="BinIgnoreAttribute"/>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class ConstructorAttribute : Attribute
    {        
    }

    /// <summary>
    /// Instructs the Aerospike LinqPad Driver not to serialize/deserialize the field/property value.
    /// </summary>
    /// <seealso cref="ConstructorAttribute"/>
    /// <seealso cref="BinNameAttribute"/>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class BinIgnoreAttribute : Attribute
    {
    }

    public static class LPDHelpers
    {
        public static IEnumerable<Record> AsEnumerable(this RecordSet recordSet)
        {
                while (recordSet.Next())
                {
                    yield return recordSet.Record;
                }
        }

        public static T Cast<T>(this Bin bin) => bin is null ? default(T) : (T) Helpers.CastToNativeType(bin.name, typeof(T), bin.name, bin.value.Object);
        public static T Cast<T>(this Value value) => value is null ? default(T) : (T) Helpers.CastToNativeType("Value", typeof(T), "Value", value.Object);

        public static AValue ToAValue(this Bin bin) => new AValue(bin);
        public static AValue ToAValue(this Value value) => new AValue(value, "Value", "Value");
        public static APrimaryKey ToAValue(this Key key) => new APrimaryKey(key);
        public static AValue ToAValue(this object value) => new AValue(value, "Object", "Value");
    }


}

namespace Aerospike.Database.LINQPadDriver
{
    public static class Helpers
    {

        /// <summary>
        /// Checks to see if <paramref name="interfaceClass"/> is a subclass of <paramref name="classToCheck"/>.
        /// If the types are generic, the underlying types are ignored.
        /// </summary>
        /// <param name="interfaceClass"></param>
        /// <param name="classToCheck"></param>
        /// <returns></returns>
        public static bool IsSubclassOfInterface(Type interfaceClass, Type classToCheck)
        {
            if (interfaceClass is null || classToCheck is null) return false;
            if (ReferenceEquals(interfaceClass, classToCheck)) return true;

            if (classToCheck.IsGenericType)
            {
                if (!classToCheck.IsGenericTypeDefinition)
                    classToCheck = classToCheck.GetGenericTypeDefinition();
            }
            if (interfaceClass.IsGenericType)
            {
                if (!interfaceClass.IsGenericTypeDefinition)
                    interfaceClass = interfaceClass.GetGenericTypeDefinition();
            }

            if (ReferenceEquals(interfaceClass, classToCheck)) return true;

            return classToCheck.GetInterfaces().Any(ctc => IsSubclassOfInterface(interfaceClass, ctc));

        }

        public static string CheckName(string name, string contextType)
        {
            var changeList = new char[] { '.', ' ', '[', ']', '(', ')', '+', '-', '=', '^', '*', '/', '\\', ',', '<', '>', ';', ':', '@', '#', '!', '%', '&', '?', '~', '`', '$', '"', '\'' };
            StringBuilder newName = new StringBuilder();

            foreach (char c in name)
            {
                if (changeList.Contains(c))
                {
                    newName.Append('_');
                }
                else
                    newName.Append(c);
            }

            var newStr = newName.ToString();

            if (LPEDC.DataContextDriver.IsCSharpKeyword(newStr))
            {
                return newStr + '_' + contextType;
            }

            return newStr;
        }
        public static int GetHashCode(string[] strings) => ((IStructuralEquatable)strings)?.GetHashCode(EqualityComparer<string>.Default) ?? 0;

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public static string GetRealTypeName(Type t, bool makeIntoNullable = false)
        {
            if (t == null) return null;
            
            if (!t.IsGenericType)
                return makeIntoNullable && t.IsValueType
                            ? $"Nullable<{t.Name}>"
                            : t.Name;

            StringBuilder sb = new StringBuilder();
            sb.Append(t.Name.Substring(0, t.Name.IndexOf('`')));
            sb.Append('<');
            bool appendComma = false;
            foreach (Type arg in t.GetGenericArguments())
            {
                if (appendComma) sb.Append(',');
                sb.Append(GetRealTypeName(arg));
                appendComma = true;
            }
            sb.Append('>');
            return makeIntoNullable && t.IsValueType
                            ? $"Nullable<{sb}>"
                            : sb.ToString();
        }

        private static string GetBinNameFromProperty(PropertyInfo property)
        {
            if (Attribute.IsDefined(property, typeof(BinIgnoreAttribute)))
                return null;

            string binName;

            if (Attribute.IsDefined(property, typeof(BinNameAttribute)))
            {
                binName = ((BinNameAttribute)Attribute.GetCustomAttribute(property, typeof(BinNameAttribute), false))
                                ?.Name
                            ?? property.Name;
            }
            else
                binName = property.Name;

            return binName;
        }

        private static string GetBinNameFromField(FieldInfo field)
        {
            if (Attribute.IsDefined(field, typeof(BinIgnoreAttribute)))
                return null;

            string binName;

            if (Attribute.IsDefined(field, typeof(BinNameAttribute)))
            {
                binName = ((BinNameAttribute)Attribute.GetCustomAttribute(field, typeof(BinNameAttribute), false))
                                ?.Name
                            ?? field.Name;
            }
            else
                binName = field.Name;

            return binName;
        }


        private static ConstructorInfo GetConstructorInfo(Type type)
        {
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            ConstructorInfo constructor = null;

            if (constructors.Length > 0)
            {
                if (constructors.Length == 1 && constructors.First().GetParameters().Length == 0)
                {
                    return null;
                }

                bool hasDefault = false;

                foreach (var item in constructors)
                {
                    if (item.CustomAttributes.Any(a => a.AttributeType == typeof(ConstructorAttribute)))
                    {
                        return item;
                    }
                    if (item.GetParameters().Length == 0)
                    {
                        hasDefault = true;
                    }
                    else
                    {
                        constructor = item;
                    }
                }

                if (hasDefault) return null;
            }

            return constructor;
        }

        public static T[] RemoveDups<T>(IEnumerable<T> items)
        {
            return items.GroupBy(g => g).Select(g => g.First()).ToArray();
        }

        public static bool SequenceEquals<T>(IEnumerable<T> items, object obj)
        {
            if(items is null) return obj is null;

            if(ReferenceEquals(items, obj)) return true;

            if (obj is IEnumerable<T> tItems) return items.SequenceEqual(tItems);

            if(IsSubclassOfInterface(typeof(ICollection), obj.GetType()))
            {
                var cItems = (ICollection)obj;

                if(items.Count() == cItems.Count)
                {
                    var newArray = Array.CreateInstance(typeof(object), cItems.Count);
                    var itemArray = items.ToArray();
                    
                    cItems.CopyTo(newArray, 0);
                    
                    Array.Sort(newArray);
                    Array.Sort(itemArray);

                    var result = true;

                    for(var idx = 0; idx < cItems.Count;idx++)
                    {
                        if (CompareTo(newArray.GetValue(idx), itemArray[idx]))
                        {
                            result = false;
                            break;
                        }
                    }

                    return result;
                }
            }

            return false;
        }

        public static bool IsAerospikeType(Type type)
        {
            return type.IsPrimitive
                        || type == typeof(string)
                        || type.IsSubclassOf(typeof(Client.Value));
        }

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long NanosFromEpoch(DateTime dt)
        {
            return (long)dt.ToUniversalTime().Subtract(UnixEpoch).TotalMilliseconds * 1000000;
        }

        public static DateTime NanoEpochToDateTime(long nanoseconds)
        {
            return UnixEpoch.AddTicks(nanoseconds / 100);
        }

        /// <summary>
        /// Format used to serialize or deserialize a date to/from string 
        /// A null value will use the default format.
        /// <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings"/>
        /// </summary>
        public static string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.ffff";
        /// <summary>
        /// Format used to serialize or deserialize a date offset to/from string 
        /// A null value will use the default format.
        /// <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings"/>
        /// </summary>
        public static string DateTimeOffsetFormat = "yyyy-MM-ddTHH:mm:ss.ffffzzz";
        /// <summary>
        /// Format used to serialize or deserialize a time to/from string 
        /// A null value will use the default format.
        /// <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings"/>
        /// </summary>
        public static string TimeSpanFormat = "c";

        /// <summary>
        /// A boolean, if true numeric values from the DB for targeted Date/Time data types are nanoseconds from Unix Epoch.
        /// If false, the numeric value represents .net ticks.
        /// <see cref="DateTime.DateTime(long)"/>
        /// <see cref="DateTimeOffset.DateTimeOffset(long, TimeSpan)"/>
        /// <see cref="Client.Exp.Val(DateTime)"/>
        /// <see cref="AllDateTimeUseUnixEpochNano"/>
        /// </summary>
        public static bool UseUnixEpochNanoForNumericDateTime = true;

        /// <summary>
        /// All Date/Time values are converted to nanoseconds from Unix Epoch Date/Time.
        /// </summary>
        /// <see cref="UseUnixEpochNanoForNumericDateTime"/>
        public static bool AllDateTimeUseUnixEpochNano = false;

        private static object ConvertToAerospikeType(object putObject)
        {
            if (putObject == null)
                return null;

            if (!IsAerospikeType(putObject.GetType()))
            {
                if (putObject is Decimal decValue)
                {
                    putObject = (double)decValue;
                }
                else if (putObject is Enum enumValue)
                {
                    putObject = putObject.ToString();
                }
                else if (putObject is DateTime dateTimeValue)
                {
                    putObject = AllDateTimeUseUnixEpochNano
                                    ? (object) NanosFromEpoch(dateTimeValue)
                                    : dateTimeValue.ToString(DateTimeFormat);
                }
                else if (putObject is DateTimeOffset dateTimeOffsetValue)
                {
                    putObject = AllDateTimeUseUnixEpochNano
                                    ? (object) NanosFromEpoch(dateTimeOffsetValue.UtcDateTime)
                                    : dateTimeOffsetValue.ToString(DateTimeOffsetFormat);
                }
                else if (putObject is TimeSpan timeSpanValue)
                {
                    putObject = AllDateTimeUseUnixEpochNano
                                    ? (object) ((long) timeSpanValue.TotalMilliseconds * 1000000L)
                                    : timeSpanValue.ToString(TimeSpanFormat);
                }
                else if (putObject is Guid guidValue)
                {
                    putObject = guidValue.ToString();
                }                
                else if (putObject is Newtonsoft.Json.Linq.JToken jToken)
                {
                    putObject = jToken.ToObject<Dictionary<string, object>>();
                }
                else if (putObject is IDictionary dictValue)
                {
                    var genericTypes = dictValue.GetType().GetGenericArguments();

                    if (genericTypes.Length == 0
                            || !IsAerospikeType(genericTypes[0])
                            || !IsAerospikeType(genericTypes[1]))
                    {
                        var newDict = new Dictionary<object, object>();
                        var keys = new List<object>();
                        var values = new List<object>();

                        foreach (var key in dictValue.Keys)
                        {
                            keys.Add(ConvertToAerospikeType(key));
                        }

                        foreach (var value in dictValue.Values)
                        {
                            values.Add(ConvertToAerospikeType(value));
                        }

                        for (var idx = 0; idx < keys.Count; idx++)
                        {
                            newDict.Add(keys[idx],
                                        values[idx]);
                        }
                        putObject = newDict;
                    }
                }
                else if (putObject is IEnumerable enumerableValue)
                {
                    var genericTypes = enumerableValue.GetType().GetGenericArguments();

                    if (genericTypes.Length == 0 || !IsAerospikeType(genericTypes[0]))
                    {
                        var newList = new List<object>();

                        foreach (var item in enumerableValue)
                        {
                            newList.Add(ConvertToAerospikeType(item));
                        }
                        putObject = newList;
                    }
                }
                else
                {
                    putObject = TransForm(putObject, nestedItem: true);
                }
            }

            return putObject;
        }

        /// <summary>
        /// Transform object into a Dictionary that can be used with generating a document or bins in Aerospike.
        /// 
        /// <see cref="Aerospike.Client.BinNameAttribute"/> -- defines the bin name, otherwise the kvPair name is used
        /// <see cref="Aerospike.Client.BinIgnoreAttribute"/> -- will ignore this kvPair
        /// </summary>
        /// <param name="instance">Item being transformed</param>
        /// <param name="transform">
        /// A action that is called to perform customized transformation. 
        /// First argument -- the name of the kvPair
        /// Second argument -- the name of the bin (can be different from kvPair if <see cref="Aerospike.Client.BinNameAttribute"/> is defined)
        /// Third argument -- the instance being transformed
        /// Fourth argument -- if true the instance is within another object.
        /// Returns the new transformed object or null to indicate that this kvPair should be skipped.
        /// </param>
        /// <param name="nestedItem">Indicates if item was nested inside another object.</param>
        /// <returns>
        /// The Dictionary used to pass to Aerospike's put command.
        /// </returns>
        /// <seealso cref="Aerospike.Client.BinNameAttribute"/>
        /// <seealso cref="Aerospike.Client.BinIgnoreAttribute"/>
        /// <seealso cref="CreateBinRecord(object, string, Bin[])"/>
        public static Dictionary<string, object> TransForm(object instance,
                                                                Func<string, string, object, bool, object> transform = null,
                                                                bool nestedItem = false)
        {

            var dictionary = new Dictionary<string, object>();

            foreach (var property in instance.GetType().GetProperties())
            {

                string binName = GetBinNameFromProperty(property);
                object putObject;

                if (string.IsNullOrEmpty(binName)) continue;

                if (transform == null)
                {
                    putObject = property.GetValue(instance);
                    dictionary.Add(binName, ConvertToAerospikeType(putObject));
                }
                else
                {
                    putObject = transform.Invoke(property.Name, binName, property.GetValue(instance), nestedItem);
                    if (putObject != null)
                        dictionary.Add(binName, putObject);
                }

            }

            foreach (var field in instance.GetType().GetFields())
            {
                string binName = GetBinNameFromField(field);
                object putObject;

                if (string.IsNullOrEmpty(binName)) continue;

                if (transform == null)
                {
                    putObject = field.GetValue(instance);
                    dictionary.Add(binName, ConvertToAerospikeType(putObject));
                }
                else
                {
                    putObject = transform.Invoke(field.Name, binName, field.GetValue(instance), nestedItem);
                    if (putObject != null)
                        dictionary.Add(binName, putObject);
                }
            }

            return dictionary;
        }
        
        public static Bin[] CreateBinRecord<K,V>(IDictionary<K,V> dict,
                                                    string prefix = null,
                                                    params Bin[] additionalBins)
        {
            var bins = new List<Aerospike.Client.Bin>(additionalBins);

            if (typeof(K) == typeof(string))
            {
                if (IsAerospikeType(typeof(V)))
                {
                    foreach (var kvPair in dict)
                    {
                        var binName = prefix == null ? kvPair.Key.ToString() : $"{prefix}.{kvPair.Key}";
                        bins.Add(new Bin(binName, kvPair.Value));
                    }                    
                }
                else
                {
                    foreach (var kvPair in dict)
                    {
                        var binName = prefix == null ? kvPair.Key.ToString() : $"{prefix}.{kvPair.Key}";
                        bins.Add(new Bin(binName, ConvertToAerospikeType(kvPair.Value)));
                    }
                }

                return bins.ToArray();
            }

            if(IsAerospikeType(typeof(K)) && IsAerospikeType(typeof(V)))
            {
                bins.Add(new Bin(prefix, dict));
            }
            else
            {
                bins.Add(new Bin(prefix, ConvertToAerospikeType(dict)));
            }

            return bins.ToArray();
        }

        public static Bin CreateBinRecord<T>(IEnumerable<T> collection,
                                                string binName)
        {            
            if(IsAerospikeType(typeof(T)))
                return new Bin(binName, collection.ToList());

            var newLst = new List<object>();

            foreach (var value in collection)
            {
                newLst.Add(ConvertToAerospikeType(value));
            }

            return new Bin(binName, newLst);
        }

        public static Bin CreateBinRecord<K,V>(IEnumerable<KeyValuePair<K,V>> collection,
                                                string binName)
        {
            if (IsAerospikeType(typeof(K)) && IsAerospikeType(typeof(V)))
                return new Bin(binName, collection.ToDictionary(k => k.Key, v => v.Value));

            var newDict = new Dictionary<object,object>();

            foreach (var kvp in collection)
            {
                newDict.Add(ConvertToAerospikeType(kvp.Key), ConvertToAerospikeType(kvp.Value));
            }

            return new Bin(binName, newDict);
        }

        /// <summary>
        /// Creates an array of bins based on <paramref name="item"/> and <paramref name="additionalBins"/>
        /// </summary>
        /// <param name="item">
        /// If item is an IDictionary&lt;string, object&gt; 
        ///     each element is evaluated and a new bin created where the key is the bin name and value is the bin&apos;s value
        /// if item is an IList&lt;object&gt;
        ///     each element id evaluated and if the element is a list or dictionary, this method is recursively called.
        ///     The bins created are added to the collection. 
        ///     If the element is not a list or dictionary, a bin is created using the <paramref name="prefix"/> as the bin name and the element as the value.
        /// If item is neither of the above. A bin is created where <paramref name="prefix"/> is the name and item is the value.
        /// </param>
        /// <param name="prefix">
        /// Depending on the type of <paramref name="item"/> will determine how it is used.
        /// If <paramref name="item"/> is a dictionary, prefix is a prefix to the key as part of the bin name.
        /// If it is a list, the prefix is passed this method or used as the bin name...
        /// Otherwise it is used as the bin name.
        /// </param>
        /// <param name="additionalBins">
        /// Bins that will be part of the array of bins returned.
        /// </param>
        /// <returns>
        /// An array for bins based on <paramref name="item"/>.
        /// </returns>
        public static Bin[] CreateBinRecord(object item, string prefix = null, params Bin[] additionalBins)
        {
            var bins = new List<Aerospike.Client.Bin>(additionalBins);

            if (item is IDictionary<string, object> dict)
            {
                bins.AddRange(CreateBinRecord(dict, prefix));
            }
            else if (item is IList<object> lst)
            {
                bins.Add(CreateBinRecord(lst, prefix));
            }
            else
            {
                bins.Add(new Bin(prefix, item));
            }

            return bins.ToArray();
        }
        
        /// <summary>
        /// Compares two values to determine if they are equal
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>True if Equal</returns>
        public static bool CompareTo(object a, object b)
        {
            if (a is null) return b is null;
            if (b is null) return false;

            if (a is string sa)
            {
                if (b is string sb) return sa == sb;

                return sa == b.ToString();
            }
            if (b is string ba)
            {               
                return ba == a.ToString();
            }


            if (a is IEnumerable<object> alist)
                return SequenceEquals(alist, b);
            if (b is IEnumerable<object> blist)
                return SequenceEquals(blist, a);

            var aType = a.GetType();
            var bType = b.GetType();

            if (Helpers.IsSubclassOfInterface(typeof(Nullable<>), aType))
            {
                return Helpers.CompareTo(((dynamic)a).Value, b);
            }

            if (Helpers.IsSubclassOfInterface(typeof(Nullable<>), bType))
            {
                return Helpers.CompareTo(a, ((dynamic)b).Value);
            }

            if (aType == bType) return a.Equals(b);

            if(!aType.IsGenericType
                && !bType.IsGenericType
                && Marshal.SizeOf(aType) < Marshal.SizeOf(bType))
            {
                if(Helpers.IsSubclassOfInterface(typeof(IConvertible), aType))
                    return ((IConvertible)a).ToType(bType, null).Equals(b);

            }

            if (Helpers.IsSubclassOfInterface(typeof(IConvertible), bType))
                return ((IConvertible)b).ToType(aType, null).Equals(a);

            return false;
        }

        /// <summary>
        /// Based on <paramref name="fldType"/>, creates a .Net Native (int, decimal, string, datetime, etc.) instance based on <paramref name="binValue"/>.
        /// </summary>
        /// <param name="fldName">
        /// Used mostly for detailed exception messages in case of errors.
        /// </param>
        /// <param name="fldType">
        /// Used to create an instance of this type.
        /// </param>
        /// <param name="binName">
        /// Used mostly for detailed exception messages in case of errors.
        /// </param>
        /// <param name="binValue">
        /// The bin value used to create <paramref name="fldType"/>. This value maybe returned, if the types match. 
        /// </param>
        /// <returns>
        /// An instance of <paramref name="fldType"/> or just <paramref name="binValue"/> depending if their types match.
        /// </returns>
        public static object CastToNativeType(string fldName, Type fldType, string binName, object binValue)
        {
            //Debugger.Launch();

            Exception CreateException<T>(T castValue)
            {
                var castStr = castValue == null
                                ? "null"
                                : (castValue is string
                                    ? $"\"{castValue}\""
                                    : castValue.ToString());
                if (binName == fldName)
                    return new ArgumentException($"Bin \"{binName}\" with Value {castStr} ({GetRealTypeName(binValue?.GetType()) ?? "<UnKnownType>"}) could not be cast to field type {GetRealTypeName(fldType)}");

                return new ArgumentException($"Bin \"{binName}\" with Value {castStr} ({GetRealTypeName(binValue?.GetType()) ?? "<UnKnownType>"}) could not be cast to Field \"{fldName}\" of type {GetRealTypeName(fldType)}");
            }

            if (fldType == typeof(object) || fldType == binValue?.GetType())
            {
                return binValue;
            }
            else
            {
                switch (binValue)
                {
                    case byte byteValue:
                        {
                            if (fldType.IsGenericType && fldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            {
                                return binValue is null 
                                        ? null
                                        : CastToNativeType(fldName, fldType.GetGenericArguments()[0], binName, binValue);
                            }

                            if (fldType == typeof(short))
                            {
                                return (short)byteValue;
                            }
                            else if (fldType == typeof(int))
                            {
                                return (int)byteValue;
                            }
                            else if (fldType == typeof(uint))
                            {
                                return (uint)byteValue;
                            }
                            else if (fldType == typeof(ulong))
                            {
                                return (ulong)byteValue;
                            }
                            else if (fldType == typeof(ushort))
                            {
                                return (ushort)byteValue;
                            }
                            else if (fldType == typeof(decimal))
                            {
                                return (decimal)byteValue;
                            }
                            else if (fldType == typeof(float))
                            {
                                return (float)byteValue;
                            }
                            else if (fldType == typeof(double))
                            {
                                return (double)byteValue;
                            }
                            else if (fldType == typeof(bool))
                            {
                                return byteValue != 0;
                            }
                            else if (fldType == typeof(string))
                            {
                                return byteValue.ToString();
                            }
                            else
                            {
                                throw CreateException(byteValue);
                            }
                        }
                    case Int64 intValue:
                        {
                            if (fldType.IsGenericType && fldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            {
                                return binValue is null 
                                        ? null
                                        : CastToNativeType(fldName, fldType.GetGenericArguments()[0], binName, binValue);
                            }

                            if (fldType == typeof(short))
                            {
                                return (short)intValue;
                            }
                            else if (fldType == typeof(int))
                            {
                                return (int)intValue;
                            }
                            else if (fldType == typeof(uint))
                            {
                                return (uint)intValue;
                            }
                            else if (fldType == typeof(ulong))
                            {
                                return (ulong)intValue;
                            }
                            else if (fldType == typeof(ushort))
                            {
                                return (ushort)intValue;
                            }
                            else if (fldType == typeof(decimal))
                            {
                                return (decimal)intValue;
                            }
                            else if (fldType == typeof(float))
                            {
                                return (float)intValue;
                            }
                            else if (fldType == typeof(double))
                            {
                                return (double)intValue;
                            }
                            else if (fldType == typeof(bool))
                            {
                                return (long)intValue != 0;
                            }
                            else if (fldType == typeof(string))
                            {
                                return intValue.ToString();
                            }
                            else if (fldType.IsEnum)
                            {
                                return Enum.ToObject(fldType, intValue);
                            }
                            else if (fldType == typeof(DateTime))
                            {

                                return UseUnixEpochNanoForNumericDateTime || AllDateTimeUseUnixEpochNano
                                        ? NanoEpochToDateTime(intValue)
                                        : new DateTime(intValue);
                            }
                            else if (fldType == typeof(DateTimeOffset))
                            {
                                return UseUnixEpochNanoForNumericDateTime || AllDateTimeUseUnixEpochNano
                                        ? new DateTimeOffset(NanoEpochToDateTime(intValue), TimeSpan.Zero)
                                        : new DateTimeOffset(intValue, TimeSpan.Zero);
                            }
                            else if (fldType == typeof(TimeSpan))
                            {
                                return UseUnixEpochNanoForNumericDateTime || AllDateTimeUseUnixEpochNano
                                        ? new TimeSpan(intValue / 100)
                                        : new TimeSpan(intValue);
                            }
                            else
                            {
                                throw CreateException(intValue);
                            }
                        }
                    case double doubleValue:
                        {
                            if (fldType.IsGenericType && fldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            {
                                return binValue is null 
                                        ? null
                                        : CastToNativeType(fldName, fldType.GetGenericArguments()[0], binName, binValue);
                            }

                            if (fldType == typeof(short))
                            {
                                return (short)doubleValue;
                            }
                            else if (fldType == typeof(int))
                            {
                                return (int)doubleValue;
                            }
                            else if (fldType == typeof(uint))
                            {
                                return (uint)doubleValue;
                            }
                            else if (fldType == typeof(ulong))
                            {
                                return (ulong)doubleValue;
                            }
                            else if (fldType == typeof(ushort))
                            {
                                return (ushort)doubleValue;
                            }
                            else if (fldType == typeof(decimal))
                            {
                                return (decimal)doubleValue;
                            }
                            else if (fldType == typeof(float))
                            {
                                return (float)doubleValue;
                            }
                            else if (fldType == typeof(double))
                            {
                                return (double)doubleValue;
                            }
                            else if (fldType == typeof(bool))
                            {
                                return doubleValue != 0;
                            }
                            else if (fldType == typeof(string))
                            {
                                return doubleValue.ToString();
                            }
                            else
                            {
                                throw CreateException(doubleValue);
                            }
                        }
                    case string strValue:
                        {
                            if (fldType.IsGenericType && fldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            {
                                if (strValue == null) return null;

                                return CastToNativeType(fldName,
                                                            fldType.GetGenericArguments()[0],
                                                            binName,
                                                            binValue);
                            }

                            if (string.IsNullOrEmpty(strValue))
                            {
                                return strValue;
                            }
                            else if (fldType == typeof(DateTime))
                            {
                                if (DateTime.TryParse(strValue, out DateTime dateTime))
                                    return dateTime;

                                return DateTime.ParseExact(strValue, DateTimeFormat, CultureInfo.InvariantCulture);
                            }
                            else if (fldType == typeof(DateTimeOffset))
                            {
                                if (DateTimeOffset.TryParse(strValue, out DateTimeOffset dateTime))
                                    return dateTime;

                                return DateTimeOffset.ParseExact(strValue, DateTimeFormat, CultureInfo.InvariantCulture);
                            }
                            else if (fldType == typeof(TimeSpan))
                            {
                                if (TimeSpan.TryParse(strValue, out TimeSpan timespan))
                                    return timespan;

                                return TimeSpan.ParseExact(strValue, DateTimeFormat, CultureInfo.InvariantCulture);
                            }
                            else if (fldType == typeof(bool))
                            {
                                return bool.Parse(strValue);
                            }
                            else if (fldType == typeof(short))
                            {
                                return short.Parse(strValue);
                            }
                            else if (fldType == typeof(int))
                            {
                                return int.Parse(strValue);
                            }
                            else if (fldType == typeof(uint))
                            {
                                return uint.Parse(strValue);
                            }
                            else if (fldType == typeof(ulong))
                            {
                                return ulong.Parse(strValue);
                            }
                            else if (fldType == typeof(ushort))
                            {
                                return ushort.Parse(strValue);
                            }
                            else if (fldType == typeof(decimal))
                            {
                                return decimal.Parse(strValue);
                            }
                            else if (fldType == typeof(float))
                            {
                                return float.Parse(strValue);
                            }
                            else if (fldType == typeof(double))
                            {
                                return double.Parse(strValue);
                            }
                            else if (fldType == typeof(Guid))
                            {
                                return new Guid(strValue);
                            }
                            else if (fldType == typeof(JObject))
                            {
                                return JObject.Parse(strValue);
                            }
                            else if (fldType == typeof(JToken))
                            {
                                return JToken.Parse(strValue);
                            }
                            else if (fldType == typeof(JArray))
                            {
                                return JArray.Parse(strValue);
                            }
                            else if (fldType.IsEnum)
                            {
                                return Enum.Parse(fldType, strValue, true);
                            }
                            else
                            {
                                throw CreateException(strValue);
                            }
                        }
                    case DateTime dtValue:
                        {
                            if (fldType.IsGenericType && fldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            {                                
                                return binValue is null
                                        ? null
                                        : CastToNativeType(fldName,
                                                            fldType.GetGenericArguments()[0],
                                                            binName,
                                                            binValue);
                            }

                            if (fldType == typeof(DateTime))
                            {
                                return dtValue;
                            }
                            else if (fldType == typeof(DateTimeOffset))
                            {
                               return new DateTimeOffset(dtValue);
                            }
                            else if (fldType == typeof(string))
                            {
                                return dtValue.ToString(DateTimeFormat);
                            }                           
                            else if (fldType == typeof(long))
                            {
                                if (AllDateTimeUseUnixEpochNano)
                                    return NanosFromEpoch(dtValue);

                                return dtValue.Ticks;
                            }
                            else if (fldType == typeof(JObject))
                            {
                                return JObject.FromObject(dtValue);
                            }
                            else if (fldType == typeof(JToken))
                            {
                                return JToken.FromObject(dtValue);
                            }
                            else
                            {
                                throw CreateException(dtValue);
                            }
                        }
                    case DateTimeOffset dtoValue:
                        {
                            if (fldType.IsGenericType && fldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            {
                                return binValue is null
                                        ? null
                                        : CastToNativeType(fldName,
                                                            fldType.GetGenericArguments()[0],
                                                            binName,
                                                            binValue);
                            }

                            if (fldType == typeof(DateTime))
                            {
                                return dtoValue.DateTime;
                            }
                            else if (fldType == typeof(DateTimeOffset))
                            {
                                return dtoValue;
                            }
                            else if (fldType == typeof(string))
                            {
                                return dtoValue.ToString(DateTimeFormat);
                            }
                            else if (fldType == typeof(long))
                            {
                                if (AllDateTimeUseUnixEpochNano)
                                    return NanosFromEpoch(dtoValue.DateTime);

                                return dtoValue.Ticks;
                            }
                            else if (fldType == typeof(JObject))
                            {
                                return JObject.FromObject(dtoValue);
                            }
                            else if (fldType == typeof(JToken))
                            {
                                return JToken.FromObject(dtoValue);
                            }
                            else
                            {
                                throw CreateException(dtoValue);
                            }
                        }
                    case IList<object> lstValue:
                        {
                            if (fldType.IsArray)
                            {
                                var itemType = fldType.GetElementType();
                                var newArray = Array.CreateInstance(itemType, lstValue.Count);
                                var idx = 0;

                                foreach (var item in lstValue.Select(i => CastToNativeType(fldName, itemType, binName, i)).ToArray())
                                {
                                    newArray.SetValue(item, idx++);
                                }

                                return newArray;
                            }

                            if (IsSubclassOfInterface(typeof(IList<>), fldType))
                            {
                                var itemTypes = fldType.GetGenericArguments();
                                var newList = (IList)Activator.CreateInstance(fldType.GetGenericTypeDefinition().MakeGenericType(itemTypes[0]));

                                foreach (var item in lstValue.Select(i => CastToNativeType(fldName, itemTypes[0], binName, i)).ToArray())
                                {
                                    newList.Add(item);
                                }

                                return newList;
                            }

                            if (fldType != typeof(string) && IsSubclassOfInterface(typeof(IEnumerable<>), fldType))
                            {
                                var itemTypes = fldType.GetGenericArguments();
                                var newArray = Array.CreateInstance(itemTypes[0], lstValue.Count);
                                var idx = 0;

                                foreach (var item in lstValue.Select(i => CastToNativeType(fldName, itemTypes[0], binName, i)).ToArray())
                                {
                                    newArray.SetValue(item, idx++);
                                }

                                return newArray;
                            }

                            throw CreateException(lstValue);
                        }                    
                    case IDictionary<object, object> dictValue:
                        {
                            if (fldType == typeof(JsonDocument))
                            {
                                return new JsonDocument(dictValue);
                            }
                            else if (fldType == typeof(Newtonsoft.Json.Linq.JObject))
                            {
                                return Newtonsoft.Json.Linq.JObject.FromObject(dictValue);
                            }


                            if (IsSubclassOfInterface(typeof(IDictionary<,>), fldType))
                            {
                                var itemTypes = fldType.GetGenericArguments();
                                var newDict = (IDictionary)Activator.CreateInstance(fldType.GetGenericTypeDefinition().MakeGenericType(itemTypes[0], itemTypes[1]));
                                var keyValues = dictValue.Keys.Select(i => CastToNativeType(fldName, itemTypes[0], binName, i)).ToArray();
                                var values = dictValue.Values.Select(i => CastToNativeType(fldName, itemTypes[1], binName, i)).ToArray();

                                for (var idx = 0; idx < dictValue.Count; idx++)
                                {
                                    newDict.Add(keyValues[idx], values[idx]);
                                }

                                return newDict;
                            }


                            return typeof(Helpers)
                                        .GetMethod("Transform")
                                        .MakeGenericMethod(fldType)
                                        .Invoke(null, new object[] { dictValue.ToDictionary(key => (string)key.Key, v => v.Value), null });                            
                        }
                    case AValue aValue:
                        {
                            if (fldType.IsGenericType)
                            {
                                if (fldType.GetGenericTypeDefinition() == typeof(Nullable<>))
                                {
                                    if (aValue.Value is null) return null;

                                    return CastToNativeType(fldName,
                                                                fldType.GetGenericArguments()[0],
                                                                binName,
                                                                binValue);
                                }
                            }

                            if (fldType == typeof(string))
                            {
                                return (string)aValue;
                            }
                            else if (fldType == typeof(DateTime))
                            {
                                return (DateTime)aValue;
                            }
                            else if (fldType == typeof(DateTimeOffset))
                            {
                                return (DateTimeOffset)aValue;
                            }
                            else if (fldType == typeof(TimeSpan))
                            {
                                return (TimeSpan)aValue;
                            }
                            else if (fldType == typeof(bool))
                            {
                                return (bool)aValue;
                            }
                            else if (fldType == typeof(short))
                            {
                                return (short)aValue;
                            }
                            else if (fldType == typeof(int))
                            {
                                return (int)aValue;
                            }
                            else if (fldType == typeof(uint))
                            {
                                return (uint)aValue;
                            }
                            else if (fldType == typeof(ulong))
                            {
                                return (ulong)aValue;
                            }
                            else if (fldType == typeof(ushort))
                            {
                                return (ushort) aValue;
                            }
                            else if (fldType == typeof(decimal))
                            {
                                return (decimal)aValue;
                            }
                            else if (fldType == typeof(float))
                            {
                                return (float)aValue;
                            }
                            else if (fldType == typeof(double))
                            {
                                return (double)aValue;
                            }
                            else if (fldType == typeof(Guid))
                            {
                                return (Guid)aValue;
                            }
                            else if (fldType == typeof(JObject))
                            {
                                return (JObject)aValue;
                            }
                            else if (fldType == typeof(JsonDocument))
                            {
                                return (JsonDocument)aValue;
                            }
                            else if (fldType.IsEnum)
                            {                                
                                return (Enum)aValue;
                            }
                            else
                            {
                                return aValue.Value;
                            }
                        }
                    default:
                    {
                        if (binValue == null 
                                && (!fldType.IsValueType
                                        || (fldType.IsGenericType
                                                && fldType.GetGenericTypeDefinition() == typeof(Nullable<>))))
                            return null;

                        if (binValue != null)
                        {
                            var binValueType = binValue.GetType();

                            if (binValueType.IsArray)
                            {
                                var binArray = (Array)binValue;

                                if (fldType.IsArray)
                                {
                                    var itemType = fldType.GetElementType();
                                    var newArray = Array.CreateInstance(itemType, binArray.Length);
                                    var idx = 0;

                                    foreach (var item in binArray)
                                    {
                                        newArray.SetValue(CastToNativeType(fldName, itemType, binName, item), idx++);
                                    }

                                    return newArray;
                                }

                                if (fldType.IsGenericType && IsSubclassOfInterface(typeof(IList<>), fldType))                                    {
                                    var itemTypes = fldType.GetGenericArguments();
                                    var newList = (IList)Activator.CreateInstance(fldType.GetGenericTypeDefinition().MakeGenericType(itemTypes[0]));

                                    foreach (var item in binArray)
                                    {
                                        newList.Add(CastToNativeType(fldName, itemTypes[0], binName, item));
                                    }

                                    return newList;
                                }

                                if (fldType == typeof(string) && binValueType.GetElementType() == typeof(byte))
                                {
                                    return Encoding.Default.GetString((byte[])binValue);
                                }
                            }
                        }

                        throw CreateException(binValue);
                    }
                }
            }
        }

        /// <summary>
        /// Wrapper around <see cref="CastToNativeType(string, Type, string, object)"/> to trap exceptions.
        /// </summary>       
        public static object CastToNativeType(ARecord asRecord, string fldName, Type fldType, string binName, object binValue)
        {
            try
            {
                return CastToNativeType(fldName, fldType, binName, binValue);
            }
            catch (ArgumentException e)
            {
                if (asRecord.DumpType == ARecord.DumpTypes.Record) asRecord.DumpType = ARecord.DumpTypes.Dynamic;

                asRecord.SetException(e);

                return null;
            }
        }

        /// <summary>
        /// Transform from Aerospike <paramref name="bins"/> into an .Net instance of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Creates an instance of Type T based on the Aerospike <paramref name="bins"/></typeparam>
        /// <param name="bins">The Aerospike Bins, where the Key is the bin name and value is the associated value</param>
        /// <param name="transform">
        /// A action that is called to perform customized transformation. 
        /// First argument -- the name of the property/field
        /// Second argument -- the property/field type
        /// Third argument -- bin name
        /// Fourth argument -- bin value
        /// Returns the new transformed object or null to indicate that this transformation should be skipped.
        /// </param>
        /// <returns>new instance of <typeparamref name="T"/></returns>
        /// <exception cref="MissingMethodException"></exception>
        /// <seealso cref="Aerospike.Client.BinNameAttribute"/>
        /// <seealso cref="Aerospike.Client.BinIgnoreAttribute"/>
        /// <seealso cref="Aerospike.Client.ConstructorAttribute"/>
        public static T Transform<T>(Dictionary<string, object> bins, Func<string, Type, string, object, object> transform = null)
        {
            var publicProps = typeof(T).GetProperties();
            var publicFields = typeof(T).GetFields();
            var constructor = GetConstructorInfo(typeof(T));
            object instance;

            if (constructor == null)
            {
                try
                {
                    instance = Activator.CreateInstance(typeof(T), true);
                }
                catch (System.Exception ex)
                {
                    throw new MissingMethodException($"Could not use the Default Constructor to create instance \"{typeof(T).Name}\". ConstructorAttribute is required.",
                                                        ex);
                }
            }
            else
            {
                var args = constructor.GetParameters();
                var values = new List<object>(args.Length);
                var fndArgs = new List<string>(args.Length);

                foreach (var arg in args)
                {
                    var fndKVP = bins.FirstOrDefault(kvp => kvp.Key.Equals(arg.Name, StringComparison.OrdinalIgnoreCase));

                    if (fndKVP.Key != null)
                    {
                        values.Add(CastToNativeType(arg.Name, arg.ParameterType, fndKVP.Key, fndKVP.Value));
                        fndArgs.Add(fndKVP.Key);
                    }
                    else
                    {
                        values.Add(arg.DefaultValue);
                    }
                }

                try
                {
                    instance = Activator.CreateInstance(typeof(T), values.ToArray());
                }
                catch (System.Exception ex)
                {
                    throw new MissingMethodException($"Could not determine Constructor or Wrong Constructor Arguments defined. Trying to create instance \"{typeof(T).Name}\". Default Constructor or ConstructorAttribute required? Matched Params are: \"{string.Join(',', fndArgs)}\"",
                                                        ex);
                }

                bins = new Dictionary<string, object>(bins);

                foreach (var fndArg in fndArgs)
                {
                    bins.Remove(fndArg);
                }
            }

            object setValue;

            foreach (var bin in bins)
            {
                var checkBinName = Helpers.CheckName(bin.Key, "Bin");

                var fndProp = publicProps.FirstOrDefault(p => GetBinNameFromProperty(p) == bin.Key || p.Name == checkBinName);

                if (fndProp != null)
                {
                    if (fndProp.CanWrite)
                    {
                        if (transform == null)
                            setValue = CastToNativeType(fndProp.Name, fndProp.PropertyType, bin.Key, bin.Value);
                        else
                        {
                            setValue = transform(fndProp.Name, fndProp.PropertyType, bin.Key, bin.Value);

                            if (setValue == null) continue;
                        }

                        fndProp.SetValue(instance, setValue);
                    }
                }
                else
                {
                    var fndFld = publicFields.FirstOrDefault(f => GetBinNameFromField(f) == bin.Key || f.Name == checkBinName);
                    if (fndFld != null)
                    {
                        if (transform == null)
                            setValue = CastToNativeType(fndFld.Name, fndFld.FieldType, bin.Key, bin.Value);
                        else
                        {
                            setValue = transform(fndFld.Name, fndFld.FieldType, bin.Key, bin.Value);

                            if (setValue == null) continue;
                        }

                        fndFld.SetValue(instance, setValue);
                    }
                }
            }

            return (T)instance;
        }

        public async static void CheckForNewSetNameRefresh(string namespaceName, string setName, bool forceRefresh = false)
        {           
            var ns = DynamicDriver._Connection?.Namespaces?.FirstOrDefault(n => n.Name == namespaceName);
            var refresh = ns is null;

            if(!forceRefresh && !string.IsNullOrEmpty(setName) && !refresh)
            {
                refresh = !ns.Sets.Any(s => s.Name == setName);
            }

            if(forceRefresh || refresh)
                await DynamicDriver._Connection.CXInfo.ForceRefresh();
        }

        public static Client.Key DetermineAerospikeKey(dynamic primaryKey, string nameSpace, string setName)
        {
            Client.Key key;

            if (primaryKey is APrimaryKey aPrimaryKey)
                primaryKey = aPrimaryKey.AerospikeKey;

            if (primaryKey is Client.Key valueKey)
            {
                if (valueKey.userKey is null && setName != valueKey.setName)
                    throw new InvalidOperationException($"An Aerospike Key (\"{primaryKey}\") was provided with only a digest and the set names where different (Old Set: \"{valueKey.setName}\" New Set: \"{setName}\"). Because of this a Primary Key Value is required.");

                key = new Client.Key(nameSpace, valueKey.digest, setName, valueKey.userKey);
            }
            else if (primaryKey is AValue aValue)
            {
                key = new Client.Key(nameSpace, setName, Value.Get(aValue.Value));
            }
            else if (primaryKey is Value value)
                key = new Client.Key(nameSpace, setName, value);
            else if (primaryKey is byte[] digest)
                key = new Client.Key(nameSpace, digest, setName, Value.NULL);
            else
                key = new Client.Key(nameSpace, setName, Value.Get(primaryKey));

            return key;
        }
    }
}
