using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Aerospike.Client;
using Google.Protobuf.Compiler;
using System.Dynamic;
using System.Windows.Controls;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Aerospike.Database.LINQPadDriver.Extensions
{

	/// <summary>
	/// A wrapper around an <see cref="Object"/> value. 
	/// This is used as an aid so that casting is not required to perform comparison operations, etc.
	/// This object also performs implicit casting to standard .Net data types while using LINQ...  
	/// </summary>
	/// <seealso cref="APrimaryKey"/>
	/// <seealso cref="AValue.ToValue(object)"/>
	/// <seealso cref="AValue.ToValue(Client.Bin)"/>
	/// <seealso cref="AValue.ToValue(Client.Value)"/>
	/// <seealso cref="APrimaryKey.ToValue(Client.Key)"/>
	/// <seealso cref="AValueHelper.ToAValue(Client.Bin)"/>
	/// <seealso cref="AValueHelper.ToAPrimaryKey(Client.Key)"/>
	/// <seealso cref="AValueHelper.ToAValue(object, string, string)"/>
	/// <seealso cref="AValueHelper.ToAValue(Value, string, string)"/>
	/// <seealso cref="AValueHelper.ToAValue{T}(T?, string, string)"/>
	/// <seealso cref="AValueHelper.Cast{TResult}(IEnumerable{AValue})"/>
	/// <seealso cref="AValueHelper.OfType{TResult}(IEnumerable{AValue})"/>
	/// <seealso cref="AValueHelper.TryGetValue{T}(IEnumerable{AValue}, T, bool)"/>
	/// <seealso cref="AValueHelper.Contains{T}(IEnumerable{AValue}, T, AValue.MatchOptions)"/>
	/// <seealso cref="AValueHelper.FindAll{T}(IEnumerable{AValue}, T, AValue.MatchOptions)"/>
	[DebuggerDisplay("{DebuggerString()}")]
    public partial class AValue 
    {
        #region Constructors
        public AValue(Aerospike.Client.Bin bin)
            : this(bin.value, bin.name)
        { }

        public AValue(Aerospike.Client.Value value, string binName = null)
            : this(value.Object, binName ?? "Value", "Value")
        { }

        public AValue(object value, string binName, string fldName)
        {
            this.Value = value is AValue aValue ? aValue.Value : value;
            this.BinName = binName;
            this.FldName = fldName;
        }

        public AValue(AValue aValue)
            : this(aValue.Value, aValue.BinName, aValue.FldName)
        {
        }
        #endregion
        #region Properties
        /// <summary>
        /// Returns the actual value from the <see cref="Aerospike.Client.Record"/>
        /// </summary>
        public Object Value { get; }
        /// <summary>
        /// Returns the Aerospike Bin Name
        /// </summary>
        public string BinName { get; }
        /// <summary>
        /// Returns the name of the associated field/property
        /// </summary>
        public string FldName { get; }

        /// <summary>
        /// The <see cref="Value"/> type
        /// </summary>
        public Type UnderlyingType { get => this.Value?.GetType(); }

        /// <summary>
        /// An empty AValue (<see cref="Value"/> is null).
        /// </summary>
        /// <seealso cref="IsEmpty"/>
        public static readonly AValue Empty = new AValue(null, null, null);

        #endregion
        #region Underlying Type Test Properties
        /// <summary>
        /// Returns true if the value is a string
        /// </summary>
        /// <seealso cref="IsCDT"/>
        /// <seealso cref="IsMap"/>
        /// <seealso cref="IsList"/>
        /// <seealso cref="IsBool"/>
        /// <seealso cref="IsFloat"/>
        /// <seealso cref="IsInt"/>
        /// <seealso cref="IsJson"/>
        /// <seealso cref="UnderlyingType"/>
        public bool IsString
        {
            get => this.UnderlyingType == typeof(string);
        }

        /// <summary>
        /// Returns true if the value is any numeric type (e.g., long, double, etc.)
        /// </summary>
        /// <seealso cref="IsCDT"/>
        /// <seealso cref="IsMap"/>
        /// <seealso cref="IsList"/>
        /// <seealso cref="IsBool"/>
        /// <seealso cref="IsFloat"/>
        /// <seealso cref="IsInt"/>
        /// <seealso cref="IsJson"/>
        /// <seealso cref="IsString"/>
        /// <seealso cref="UnderlyingType"/>
        public bool IsNumeric
        {
            get => Helpers.IsNumeric(this.UnderlyingType);
        }

        /// <summary>
        /// Returns true if the value is any whole number type (e.g., int, uint, long, ulong, etc.)
        /// </summary>
        /// <seealso cref="IsCDT"/>
        /// <seealso cref="IsMap"/>
        /// <seealso cref="IsList"/>
        /// <seealso cref="IsBool"/>
        /// <seealso cref="IsFloat"/>
        /// <seealso cref="IsJson"/>
        /// <seealso cref="IsNumeric"/>
        /// <seealso cref="IsString"/>
        /// <seealso cref="UnderlyingType"/>
        public bool IsInt
        {
            get => Helpers.IsInt(this.UnderlyingType);
        }

        /// <summary>
        /// Returns true if the value is any float type (e.g., float, double)
        /// </summary>
        /// <seealso cref="IsCDT"/>
        /// <seealso cref="IsMap"/>
        /// <seealso cref="IsList"/>
        /// <seealso cref="IsBool"/>
        /// <seealso cref="IsJson"/>
        /// <seealso cref="IsInt"/>
        /// <seealso cref="IsNumeric"/>
        /// <seealso cref="IsString"/>
        /// <seealso cref="UnderlyingType"/>
        public bool IsFloat
        {
            get => Helpers.IsFloat(this.UnderlyingType);
        }

        /// <summary>
        /// Returns true if the value is boolean
        /// </summary>
        /// <seealso cref="IsCDT"/>
        /// <seealso cref="IsMap"/>
        /// <seealso cref="IsList"/>
        /// <seealso cref="IsJson"/>
        /// <seealso cref="IsFloat"/>
        /// <seealso cref="IsInt"/>
        /// <seealso cref="IsNumeric"/>
        /// <seealso cref="IsString"/>
        /// <seealso cref="UnderlyingType"/>
        public bool IsBool
        {
            get => this.UnderlyingType == typeof(bool);
        }

        /// <summary>
        /// Returns true if the value is a IList
        /// </summary>
        /// <seealso cref="IsCDT"/>
        /// <seealso cref="IsMap"/>
        /// <seealso cref="IsJson"/>
        /// <seealso cref="IsBool"/>
        /// <seealso cref="IsFloat"/>
        /// <seealso cref="IsInt"/>
        /// <seealso cref="IsNumeric"/>
        /// <seealso cref="IsString"/>
        /// <seealso cref="UnderlyingType"/>
        public bool IsList
        {
            get => Helpers.IsSubclassOfInterface(typeof(IList<>), this.UnderlyingType);
        }

        /// <summary>
        /// Returns true if the value is a IDictionary (map)
        /// </summary>
        /// <seealso cref="IsCDT"/>
        /// <seealso cref="IsJson"/>
        /// <seealso cref="IsList"/>
        /// <seealso cref="IsBool"/>
        /// <seealso cref="IsFloat"/>
        /// <seealso cref="IsInt"/>
        /// <seealso cref="IsNumeric"/>
        /// <seealso cref="IsString"/>
        /// <seealso cref="UnderlyingType"/>
        public bool IsMap
        {
            get => Helpers.IsSubclassOfInterface(typeof(IDictionary<,>), this.UnderlyingType);
        }

        /// <summary>
        /// Returns true if the value is a IDictionary
        /// </summary>
        /// <seealso cref="IsMap"/>
        /// <seealso cref="IsCDT"/>
        /// <seealso cref="IsJson"/>
        /// <seealso cref="IsList"/>
        /// <seealso cref="IsBool"/>
        /// <seealso cref="IsFloat"/>
        /// <seealso cref="IsInt"/>
        /// <seealso cref="IsNumeric"/>
        /// <seealso cref="IsString"/>
        /// <seealso cref="UnderlyingType"/>
        public bool IsDictionary
        {
            get => IsMap;
        }

        /// <summary>
        /// Returns true if the value is a Collection Data Type (e.g., IList, IDictionary)
        /// </summary>
        /// <seealso cref="IsJson"/>
        /// <seealso cref="IsMap"/>
        /// <seealso cref="IsList"/>
        /// <seealso cref="IsBool"/>
        /// <seealso cref="IsFloat"/>
        /// <seealso cref="IsInt"/>
        /// <seealso cref="IsNumeric"/>
        /// <seealso cref="IsString"/>
        /// <seealso cref="UnderlyingType"/>
        public bool IsCDT
        {
            get => this.IsList || this.IsMap;
        }

        /// <summary>
        /// Returns true if the value is a JSON Data Type (e.g., JObject, JArray, etc.)
        /// </summary>
        /// <seealso cref="IsCDT"/>
        /// <seealso cref="IsMap"/>
        /// <seealso cref="IsList"/>
        /// <seealso cref="IsBool"/>
        /// <seealso cref="IsFloat"/>
        /// <seealso cref="IsInt"/>
        /// <seealso cref="IsNumeric"/>
        /// <seealso cref="IsString"/>
        /// <seealso cref="UnderlyingType"/>
        public bool IsJson
        {
            get => Helpers.IsJson(this.UnderlyingType);
        }

        public bool IsGeoJson
        {
            get => this.Value is Client.Value.GeoJSONValue || GeoJSONHelpers.IsGeoValue(this.UnderlyingType);
        }

        public bool IsDateTime
        {
            get => this.UnderlyingType == typeof(DateTime);
        }

        public bool IsDateTimeOffset
        {
            get => this.UnderlyingType == typeof(DateTimeOffset);
        }

        public bool IsTimeSpan
        {
            get => this.UnderlyingType == typeof(TimeSpan);
        }

        /// <summary>
        /// Returns true if the underlying value is a <see cref="KeyValuePair"/>.
        /// </summary>
        public bool IsKeyValuePair
        {
            get => Helpers.IsSubclassOfInterface(typeof(KeyValuePair<,>), this.UnderlyingType);
        }

        /// <summary>
        /// Returns true if <see cref="Value"/> is null
        /// </summary>
        /// <seealso cref="Empty"/>
        public bool IsEmpty
        {
            get => this.Value is null;
        }
        #endregion
        #region To type Methods
        
        /// <summary>
        /// Tries to convert <see cref="Value"/> to a IDictionary. If not possible an empty IDictionary is returned.
        /// </summary>
        /// <returns>
        /// An IDictionary, if possible, or an empty IDictionary.
        /// </returns>
        public IDictionary<object, object> ToDictionary()
        {
            if (this.Value is JObject jObject)
            {
                return CDTConverter.ConvertToDictionary(jObject)
                        .ToDictionary(kvp => (object)kvp.Key, kvp => kvp.Value);
            }
            else if (this.Value is JProperty jProp)
            {
                return CDTConverter.ConvertToDictionary(jProp)
                        .ToDictionary(kvp => (object)kvp.Key, kvp => kvp.Value);
            }
            else if (this.Value is IDictionary<object, object> oDict)
                return oDict;
            else if (this.Value is IDictionary<string, object> sDict)
                return sDict.ToDictionary(kvp => (object)kvp.Key, kvp => kvp.Value);
            else if (this.Value is IDictionary<AValue, AValue> aDict)
                return aDict.ToDictionary(kvp => (object)kvp.Key, kvp => (object)kvp.Value);
            else if (this.Value is System.Collections.IDictionary iDict)
            {
                var kvps = iDict.Keys.Cast<object>().Zip(iDict.Values.Cast<object>(),
                                                            (k, v) => new KeyValuePair<object, object>(k, v));
                return new Dictionary<object, object>(kvps);
            }

            return new Dictionary<object, object>(0);
        }

        /// <summary>
        /// Tries to convert <see cref="Value"/> to a IDictionary. If not possible an empty IDictionary is returned.
        /// </summary>
        /// <typeparam name="K">
        /// The key value as type K.
        /// </typeparam>
        /// <typeparam name="V">
        /// The value as type V.
        /// </typeparam>
        /// <param name="keySelector">
        /// The function that will transform the key as an <see cref="AValue"/> to <typeparamref name="K"/>.
        /// </param>
        /// <param name="valueSelector">
        /// The function that will transform the value as an <see cref="AValue"/> to <typeparamref name="V"/>.
        /// </param>
        /// <returns>
        /// An IDictionary, if possible, or an empty IDictionary.
        /// </returns>
        public IDictionary<K, V> ToDictionary<K,V>(Func<AValue,K> keySelector,
                                                    Func<AValue, V> valueSelector)
        {
            if (this.Value is JObject jObject)
            {
                return CDTConverter.ConvertToDictionary(jObject)
                        .ToDictionary(kvp => keySelector(kvp.Key.ToAValue()), kvp => valueSelector(kvp.Value?.ToAValue()));
            }
            else if (this.Value is JProperty jProp)
            {
                return CDTConverter.ConvertToDictionary(jProp)
                        .ToDictionary(kvp => keySelector(kvp.Key.ToAValue()), kvp => valueSelector(kvp.Value?.ToAValue()));
            }
            else if (this.Value is IDictionary<object, object> oDict)
                return oDict
                        .ToDictionary(kvp => keySelector(kvp.Key.ToAValue()), kvp => valueSelector(kvp.Value?.ToAValue()));
            else if (this.Value is IDictionary<string, object> sDict)
                return sDict.ToDictionary(kvp => keySelector(kvp.Key.ToAValue()), kvp => valueSelector(kvp.Value?.ToAValue()));
            else if (this.Value is IDictionary<AValue, AValue> aDict)
                return aDict.ToDictionary(kvp => keySelector(kvp.Key), kvp => valueSelector(kvp.Value));
            else if (this.Value is System.Collections.IDictionary iDict)
            {
                var kvpList = new List<KeyValuePair<K, V>>();
                foreach(KeyValuePair<object,object> kvp in iDict)
                {
                    kvpList.Add(new KeyValuePair<K, V>(keySelector(kvp.Key.ToAValue()),
                                                        valueSelector(kvp.Value?.ToAValue())));
                }
            }

            return new Dictionary<K, V>(0);
        }

        /// <summary>
        /// Tries to convert <see cref="Value"/> to a IList. If not possible an empty list is returned.
        /// </summary>
        /// <returns>
        /// An IList, if possible, otherwise an empty IList.
        /// </returns>
        public IList<object> ToList()
        {
            if (this.Value is IList<object> iList)
            {
                return iList;
            }
            else if (this.Value is JObject
                        || this.Value is JProperty)
            {
                return this.ToDictionary()
                            .Cast<object>().ToList();
            }
            else if (this.Value is JArray jArray)
            {
                return CDTConverter.ConvertToList(jArray);
            }
            else if (this.Value is System.Collections.IEnumerable iEnum)
            {
                return iEnum.Cast<object>().ToList();
            }
            else if (this.Value is IGeoJSONCollection geoCollection)
            {
                return geoCollection
                        .Cast<object>()
                        .ToList();
            }

            return new List<object>(0);
        }

        /// <summary>
        /// Always convert <see cref="Value"/> to a List. 
        /// If <see cref="Value"/> is not a collection, the item is returned in a list.
        /// </summary>
        /// <returns>
        /// A list of at least one element. If <see cref="Value"/> is a collection, that collection will be converted to an IList.
        /// </returns>
        public IList<object> ToListItem()
        {
            if (this.IsCDT
                || this.Value is JProperty)
            {
                return this.ToList();
            }
            if (this.Value is JValue jValue)
                return new List<object>(1) { jValue.Value };

            return new List<object>(1) { this.Value };
        }

        /// <summary>
        /// This will convert a list of <see cref="JsonDocument"/>/<see cref="JObject"/> to a list of dictionary items if they match that patterns.
        /// If the value is already a list of dictionary items, that is returned.
        /// </summary>
        /// <returns>
        /// a list of dictionary items or an empty list.
        /// </returns>
        public IEnumerable<IDictionary<string, object>> ToCDT()
        {
            var listItem = this.ToList();

            if (listItem.Count > 0)
            {
                if (Helpers.IsJsonDoc(listItem.GetType().GenericTypeArguments[0]))
                {
                    return Aerospike.Client.LPDHelpers.ToCDT(listItem.Cast<JObject>());
                }

                static JsonDocument IsDoc(object value)
                {
                    if (value is IDictionary<object, object> dict)
                    {
                        return new JsonDocument(dict);
                    }
                    if (value is JsonDocument jdoc)
                    {
                        return jdoc;
                    }
                    if (value is JObject jObj)
                    {
                        return new JsonDocument(jObj);
                    }

                    return null;
                }

                return Aerospike.Client.LPDHelpers.ToCDT(listItem
                                                            .Select(i => IsDoc(i))
                                                            .Where(i => i != null));
            }

            return new List<IDictionary<string, object>>(0); ;
        }

        #region IConvertible
        public TypeCode GetTypeCode()
        {
            return Type.GetTypeCode(this.UnderlyingType);
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            if (this.Value is IConvertible iConvertible)
                return iConvertible.ToBoolean(provider);

            return (bool)this;
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            if (this.Value is IConvertible iConvertible)
                return iConvertible.ToByte(provider);

            return (byte)this;
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            if (this.Value is IConvertible iConvertible)
                return iConvertible.ToChar(provider);

            return (char)this;
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            if (this.Value is IConvertible iConvertible)
                return iConvertible.ToDateTime(provider);

            return (DateTime)this;
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            if (this.Value is IConvertible iConvertible)
                return iConvertible.ToDecimal(provider);

            return (decimal)this;
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            if (this.Value is IConvertible iConvertible)
                return iConvertible.ToDouble(provider);

            return (double)this;
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            if (this.Value is IConvertible iConvertible)
                return iConvertible.ToInt16(provider);

            return (short)this;
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            if (this.Value is IConvertible iConvertible)
                return iConvertible.ToInt32(provider);

            return (int)this;
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            if (this.Value is IConvertible iConvertible)
                return iConvertible.ToInt64(provider);

            return (long)this;
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            if (this.Value is IConvertible iConvertible)
                return iConvertible.ToSByte(provider);

            return (sbyte)this;
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            if (this.Value is IConvertible iConvertible)
                return iConvertible.ToSingle(provider);

            return (float)this;
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            if (this.Value is IConvertible iConvertible)
                return iConvertible.ToString(provider);

            return (string)this;
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            if (this.Value is IConvertible iConvertible)
                return iConvertible.ToType(conversionType, provider);

            return Helpers.CastToNativeType(this.FldName, conversionType, this.BinName, this.Value);
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            if (this.Value is IConvertible iConvertible)
                return iConvertible.ToUInt16(provider);

            return (ushort)this;
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            if (this.Value is IConvertible iConvertible)
                return iConvertible.ToUInt32(provider);

            return (uint)this;
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            if (this.Value is IConvertible iConvertible)
                return iConvertible.ToUInt64(provider);

            return (ulong)this;
        }
        #endregion

        public static AValue ToValue(Aerospike.Client.Value value) => new AValue(value, "Value");
        public static AValue ToValue(Aerospike.Client.Bin bin) => new AValue(bin);
        public static AValue ToValue(object value) => new AValue(value, "Value", "ToValue");


        virtual public object ToDump()
        {
            return this.Value;
        }
        #endregion
        #region Convert and As methods
        /// <summary>
        /// Converts <see cref="Value"/> into a .net native type, a <see cref="Newtonsoft.Json"/>, or <see cref="GeoJSON.Net.GeoJSONObject"/> instance.
        /// </summary>
        /// <typeparam name="T">Try to convert <see cref="Value"/> to this type</typeparam>
        /// <returns>
        /// The converted value
        /// </returns>
        /// <exception cref="InvalidCastException">Thrown if value cannot be converted</exception>
        public T Convert<T>() => this.Value is T tValue ? tValue : (T)Helpers.CastToNativeTypeInvalidCast(this.FldName, typeof(T), this.BinName, this.Value);

        /// <summary>
        /// Returns an enumerable object converting each element to <typeparamref name="T"/>. 
        /// If not possible an <see cref="ArgumentException"/> is thrown.
        /// <seealso cref="AsEnumerable()"/>
        /// </summary>
        /// <typeparam name="T">The element type</typeparam>
        /// <returns>
        /// Returns an enumerable object
        /// </returns>
        /// <exception cref="ArgumentException">Thrown if value cannot be converted</exception>
        /// <seealso cref="AsEnumerable()"/>
        /// <seealso cref="Convert{T}()"/>
        public IEnumerable<T> AsEnumerable<T>() => (T[])this.Convert<T[]>();

        /// <summary>
        /// Returns an enumerable object such that each item is an <see cref="AValue"/>.
        /// <seealso cref="AsEnumerable{T}()"/>
        /// </summary>
        /// <returns>
        /// Returns an enumerable object where each element is an <see cref="AValue"/> or an empty Enumerable if the <see cref="Value"/> is not a CDT.
        /// </returns>
        /// <seealso cref="AValueHelper.Cast{TResult}(IEnumerable{AValue})"/>
        /// <seealso cref="AValueHelper.OfType{TResult}(IEnumerable{AValue})"/>
        /// <seealso cref="AValueHelper.Convert{TResult}(IEnumerable{AValue})"/>
        /// <seealso cref="AValueHelper.TryGetValue{T, R}(IEnumerable{AValue}, T, R)"/>        
        public IEnumerable<AValue> AsEnumerable()
        {
            AValue NewAValue(object value, int currIdx)
            {
                var binName = currIdx < 0 ? this.BinName : $"{this.BinName}[{currIdx}]";
                var fldName = currIdx < 0 ? this.FldName : $"{this.FldName}[{currIdx}]";

                switch (value)
                {
                    case AValue avalue:
                        return NewAValue(avalue.Value, currIdx);

                    case KeyValuePair<object, object> kvpo:
                        {
                            return new AValue(new KeyValuePair<AValue, AValue>(NewAValue(kvpo.Key, currIdx),
                                                                                NewAValue(kvpo.Value, currIdx)),
                                                binName, fldName);
                        }
                    case KeyValuePair<string, object> kvps:
                        {
                            return new AValue(new KeyValuePair<AValue, AValue>(NewAValue(kvps.Key, currIdx),
                                                                                NewAValue(kvps.Value, currIdx)),
                                                binName, fldName);
                        }
                    case IDictionary<object, object> dicto:
                        {
                            int idx = 0;
                            return new AValue(dicto.ToDictionary(k => NewAValue(k.Key, idx),
                                                                    v => NewAValue(v.Value, idx++)),
                                                binName,
                                                fldName);
                        }
                    case IDictionary<string, object> dicts:
                        {
                            int idx = 0;
                            return new AValue(dicts.ToDictionary(k => NewAValue(k.Key, idx),
                                                                    v => NewAValue(v.Value, idx++)),
                                                binName,
                                                fldName);
                        }
                    case IList<object> lsto:
                        {
                            int idx = 0;
                            return new AValue(lsto.Select(v => NewAValue(v, idx++)),
                                                binName,
                                                fldName);
                        }
                    case JsonDocument jDoc:
                        {
                            int idx = 0;
                            return new AValue(jDoc.ToDictionary()
                                                    .ToDictionary(k => NewAValue(k.Key, idx),
                                                                    v => NewAValue(v.Value, idx++)),
                                                binName,
                                                fldName);
                        }
                    case JObject jObj:
                        {
                            int idx = 0;
                            return new AValue(CDTConverter.ConvertToDictionary(jObj)
                                                    .ToDictionary(k => NewAValue(k.Key, idx),
                                                                    v => NewAValue(v.Value, idx++)),
                                                binName,
                                                fldName);
                        }
                    case JProperty jProp:
                        {
                            int idx = 0;
                            return new AValue(CDTConverter.ConvertToDictionary(jProp)
                                                    .ToDictionary(k => NewAValue(k.Key, idx),
                                                                    v => NewAValue(v.Value, idx++)),
                                                binName,
                                                fldName);
                        }
                }

                return new AValue(value, binName, fldName);
            }

            switch (this.Value)
            {
                case IDictionary<AValue, AValue> dicta:
                    {
                        int idx = 0;
                        return dicta.Select(kvp => NewAValue(kvp, idx++));
                    }
                case IDictionary<object, object> dicto:
                    {
                        int idx = 0;
                        return dicto.Select(v => NewAValue(v, idx++));
                    }
                case IDictionary<string, object> dicts:
                    {
                        int idx = 0;
                        return dicts.Select(v => NewAValue(v, idx++));
                    }
                case IList<object> lsto:
                    {
                        int idx = 0;
                        return lsto.Select(v => NewAValue(v, idx++));
                    }
                case IList<JsonDocument> lstDoc:
                    {
                        int idx = 0;
                        return lstDoc.Select(v => NewAValue(v, idx++));
                    }
                case IList<JObject> lstJObt:
                    {
                        int idx = 0;
                        return lstJObt.Select(v => NewAValue(v, idx++));
                    }
                case IList<JProperty> lstJProp:
                    {
                        int idx = 0;
                        return lstJProp.Select(v => NewAValue(v, idx++));
                    }
                case JProperty jProp:
                    {
                        int idx = 0;
                        return CDTConverter.ConvertToDictionary(jProp)
                                .Select(v => NewAValue(v, idx++));
                    }
                case IEnumerable<AValue> lsta:
                    return lsta;
            }

            return Enumerable.Empty<AValue>();
        }
        #endregion
        #region Equal and GetHashCode Methods
        protected virtual bool DigestRequired() => false;
        protected virtual bool CompareDigest(object value) => false;

        public bool Equals(Aerospike.Client.Key key)
        {
            if (this.DigestRequired())
                return this.CompareDigest(key);

            if (this.Value is null || key is null)
            {
                if (key is null) return false;
            }

            if(this.Value is byte[] bytes && bytes.Length == 20)
                return key.digest.SequenceEqual(bytes);

            return this.Equals(key.userKey);
        }
        public bool Equals(Aerospike.Client.Key key1, Aerospike.Client.Key key2)
        {
            if (key1 is null) return key2 is null;
            if (key1.userKey is null)
            {
                if (key2 is null) return false;
                return key2.userKey is null;
            }

            return key1.Equals(key2);
        }
        public int GetHashCode(Aerospike.Client.Key key) => key?.GetHashCode() ?? 0;

        public virtual bool Equals(Aerospike.Client.Value value)
        {
            if (this.DigestRequired())
                return this.CompareDigest(value?.Object);

            if (this.Value is null || value is null)
            {
                if (value is null) return false;
                return value.Type == Aerospike.Client.Value.NullValue.Instance.Type;
            }
            return Helpers.Equals(this.Value, value.Object);
        }
        public bool Equals(Aerospike.Client.Value v1, Aerospike.Client.Value v2)
        {
            if (v1 is null) return v2 is null;
            if (v1.Object is null)
            {
                if (v2 is null) return false;
                return v2.Object is null;
            }

            return v1.Object.Equals(v2.Object);
        }
        public int GetHashCode(Aerospike.Client.Value value) => value?.Object?.GetHashCode() ?? 0;

        public virtual bool Equals(AValue value)
        {
            if (this.DigestRequired())
                return this.CompareDigest(value);

            if (this.Value is null || value is null)
            {
                if (value is null) return false;
                return value.Value is null;
            }

            if (value.DigestRequired())
                return value.CompareDigest(this);
            if(value is APrimaryKey aPK)
                return aPK.Equals(this);
            
            return Helpers.Equals(this.Value, value.Value);
        }
        public bool Equals(AValue v1, AValue v2)
        {
            if (v1 is null) return v2 is null;

            return v1.Equals(v2);
        }
        public int GetHashCode(AValue value) => value?.GetHashCode() ?? 0;

        public override int GetHashCode() => this.Value?.GetHashCode() ?? 0;

        public virtual bool Equals(byte[] byteArray) => Helpers.Equals(this.Value, byteArray);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (ReferenceEquals(this.Value, obj)) return true;
            if (obj is null) return false;
            if (obj is Aerospike.Client.Key key) return this.Equals(key);
            if (obj is Aerospike.Client.Value value) return this.Equals(value);
            if (obj is AValue pValue) return this.Equals(pValue);
            if (obj is byte[] byteArray) return Helpers.Equals(this.Value, obj);
			
            var invokeEquals = this.GetType().GetMethod("Equals", new Type[] { obj.GetType() });

            if (invokeEquals is null || invokeEquals.GetParameters().First().ParameterType == typeof(object))
                return Helpers.Equals(this.Value, obj);

            return (bool)invokeEquals.Invoke(this, new object[] { obj });
        }

        #endregion
        #region ToString
        public override string ToString() => this.Value?.ToString();
        public string DebuggerString() => $"{this.FldName ?? this.BinName}{{{this.Value} ({this.UnderlyingType?.Name})}}";
        
        /// <summary>
        /// Displays all the  public properties and fields for this object.
        /// </summary>
        /// <returns>
        /// Returns this instance.
        /// </returns>
        public AValue DebugDump()
        {
            var dictionary = new Dictionary<string, object>();

            foreach (var property in this.GetType().GetProperties())
                dictionary.Add(property.Name, property.GetValue(this));
            foreach (var field in this.GetType().GetFields())
                dictionary.Add(field.Name, field.GetValue(this));

            var underlyingType = this.IsEmpty
                                    ? "Empty"
                                    : Helpers.GetRealTypeName(this.UnderlyingType);
            LINQPad.Extensions.Dump(dictionary, $"{this.GetType().Name} {this.FldName ?? this.BinName} ({underlyingType}):");
            return this;
        }

        /// <summary>
        /// Returns the JSON string for <see cref="Value"/> using the given formatting and converters only if <see cref="Value"/> is a JToken.
        /// Otherwise the ToString of <see cref="Value"/>.
        /// </summary>
        /// <param name="formatting">Indicates how the output should be formatted.</param>
        /// <param name="converters">A collection of <see cref="JsonConverter"/>s which will be used when writing the token.</param>
        /// <returns>The JSON string or the ToString of <see cref="Value"/></returns>
        public string ToString(Formatting formatting, params JsonConverter[] converters)
        {
            if (this.Value is JToken jToken)
                return jToken.ToString(formatting, converters);
            return this.ToString();
        }
        #endregion

        #region Compare To methods

        public class EqualityComparer : IEqualityComparer<AValue>
        {
            public bool Equals(AValue x, AValue y)
                            => x?.Equals(y) ?? false;

            public int GetHashCode(AValue obj)
                            => obj?.GetHashCode() ?? 0;

            public static EqualityComparer Instance => new EqualityComparer();
        }

        public int CompareTo(object other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return this.Value is null ? 0 : 1;
            if (this.Value is null) return -1;
            if (other is Aerospike.Client.Key key) return this.CompareTo(key);
            if (other is Aerospike.Client.Value value) return this.CompareTo(value);
            if (other is AValue pValue) return this.CompareTo(pValue);

            var invokeCompare = this.GetType().GetMethod("CompareTo", new Type[] { other.GetType() });

            if (invokeCompare is null || invokeCompare.GetParameters().First().ParameterType == typeof(object)) 
                return Helpers.GetStableHashCode(this.Value).CompareTo(Helpers.GetStableHashCode(other));

            return (int)invokeCompare.Invoke(this, new object[] { other });
        }

        public int CompareTo(AValue other)
        {
            if (other is null) return 1;
            return this.CompareTo(other.Value);
        }

        public int CompareTo(Aerospike.Client.Key other)
        {
            if (other is null) return this.Value is null ? 0 : 1;
            if (this.Equals(other)) return 0;
            if (this.Value is null) return 1;
            if (other.userKey is null) return Helpers.GetStableHashCode(this.Value).CompareTo(Helpers.GetStableHashCode(other.digest));

            return this.CompareTo(other.userKey);
        }

        public int CompareTo(Aerospike.Client.Value other)
        {
            if (other is null) return 1;
            return this.CompareTo(other.Object);
        }
        #endregion

        #region AValue operator methods 
        public static bool operator ==(AValue value1, AValue value2)
        {
            if (value1 is null) return value2 is null;

            return value1.Equals(value2);
        }
        public static bool operator !=(AValue value1, AValue value2) => !(value1 == value2);

        public static bool operator <(AValue value1, AValue value2) => value1 is null ? value2 is not null : value1.CompareTo(value2) < 0;
        public static bool operator >(AValue value1, AValue value2) => value1 is not null && value1.CompareTo(value2) > 0;
        public static bool operator <=(AValue value1, AValue value2) => value1 is null || value1.CompareTo(value2) <= 0;
        public static bool operator >=(AValue value1, AValue value2) => value1 is null ? value2 is null : value1.CompareTo(value2) >= 0;

        public static bool operator ==(AValue aValue, Aerospike.Client.Value oValue) => aValue?.Equals(oValue) ?? oValue is null;
        public static bool operator !=(AValue aValue, Aerospike.Client.Value oValue) => !(aValue?.Equals(oValue) ?? oValue is null);
        public static bool operator ==(Aerospike.Client.Value oValue, AValue aValue) => aValue?.Equals(oValue) ?? oValue is null;
        public static bool operator !=(Aerospike.Client.Value oValue, AValue aValue) => !(aValue?.Equals(oValue) ?? oValue is null);

        public static bool operator <(Aerospike.Client.Value oValue, AValue aValue) => aValue is not null && aValue.CompareTo(oValue) > 0;
        public static bool operator >(Aerospike.Client.Value oValue, AValue aValue) => aValue is null ? oValue is not null : aValue.CompareTo(oValue) < 0;
        public static bool operator <=(Aerospike.Client.Value oValue, AValue aValue) => aValue is null ? oValue is null : aValue.CompareTo(oValue) >= 0;
        public static bool operator >=(Aerospike.Client.Value oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) <= 0;

        public static bool operator <(AValue aValue, Aerospike.Client.Value oValue) => aValue is null ? oValue is not null : aValue.CompareTo(oValue) < 0;
        public static bool operator >(AValue aValue, Aerospike.Client.Value oValue) => aValue is not null && aValue.CompareTo(oValue) > 0;
        public static bool operator <=(AValue aValue, Aerospike.Client.Value oValue) => aValue is null || aValue.CompareTo(oValue) <= 0;
        public static bool operator >=(AValue aValue, Aerospike.Client.Value oValue) => aValue is null ? oValue is null : aValue.CompareTo(oValue) >= 0;

        public static bool operator ==(AValue aValue, Aerospike.Client.Key oValue) => aValue?.Equals(oValue) ?? oValue is null;
        public static bool operator !=(AValue aValue, Aerospike.Client.Key oValue) => !(aValue?.Equals(oValue) ?? oValue is null);
        public static bool operator ==(Aerospike.Client.Key oValue, AValue aValue) => aValue?.Equals(oValue) ?? oValue is null;
        public static bool operator !=(Aerospike.Client.Key oValue, AValue aValue) => !(aValue?.Equals(oValue) ?? oValue is null);

        public static bool operator <(Aerospike.Client.Key oValue, AValue aValue) => aValue is not null && aValue.CompareTo(oValue) > 0;
        public static bool operator >(Aerospike.Client.Key oValue, AValue aValue) => aValue is null ? oValue is not null : aValue.CompareTo(oValue) < 0;
        public static bool operator <=(Aerospike.Client.Key oValue, AValue aValue) => aValue is null ? oValue is null : aValue.CompareTo(oValue) >= 0;
        public static bool operator >=(Aerospike.Client.Key oValue, AValue aValue) => aValue is null || aValue?.CompareTo(oValue) <= 0;

        public static bool operator <(AValue aValue, Aerospike.Client.Key oValue) => aValue is null ? oValue is not null : aValue?.CompareTo(oValue) < 0;
        public static bool operator >(AValue aValue, Aerospike.Client.Key oValue) => aValue is not null && aValue?.CompareTo(oValue) > 0;
        public static bool operator <=(AValue aValue, Aerospike.Client.Key oValue) => aValue is null || aValue?.CompareTo(oValue) <= 0;
        public static bool operator >=(AValue aValue, Aerospike.Client.Key oValue) => aValue is null ? oValue is null : aValue?.CompareTo(oValue) >= 0;
    #endregion
    
        #region Methods against Collections
        /// <summary>
        /// Returns the number of elements/chars if <see cref="Value"/> is either a collection or string.
        /// If <see cref="Value"/> is neither a collection or string, -1 is returned.
        /// </summary>
        /// <returns>
        /// Number of items in a collection or chars in a string, otherwise -1.
        /// </returns>
        public int Count()
        {
            return this.Value switch
            {
                string sValue => sValue.Length,
                JContainer jContainer => jContainer.Count,
                System.Collections.ICollection oValue => oValue.Count,
                System.Collections.IEnumerable iValue => iValue.Cast<object>().Count(),
                _ => -1
            };
        }

        [Flags]
        public enum MatchOptions
        {
            /// <summary>
            /// Depending on the <see cref="UnderlyingType"/> and matching options:
            ///     <see cref="System.Collections.IEnumerable"/> -- tries to match an element
            ///     <see cref="System.Collections.IDictionary"/> -- tries to match on Key
            ///     other types -- Tries to match on <see cref="AValue.Value"/>
            /// </summary>
            Value = 0x0001,
            /// <summary>
            /// Uses <see cref="AValue.Equals(object)"/> for matching.
            /// This is the default matching method.
            /// </summary>
            Equals = 0x0002,
            /// <summary>
            /// Depending on the <see cref="UnderlyingType"/> and matching options (default is <see cref="Equals"/>:
            ///     <see cref="System.Collections.IEnumerable"/> -- tries to match an element
            ///     <see cref="System.Collections.IDictionary"/> -- tries to match on Key or Value
            ///     other types -- Tries to match on <see cref="AValue.Value"/>
            /// If provided, <see cref="Value"/> is ignored.
            /// </summary>
            Any = 0x0004 | Value, 
            /// <summary>
            /// If the value is a string, the match occurs if this value is a substring of <see cref="AValue.Value"/>. 
            /// If not a string, the defined matching method is used.
            /// </summary>
            SubString = 0x0008,
            /// <summary>
            /// Will not try to match any elements in a collection. It will apply <see cref="AValue.Equals(object)"/> to all <see cref="AValue.Value"/>.
            /// If provided, all other options are ignored except <see cref="Regex"/>.
            /// </summary>
            Exact =  0x0010,
            /// <summary>
            /// <see cref="ToString()"/> is call on the <see cref="AValue.Value"/>, and the RegEx is applied.
            /// </summary>
            Regex = 0x0020
        }

        /// <summary>
        /// Determines if <paramref name="matchValue"/> matches <see cref="Value"/> based on <paramref name="options"/>.        
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="matchValue"/></typeparam>
        /// <param name="matchValue">
        /// The value used to determined a match based on <paramref name="options"/>.
        /// If <paramref name="options"/> is <see cref="MatchOptions.Regex"/>, this param should be a RegEx string or a <see cref="Regex"/> instance.
        /// If this param is a <see cref="KeyValuePair{TKey, TValue}"/>, both the key and value must match.
        /// </param>
        /// <param name="options">
        /// Matching options based on <see cref="MatchOptions"/>.
        /// </param>
        /// <returns>
        /// True if a match occurred.
        /// </returns>
        /// <seealso cref="MatchOptions"/>
        /// <seealso cref="Contains{K, T}(K, T)"/>
        /// <seealso cref="ContainsKey{K}(K)"/>
        /// <seealso cref="TryGetValue(object, bool)"/>
        /// <seealso cref="TryGetValue(object, out AValue)"/>
        /// <seealso cref="TryGetValue{R}(object, out R)"/>
        /// <seealso cref="TryGetValue{R}(object, R)"/>
        /// <seealso cref="FindAll{T}(T, MatchOptions)"/>
        /// <seealso cref="Equals(object)"/>
        public bool Contains<T>(T matchValue, MatchOptions options = MatchOptions.Value | MatchOptions.Equals)
        {
            Regex regex = null;
            string strMatchValue = matchValue is string sValue ? sValue : null;
            bool isKVP = Helpers.IsSubclassOfInterface(typeof(KeyValuePair<,>), matchValue.GetType());

            if(options.HasFlag(MatchOptions.Regex))
            {
                if (matchValue is Regex rValue)
                {
                    regex = rValue;
                }
                else if (strMatchValue is not null)
                {
                    regex = new Regex(strMatchValue);
                }
                else
                    throw new ArgumentException($"Match Option Regex was supplied but value provided \"{matchValue}\" was not a string or Regex instance.", nameof(options));
            }

            bool Match(object value)
            {
                if (value is AValue aValue) return Match(aValue.Value);

                if(regex is not null)
                {
                    if (value is null) return false;
                    return regex.IsMatch(value.ToString());
                }
                else if(options.HasFlag(MatchOptions.SubString)
                            && strMatchValue is not null
                            && value is string sValue)
                {
                    return sValue.Contains(strMatchValue);
                }
              
                return isKVP
                        ? Helpers.EqualsKVP(value, matchValue, out var ignore)
                        : Helpers.Equals(value, matchValue);
            }

            bool Matches(object value)
            {
                if(value is AValue aValue) { return Matches(aValue.Value); }

                switch(value)
                {
                    case IEnumerable<AValue> aCollection:
                        return Matches(aCollection.Select(a => a.Value));
                    case string sValue:
                        {
                            if (options.HasFlag(MatchOptions.Exact))
                                return options.HasFlag(MatchOptions.Regex)
                                            ? Match(sValue)
                                            : Helpers.Equals(sValue, matchValue);
                            return Match(sValue);
                        }
                    case JArray jArray:
                        {
                            if (options.HasFlag(MatchOptions.Exact))
                                return options.HasFlag(MatchOptions.Regex)
                                            ? Match(jArray)
                                            : Helpers.Equals(jArray, matchValue);

                            return jArray.Any(j => j.Type == JTokenType.Array
                                                        ? Match(j)
                                                        : Match(j.ToString()));
                        }
                    case JsonDocument jDoc:
                        return Matches(jDoc.ToDictionary().AsEnumerable());
                    case JObject jObj:
                        return Matches(CDTConverter.ConvertToDictionary(jObj).AsEnumerable());
                    case JProperty jProperty:
                        return Matches(CDTConverter.ConvertToDictionary(jProperty).AsEnumerable());                                      
                    case IEnumerable<KeyValuePair<string,object>> kvpCollection:
                        {
                            if (options.HasFlag(MatchOptions.Exact))
                                return options.HasFlag(MatchOptions.Regex)
                                            ? Match(kvpCollection)
                                            : Helpers.Equals(kvpCollection, matchValue);

                            foreach(var kvp in kvpCollection)
                            {
                                if (Match(kvp.Key))
                                    return true;
                                else if(options.HasFlag(MatchOptions.Any)
                                            && Match(kvp.Value))
                                    return true;                                            
                            }
                            break;                           
                        }
                    case IEnumerable<KeyValuePair<object, object>> kvpCollection:
                        {
                            if (options.HasFlag(MatchOptions.Exact))
                                return options.HasFlag(MatchOptions.Regex)
                                            ? Match(kvpCollection)
                                            : Helpers.Equals(kvpCollection, matchValue);

                            foreach (var kvp in kvpCollection)
                            {
                                if (Match(kvp.Key))
                                    return true;
                                else if (options.HasFlag(MatchOptions.Any)
                                            && Match(kvp.Value))
                                    return true;

                            }
                            break;
                        }
                    case IEnumerable<object> objCollection:
                        {
                            if (options.HasFlag(MatchOptions.Exact))
                                return options.HasFlag(MatchOptions.Regex)
                                            ? Match(objCollection)
                                            : Helpers.Equals(objCollection, matchValue);

                            return objCollection.Any(j => Match(j));
                        }
                    case IEnumerable<T> collection:
                        {
                            if (options.HasFlag(MatchOptions.Exact))
                                return options.HasFlag(MatchOptions.Regex)
                                            ? Match(collection)
                                            : Helpers.Equals(collection, matchValue);

                            return collection.Any(j => Match(j));
                        }                    
                    case System.Collections.IDictionary cDict:
                        {
                            if (options.HasFlag(MatchOptions.Exact))
                                return options.HasFlag(MatchOptions.Regex)
                                            ? Match(cDict)
                                            : Helpers.Equals(cDict, matchValue);

                            foreach (object obj in cDict.Keys)
                            {
                                if (Match(obj))
                                    return true;                               
                            }

                            if (options.HasFlag(MatchOptions.Any))
                            {
                                foreach (object obj in cDict.Values)
                                {
                                    if (Match(obj))
                                        return true;
                                }
                            }
                            break;
                        }
                    case System.Collections.IEnumerable iValue:
                        {
                            if (options.HasFlag(MatchOptions.Exact))
                                return options.HasFlag(MatchOptions.Regex)
                                            ? Match(iValue)
                                            : Helpers.Equals(iValue, matchValue);

                            foreach(object obj in iValue)
                            {
                                if(Match(obj)) return true;
                            }
                            break;
                        }
                    default:
                        {
                            if (options.HasFlag(MatchOptions.Exact))
                                return options.HasFlag(MatchOptions.Regex)
                                            ? Match(value)
                                            : Helpers.Equals(value, matchValue);
                            return Match(value);
                        }                       
                }

                return false;
            }

            return Matches(this.Value);
        }

        /// <summary>
        /// Determines if <paramref name="matchValue"/> matches <see cref="Value"/> based on <paramref name="options"/> and return the found value..        
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="matchValue"/></typeparam>
        /// <param name="matchValue">
        /// The value used to determined a match based on <paramref name="options"/>.
        /// If <paramref name="options"/> is <see cref="MatchOptions.Regex"/>, this param should be a RegEx string or a <see cref="Regex"/> instance.
        /// If this param is a <see cref="KeyValuePair{TKey, TValue}"/>, both the key and value must match.
        /// </param>
        /// <param name="options">
        /// Matching options based on <see cref="MatchOptions"/>.
        /// </param>
        /// <returns>
        /// All matched values or an empty collection.
        /// </returns>
        /// <seealso cref="MatchOptions"/>
        /// <seealso cref="Contains{T}(T, MatchOptions)"/>
        /// <seealso cref="Contains{K, T}(K, T)"/>
        /// <seealso cref="ContainsKey{K}(K)"/>
        /// <seealso cref="TryGetValue(object, bool)"/>
        /// <seealso cref="TryGetValue(object, out AValue)"/>
        /// <seealso cref="TryGetValue{R}(object, out R)"/>
        /// <seealso cref="TryGetValue{R}(object, R)"/>        
        public IEnumerable<AValue> FindAll<T>(T matchValue,
                                                MatchOptions options = MatchOptions.Value | MatchOptions.Equals)
        {            
            Regex regex = null;
            string strMatchValue = matchValue is string sValue ? sValue : null;
            bool isKVP = Helpers.IsSubclassOfInterface(typeof(KeyValuePair<,>), matchValue.GetType());

            if (options.HasFlag(MatchOptions.Regex))
            {
                if (matchValue is Regex rValue)
                {
                    regex = rValue;
                }
                else if (strMatchValue is not null)
                {
                    regex = new Regex(strMatchValue);
                }
                else
                    throw new ArgumentException($"Match Option Regex was supplied but value provided \"{matchValue}\" was not a string or Regex instance.", nameof(options));
            }

            bool Match(object value)
            {
                if (value is AValue aValue) return Match(aValue.Value);

                if (regex is not null)
                {
                    if (value is null) return false;
                    return regex.IsMatch(value.ToString());
                }
                else if (options.HasFlag(MatchOptions.SubString)
                            && strMatchValue is not null
                            && value is string sValue)
                {
                    return sValue.Contains(strMatchValue);
                }

                return isKVP
                        ? Helpers.EqualsKVP(value, matchValue, out var ignore)
                        : Helpers.Equals(value, matchValue);
            }

            IEnumerable<AValue> Matches(object value)
            {
                if (value is AValue aValue) { return Matches(aValue.Value); }

                switch (value)
                {
                    case IEnumerable<AValue> aCollection:
                        return Matches(aCollection.Select(a => a.Value));
                    case string sValue:
                        {
                            if (options.HasFlag(MatchOptions.Exact))
                            {
                                if (options.HasFlag(MatchOptions.Regex)
                                            ? Match(sValue)
                                            : Helpers.Equals(sValue, matchValue))
                                    return new AValue[] { new AValue(sValue, this.BinName, this.FldName) };                                
                            }
                            else if(Match(sValue))
                                return new AValue[] { new AValue(sValue, this.BinName, this.FldName) };
                            
                            return null;
                        }
                    case JArray jArray:
                        {
                            if (options.HasFlag(MatchOptions.Exact))
                            {
                                if (options.HasFlag(MatchOptions.Regex)
                                            ? Match(jArray)
                                            : Helpers.Equals(jArray, matchValue))
                                    return new AValue[] { new AValue(jArray, this.BinName, this.FldName) };
                                else 
                                    return Enumerable.Empty<AValue>();
                            }
                            var items = new List<AValue>();

                            foreach( var item in jArray)
                            {
                                if (item.Type == JTokenType.Array)
                                {
                                    if (Match(item))
                                        items.Add(new AValue(item, this.BinName, this.FldName));
                                }
                                else if (Match(item.ToString()))
                                    items.Add(new AValue(item, this.BinName, this.FldName));
                            }
                            return items.Count == 0 ? null : items;
                        }
                    case JsonDocument jDoc:
                        return Matches(jDoc.ToDictionary().AsEnumerable());
                    case JObject jObj:
                        return Matches(CDTConverter.ConvertToDictionary(jObj).AsEnumerable());
                    case JProperty jProperty:
                        return Matches(CDTConverter.ConvertToDictionary(jProperty).AsEnumerable());
                    case IEnumerable<KeyValuePair<string, object>> kvpCollection:
                        {
                            if (options.HasFlag(MatchOptions.Exact))
                            {
                                if (options.HasFlag(MatchOptions.Regex)
                                            ? Match(kvpCollection)
                                            : Helpers.Equals(kvpCollection, matchValue))
                                    return new AValue[] { new AValue(kvpCollection, this.BinName, this.FldName) };
                                else
                                    return null;
                            }

                            var items = new List<AValue>();

                            foreach (var kvp in kvpCollection)
                            {
                                if (Match(kvp.Key)
                                        || (options.HasFlag(MatchOptions.Any)
                                                    && Match(kvp.Value)))
                                    items.Add(new AValue(kvp, this.BinName, this.FldName));                                
                            }
                            return items.Any() ? items : null;
                        }
                    case IEnumerable<KeyValuePair<object, object>> kvpCollection:
                        {
                            if (options.HasFlag(MatchOptions.Exact))
                            {
                                if (options.HasFlag(MatchOptions.Regex)
                                            ? Match(kvpCollection)
                                            : Helpers.Equals(kvpCollection, matchValue))
                                    return new AValue[] { new AValue(kvpCollection, this.BinName, this.FldName) };
                                else
                                    return null;
                            }

                            var items = new List<AValue>();

                            foreach (var kvp in kvpCollection)
                            {                                
                                if (Match(kvp.Key)
                                        || (options.HasFlag(MatchOptions.Any)
                                                    && Match(kvp.Value)))
                                    items.Add(new AValue(kvp, this.BinName, this.FldName));
                            }
                            return items.Any() ? items : null;
                        }
                    case IEnumerable<object> objCollection:
                        {
                            if (options.HasFlag(MatchOptions.Exact))
                            {
                                if (options.HasFlag(MatchOptions.Regex)
                                            ? Match(objCollection)
                                            : Helpers.Equals(objCollection, matchValue))
                                    return new AValue[] { new AValue(objCollection, this.BinName, this.FldName) };
                                else
                                    return null;
                            }

                            var items = objCollection
                                            .Where(j => Match(j))
                                            .Select(j => j is AValue jValue 
                                                            ? jValue
                                                            : new AValue(j, this.BinName, this.FldName));
                            return items.Any()
                                    ? items
                                    : null;
                        }
                    case IEnumerable<T> collection:
                        {
                            if (options.HasFlag(MatchOptions.Exact))
                            {
                                if (options.HasFlag(MatchOptions.Regex)
                                ? Match(collection)
                                            : Helpers.Equals(collection, matchValue))
                                    return new AValue[] { new AValue(collection, this.BinName, this.FldName) };
                                else
                                    return null;
                            }

                            var items = collection
                                            .Where(j => Match(j))
                                            .Select(j => j is AValue jValue
                                                            ? jValue
                                                            : new AValue(j, this.BinName, this.FldName));
                            return items.Any()
                                    ? items
                                    : null;
                        }                    
                    case System.Collections.IDictionary cDict:
                        {
                            if (options.HasFlag(MatchOptions.Exact))
                            {
                                if (options.HasFlag(MatchOptions.Regex)
                                            ? Match(cDict)
                                            : Helpers.Equals(cDict, matchValue))
                                    return new AValue[] { new AValue(cDict, this.BinName, this.FldName) };
                                else
                                    return null;
                            }

                            var items = new List<AValue>();

                            foreach (object obj in cDict.Keys)
                            {
                                if (Match(obj))
                                    items.Add(new AValue(obj, this.BinName, this.FldName));
                            }

                            if (options.HasFlag(MatchOptions.Any))
                            {
                                foreach (object obj in cDict.Values)
                                {
                                    if (Match(obj))
                                        items.Add(new AValue(obj, this.BinName, this.FldName));
                                }
                            }

                            return items.Any()
                                    ? items.Distinct(EqualityComparer.Instance)
                                    : null;
                        }
                    case System.Collections.IEnumerable iValue:
                        {
                            if (options.HasFlag(MatchOptions.Exact))
                            {
                                if (options.HasFlag(MatchOptions.Regex)
                                            ? Match(iValue)
                                            : Helpers.Equals(iValue, matchValue))
                                    return new AValue[] { new AValue(iValue, this.BinName, this.FldName) };
                                else
                                    return null;
                            }

                            var items = new List<AValue>();

                            foreach (object obj in iValue)
                            {
                                if (Match(obj))
                                {
                                    if (obj is AValue aObj)
                                        items.Add(aObj);
                                    else
                                        items.Add(new AValue(obj, this.BinName, this.FldName));
                                }
                            }
                            return items.Any()
                                    ? items
                                    : null;
                        }
                    default:
                        {
                            if (options.HasFlag(MatchOptions.Exact))
                            {
                                if (options.HasFlag(MatchOptions.Regex)
                                            ? Match(value)
                                            : Helpers.Equals(value, matchValue))
                                    return new AValue[] { new AValue(value, this.BinName, this.FldName) };
                                else
                                    return null;
                            }

                            if(Match(value))
                            {
                                return new AValue[] { new AValue(value, this.BinName, this.FldName) };
                            }
                        }
                        break;
                }

                return null;
            }

            return Matches(this.Value)?.Where(i => i is not null)
                    ?? Enumerable.Empty<AValue>();
        }

        /// <summary>
        /// Determines if <paramref name="key"/> and <paramref name="value"/> is contained in <see cref="Value"/>.
        /// If <see cref="Value"/> is a <seealso cref="System.Collections.IDictionary"/>, <seealso cref="JsonDocument"/>, or <see cref="KeyValuePair{TKey, TValue}"/>, each Key/Value pair is compared. 
        /// If <see cref="Value"/> is anything else, false is always returned.
        /// </summary>
        /// <typeparam name="K">The type of <paramref name="key"/></typeparam>
        /// <typeparam name="T">The type of <paramref name="value"/></typeparam>
        /// <param name="key">The key used to obtain the value</param>
        /// <param name="value">The value used to determined if it exists</param>
        /// <returns>
        /// True if it is contained within a collection or false otherwise.
        /// </returns>
        /// <seealso cref="Contains{T}(T, MatchOptions)"/>
        /// <seealso cref="ContainsKey{K}(K)"/>
        /// <seealso cref="TryGetValue(object, bool)"/>
        /// <seealso cref="TryGetValue(object, out AValue)"/>
        /// <seealso cref="TryGetValue{R}(object, out R)"/>
        /// <seealso cref="TryGetValue{R}(object, R)"/>
        /// <seealso cref="Equals(object)"/>
        public bool Contains<K, T>(K key, T value)
        {
            return this.Value switch
            {
                JObject jObj
                    => key is string sKey
                            && jObj.ContainsKey(sKey)
                            && Helpers.Equals(jObj[sKey], value),
                IDictionary<K, T> tDict
                    => tDict.ContainsKey(key)
                            && Helpers.Equals(tDict[key], value),
                System.Collections.IDictionary cDict
                    => cDict.Contains((object)key)
                            && Helpers.Equals(cDict[key], value),
                KeyValuePair<K, T> kvp
                    => Helpers.Equals(kvp.Key, key) && Helpers.Equals(kvp.Value, value),
                _ => false
            };
        }

        /// <summary>
        /// Determines if <paramref name="key"/> is a key/property field within a <see cref="System.Collections.IDictionary"/> or <see cref="JObject"/>.
        /// </summary>
        /// <typeparam name="K">The type of <paramref name="key"/></typeparam>
        /// <param name="key">The value used to determined if the key exists</param>
        /// <returns>
        /// True is the key/property field exists.
        /// </returns>
        /// <seealso cref="Contains{K, T}(K, T)"/>
        /// <seealso cref="Contains{T}(T, MatchOptions)"/>
        /// <seealso cref="TryGetValue(object, bool)"/>
        /// <seealso cref="TryGetValue(object, out AValue)"/>
        /// <seealso cref="TryGetValue{R}(object, out R)"/>
        /// <seealso cref="TryGetValue{R}(object, R)"/>
        /// <seealso cref="Equals(object)"/>
        public bool ContainsKey<K>(K key)
        {
            return this.Value switch
            {
                JObject jObj
                    => key is string sKey
                            && jObj.ContainsKey(sKey),                
                System.Collections.IDictionary cDict
                    => cDict.Contains((object) key),
                _ => Helpers.EqualsKVP(this.Value, key, out var _)
            };
        }

        /// <summary>
        /// Returns the converted value based on <typeparamref name="R"/>, if possible, only if there is a match based on <paramref name="matchValue"/> and this AValue&apos;s <see cref="Value"/>.
        /// If the matched value cannot be converted to <typeparamref name="R"/>, this will return false.
        /// A match occurs when any of the following happens:
        ///     <see cref="Value"/> is a <see cref="IEnumerable{T}"/> or <see cref="JArray"/> and one of the elements matches <paramref name="matchValue"/>
        ///     <see cref="Value"/> is a <see cref="IDictionary{TKey, TValue}"/> and the key matches <paramref name="matchValue"/>
        ///     <see cref="Value"/> is a <see cref="KeyValuePair{TKey, TValue}"/> and the key matches <paramref name="matchValue"/>
        ///         If <paramref name="matchValue"/> is a <see cref="KeyValuePair{TKey, TValue}"/>, both the key and value must match
        ///     <see cref="Value"/> is a <see cref="string"/> and <paramref name="matchValue"/> is contained within
        ///     Otherwise <see cref="Value"/> <see cref="Equals(Object)"/> is applied against <paramref name="matchValue"/>
        /// </summary>
        /// <typeparam name="R">The type used to convert the matched value</typeparam>        
        /// <param name="matchValue">
        /// The value used to determine if a match occurred.
        /// </param>
        /// <param name="resultValue">
        /// Returns the converted matched value, if possible. 
        /// If a match dose not occur or the value could not be converted, this will be the default value of type <typeparamref name="R"/>. 
        /// </param>
        /// <returns>
        /// True to indicate that a match occurred and the matched value could be converted.
        /// </returns>
        /// <exception cref="InvalidCastException">
        /// Thrown if <paramref name="matchValue"/> cannot be converted to <typeparamref name="R"/>
        /// </exception>
        /// <seealso cref="Convert"/>
        /// <seealso cref="Value"/>
        /// <seealso cref="Contains{K, T}(K, T)"/>
        /// <seealso cref="Contains{T}(T, MatchOptions)"/>
        /// <seealso cref="ContainsKey{K}(K)"/>
        /// <seealso cref="Equals(object)"/>
        /// <seealso cref="TryGetValue(object, bool)"/>
        /// <seealso cref="TryGetValue(object, out AValue)"/>
        /// <seealso cref="TryGetValue{R}(object, R)"/>
        public bool TryGetValue<R>(object matchValue, out R resultValue)
        {
            if (matchValue is null)
            {
                if(this.IsEmpty)
                {
                    if (typeof(R) == typeof(AValue))
                        resultValue = (R)(object)Empty;
                    else
                        resultValue = default;
                    return true;
                }

                resultValue = default;
                return false;
            }

            if (matchValue is AValue akey)
                return this.TryGetValue(akey.Value, out resultValue);

            bool TryConvertToTypeO(object oValue, out R resultValue)
            {
                try
                {
                    resultValue =  oValue is R rValue
                                    ? rValue
                                    : ( typeof(R) == typeof(AValue)
                                        ? (R) (object) new AValue(oValue, this.BinName, this.FldName)
                                        : (R) Helpers.CastToNativeTypeInvalidCast(this.FldName, typeof(R), this.BinName, oValue));
                    return true;
                }
                catch (ArgumentException) { }

                resultValue = default;
                return false;
            }
            bool TryConvertToTypeA (AValue aValue, out R resultValue)
                    => TryConvertToTypeO(aValue.Value, out resultValue);
            bool TryConvertToType(out R resultValue) => TryConvertToTypeA(this, out resultValue);
                        
            switch (this.Value)
            {
                case R rValue:
                    if (matchValue.Equals(rValue))
                    {
                        resultValue = rValue;
                        return true;
                    }
                    break;
                case AValue aValue:
                    if(aValue.Equals(matchValue))
                    {
                        return TryConvertToTypeA(aValue, out resultValue);
                    }
                    break;
                case string sValue:
                    if((matchValue is string sKey
                                && sValue.Contains(sKey))
                            || Helpers.Equals(matchValue, sValue))
                    {
                        return TryConvertToType(out resultValue);
                    }
                    break;
                case JArray jArray:
                    if(jArray.Contains(JToken.FromObject(matchValue)))
                    {
                        return TryConvertToType(out resultValue);
                    }
                    break;
                case IEnumerable<AValue> aCollection:
                    {
                        var fndValue = aCollection.FirstOrDefault(f => f.Equals(matchValue));
                        if (fndValue is not null)
                            return TryConvertToTypeA(fndValue, out resultValue);
                    }
                    break;
                case IEnumerable<object> oCollection:
                    {
                        var fndValue = oCollection.FirstOrDefault(f => Helpers.Equals(matchValue, f));
                        if (fndValue is not null)
                            return TryConvertToTypeO(fndValue, out resultValue);
                    }
                    break;
                case IDictionary<AValue,AValue> aDict:
                    {
                        var fndValue = aDict.FirstOrDefault(f => f.Key.Equals(matchValue));
                        if (fndValue.Key is not null)
                            return TryConvertToTypeA(fndValue.Value, out resultValue);
                    }
                    break;
                case IDictionary<object,object> cDict:                    
                    {
                        var fndValue = cDict.FirstOrDefault(f => Helpers.Equals(matchValue, f.Key));
                        if (fndValue.Key is not null)
                            return TryConvertToTypeO(fndValue.Value, out resultValue);
                    }
                    break;
                case IDictionary<string, object> sDict:
                    {
                        var fndValue = sDict.FirstOrDefault(f => Helpers.Equals(matchValue, f.Key));
                        if (fndValue.Key is not null)
                            return TryConvertToTypeO(fndValue.Value, out resultValue);
                    }
                    break;
                case System.Collections.IEnumerable collection:
                    {
                        var fndValue = collection
                                            .Cast<object>()
                                            .FirstOrDefault(f => Helpers.Equals(matchValue, f));
                        if (fndValue is not null)
                            return TryConvertToTypeO(fndValue, out resultValue);
                    }
                    break;                
            }

            if (Helpers.EqualsKVP(this.Value, matchValue, out object kvpValue))
            {
                return TryConvertToTypeO(kvpValue, out resultValue);               
            }
            else if (Helpers.Equals(this.Value, matchValue))
            {
                return TryConvertToTypeO(this.Value, out resultValue);
            }

            resultValue = default;
            return false;
        }

        /// <summary>
        /// Returns the converted value based on <typeparamref name="R"/>, if possible, only if there is a match based on <paramref name="matchValue"/> and this AValue&apos;s <see cref="Value"/>.
        /// If the matched value cannot be converted to <typeparamref name="R"/>, this will return false.
        /// A match occurs when any of the following happens:
        ///     <see cref="Value"/> is a <see cref="IEnumerable{T}"/> or <see cref="JArray"/> and one of the elements matches <paramref name="matchValue"/>
        ///     <see cref="Value"/> is a <see cref="IDictionary{TKey, TValue}"/> and the key matches <paramref name="matchValue"/>
        ///     <see cref="Value"/> is a <see cref="KeyValuePair{TKey, TValue}"/> and the key matches <paramref name="matchValue"/>
        ///         If <paramref name="matchValue"/> is a <see cref="KeyValuePair{TKey, TValue}"/>, both the key and value must match
        ///     <see cref="Value"/> is a <see cref="string"/> and <paramref name="matchValue"/> is contained within
        ///     Otherwise <see cref="Value"/> <see cref="Equals(Object)"/> is applied against <paramref name="matchValue"/>
        /// </summary>
        /// <typeparam name="R">The type used to convert the matched value</typeparam>
        /// <param name="matchValue">
        /// The value used to determine if a match occurred.
        /// </param>
        /// <param name="defaultValue">
        /// The default value if a match dose not occur or the value could not be converted.
        /// </param>
        /// <returns>
        /// Returns the converted matched value, if possible. 
        /// If a match dose not occur or the value could not be converted, this will return <paramref name="defaultValue"/>. 
        /// </returns>
        /// <seealso cref="Convert"/>
        /// <seealso cref="Value"/>
        /// <seealso cref="Contains{K, T}(K, T)"/>
        /// <seealso cref="Contains{T}(T, MatchOptions)"/>
        /// <seealso cref="ContainsKey{K}(K)"/>
        /// <seealso cref="Equals(object)"/>
        /// <seealso cref="TryGetValue(object, bool)"/>
        /// <seealso cref="TryGetValue(object, out AValue)"/>
        /// <seealso cref="TryGetValue{R}(object, out R)"/>        
        public R TryGetValue<R>(object matchValue, R defaultValue = default)
                    => TryGetValue<R>(matchValue, out R matchedValue) ? matchedValue : defaultValue;

        /// <summary>
        /// Returns <see cref="AValue"/>, only if there is a match based on <paramref name="matchValue"/> and this AValue&apos;s <see cref="Value"/>.
        /// 
        /// A match occurs when any of the following happens:
        ///     <see cref="Value"/> is a <see cref="IEnumerable{T}"/> or <see cref="JArray"/> and one of the elements matches <paramref name="matchValue"/>
        ///     <see cref="Value"/> is a <see cref="IDictionary{TKey, TValue}"/> and the key matches <paramref name="matchValue"/>
        ///     <see cref="Value"/> is a <see cref="KeyValuePair{TKey, TValue}"/> and the key matches <paramref name="matchValue"/>
        ///         If <paramref name="matchValue"/> is a <see cref="KeyValuePair{TKey, TValue}"/>, both the key and value must match
        ///     <see cref="Value"/> is a <see cref="string"/> and <paramref name="matchValue"/> is contained within
        ///     Otherwise <see cref="Value"/> <see cref="Equals(Object)"/> is applied against <paramref name="matchValue"/>
        /// </summary>
        /// <param name="matchValue">
        /// The value used to determine if a match occurred.
        /// </param>
        /// <param name="resultValue">
        /// Returns the AValue which matched <paramref name="matchValue"/>.
        /// If a match dose not occur, this will be null. 
        /// </param>
        /// <returns>
        /// True to indicate the match was found.
        /// </returns>
        /// <seealso cref="Convert"/>
        /// <seealso cref="Value"/>
        /// <seealso cref="Contains{K, T}(K, T)"/>
        /// <seealso cref="Contains{T}(T, MatchOptions)"/>
        /// <seealso cref="ContainsKey{K}(K)"/>
        /// <seealso cref="Equals(object)"/>
        /// <seealso cref="TryGetValue(object, bool)"/>
        /// <seealso cref="TryGetValue{R}(object, out R)"/>
        /// <seealso cref="TryGetValue{R}(object, R)"/>
        public bool TryGetValue(object matchValue, out AValue resultValue)
                        => TryGetValue<AValue>(matchValue, out resultValue);

        /// <summary>
        /// Returns <see cref="AValue"/>, only if there is a match based on <paramref name="matchValue"/> and this AValue&apos;s <see cref="Value"/>.
        /// 
        /// A match occurs when any of the following happens:
        ///     <see cref="Value"/> is a <see cref="IEnumerable{T}"/> or <see cref="JArray"/> and one of the elements matches <paramref name="matchValue"/>
        ///     <see cref="Value"/> is a <see cref="IDictionary{TKey, TValue}"/> and the key matches <paramref name="matchValue"/>
        ///     <see cref="Value"/> is a <see cref="KeyValuePair{TKey, TValue}"/> and the key matches <paramref name="matchValue"/>
        ///         If <paramref name="matchValue"/> is a <see cref="KeyValuePair{TKey, TValue}"/>, both the key and value must match
        ///     <see cref="Value"/> is a <see cref="string"/> and <paramref name="matchValue"/> is contained within
        ///     Otherwise <see cref="Value"/> <see cref="Equals(Object)"/> is applied against <paramref name="matchValue"/>
        /// </summary>
        /// <param name="matchValue">
        /// The value used to determine if a match occurred.
        /// </param>
        /// <param name="returnEmptyAValue">
        /// If true and if a match was not found, a null AValue will be returned.
        /// </param>
        /// <returns>
        /// Returns the matched <see cref="AValue"/>. If no match found either <see cref="AValue.Empty"/>, or null is returned.
        /// </returns>
        /// <seealso cref="Convert"/>
        /// <seealso cref="Value"/>
        /// <seealso cref="Contains{K, T}(K, T)"/>
        /// <seealso cref="Contains{T}(T, MatchOptions)"/>
        /// <seealso cref="ContainsKey{K}(K)"/>
        /// <seealso cref="Equals(object)"/>
        /// <seealso cref="TryGetValue(object, bool)"/>
        /// <seealso cref="TryGetValue(object, out AValue)"/>
        /// <seealso cref="TryGetValue{R}(object, out R)"/>
        /// <seealso cref="TryGetValue{R}(object, R)"/>
        public AValue TryGetValue(object matchValue, bool returnEmptyAValue = false)
                    => TryGetValue<AValue>(matchValue, out AValue matchedValue) 
                            ? matchedValue
                            : (returnEmptyAValue ? Empty : null);
       
        #endregion
    }
}

