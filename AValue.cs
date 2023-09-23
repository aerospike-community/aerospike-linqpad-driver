
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

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
    /// <seealso cref="Aerospike.Client.LPDHelpers.ToAValue(Client.Bin)"/>
    /// <seealso cref="Aerospike.Client.LPDHelpers.ToAPrimaryKey(Client.Key)"/>
    /// <seealso cref="Aerospike.Client.LPDHelpers.ToAValue(Client.Value)"/>
    /// <seealso cref="Aerospike.Client.LPDHelpers.ToAValue(object)"/>
    /// <seealso cref="AValueHelper.Cast{TResult}(IEnumerable{AValue})"/>
    /// <seealso cref="AValueHelper.OfType{TResult}(IEnumerable{AValue})"/>
    [DebuggerDisplay("{DebuggerString()}")]
    public class AValue : IConvertible,
                            IComparable,
                            IEquatable<AValue>,
                            IEqualityComparer<AValue>,
                            IComparable<AValue>,
                            IEquatable<Aerospike.Client.Key>,
                            IEqualityComparer<Aerospike.Client.Key>,
                            IComparable<Aerospike.Client.Key>,
                            IEquatable<Aerospike.Client.Value>,
                            IEqualityComparer<Aerospike.Client.Value>,
                            IComparable<Aerospike.Client.Value>
                    , IEquatable< string >
            , IEqualityComparer< string >
            , IComparable< string >            
                     , IEquatable< bool >
            , IEqualityComparer< bool >
            , IComparable< bool >            
                     , IEquatable< Enum >
            , IEqualityComparer< Enum >
            , IComparable< Enum >            
                     , IEquatable< Guid >
            , IEqualityComparer< Guid >
            , IComparable< Guid >            
                     , IEquatable< short >
            , IEqualityComparer< short >
            , IComparable< short >            
                     , IEquatable< int >
            , IEqualityComparer< int >
            , IComparable< int >            
                     , IEquatable< long >
            , IEqualityComparer< long >
            , IComparable< long >            
                     , IEquatable< ushort >
            , IEqualityComparer< ushort >
            , IComparable< ushort >            
                     , IEquatable< uint >
            , IEqualityComparer< uint >
            , IComparable< uint >            
                     , IEquatable< ulong >
            , IEqualityComparer< ulong >
            , IComparable< ulong >            
                     , IEquatable< decimal >
            , IEqualityComparer< decimal >
            , IComparable< decimal >            
                     , IEquatable< float >
            , IEqualityComparer< float >
            , IComparable< float >            
                     , IEquatable< double >
            , IEqualityComparer< double >
            , IComparable< double >            
                     , IEquatable< byte >
            , IEqualityComparer< byte >
            , IComparable< byte >            
                     , IEquatable< sbyte >
            , IEqualityComparer< sbyte >
            , IComparable< sbyte >            
                     , IEquatable< DateTime >
            , IEqualityComparer< DateTime >
            , IComparable< DateTime >            
                     , IEquatable< DateTimeOffset >
            , IEqualityComparer< DateTimeOffset >
            , IComparable< DateTimeOffset >            
                     , IEquatable< TimeSpan >
            , IEqualityComparer< TimeSpan >
            , IComparable< TimeSpan >            
           
                     , IEquatable< JObject >
            , IEqualityComparer< JObject >
                     , IEquatable< JArray >
            , IEqualityComparer< JArray >
                     , IEquatable< JValue >
            , IEqualityComparer< JValue >
                     , IEquatable< JToken >
            , IEqualityComparer< JToken >
           
    {

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
        public Type UnderlyingType { get => this.Value.GetType(); }


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
        /// Returns true if the value is a IDictionary
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
        /// Tries to convert <see cref="Value"/> to a JToken.
        /// </summary>
        /// <returns>A <see cref="JToken"/> or an empty JToken</returns>
        public JToken ToJson()
        {
            if (
                            this.UnderlyingType == typeof(JObject) ||
                            this.UnderlyingType == typeof(JArray) ||
                            this.UnderlyingType == typeof(JValue) ||
                            this.UnderlyingType == typeof(JToken) ||
                            false)
            {
                return (JToken)this.Value;
            }

            try
            {
                return (JToken)Newtonsoft.Json.JsonConvert.SerializeObject(this.Value);
            } catch
            {
                return new JObject();
            }
        }

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
            else if (this.Value is System.Collections.IDictionary iDict)
            {                
                var kvps = iDict.Keys.Cast<object>().Zip(iDict.Values.Cast<object>(),
                                                            (k,v) => new KeyValuePair<object,object>(k, v));
                return new Dictionary<object, object>(kvps);
            }

            return new Dictionary<object, object>(0);
        }

        /// <summary>
        /// Tries to convert <see cref="Value"/> to a IList. If not possible an empty list is returned.
        /// </summary>
        /// <returns>
        /// An IList, if possible, otherwise an empty IList.
        /// </returns>
        public IList<object> ToList()
        {    
            if(this.Value is IList<object> iList)
            {
                return iList;
            }  
            else if (this.Value is JObject
                        || this.Value is JProperty)
            {
                return this.ToDictionary()
                            .Cast<object>().ToList();
            }
            else if(this.Value is JArray jArray)
            {
                return CDTConverter.ConvertToList(jArray);                
            }                                
            else if(this.Value is System.Collections.IEnumerable iEnum)
            {
                return iEnum.Cast<object>().ToList();
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
            if(this.IsCDT
                || this.Value is JProperty)
            {
                return this.ToList();
            }
            if(this.Value is JValue jValue)
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
        public IEnumerable<IDictionary<string,object>> ToCDT()
        {            
            var listItem = this.ToList();

            if(listItem.Count > 0)
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
                    
                    return null;
                }

                return Aerospike.Client.LPDHelpers.ToCDT(listItem
                                                            .Select(i => IsDoc(i))
                                                            .Where(i => i != null));
            }
       
            return new List<IDictionary<string,object>>(0);;
        }

        /// <summary>
        /// Converts <see cref="Value"/> into a .net native type
        /// </summary>
        /// <typeparam name="T">.Net Type to convert to</typeparam>
        /// <returns>
        /// The converted value
        /// </returns>
        public T Convert<T>() => this.Value is T ? (T) this.Value : (T) Helpers.CastToNativeType(this.FldName, typeof(T), this.BinName, this.Value);

        /// <summary>
        /// Returns an enumerable object, if possible.
        /// </summary>
        /// <typeparam name="T">The element type</typeparam>
        /// <returns>
        /// Returns an enumerable object
        /// </returns>
        public IEnumerable<T> AsEnumerable<T>() => (T[]) this.Convert<T[]>();
        /// <summary>
        /// Returns an enumerable object, if possible.
        /// </summary>
        /// <returns>
        /// Returns an enumerable object
        /// </returns>
        public System.Collections.IEnumerable AsEnumerable() => (object[]) this.Convert<object[]>();

        virtual public object ToDump()
        {
            return this.Value;
        }

        protected virtual bool DigestRequired() => false;
        protected virtual bool CompareDigest(object value) => false;

        public bool Equals(Aerospike.Client.Key key)
        {
            if(this.DigestRequired())
                return this.CompareDigest(key);
                                                
           if(this.Value is null || key is null)
           {
                if(key is null) return false;                
           }
           return this.Equals(key.userKey);
        }
        public bool Equals(Aerospike.Client.Key key1, Aerospike.Client.Key key2)
        {
            if(key1 is null) return key2 is null;
            if(key1.userKey is null) 
            {
                if(key2 is null) return false;
                return key2.userKey is null ;
            }

            return key1.Equals(key2);
        }
        public int GetHashCode(Aerospike.Client.Key key) => key?.GetHashCode() ?? 0;

        public bool Equals(Aerospike.Client.Value value)
        {
            if(this.DigestRequired())
                return this.CompareDigest(value?.Object);
                                                
           if(this.Value is null || value is null)
           {
                if(value is null) return false;
                return value.Type == Aerospike.Client.Value.NullValue.Instance.Type;
           }
           return Helpers.Equals(this.Value, value.Object);
        }
        public bool Equals(Aerospike.Client.Value v1, Aerospike.Client.Value v2)
        {
            if(v1 is null) return v2 is null;
            if(v1.Object is null)
            {
                if(v2 is null) return false;
                return v2.Object is null;
            }

            return v1.Object.Equals(v2.Object);
        }
        public int GetHashCode(Aerospike.Client.Value value) => value?.Object?.GetHashCode() ?? 0;
        
        public bool Equals(AValue value)
        {
            if(this.DigestRequired())
                return this.CompareDigest(value);
                                                
           if(this.Value is null || value is null)
           {
                if(value is null) return false;
                return value.Value is null;
           }

           if(value.DigestRequired()) return value.CompareDigest(this);

           return Helpers.Equals(this.Value, value.Value);
        }
        public bool Equals(AValue v1, AValue v2)
        {
            if(v1 is null) return v2 is null;
            
            return v1.Equals(v2);
        }
        public int GetHashCode(AValue value) => value?.GetHashCode() ?? 0;

        public override string ToString() => this.Value?.ToString();
        public string DebuggerString() => $"{this.FldName ?? this.BinName}{{{this.Value} ({this.UnderlyingType?.Name})}}";

#pragma warning disable CS0618 // Type or member is obsolete
        /// <summary>
        /// Returns the string for <see cref="Value"/> using the given format, if possible.         
        /// Otherwise the ToString of <see cref="Value"/> is used.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="throwOnFormatException">
        /// If true and a format exception occurs, it will be re-thrown.
        /// The default is false and the ToString value is returned.
        /// </param>
        /// <returns>The string value of <see cref="Value"/></returns>
        public string ToString(string format, bool throwOnFormatException = false)
        {
            try
            {
                switch(this.Value)
                {
                        case System.Enum vEnum:
                        return vEnum.ToString(format);
                            case System.Guid vGuid:
                        return vGuid.ToString(format);
                            case System.Int16 vInt16:
                        return vInt16.ToString(format);
                            case System.Int32 vInt32:
                        return vInt32.ToString(format);
                            case System.Int64 vInt64:
                        return vInt64.ToString(format);
                            case System.UInt16 vUInt16:
                        return vUInt16.ToString(format);
                            case System.UInt32 vUInt32:
                        return vUInt32.ToString(format);
                            case System.UInt64 vUInt64:
                        return vUInt64.ToString(format);
                            case System.Decimal vDecimal:
                        return vDecimal.ToString(format);
                            case System.Single vSingle:
                        return vSingle.ToString(format);
                            case System.Double vDouble:
                        return vDouble.ToString(format);
                            case System.Byte vByte:
                        return vByte.ToString(format);
                            case System.SByte vSByte:
                        return vSByte.ToString(format);
                            case System.DateTime vDateTime:
                        return vDateTime.ToString(format);
                            case System.DateTimeOffset vDateTimeOffset:
                        return vDateTimeOffset.ToString(format);
                            case System.TimeSpan vTimeSpan:
                        return vTimeSpan.ToString(format);
                                            default:
                        break;
                }
                throwOnFormatException = false;
                return String.Format($"{{0:{format}}}", this.Value);
            }
            catch(FormatException)
            {
                if(throwOnFormatException) throw;
            }

            return this.ToString();
        }

        /// <summary>
        /// Returns the string for <see cref="Value"/> using the given provider, if possible.         
        /// Otherwise the ToString of <see cref="Value"/> is used.
        /// </summary>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <param name="throwOnFormatException">
        /// If true and a format exception occurs, it will be re-thrown.
        /// The default is false and the ToString value is returned.
        /// </param>
        /// <returns>The string value of <see cref="Value"/></returns>
        public string ToString(IFormatProvider provider, bool throwOnFormatException = false)
        {
            try
            {
                switch(this.Value)
                {
                        case System.String vString:
                        return vString.ToString(provider);
                            case System.Boolean vBoolean:
                        return vBoolean.ToString(provider);
                            case System.Enum vEnum:
                        return vEnum.ToString(provider);
                            case System.Int16 vInt16:
                        return vInt16.ToString(provider);
                            case System.Int32 vInt32:
                        return vInt32.ToString(provider);
                            case System.Int64 vInt64:
                        return vInt64.ToString(provider);
                            case System.UInt16 vUInt16:
                        return vUInt16.ToString(provider);
                            case System.UInt32 vUInt32:
                        return vUInt32.ToString(provider);
                            case System.UInt64 vUInt64:
                        return vUInt64.ToString(provider);
                            case System.Decimal vDecimal:
                        return vDecimal.ToString(provider);
                            case System.Single vSingle:
                        return vSingle.ToString(provider);
                            case System.Double vDouble:
                        return vDouble.ToString(provider);
                            case System.Byte vByte:
                        return vByte.ToString(provider);
                            case System.SByte vSByte:
                        return vSByte.ToString(provider);
                            case System.DateTime vDateTime:
                        return vDateTime.ToString(provider);
                            case System.DateTimeOffset vDateTimeOffset:
                        return vDateTimeOffset.ToString(provider);
                                            default:
                        break;
                }

                return String.Format(provider, "{0}", this.Value);
            }
            catch(FormatException)
            {
                if(throwOnFormatException) throw;
            }
            return this.ToString();
        }

        /// <summary>
        /// Returns the string for <see cref="Value"/> using the given format and provider, if possible.         
        /// Otherwise the ToString of <see cref="Value"/> is used.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param> 
        /// <param name="throwOnFormatException">
        /// If true and a format exception occurs, it will be re-thrown.
        /// The default is false and the ToString value is returned.
        /// </param>
        /// <returns>The string value of <see cref="Value"/></returns>
        public string ToString(string format, IFormatProvider provider, bool throwOnFormatException = false)
        {
            try
            {
                switch(this.Value)
                {
                        case System.Enum vEnum:
                        return vEnum.ToString(format, provider);
                            case System.Guid vGuid:
                        return vGuid.ToString(format, provider);
                            case System.Int16 vInt16:
                        return vInt16.ToString(format, provider);
                            case System.Int32 vInt32:
                        return vInt32.ToString(format, provider);
                            case System.Int64 vInt64:
                        return vInt64.ToString(format, provider);
                            case System.UInt16 vUInt16:
                        return vUInt16.ToString(format, provider);
                            case System.UInt32 vUInt32:
                        return vUInt32.ToString(format, provider);
                            case System.UInt64 vUInt64:
                        return vUInt64.ToString(format, provider);
                            case System.Decimal vDecimal:
                        return vDecimal.ToString(format, provider);
                            case System.Single vSingle:
                        return vSingle.ToString(format, provider);
                            case System.Double vDouble:
                        return vDouble.ToString(format, provider);
                            case System.Byte vByte:
                        return vByte.ToString(format, provider);
                            case System.SByte vSByte:
                        return vSByte.ToString(format, provider);
                            case System.DateTime vDateTime:
                        return vDateTime.ToString(format, provider);
                            case System.DateTimeOffset vDateTimeOffset:
                        return vDateTimeOffset.ToString(format, provider);
                            case System.TimeSpan vTimeSpan:
                        return vTimeSpan.ToString(format, provider);
                                            default:
                        break;
                }
                throwOnFormatException = false;
                return String.Format(provider, $"{{0:{format}}}", this.Value);
            }
            catch(FormatException)
            {
                if(throwOnFormatException) throw;
            }
            return this.ToString();
        }
#pragma warning restore CS0618 // Type or member is obsolete

        /// <summary>
        /// Returns the JSON string for <see cref="Value"/> using the given formatting and converters only if <see cref="Value"/> is a JToken.
        /// Otherwise the ToString of <see cref="Value"/>.
        /// </summary>
        /// <param name="formatting">Indicates how the output should be formatted.</param>
        /// <param name="converters">A collection of <see cref="JsonConverter"/>s which will be used when writing the token.</param>
        /// <returns>The JSON string or the ToString of <see cref="Value"/></returns>
        public string ToString(Formatting formatting, params JsonConverter[] converters)
        {
            if(this.Value is JToken jToken)
                return jToken.ToString(formatting, converters);
            return this.ToString();
        }

        public override int GetHashCode() => this.Value?.GetHashCode() ?? 0;

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(this,obj)) return true;
            if(obj is Aerospike.Client.Key key) return this.Equals(key);
            if(obj is Aerospike.Client.Value value) return this.Equals(value);
            if(obj is AValue pValue) return this.Equals(pValue);
            
            var invokeEquals = this.GetType().GetMethod("Equals", new Type[] { obj.GetType() });

            if(invokeEquals is null) return Helpers.Equals(this.Value, obj);

            return (bool) invokeEquals.Invoke(this, new object[] { obj });            
        }

        public int CompareTo(object other)
        {
            if(ReferenceEquals(this,other)) return 0;
            if(other is null) return this.Value is null ? 0 : 1;
            if(this.Value is null) return -1;
            if(other is Aerospike.Client.Key key) return this.CompareTo(key);
            if(other is Aerospike.Client.Value value) return this.CompareTo(value);
            if(other is AValue pValue) return this.CompareTo(pValue);
            
            var invokeCompare = this.GetType().GetMethod("CompareTo", new Type[] { other.GetType() });

            if(invokeCompare is null) return Helpers.GetStableHashCode(this.Value).CompareTo(Helpers.GetStableHashCode(other));

            return (int) invokeCompare.Invoke(this, new object[] { other });
        }

        public int CompareTo(AValue other)
        {
            if(other is null) return 1;
            return this.CompareTo(other.Value);
        }

        public int CompareTo(Aerospike.Client.Key other)
        {
            if(other is null) return this.Value is null ? 0 : 1;
            if(this.Equals(other)) return 0;
            if(this.Value is null) return 1;
            if(other.userKey is null) return Helpers.GetStableHashCode(this.Value).CompareTo(Helpers.GetStableHashCode(other.digest));
            
            return this.CompareTo(other.userKey);
        }

        public int CompareTo(Aerospike.Client.Value other)
        {
             if(other is null) return 1;
             return this.CompareTo(other.Object);
        }

         public TypeCode GetTypeCode()
        {
            return Type.GetTypeCode(this.UnderlyingType);
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            if(this.Value is IConvertible iConvertible)
                return iConvertible.ToBoolean(provider);

            return (bool) this;
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            if(this.Value is IConvertible iConvertible)
                return iConvertible.ToByte(provider);

            return (byte) this;
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            if(this.Value is IConvertible iConvertible)
                return iConvertible.ToChar(provider);

            return (char) this;
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            if(this.Value is IConvertible iConvertible)
                return iConvertible.ToDateTime(provider);

            return (DateTime) this;
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            if(this.Value is IConvertible iConvertible)
                return iConvertible.ToDecimal(provider);

            return (decimal) this;
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            if(this.Value is IConvertible iConvertible)
                return iConvertible.ToDouble(provider);

            return (double) this;
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            if(this.Value is IConvertible iConvertible)
                return iConvertible.ToInt16(provider);

            return (short) this;
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            if(this.Value is IConvertible iConvertible)
                return iConvertible.ToInt32(provider);

            return (int) this;
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            if(this.Value is IConvertible iConvertible)
                return iConvertible.ToInt64(provider);

            return (long) this;
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            if(this.Value is IConvertible iConvertible)
                return iConvertible.ToSByte(provider);

            return (sbyte) this;
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            if(this.Value is IConvertible iConvertible)
                return iConvertible.ToSingle(provider);

            return (float) this;
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            if(this.Value is IConvertible iConvertible)
                return iConvertible.ToString(provider);

            return (string) this;
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            if(this.Value is IConvertible iConvertible)
                return iConvertible.ToType(conversionType, provider);

            return Helpers.CastToNativeType(this.FldName, conversionType, this.BinName, this.Value);
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            if(this.Value is IConvertible iConvertible)
                return iConvertible.ToUInt16(provider);

            return (ushort) this;
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            if(this.Value is IConvertible iConvertible)
                return iConvertible.ToUInt32(provider);

            return (uint) this;
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            if(this.Value is IConvertible iConvertible)
                return iConvertible.ToUInt64(provider);

            return (ulong) this;
        }

        public static AValue ToValue(Aerospike.Client.Value value) => new AValue(value, "Value");
        public static AValue ToValue(Aerospike.Client.Bin bin) => new AValue(bin);
        public static AValue ToValue(object value) => new AValue(value, "Value", "ToValue");
            
        public static bool operator==(AValue value1, AValue value2)
        {
            if(value1 is null) return value2 is null;

            return value1.Equals(value2);
        }
	    public static bool operator!=(AValue value1, AValue value2) => !(value1 == value2);

        public static bool operator<(AValue value1, AValue value2) => value1 is null ? !(value2 is null) : value1.CompareTo(value2) < 0;
	    public static bool operator>(AValue value1, AValue value2) => !(value1 is null) && value1.CompareTo(value2) > 0;
        public static bool operator<=(AValue value1, AValue value2) => value1 is null || value1.CompareTo(value2) <= 0;
        public static bool operator>=(AValue value1, AValue value2) => value1 is null ? value2 is null : value1.CompareTo(value2) >= 0;
       
        public static bool operator==(AValue aValue, Aerospike.Client.Value oValue) => aValue?.Equals(oValue) ?? oValue is null;
	    public static bool operator!=(AValue aValue, Aerospike.Client.Value oValue) => !(aValue?.Equals(oValue) ?? oValue is null);               
        public static bool operator==(Aerospike.Client.Value oValue, AValue aValue) => aValue?.Equals(oValue) ?? oValue is null;
	    public static bool operator!=(Aerospike.Client.Value oValue, AValue aValue) => !(aValue?.Equals(oValue) ?? oValue is null);
       
        public static bool operator<(Aerospike.Client.Value oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
	    public static bool operator>(Aerospike.Client.Value oValue, AValue aValue) => aValue is null ? !(oValue is null) : aValue.CompareTo(oValue) < 0;
        public static bool operator<=(Aerospike.Client.Value oValue, AValue aValue) => aValue is null ? oValue is null : aValue.CompareTo(oValue) >= 0;
        public static bool operator>=(Aerospike.Client.Value oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) <= 0;

        public static bool operator<(AValue aValue, Aerospike.Client.Value oValue) => aValue is null ? !(oValue is null) : aValue.CompareTo(oValue) < 0;
	    public static bool operator>(AValue aValue, Aerospike.Client.Value oValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
        public static bool operator<=(AValue aValue, Aerospike.Client.Value oValue) => aValue is null || aValue.CompareTo(oValue) <= 0;
        public static bool operator>=(AValue aValue, Aerospike.Client.Value oValue) => aValue is null ? oValue is null : aValue.CompareTo(oValue) >= 0;

        public static bool operator==(AValue aValue, Aerospike.Client.Key oValue) => aValue?.Equals(oValue) ?? oValue is null;
	    public static bool operator!=(AValue aValue, Aerospike.Client.Key oValue) => !(aValue?.Equals(oValue) ?? oValue is null);               
        public static bool operator==(Aerospike.Client.Key oValue, AValue aValue) => aValue?.Equals(oValue) ?? oValue is null;
	    public static bool operator!=(Aerospike.Client.Key oValue, AValue aValue) => !(aValue?.Equals(oValue) ?? oValue is null);

        public static bool operator<(Aerospike.Client.Key oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
	    public static bool operator>(Aerospike.Client.Key oValue, AValue aValue) => aValue is null ? !(oValue is null) : aValue.CompareTo(oValue) < 0;
        public static bool operator<=(Aerospike.Client.Key oValue, AValue aValue) => aValue is null ? oValue is null : aValue.CompareTo(oValue) >= 0;
        public static bool operator>=(Aerospike.Client.Key oValue, AValue aValue) => aValue is null || aValue?.CompareTo(oValue) <= 0;

        public static bool operator<(AValue aValue, Aerospike.Client.Key oValue) => aValue is null ? !(oValue is null) : aValue?.CompareTo(oValue) < 0;
	    public static bool operator>(AValue aValue, Aerospike.Client.Key oValue) => !(aValue is null) && aValue?.CompareTo(oValue) > 0;
        public static bool operator<=(AValue aValue, Aerospike.Client.Key oValue) => aValue is null || aValue?.CompareTo(oValue) <= 0;
        public static bool operator>=(AValue aValue, Aerospike.Client.Key oValue) => aValue is null ? oValue is null : aValue?.CompareTo(oValue) >= 0;

        
            public static implicit operator string (AValue v) => v.Convert< string >();
            //public static implicit operator string[] (AValue v) => v.Convert<string[] >();            
            
            public static bool operator==(AValue av, string v) => av?.Equals(v) ?? false;
	        public static bool operator!=(AValue av, string v) => !(av == v);
            public static bool operator==(string v, AValue av) => av == v;
	        public static bool operator!=(string v, AValue av) => av != v;
           
            public static bool operator==(AValue av, List< string > v)
                    => Helpers.SequenceEquals(v, av?.Value);
	        public static bool operator!=(AValue av, List< string > v) => !(av == v);
            public static bool operator==(List< string > v, AValue av) => av == v;
	        public static bool operator!=(List< string > v, AValue av) => av != v;

            public static bool operator<(string oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
	        public static bool operator>(string oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) < 0;
            public static bool operator<=(string oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;
            public static bool operator>=(string oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) <= 0;

            public static bool operator<(AValue aValue, string oValue) => aValue is null || aValue.CompareTo(oValue) < 0;
	        public static bool operator>(AValue aValue, string oValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
            public static bool operator<=(AValue aValue, string oValue) => aValue is null || aValue.CompareTo(oValue) <= 0;
            public static bool operator>=(AValue aValue, string oValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;

             
            public bool Equals(string value)
            {
                return this.DigestRequired()
                            ? this.CompareDigest(value)
                            : this.CompareTo(value) == 0;
            }

            public bool Equals(string v1, string v2) => v1 == v2;
            public int GetHashCode(string value) => value.GetHashCode();

            public int CompareTo(string value)
            {
                                if(this.Value is null) return value is null ? 0 : -1;
                if(value is null) return 1;
                if(this.Value is string sValue) return sValue.CompareTo(value);
                if(this.Value is Guid gValue) return gValue.ToString().CompareTo(value);
                return Helpers.GetStableHashCode(this.Value).CompareTo(Helpers.GetStableHashCode(value));
                            }

        
            public static implicit operator bool (AValue v) => v.Convert< bool >();
            //public static implicit operator bool[] (AValue v) => v.Convert<bool[] >();            
            
            public static bool operator==(AValue av, bool v) => av?.Equals(v) ?? false;
	        public static bool operator!=(AValue av, bool v) => !(av == v);
            public static bool operator==(bool v, AValue av) => av == v;
	        public static bool operator!=(bool v, AValue av) => av != v;
           
            public static bool operator==(AValue av, List< bool > v)
                    => Helpers.SequenceEquals(v, av?.Value);
	        public static bool operator!=(AValue av, List< bool > v) => !(av == v);
            public static bool operator==(List< bool > v, AValue av) => av == v;
	        public static bool operator!=(List< bool > v, AValue av) => av != v;

            public static bool operator<(bool oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
	        public static bool operator>(bool oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) < 0;
            public static bool operator<=(bool oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;
            public static bool operator>=(bool oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) <= 0;

            public static bool operator<(AValue aValue, bool oValue) => aValue is null || aValue.CompareTo(oValue) < 0;
	        public static bool operator>(AValue aValue, bool oValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
            public static bool operator<=(AValue aValue, bool oValue) => aValue is null || aValue.CompareTo(oValue) <= 0;
            public static bool operator>=(AValue aValue, bool oValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;

                        public bool Tobool() => (bool) this;
              
            public bool Equals(bool value)
            {
                return this.DigestRequired()
                            ? this.CompareDigest(value)
                            : this.CompareTo(value) == 0;
            }

            public bool Equals(bool v1, bool v2) => v1 == v2;
            public int GetHashCode(bool value) => value.GetHashCode();

            public int CompareTo(bool value)
            {
                                if(this.Value is null) return -1;
               
                try {
                    var tValue = this.Value;

                    if(tValue is bool cValue)
                        return cValue.CompareTo(value);

                                    if(tValue is string)
                         return Helpers.GetStableHashCode(tValue).CompareTo(Helpers.GetStableHashCode(value));
                    
                    tValue = ((IConvertible)tValue).ToType(typeof(decimal), null);

                    return ((decimal)tValue).CompareTo((decimal)((IConvertible)value).ToType(typeof(decimal), null));
                 
                                                  
                } catch  {}

                return Helpers.GetStableHashCode(this.Value).CompareTo(Helpers.GetStableHashCode(value));
                            }

        
            public static implicit operator Enum (AValue v) => v.Convert< Enum >();
            //public static implicit operator Enum[] (AValue v) => v.Convert<Enum[] >();            
            
            public static bool operator==(AValue av, Enum v) => av?.Equals(v) ?? false;
	        public static bool operator!=(AValue av, Enum v) => !(av == v);
            public static bool operator==(Enum v, AValue av) => av == v;
	        public static bool operator!=(Enum v, AValue av) => av != v;
           
            public static bool operator==(AValue av, List< Enum > v)
                    => Helpers.SequenceEquals(v, av?.Value);
	        public static bool operator!=(AValue av, List< Enum > v) => !(av == v);
            public static bool operator==(List< Enum > v, AValue av) => av == v;
	        public static bool operator!=(List< Enum > v, AValue av) => av != v;

            public static bool operator<(Enum oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
	        public static bool operator>(Enum oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) < 0;
            public static bool operator<=(Enum oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;
            public static bool operator>=(Enum oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) <= 0;

            public static bool operator<(AValue aValue, Enum oValue) => aValue is null || aValue.CompareTo(oValue) < 0;
	        public static bool operator>(AValue aValue, Enum oValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
            public static bool operator<=(AValue aValue, Enum oValue) => aValue is null || aValue.CompareTo(oValue) <= 0;
            public static bool operator>=(AValue aValue, Enum oValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;

                        public Enum ToEnum() => (Enum) this;
              
            public bool Equals(Enum value)
            {
                return this.DigestRequired()
                            ? this.CompareDigest(value)
                            : this.CompareTo(value) == 0;
            }

            public bool Equals(Enum v1, Enum v2) => v1 == v2;
            public int GetHashCode(Enum value) => value.GetHashCode();

            public int CompareTo(Enum value)
            {
                                if(this.Value is null) return -1;
               
                try {
                    var tValue = this.Value;

                    if(tValue is Enum cValue)
                        return cValue.CompareTo(value);

                                    if(tValue is string)
                         return Helpers.GetStableHashCode(tValue).CompareTo(Helpers.GetStableHashCode(value));
                    
                    tValue = ((IConvertible)tValue).ToType(typeof(decimal), null);

                    return ((decimal)tValue).CompareTo((decimal)((IConvertible)value).ToType(typeof(decimal), null));
                 
                                                  
                } catch  {}

                return Helpers.GetStableHashCode(this.Value).CompareTo(Helpers.GetStableHashCode(value));
                            }

        
            public static implicit operator Guid (AValue v) => v.Convert< Guid >();
            //public static implicit operator Guid[] (AValue v) => v.Convert<Guid[] >();            
            
            public static bool operator==(AValue av, Guid v) => av?.Equals(v) ?? false;
	        public static bool operator!=(AValue av, Guid v) => !(av == v);
            public static bool operator==(Guid v, AValue av) => av == v;
	        public static bool operator!=(Guid v, AValue av) => av != v;
           
            public static bool operator==(AValue av, List< Guid > v)
                    => Helpers.SequenceEquals(v, av?.Value);
	        public static bool operator!=(AValue av, List< Guid > v) => !(av == v);
            public static bool operator==(List< Guid > v, AValue av) => av == v;
	        public static bool operator!=(List< Guid > v, AValue av) => av != v;

            public static bool operator<(Guid oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
	        public static bool operator>(Guid oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) < 0;
            public static bool operator<=(Guid oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;
            public static bool operator>=(Guid oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) <= 0;

            public static bool operator<(AValue aValue, Guid oValue) => aValue is null || aValue.CompareTo(oValue) < 0;
	        public static bool operator>(AValue aValue, Guid oValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
            public static bool operator<=(AValue aValue, Guid oValue) => aValue is null || aValue.CompareTo(oValue) <= 0;
            public static bool operator>=(AValue aValue, Guid oValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;

                        public Guid ToGuid() => (Guid) this;
              
            public bool Equals(Guid value)
            {
                return this.DigestRequired()
                            ? this.CompareDigest(value)
                            : this.CompareTo(value) == 0;
            }

            public bool Equals(Guid v1, Guid v2) => v1 == v2;
            public int GetHashCode(Guid value) => value.GetHashCode();

            public int CompareTo(Guid value)
            {
                                if(this.Value is null) return -1;
                if(this.Value is Guid gValue) return gValue.CompareTo(value);
                if(this.Value is string sValue) return sValue.CompareTo(value.ToString());
                return Helpers.GetStableHashCode(this.Value).CompareTo(Helpers.GetStableHashCode(value));
                            }

        
            public static implicit operator short (AValue v) => v.Convert< short >();
            //public static implicit operator short[] (AValue v) => v.Convert<short[] >();            
            
            public static bool operator==(AValue av, short v) => av?.Equals(v) ?? false;
	        public static bool operator!=(AValue av, short v) => !(av == v);
            public static bool operator==(short v, AValue av) => av == v;
	        public static bool operator!=(short v, AValue av) => av != v;
           
            public static bool operator==(AValue av, List< short > v)
                    => Helpers.SequenceEquals(v, av?.Value);
	        public static bool operator!=(AValue av, List< short > v) => !(av == v);
            public static bool operator==(List< short > v, AValue av) => av == v;
	        public static bool operator!=(List< short > v, AValue av) => av != v;

            public static bool operator<(short oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
	        public static bool operator>(short oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) < 0;
            public static bool operator<=(short oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;
            public static bool operator>=(short oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) <= 0;

            public static bool operator<(AValue aValue, short oValue) => aValue is null || aValue.CompareTo(oValue) < 0;
	        public static bool operator>(AValue aValue, short oValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
            public static bool operator<=(AValue aValue, short oValue) => aValue is null || aValue.CompareTo(oValue) <= 0;
            public static bool operator>=(AValue aValue, short oValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;

                        public short Toshort() => (short) this;
              
            public bool Equals(short value)
            {
                return this.DigestRequired()
                            ? this.CompareDigest(value)
                            : this.CompareTo(value) == 0;
            }

            public bool Equals(short v1, short v2) => v1 == v2;
            public int GetHashCode(short value) => value.GetHashCode();

            public int CompareTo(short value)
            {
                                if(this.Value is null) return -1;
               
                try {
                    var tValue = this.Value;

                    if(tValue is short cValue)
                        return cValue.CompareTo(value);

                                    if(tValue is string)
                         return Helpers.GetStableHashCode(tValue).CompareTo(Helpers.GetStableHashCode(value));
                    
                    tValue = ((IConvertible)tValue).ToType(typeof(decimal), null);

                    return ((decimal)tValue).CompareTo((decimal)((IConvertible)value).ToType(typeof(decimal), null));
                 
                                                  
                } catch  {}

                return Helpers.GetStableHashCode(this.Value).CompareTo(Helpers.GetStableHashCode(value));
                            }

        
            public static implicit operator int (AValue v) => v.Convert< int >();
            //public static implicit operator int[] (AValue v) => v.Convert<int[] >();            
            
            public static bool operator==(AValue av, int v) => av?.Equals(v) ?? false;
	        public static bool operator!=(AValue av, int v) => !(av == v);
            public static bool operator==(int v, AValue av) => av == v;
	        public static bool operator!=(int v, AValue av) => av != v;
           
            public static bool operator==(AValue av, List< int > v)
                    => Helpers.SequenceEquals(v, av?.Value);
	        public static bool operator!=(AValue av, List< int > v) => !(av == v);
            public static bool operator==(List< int > v, AValue av) => av == v;
	        public static bool operator!=(List< int > v, AValue av) => av != v;

            public static bool operator<(int oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
	        public static bool operator>(int oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) < 0;
            public static bool operator<=(int oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;
            public static bool operator>=(int oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) <= 0;

            public static bool operator<(AValue aValue, int oValue) => aValue is null || aValue.CompareTo(oValue) < 0;
	        public static bool operator>(AValue aValue, int oValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
            public static bool operator<=(AValue aValue, int oValue) => aValue is null || aValue.CompareTo(oValue) <= 0;
            public static bool operator>=(AValue aValue, int oValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;

                        public int Toint() => (int) this;
              
            public bool Equals(int value)
            {
                return this.DigestRequired()
                            ? this.CompareDigest(value)
                            : this.CompareTo(value) == 0;
            }

            public bool Equals(int v1, int v2) => v1 == v2;
            public int GetHashCode(int value) => value.GetHashCode();

            public int CompareTo(int value)
            {
                                if(this.Value is null) return -1;
               
                try {
                    var tValue = this.Value;

                    if(tValue is int cValue)
                        return cValue.CompareTo(value);

                                    if(tValue is string)
                         return Helpers.GetStableHashCode(tValue).CompareTo(Helpers.GetStableHashCode(value));
                    
                    tValue = ((IConvertible)tValue).ToType(typeof(decimal), null);

                    return ((decimal)tValue).CompareTo((decimal)((IConvertible)value).ToType(typeof(decimal), null));
                 
                                                  
                } catch  {}

                return Helpers.GetStableHashCode(this.Value).CompareTo(Helpers.GetStableHashCode(value));
                            }

        
            public static implicit operator long (AValue v) => v.Convert< long >();
            //public static implicit operator long[] (AValue v) => v.Convert<long[] >();            
            
            public static bool operator==(AValue av, long v) => av?.Equals(v) ?? false;
	        public static bool operator!=(AValue av, long v) => !(av == v);
            public static bool operator==(long v, AValue av) => av == v;
	        public static bool operator!=(long v, AValue av) => av != v;
           
            public static bool operator==(AValue av, List< long > v)
                    => Helpers.SequenceEquals(v, av?.Value);
	        public static bool operator!=(AValue av, List< long > v) => !(av == v);
            public static bool operator==(List< long > v, AValue av) => av == v;
	        public static bool operator!=(List< long > v, AValue av) => av != v;

            public static bool operator<(long oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
	        public static bool operator>(long oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) < 0;
            public static bool operator<=(long oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;
            public static bool operator>=(long oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) <= 0;

            public static bool operator<(AValue aValue, long oValue) => aValue is null || aValue.CompareTo(oValue) < 0;
	        public static bool operator>(AValue aValue, long oValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
            public static bool operator<=(AValue aValue, long oValue) => aValue is null || aValue.CompareTo(oValue) <= 0;
            public static bool operator>=(AValue aValue, long oValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;

                        public long Tolong() => (long) this;
              
            public bool Equals(long value)
            {
                return this.DigestRequired()
                            ? this.CompareDigest(value)
                            : this.CompareTo(value) == 0;
            }

            public bool Equals(long v1, long v2) => v1 == v2;
            public int GetHashCode(long value) => value.GetHashCode();

            public int CompareTo(long value)
            {
                                if(this.Value is null) return -1;
               
                try {
                    var tValue = this.Value;

                    if(tValue is long cValue)
                        return cValue.CompareTo(value);

                                    if(tValue is string)
                         return Helpers.GetStableHashCode(tValue).CompareTo(Helpers.GetStableHashCode(value));
                    
                    tValue = ((IConvertible)tValue).ToType(typeof(decimal), null);

                    return ((decimal)tValue).CompareTo((decimal)((IConvertible)value).ToType(typeof(decimal), null));
                 
                                                  
                } catch  {}

                return Helpers.GetStableHashCode(this.Value).CompareTo(Helpers.GetStableHashCode(value));
                            }

        
            public static implicit operator ushort (AValue v) => v.Convert< ushort >();
            //public static implicit operator ushort[] (AValue v) => v.Convert<ushort[] >();            
            
            public static bool operator==(AValue av, ushort v) => av?.Equals(v) ?? false;
	        public static bool operator!=(AValue av, ushort v) => !(av == v);
            public static bool operator==(ushort v, AValue av) => av == v;
	        public static bool operator!=(ushort v, AValue av) => av != v;
           
            public static bool operator==(AValue av, List< ushort > v)
                    => Helpers.SequenceEquals(v, av?.Value);
	        public static bool operator!=(AValue av, List< ushort > v) => !(av == v);
            public static bool operator==(List< ushort > v, AValue av) => av == v;
	        public static bool operator!=(List< ushort > v, AValue av) => av != v;

            public static bool operator<(ushort oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
	        public static bool operator>(ushort oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) < 0;
            public static bool operator<=(ushort oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;
            public static bool operator>=(ushort oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) <= 0;

            public static bool operator<(AValue aValue, ushort oValue) => aValue is null || aValue.CompareTo(oValue) < 0;
	        public static bool operator>(AValue aValue, ushort oValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
            public static bool operator<=(AValue aValue, ushort oValue) => aValue is null || aValue.CompareTo(oValue) <= 0;
            public static bool operator>=(AValue aValue, ushort oValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;

                        public ushort Toushort() => (ushort) this;
              
            public bool Equals(ushort value)
            {
                return this.DigestRequired()
                            ? this.CompareDigest(value)
                            : this.CompareTo(value) == 0;
            }

            public bool Equals(ushort v1, ushort v2) => v1 == v2;
            public int GetHashCode(ushort value) => value.GetHashCode();

            public int CompareTo(ushort value)
            {
                                if(this.Value is null) return -1;
               
                try {
                    var tValue = this.Value;

                    if(tValue is ushort cValue)
                        return cValue.CompareTo(value);

                                    if(tValue is string)
                         return Helpers.GetStableHashCode(tValue).CompareTo(Helpers.GetStableHashCode(value));
                    
                    tValue = ((IConvertible)tValue).ToType(typeof(decimal), null);

                    return ((decimal)tValue).CompareTo((decimal)((IConvertible)value).ToType(typeof(decimal), null));
                 
                                                  
                } catch  {}

                return Helpers.GetStableHashCode(this.Value).CompareTo(Helpers.GetStableHashCode(value));
                            }

        
            public static implicit operator uint (AValue v) => v.Convert< uint >();
            //public static implicit operator uint[] (AValue v) => v.Convert<uint[] >();            
            
            public static bool operator==(AValue av, uint v) => av?.Equals(v) ?? false;
	        public static bool operator!=(AValue av, uint v) => !(av == v);
            public static bool operator==(uint v, AValue av) => av == v;
	        public static bool operator!=(uint v, AValue av) => av != v;
           
            public static bool operator==(AValue av, List< uint > v)
                    => Helpers.SequenceEquals(v, av?.Value);
	        public static bool operator!=(AValue av, List< uint > v) => !(av == v);
            public static bool operator==(List< uint > v, AValue av) => av == v;
	        public static bool operator!=(List< uint > v, AValue av) => av != v;

            public static bool operator<(uint oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
	        public static bool operator>(uint oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) < 0;
            public static bool operator<=(uint oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;
            public static bool operator>=(uint oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) <= 0;

            public static bool operator<(AValue aValue, uint oValue) => aValue is null || aValue.CompareTo(oValue) < 0;
	        public static bool operator>(AValue aValue, uint oValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
            public static bool operator<=(AValue aValue, uint oValue) => aValue is null || aValue.CompareTo(oValue) <= 0;
            public static bool operator>=(AValue aValue, uint oValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;

                        public uint Touint() => (uint) this;
              
            public bool Equals(uint value)
            {
                return this.DigestRequired()
                            ? this.CompareDigest(value)
                            : this.CompareTo(value) == 0;
            }

            public bool Equals(uint v1, uint v2) => v1 == v2;
            public int GetHashCode(uint value) => value.GetHashCode();

            public int CompareTo(uint value)
            {
                                if(this.Value is null) return -1;
               
                try {
                    var tValue = this.Value;

                    if(tValue is uint cValue)
                        return cValue.CompareTo(value);

                                    if(tValue is string)
                         return Helpers.GetStableHashCode(tValue).CompareTo(Helpers.GetStableHashCode(value));
                    
                    tValue = ((IConvertible)tValue).ToType(typeof(decimal), null);

                    return ((decimal)tValue).CompareTo((decimal)((IConvertible)value).ToType(typeof(decimal), null));
                 
                                                  
                } catch  {}

                return Helpers.GetStableHashCode(this.Value).CompareTo(Helpers.GetStableHashCode(value));
                            }

        
            public static implicit operator ulong (AValue v) => v.Convert< ulong >();
            //public static implicit operator ulong[] (AValue v) => v.Convert<ulong[] >();            
            
            public static bool operator==(AValue av, ulong v) => av?.Equals(v) ?? false;
	        public static bool operator!=(AValue av, ulong v) => !(av == v);
            public static bool operator==(ulong v, AValue av) => av == v;
	        public static bool operator!=(ulong v, AValue av) => av != v;
           
            public static bool operator==(AValue av, List< ulong > v)
                    => Helpers.SequenceEquals(v, av?.Value);
	        public static bool operator!=(AValue av, List< ulong > v) => !(av == v);
            public static bool operator==(List< ulong > v, AValue av) => av == v;
	        public static bool operator!=(List< ulong > v, AValue av) => av != v;

            public static bool operator<(ulong oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
	        public static bool operator>(ulong oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) < 0;
            public static bool operator<=(ulong oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;
            public static bool operator>=(ulong oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) <= 0;

            public static bool operator<(AValue aValue, ulong oValue) => aValue is null || aValue.CompareTo(oValue) < 0;
	        public static bool operator>(AValue aValue, ulong oValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
            public static bool operator<=(AValue aValue, ulong oValue) => aValue is null || aValue.CompareTo(oValue) <= 0;
            public static bool operator>=(AValue aValue, ulong oValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;

                        public ulong Toulong() => (ulong) this;
              
            public bool Equals(ulong value)
            {
                return this.DigestRequired()
                            ? this.CompareDigest(value)
                            : this.CompareTo(value) == 0;
            }

            public bool Equals(ulong v1, ulong v2) => v1 == v2;
            public int GetHashCode(ulong value) => value.GetHashCode();

            public int CompareTo(ulong value)
            {
                                if(this.Value is null) return -1;
               
                try {
                    var tValue = this.Value;

                    if(tValue is ulong cValue)
                        return cValue.CompareTo(value);

                                    if(tValue is string)
                         return Helpers.GetStableHashCode(tValue).CompareTo(Helpers.GetStableHashCode(value));
                    
                    tValue = ((IConvertible)tValue).ToType(typeof(decimal), null);

                    return ((decimal)tValue).CompareTo((decimal)((IConvertible)value).ToType(typeof(decimal), null));
                 
                                                  
                } catch  {}

                return Helpers.GetStableHashCode(this.Value).CompareTo(Helpers.GetStableHashCode(value));
                            }

        
            public static implicit operator decimal (AValue v) => v.Convert< decimal >();
            //public static implicit operator decimal[] (AValue v) => v.Convert<decimal[] >();            
            
            public static bool operator==(AValue av, decimal v) => av?.Equals(v) ?? false;
	        public static bool operator!=(AValue av, decimal v) => !(av == v);
            public static bool operator==(decimal v, AValue av) => av == v;
	        public static bool operator!=(decimal v, AValue av) => av != v;
           
            public static bool operator==(AValue av, List< decimal > v)
                    => Helpers.SequenceEquals(v, av?.Value);
	        public static bool operator!=(AValue av, List< decimal > v) => !(av == v);
            public static bool operator==(List< decimal > v, AValue av) => av == v;
	        public static bool operator!=(List< decimal > v, AValue av) => av != v;

            public static bool operator<(decimal oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
	        public static bool operator>(decimal oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) < 0;
            public static bool operator<=(decimal oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;
            public static bool operator>=(decimal oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) <= 0;

            public static bool operator<(AValue aValue, decimal oValue) => aValue is null || aValue.CompareTo(oValue) < 0;
	        public static bool operator>(AValue aValue, decimal oValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
            public static bool operator<=(AValue aValue, decimal oValue) => aValue is null || aValue.CompareTo(oValue) <= 0;
            public static bool operator>=(AValue aValue, decimal oValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;

                        public decimal Todecimal() => (decimal) this;
              
            public bool Equals(decimal value)
            {
                return this.DigestRequired()
                            ? this.CompareDigest(value)
                            : this.CompareTo(value) == 0;
            }

            public bool Equals(decimal v1, decimal v2) => v1 == v2;
            public int GetHashCode(decimal value) => value.GetHashCode();

            public int CompareTo(decimal value)
            {
                                if(this.Value is null) return -1;
               
                try {
                    var tValue = this.Value;

                    if(tValue is decimal cValue)
                        return cValue.CompareTo(value);

                                    if(tValue is string)
                         return Helpers.GetStableHashCode(tValue).CompareTo(Helpers.GetStableHashCode(value));
                    
                    tValue = ((IConvertible)tValue).ToType(typeof(decimal), null);

                    return ((decimal)tValue).CompareTo((decimal)((IConvertible)value).ToType(typeof(decimal), null));
                 
                                                  
                } catch  {}

                return Helpers.GetStableHashCode(this.Value).CompareTo(Helpers.GetStableHashCode(value));
                            }

        
            public static implicit operator float (AValue v) => v.Convert< float >();
            //public static implicit operator float[] (AValue v) => v.Convert<float[] >();            
            
            public static bool operator==(AValue av, float v) => av?.Equals(v) ?? false;
	        public static bool operator!=(AValue av, float v) => !(av == v);
            public static bool operator==(float v, AValue av) => av == v;
	        public static bool operator!=(float v, AValue av) => av != v;
           
            public static bool operator==(AValue av, List< float > v)
                    => Helpers.SequenceEquals(v, av?.Value);
	        public static bool operator!=(AValue av, List< float > v) => !(av == v);
            public static bool operator==(List< float > v, AValue av) => av == v;
	        public static bool operator!=(List< float > v, AValue av) => av != v;

            public static bool operator<(float oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
	        public static bool operator>(float oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) < 0;
            public static bool operator<=(float oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;
            public static bool operator>=(float oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) <= 0;

            public static bool operator<(AValue aValue, float oValue) => aValue is null || aValue.CompareTo(oValue) < 0;
	        public static bool operator>(AValue aValue, float oValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
            public static bool operator<=(AValue aValue, float oValue) => aValue is null || aValue.CompareTo(oValue) <= 0;
            public static bool operator>=(AValue aValue, float oValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;

                        public float Tofloat() => (float) this;
              
            public bool Equals(float value)
            {
                return this.DigestRequired()
                            ? this.CompareDigest(value)
                            : this.CompareTo(value) == 0;
            }

            public bool Equals(float v1, float v2) => v1 == v2;
            public int GetHashCode(float value) => value.GetHashCode();

            public int CompareTo(float value)
            {
                                if(this.Value is null) return -1;
               
                try {
                    var tValue = this.Value;

                    if(tValue is float cValue)
                        return cValue.CompareTo(value);

                                    if(tValue is string)
                         return Helpers.GetStableHashCode(tValue).CompareTo(Helpers.GetStableHashCode(value));
                    
                    tValue = ((IConvertible)tValue).ToType(typeof(decimal), null);

                    return ((decimal)tValue).CompareTo((decimal)((IConvertible)value).ToType(typeof(decimal), null));
                 
                                                  
                } catch  {}

                return Helpers.GetStableHashCode(this.Value).CompareTo(Helpers.GetStableHashCode(value));
                            }

        
            public static implicit operator double (AValue v) => v.Convert< double >();
            //public static implicit operator double[] (AValue v) => v.Convert<double[] >();            
            
            public static bool operator==(AValue av, double v) => av?.Equals(v) ?? false;
	        public static bool operator!=(AValue av, double v) => !(av == v);
            public static bool operator==(double v, AValue av) => av == v;
	        public static bool operator!=(double v, AValue av) => av != v;
           
            public static bool operator==(AValue av, List< double > v)
                    => Helpers.SequenceEquals(v, av?.Value);
	        public static bool operator!=(AValue av, List< double > v) => !(av == v);
            public static bool operator==(List< double > v, AValue av) => av == v;
	        public static bool operator!=(List< double > v, AValue av) => av != v;

            public static bool operator<(double oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
	        public static bool operator>(double oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) < 0;
            public static bool operator<=(double oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;
            public static bool operator>=(double oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) <= 0;

            public static bool operator<(AValue aValue, double oValue) => aValue is null || aValue.CompareTo(oValue) < 0;
	        public static bool operator>(AValue aValue, double oValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
            public static bool operator<=(AValue aValue, double oValue) => aValue is null || aValue.CompareTo(oValue) <= 0;
            public static bool operator>=(AValue aValue, double oValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;

                        public double Todouble() => (double) this;
              
            public bool Equals(double value)
            {
                return this.DigestRequired()
                            ? this.CompareDigest(value)
                            : this.CompareTo(value) == 0;
            }

            public bool Equals(double v1, double v2) => v1 == v2;
            public int GetHashCode(double value) => value.GetHashCode();

            public int CompareTo(double value)
            {
                                if(this.Value is null) return -1;
               
                try {
                    var tValue = this.Value;

                    if(tValue is double cValue)
                        return cValue.CompareTo(value);

                                    if(tValue is string)
                         return Helpers.GetStableHashCode(tValue).CompareTo(Helpers.GetStableHashCode(value));
                    
                    tValue = ((IConvertible)tValue).ToType(typeof(decimal), null);

                    return ((decimal)tValue).CompareTo((decimal)((IConvertible)value).ToType(typeof(decimal), null));
                 
                                                  
                } catch  {}

                return Helpers.GetStableHashCode(this.Value).CompareTo(Helpers.GetStableHashCode(value));
                            }

        
            public static implicit operator byte (AValue v) => v.Convert< byte >();
            //public static implicit operator byte[] (AValue v) => v.Convert<byte[] >();            
            
            public static bool operator==(AValue av, byte v) => av?.Equals(v) ?? false;
	        public static bool operator!=(AValue av, byte v) => !(av == v);
            public static bool operator==(byte v, AValue av) => av == v;
	        public static bool operator!=(byte v, AValue av) => av != v;
           
            public static bool operator==(AValue av, List< byte > v)
                    => Helpers.SequenceEquals(v, av?.Value);
	        public static bool operator!=(AValue av, List< byte > v) => !(av == v);
            public static bool operator==(List< byte > v, AValue av) => av == v;
	        public static bool operator!=(List< byte > v, AValue av) => av != v;

            public static bool operator<(byte oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
	        public static bool operator>(byte oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) < 0;
            public static bool operator<=(byte oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;
            public static bool operator>=(byte oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) <= 0;

            public static bool operator<(AValue aValue, byte oValue) => aValue is null || aValue.CompareTo(oValue) < 0;
	        public static bool operator>(AValue aValue, byte oValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
            public static bool operator<=(AValue aValue, byte oValue) => aValue is null || aValue.CompareTo(oValue) <= 0;
            public static bool operator>=(AValue aValue, byte oValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;

                        public byte Tobyte() => (byte) this;
              
            public bool Equals(byte value)
            {
                return this.DigestRequired()
                            ? this.CompareDigest(value)
                            : this.CompareTo(value) == 0;
            }

            public bool Equals(byte v1, byte v2) => v1 == v2;
            public int GetHashCode(byte value) => value.GetHashCode();

            public int CompareTo(byte value)
            {
                                if(this.Value is null) return -1;
               
                try {
                    var tValue = this.Value;

                    if(tValue is byte cValue)
                        return cValue.CompareTo(value);

                                    if(tValue is string)
                         return Helpers.GetStableHashCode(tValue).CompareTo(Helpers.GetStableHashCode(value));
                    
                    tValue = ((IConvertible)tValue).ToType(typeof(decimal), null);

                    return ((decimal)tValue).CompareTo((decimal)((IConvertible)value).ToType(typeof(decimal), null));
                 
                                                  
                } catch  {}

                return Helpers.GetStableHashCode(this.Value).CompareTo(Helpers.GetStableHashCode(value));
                            }

        
            public static implicit operator sbyte (AValue v) => v.Convert< sbyte >();
            //public static implicit operator sbyte[] (AValue v) => v.Convert<sbyte[] >();            
            
            public static bool operator==(AValue av, sbyte v) => av?.Equals(v) ?? false;
	        public static bool operator!=(AValue av, sbyte v) => !(av == v);
            public static bool operator==(sbyte v, AValue av) => av == v;
	        public static bool operator!=(sbyte v, AValue av) => av != v;
           
            public static bool operator==(AValue av, List< sbyte > v)
                    => Helpers.SequenceEquals(v, av?.Value);
	        public static bool operator!=(AValue av, List< sbyte > v) => !(av == v);
            public static bool operator==(List< sbyte > v, AValue av) => av == v;
	        public static bool operator!=(List< sbyte > v, AValue av) => av != v;

            public static bool operator<(sbyte oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
	        public static bool operator>(sbyte oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) < 0;
            public static bool operator<=(sbyte oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;
            public static bool operator>=(sbyte oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) <= 0;

            public static bool operator<(AValue aValue, sbyte oValue) => aValue is null || aValue.CompareTo(oValue) < 0;
	        public static bool operator>(AValue aValue, sbyte oValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
            public static bool operator<=(AValue aValue, sbyte oValue) => aValue is null || aValue.CompareTo(oValue) <= 0;
            public static bool operator>=(AValue aValue, sbyte oValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;

                        public sbyte Tosbyte() => (sbyte) this;
              
            public bool Equals(sbyte value)
            {
                return this.DigestRequired()
                            ? this.CompareDigest(value)
                            : this.CompareTo(value) == 0;
            }

            public bool Equals(sbyte v1, sbyte v2) => v1 == v2;
            public int GetHashCode(sbyte value) => value.GetHashCode();

            public int CompareTo(sbyte value)
            {
                                if(this.Value is null) return -1;
               
                try {
                    var tValue = this.Value;

                    if(tValue is sbyte cValue)
                        return cValue.CompareTo(value);

                                    if(tValue is string)
                         return Helpers.GetStableHashCode(tValue).CompareTo(Helpers.GetStableHashCode(value));
                    
                    tValue = ((IConvertible)tValue).ToType(typeof(decimal), null);

                    return ((decimal)tValue).CompareTo((decimal)((IConvertible)value).ToType(typeof(decimal), null));
                 
                                                  
                } catch  {}

                return Helpers.GetStableHashCode(this.Value).CompareTo(Helpers.GetStableHashCode(value));
                            }

        
            public static implicit operator DateTime (AValue v) => v.Convert< DateTime >();
            //public static implicit operator DateTime[] (AValue v) => v.Convert<DateTime[] >();            
            
            public static bool operator==(AValue av, DateTime v) => av?.Equals(v) ?? false;
	        public static bool operator!=(AValue av, DateTime v) => !(av == v);
            public static bool operator==(DateTime v, AValue av) => av == v;
	        public static bool operator!=(DateTime v, AValue av) => av != v;
           
            public static bool operator==(AValue av, List< DateTime > v)
                    => Helpers.SequenceEquals(v, av?.Value);
	        public static bool operator!=(AValue av, List< DateTime > v) => !(av == v);
            public static bool operator==(List< DateTime > v, AValue av) => av == v;
	        public static bool operator!=(List< DateTime > v, AValue av) => av != v;

            public static bool operator<(DateTime oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
	        public static bool operator>(DateTime oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) < 0;
            public static bool operator<=(DateTime oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;
            public static bool operator>=(DateTime oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) <= 0;

            public static bool operator<(AValue aValue, DateTime oValue) => aValue is null || aValue.CompareTo(oValue) < 0;
	        public static bool operator>(AValue aValue, DateTime oValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
            public static bool operator<=(AValue aValue, DateTime oValue) => aValue is null || aValue.CompareTo(oValue) <= 0;
            public static bool operator>=(AValue aValue, DateTime oValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;

                        public DateTime ToDateTime() => (DateTime) this;
              
            public bool Equals(DateTime value)
            {
                return this.DigestRequired()
                            ? this.CompareDigest(value)
                            : this.CompareTo(value) == 0;
            }

            public bool Equals(DateTime v1, DateTime v2) => v1 == v2;
            public int GetHashCode(DateTime value) => value.GetHashCode();

            public int CompareTo(DateTime value)
            {
                                if(this.Value is null) return -1;
               
                try {
                    var tValue = this.Value;

                    if(tValue is DateTime cValue)
                        return cValue.CompareTo(value);

                                    if(tValue is string || tValue.GetType().IsPrimitive)
                        tValue = this.Convert< DateTime >();
                     if(tValue is IComparable vValue)
                        return vValue.CompareTo(value);                     
                 
                                                  
                } catch  {}

                return Helpers.GetStableHashCode(this.Value).CompareTo(Helpers.GetStableHashCode(value));
                            }

        
            public static implicit operator DateTimeOffset (AValue v) => v.Convert< DateTimeOffset >();
            //public static implicit operator DateTimeOffset[] (AValue v) => v.Convert<DateTimeOffset[] >();            
            
            public static bool operator==(AValue av, DateTimeOffset v) => av?.Equals(v) ?? false;
	        public static bool operator!=(AValue av, DateTimeOffset v) => !(av == v);
            public static bool operator==(DateTimeOffset v, AValue av) => av == v;
	        public static bool operator!=(DateTimeOffset v, AValue av) => av != v;
           
            public static bool operator==(AValue av, List< DateTimeOffset > v)
                    => Helpers.SequenceEquals(v, av?.Value);
	        public static bool operator!=(AValue av, List< DateTimeOffset > v) => !(av == v);
            public static bool operator==(List< DateTimeOffset > v, AValue av) => av == v;
	        public static bool operator!=(List< DateTimeOffset > v, AValue av) => av != v;

            public static bool operator<(DateTimeOffset oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
	        public static bool operator>(DateTimeOffset oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) < 0;
            public static bool operator<=(DateTimeOffset oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;
            public static bool operator>=(DateTimeOffset oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) <= 0;

            public static bool operator<(AValue aValue, DateTimeOffset oValue) => aValue is null || aValue.CompareTo(oValue) < 0;
	        public static bool operator>(AValue aValue, DateTimeOffset oValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
            public static bool operator<=(AValue aValue, DateTimeOffset oValue) => aValue is null || aValue.CompareTo(oValue) <= 0;
            public static bool operator>=(AValue aValue, DateTimeOffset oValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;

                        public DateTimeOffset ToDateTimeOffset() => (DateTimeOffset) this;
              
            public bool Equals(DateTimeOffset value)
            {
                return this.DigestRequired()
                            ? this.CompareDigest(value)
                            : this.CompareTo(value) == 0;
            }

            public bool Equals(DateTimeOffset v1, DateTimeOffset v2) => v1 == v2;
            public int GetHashCode(DateTimeOffset value) => value.GetHashCode();

            public int CompareTo(DateTimeOffset value)
            {
                                if(this.Value is null) return -1;
               
                try {
                    var tValue = this.Value;

                    if(tValue is DateTimeOffset cValue)
                        return cValue.CompareTo(value);

                                    if(tValue is string || tValue.GetType().IsPrimitive)
                        tValue = this.Convert< DateTimeOffset >();
                     if(tValue is IComparable vValue)
                        return vValue.CompareTo(value);                     
                 
                                                  
                } catch  {}

                return Helpers.GetStableHashCode(this.Value).CompareTo(Helpers.GetStableHashCode(value));
                            }

        
            public static implicit operator TimeSpan (AValue v) => v.Convert< TimeSpan >();
            //public static implicit operator TimeSpan[] (AValue v) => v.Convert<TimeSpan[] >();            
            
            public static bool operator==(AValue av, TimeSpan v) => av?.Equals(v) ?? false;
	        public static bool operator!=(AValue av, TimeSpan v) => !(av == v);
            public static bool operator==(TimeSpan v, AValue av) => av == v;
	        public static bool operator!=(TimeSpan v, AValue av) => av != v;
           
            public static bool operator==(AValue av, List< TimeSpan > v)
                    => Helpers.SequenceEquals(v, av?.Value);
	        public static bool operator!=(AValue av, List< TimeSpan > v) => !(av == v);
            public static bool operator==(List< TimeSpan > v, AValue av) => av == v;
	        public static bool operator!=(List< TimeSpan > v, AValue av) => av != v;

            public static bool operator<(TimeSpan oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
	        public static bool operator>(TimeSpan oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) < 0;
            public static bool operator<=(TimeSpan oValue, AValue aValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;
            public static bool operator>=(TimeSpan oValue, AValue aValue) => aValue is null || aValue.CompareTo(oValue) <= 0;

            public static bool operator<(AValue aValue, TimeSpan oValue) => aValue is null || aValue.CompareTo(oValue) < 0;
	        public static bool operator>(AValue aValue, TimeSpan oValue) => !(aValue is null) && aValue.CompareTo(oValue) > 0;
            public static bool operator<=(AValue aValue, TimeSpan oValue) => aValue is null || aValue.CompareTo(oValue) <= 0;
            public static bool operator>=(AValue aValue, TimeSpan oValue) => !(aValue is null) && aValue.CompareTo(oValue) >= 0;

                        public TimeSpan ToTimeSpan() => (TimeSpan) this;
              
            public bool Equals(TimeSpan value)
            {
                return this.DigestRequired()
                            ? this.CompareDigest(value)
                            : this.CompareTo(value) == 0;
            }

            public bool Equals(TimeSpan v1, TimeSpan v2) => v1 == v2;
            public int GetHashCode(TimeSpan value) => value.GetHashCode();

            public int CompareTo(TimeSpan value)
            {
                                if(this.Value is null) return -1;
               
                try {
                    var tValue = this.Value;

                    if(tValue is TimeSpan cValue)
                        return cValue.CompareTo(value);

                                    if(tValue is string || tValue.GetType().IsPrimitive)
                        tValue = this.Convert< TimeSpan >();
                     if(tValue is IComparable vValue)
                        return vValue.CompareTo(value);                     
                 
                                                  
                } catch  {}

                return Helpers.GetStableHashCode(this.Value).CompareTo(Helpers.GetStableHashCode(value));
                            }

        
        
            public static implicit operator JObject (AValue key) => key is null ? null : (JObject) key.Convert< JObject >();
            //public static implicit operator JObject[] (AValue key) => (JObject[]) key.Convert<JObject[]>();
            
            public JObject ToJObject() => (JObject) this;

            public bool Equals(JObject value)
            {
                try {
                    return this.DigestRequired()
                            ? this.CompareDigest(value)
                            : ((JObject) this).Equals(value);
                } catch {}
                return false;
            }

            public bool Equals(JObject v1, JObject v2)
            {
                if(ReferenceEquals(v1,v2)) return true;
                if(v1 is null) return v2 is null;

                return v1.Equals(v2);
            }
            public int GetHashCode(JObject value) => value?.GetHashCode() ?? 0;

        
            public static implicit operator JArray (AValue key) => key is null ? null : (JArray) key.Convert< JArray >();
            //public static implicit operator JArray[] (AValue key) => (JArray[]) key.Convert<JArray[]>();
            
            public JArray ToJArray() => (JArray) this;

            public bool Equals(JArray value)
            {
                try {
                    return this.DigestRequired()
                            ? this.CompareDigest(value)
                            : ((JArray) this).Equals(value);
                } catch {}
                return false;
            }

            public bool Equals(JArray v1, JArray v2)
            {
                if(ReferenceEquals(v1,v2)) return true;
                if(v1 is null) return v2 is null;

                return v1.Equals(v2);
            }
            public int GetHashCode(JArray value) => value?.GetHashCode() ?? 0;

        
            public static implicit operator JValue (AValue key) => key is null ? null : (JValue) key.Convert< JValue >();
            //public static implicit operator JValue[] (AValue key) => (JValue[]) key.Convert<JValue[]>();
            
            public JValue ToJValue() => (JValue) this;

            public bool Equals(JValue value)
            {
                try {
                    return this.DigestRequired()
                            ? this.CompareDigest(value)
                            : ((JValue) this).Equals(value);
                } catch {}
                return false;
            }

            public bool Equals(JValue v1, JValue v2)
            {
                if(ReferenceEquals(v1,v2)) return true;
                if(v1 is null) return v2 is null;

                return v1.Equals(v2);
            }
            public int GetHashCode(JValue value) => value?.GetHashCode() ?? 0;

        
            public static implicit operator JToken (AValue key) => key is null ? null : (JToken) key.Convert< JToken >();
            //public static implicit operator JToken[] (AValue key) => (JToken[]) key.Convert<JToken[]>();
            
            public JToken ToJToken() => (JToken) this;

            public bool Equals(JToken value)
            {
                try {
                    return this.DigestRequired()
                            ? this.CompareDigest(value)
                            : ((JToken) this).Equals(value);
                } catch {}
                return false;
            }

            public bool Equals(JToken v1, JToken v2)
            {
                if(ReferenceEquals(v1,v2)) return true;
                if(v1 is null) return v2 is null;

                return v1.Equals(v2);
            }
            public int GetHashCode(JToken value) => value?.GetHashCode() ?? 0;

                
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

        /// <summary>
        /// Determines if <paramref name="value"/> is contained in <see cref="Value"/>.
        /// If <see cref="Value"/> is a collection, each element is compared. 
        /// If <see cref="Value"/> is a string and <paramref name="value"/> is a string, determines if param is contained in the Value, otherwise Equals is applied.
        /// If <see cref="Value"/> is an instance, the Equals method is applied.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="value"/></typeparam>
        /// <param name="value">The value used to determined if it exists</param>
        /// <returns>
        /// True if it is contained within a collection or is equal to an instance.
        /// </returns>
        /// <seealso cref="Contains{K, T}(K, T)"/>
        /// <seealso cref="ContainsKey{K}(K)"/>
        public bool Contains<T>(T value)
        {
            return this.Value switch
            {
                string sValue => !(value is null)
                                    && (value is string svalue
                                            && sValue.Contains(svalue))
                                        || this.Equals(value),
                JArray jArray => jArray.Contains(JToken.FromObject(value)),
                IEnumerable<T> iValue => iValue.Contains(value),
                IEnumerable<KeyValuePair<string, object>> iKeyValuePair
                    => iKeyValuePair.Any(kvp => Helpers.Equals(kvp.Value, value)),
                System.Collections.IDictionary cDict
                    => cDict.Values.Cast<object>().Any(i => Helpers.Equals(i, value)),
                System.Collections.IEnumerable iValue => iValue.Cast<object>().Any(i => Helpers.Equals(i, value)),                
                _ => this.Equals(value)
            };
        }

        /// <summary>
        /// Determines if <paramref name="key"/> and <paramref name="value"/> is contained in <see cref="Value"/>.
        /// If <see cref="Value"/> is a IDictionary, <seealso cref="JsonDocument"/>, or <see cref="IDictionary{TKey, TValue}"/>, each Key/Value pair is compared. 
        /// If <see cref="Value"/> is an instance, false is always returned.
        /// </summary>
        /// <typeparam name="K">The type of <paramref name="key"/></typeparam>
        /// <typeparam name="T">The type of <paramref name="value"/></typeparam>
        /// <param name="key">The key used to obtain the value</param>
        /// <param name="value">The value used to determined if it exists</param>
        /// <returns>
        /// True if it is contained within a collection or false otherwise.
        /// </returns>
        /// <seealso cref="Contains{T}(T)"/>
        /// <seealso cref="ContainsKey{K}(K)"/>
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
                _ => false
            };
        }

        /// <summary>
        /// Determines if <paramref name="key"/> is a key/property field within a Dictionary or JObject.
        /// </summary>
        /// <typeparam name="K">The type of <paramref name="key"/></typeparam>
        /// <param name="key">The value used to determined if the key exists</param>
        /// <returns>
        /// True is the key/property field exists.
        /// </returns>
        /// <seealso cref="Contains{K, T}(K, T)"/>
        /// <seealso cref="Contains{T}(T)"/>
        public bool ContainsKey<K>(K key)
        {
            return this.Value switch
            {
                JObject jObj
                    => key is string sKey
                            && jObj.ContainsKey(sKey),                
                System.Collections.IDictionary cDict
                    => cDict.Contains((object) key),
                _ => false
            };
        }
    }
}
