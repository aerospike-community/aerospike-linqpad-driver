 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

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
    /// <seealso cref="Aerospike.Client.LPDHelpers.ToAValue(Client.Key)"/>
    /// <seealso cref="Aerospike.Client.LPDHelpers.ToAValue(Client.Value)"/>
    /// <seealso cref="Aerospike.Client.LPDHelpers.ToAValue(object)"/>
    /// <seealso cref="AValueHelper.Cast{TResult}(IEnumerable{AValue})"/>
    /// <seealso cref="AValueHelper.OfType{TResult}(IEnumerable{AValue})"/>
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
           
                     , IEquatable< JsonDocument >
            , IEqualityComparer< JsonDocument >
                     , IEquatable< JObject >
            , IEqualityComparer< JObject >
           
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
        /// Converts <see cref="Value"/> into a >net native type
        /// </summary>
        /// <typeparam name="T">.Net Type to convert to</typeparam>
        /// <returns>
        /// The converted value
        /// </returns>
        public T Convert<T>() => (T) Helpers.CastToNativeType(this.FldName, typeof(T), this.BinName, this.Value);

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

        public override int GetHashCode() => this.Value?.GetHashCode() ?? 0;

        public override bool Equals(object obj)
        {
            if(ReferenceEquals(this,obj)) return true;
            if(obj is Aerospike.Client.Key key) return this.Equals(key);
            if(obj is Aerospike.Client.Value value) return this.Equals(value);
            if(obj is AValue pValue) return this.Equals(pValue);
            
            return Helpers.Equals(this.Value, obj);
        }

        public int CompareTo(object other)
        {
            if(ReferenceEquals(this,other)) return 0;
            if(other is null) return this.Value is null ? 0 : 1;
            if(this.Value is null) return -1;
            if(other is Aerospike.Client.Key key) return this.CompareTo(key);
            if(other is Aerospike.Client.Value value) return this.CompareTo(value);
            if(other is AValue pValue) return this.CompareTo(pValue);
            
            var cValue = Helpers.CastToNativeType(this.FldName, other.GetType(), this.BinName, this.Value);
            
            if(cValue is IComparable iCompareValue) return iCompareValue.CompareTo(other);
            
            return this.Value.GetHashCode().CompareTo(other.GetHashCode());
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
            if(other.userKey is null) return this.Value.GetHashCode().CompareTo(other.digest.GetHashCode());
            
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
            public static implicit operator string[] (AValue v) => v.Convert<string[] >();
            
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

            public bool Equals(string value) => (this.Value is string
                                                                || this.UnderlyingType.IsValueType
                                                                || !Helpers.IsSubclassOfInterface(typeof(IEnumerable<>), this.UnderlyingType))
                                                           && (this.DigestRequired()
                                                                ? this.CompareDigest(value)
                                                                : (string)this == value);

            public bool Equals(string v1, string v2) => v1 == v2;
            public int GetHashCode(string value) => value.GetHashCode();

            public int CompareTo(string value)
            {
                                if(this.Value is null) return value is null ? 0 : -1;
                if(value is null) return 1;
                                if(this.Value is IComparable< string > vValue)
                    return vValue.CompareTo(value);

                var cValue = (string) this;

                if(cValue is IComparable coValue)
                    return coValue.CompareTo(value);
                
                return this.Value.GetHashCode().CompareTo(value.GetHashCode());
            }

        
            public static implicit operator bool (AValue v) => v.Convert< bool >();
            public static implicit operator bool[] (AValue v) => v.Convert<bool[] >();
            
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

            public bool Equals(bool value) => (this.Value is string
                                                                || this.UnderlyingType.IsValueType
                                                                || !Helpers.IsSubclassOfInterface(typeof(IEnumerable<>), this.UnderlyingType))
                                                           && (this.DigestRequired()
                                                                ? this.CompareDigest(value)
                                                                : (bool)this == value);

            public bool Equals(bool v1, bool v2) => v1 == v2;
            public int GetHashCode(bool value) => value.GetHashCode();

            public int CompareTo(bool value)
            {
                                if(this.Value is null) return -1;
                                if(this.Value is IComparable< bool > vValue)
                    return vValue.CompareTo(value);

                var cValue = (bool) this;

                if(cValue is IComparable coValue)
                    return coValue.CompareTo(value);
                
                return this.Value.GetHashCode().CompareTo(value.GetHashCode());
            }

        
            public static implicit operator Enum (AValue v) => v.Convert< Enum >();
            public static implicit operator Enum[] (AValue v) => v.Convert<Enum[] >();
            
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

            public bool Equals(Enum value) => (this.Value is string
                                                                || this.UnderlyingType.IsValueType
                                                                || !Helpers.IsSubclassOfInterface(typeof(IEnumerable<>), this.UnderlyingType))
                                                           && (this.DigestRequired()
                                                                ? this.CompareDigest(value)
                                                                : (Enum)this == value);

            public bool Equals(Enum v1, Enum v2) => v1 == v2;
            public int GetHashCode(Enum value) => value.GetHashCode();

            public int CompareTo(Enum value)
            {
                                if(this.Value is null) return -1;
                                if(this.Value is IComparable< Enum > vValue)
                    return vValue.CompareTo(value);

                var cValue = (Enum) this;

                if(cValue is IComparable coValue)
                    return coValue.CompareTo(value);
                
                return this.Value.GetHashCode().CompareTo(value.GetHashCode());
            }

        
            public static implicit operator Guid (AValue v) => v.Convert< Guid >();
            public static implicit operator Guid[] (AValue v) => v.Convert<Guid[] >();
            
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

            public bool Equals(Guid value) => (this.Value is string
                                                                || this.UnderlyingType.IsValueType
                                                                || !Helpers.IsSubclassOfInterface(typeof(IEnumerable<>), this.UnderlyingType))
                                                           && (this.DigestRequired()
                                                                ? this.CompareDigest(value)
                                                                : (Guid)this == value);

            public bool Equals(Guid v1, Guid v2) => v1 == v2;
            public int GetHashCode(Guid value) => value.GetHashCode();

            public int CompareTo(Guid value)
            {
                                if(this.Value is null) return -1;
                                if(this.Value is IComparable< Guid > vValue)
                    return vValue.CompareTo(value);

                var cValue = (Guid) this;

                if(cValue is IComparable coValue)
                    return coValue.CompareTo(value);
                
                return this.Value.GetHashCode().CompareTo(value.GetHashCode());
            }

        
            public static implicit operator short (AValue v) => v.Convert< short >();
            public static implicit operator short[] (AValue v) => v.Convert<short[] >();
            
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

            public bool Equals(short value) => (this.Value is string
                                                                || this.UnderlyingType.IsValueType
                                                                || !Helpers.IsSubclassOfInterface(typeof(IEnumerable<>), this.UnderlyingType))
                                                           && (this.DigestRequired()
                                                                ? this.CompareDigest(value)
                                                                : (short)this == value);

            public bool Equals(short v1, short v2) => v1 == v2;
            public int GetHashCode(short value) => value.GetHashCode();

            public int CompareTo(short value)
            {
                                if(this.Value is null) return -1;
                                if(this.Value is IComparable< short > vValue)
                    return vValue.CompareTo(value);

                var cValue = (short) this;

                if(cValue is IComparable coValue)
                    return coValue.CompareTo(value);
                
                return this.Value.GetHashCode().CompareTo(value.GetHashCode());
            }

        
            public static implicit operator int (AValue v) => v.Convert< int >();
            public static implicit operator int[] (AValue v) => v.Convert<int[] >();
            
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

            public bool Equals(int value) => (this.Value is string
                                                                || this.UnderlyingType.IsValueType
                                                                || !Helpers.IsSubclassOfInterface(typeof(IEnumerable<>), this.UnderlyingType))
                                                           && (this.DigestRequired()
                                                                ? this.CompareDigest(value)
                                                                : (int)this == value);

            public bool Equals(int v1, int v2) => v1 == v2;
            public int GetHashCode(int value) => value.GetHashCode();

            public int CompareTo(int value)
            {
                                if(this.Value is null) return -1;
                                if(this.Value is IComparable< int > vValue)
                    return vValue.CompareTo(value);

                var cValue = (int) this;

                if(cValue is IComparable coValue)
                    return coValue.CompareTo(value);
                
                return this.Value.GetHashCode().CompareTo(value.GetHashCode());
            }

        
            public static implicit operator long (AValue v) => v.Convert< long >();
            public static implicit operator long[] (AValue v) => v.Convert<long[] >();
            
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

            public bool Equals(long value) => (this.Value is string
                                                                || this.UnderlyingType.IsValueType
                                                                || !Helpers.IsSubclassOfInterface(typeof(IEnumerable<>), this.UnderlyingType))
                                                           && (this.DigestRequired()
                                                                ? this.CompareDigest(value)
                                                                : (long)this == value);

            public bool Equals(long v1, long v2) => v1 == v2;
            public int GetHashCode(long value) => value.GetHashCode();

            public int CompareTo(long value)
            {
                                if(this.Value is null) return -1;
                                if(this.Value is IComparable< long > vValue)
                    return vValue.CompareTo(value);

                var cValue = (long) this;

                if(cValue is IComparable coValue)
                    return coValue.CompareTo(value);
                
                return this.Value.GetHashCode().CompareTo(value.GetHashCode());
            }

        
            public static implicit operator ushort (AValue v) => v.Convert< ushort >();
            public static implicit operator ushort[] (AValue v) => v.Convert<ushort[] >();
            
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

            public bool Equals(ushort value) => (this.Value is string
                                                                || this.UnderlyingType.IsValueType
                                                                || !Helpers.IsSubclassOfInterface(typeof(IEnumerable<>), this.UnderlyingType))
                                                           && (this.DigestRequired()
                                                                ? this.CompareDigest(value)
                                                                : (ushort)this == value);

            public bool Equals(ushort v1, ushort v2) => v1 == v2;
            public int GetHashCode(ushort value) => value.GetHashCode();

            public int CompareTo(ushort value)
            {
                                if(this.Value is null) return -1;
                                if(this.Value is IComparable< ushort > vValue)
                    return vValue.CompareTo(value);

                var cValue = (ushort) this;

                if(cValue is IComparable coValue)
                    return coValue.CompareTo(value);
                
                return this.Value.GetHashCode().CompareTo(value.GetHashCode());
            }

        
            public static implicit operator uint (AValue v) => v.Convert< uint >();
            public static implicit operator uint[] (AValue v) => v.Convert<uint[] >();
            
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

            public bool Equals(uint value) => (this.Value is string
                                                                || this.UnderlyingType.IsValueType
                                                                || !Helpers.IsSubclassOfInterface(typeof(IEnumerable<>), this.UnderlyingType))
                                                           && (this.DigestRequired()
                                                                ? this.CompareDigest(value)
                                                                : (uint)this == value);

            public bool Equals(uint v1, uint v2) => v1 == v2;
            public int GetHashCode(uint value) => value.GetHashCode();

            public int CompareTo(uint value)
            {
                                if(this.Value is null) return -1;
                                if(this.Value is IComparable< uint > vValue)
                    return vValue.CompareTo(value);

                var cValue = (uint) this;

                if(cValue is IComparable coValue)
                    return coValue.CompareTo(value);
                
                return this.Value.GetHashCode().CompareTo(value.GetHashCode());
            }

        
            public static implicit operator ulong (AValue v) => v.Convert< ulong >();
            public static implicit operator ulong[] (AValue v) => v.Convert<ulong[] >();
            
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

            public bool Equals(ulong value) => (this.Value is string
                                                                || this.UnderlyingType.IsValueType
                                                                || !Helpers.IsSubclassOfInterface(typeof(IEnumerable<>), this.UnderlyingType))
                                                           && (this.DigestRequired()
                                                                ? this.CompareDigest(value)
                                                                : (ulong)this == value);

            public bool Equals(ulong v1, ulong v2) => v1 == v2;
            public int GetHashCode(ulong value) => value.GetHashCode();

            public int CompareTo(ulong value)
            {
                                if(this.Value is null) return -1;
                                if(this.Value is IComparable< ulong > vValue)
                    return vValue.CompareTo(value);

                var cValue = (ulong) this;

                if(cValue is IComparable coValue)
                    return coValue.CompareTo(value);
                
                return this.Value.GetHashCode().CompareTo(value.GetHashCode());
            }

        
            public static implicit operator decimal (AValue v) => v.Convert< decimal >();
            public static implicit operator decimal[] (AValue v) => v.Convert<decimal[] >();
            
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

            public bool Equals(decimal value) => (this.Value is string
                                                                || this.UnderlyingType.IsValueType
                                                                || !Helpers.IsSubclassOfInterface(typeof(IEnumerable<>), this.UnderlyingType))
                                                           && (this.DigestRequired()
                                                                ? this.CompareDigest(value)
                                                                : (decimal)this == value);

            public bool Equals(decimal v1, decimal v2) => v1 == v2;
            public int GetHashCode(decimal value) => value.GetHashCode();

            public int CompareTo(decimal value)
            {
                                if(this.Value is null) return -1;
                                if(this.Value is IComparable< decimal > vValue)
                    return vValue.CompareTo(value);

                var cValue = (decimal) this;

                if(cValue is IComparable coValue)
                    return coValue.CompareTo(value);
                
                return this.Value.GetHashCode().CompareTo(value.GetHashCode());
            }

        
            public static implicit operator float (AValue v) => v.Convert< float >();
            public static implicit operator float[] (AValue v) => v.Convert<float[] >();
            
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

            public bool Equals(float value) => (this.Value is string
                                                                || this.UnderlyingType.IsValueType
                                                                || !Helpers.IsSubclassOfInterface(typeof(IEnumerable<>), this.UnderlyingType))
                                                           && (this.DigestRequired()
                                                                ? this.CompareDigest(value)
                                                                : (float)this == value);

            public bool Equals(float v1, float v2) => v1 == v2;
            public int GetHashCode(float value) => value.GetHashCode();

            public int CompareTo(float value)
            {
                                if(this.Value is null) return -1;
                                if(this.Value is IComparable< float > vValue)
                    return vValue.CompareTo(value);

                var cValue = (float) this;

                if(cValue is IComparable coValue)
                    return coValue.CompareTo(value);
                
                return this.Value.GetHashCode().CompareTo(value.GetHashCode());
            }

        
            public static implicit operator double (AValue v) => v.Convert< double >();
            public static implicit operator double[] (AValue v) => v.Convert<double[] >();
            
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

            public bool Equals(double value) => (this.Value is string
                                                                || this.UnderlyingType.IsValueType
                                                                || !Helpers.IsSubclassOfInterface(typeof(IEnumerable<>), this.UnderlyingType))
                                                           && (this.DigestRequired()
                                                                ? this.CompareDigest(value)
                                                                : (double)this == value);

            public bool Equals(double v1, double v2) => v1 == v2;
            public int GetHashCode(double value) => value.GetHashCode();

            public int CompareTo(double value)
            {
                                if(this.Value is null) return -1;
                                if(this.Value is IComparable< double > vValue)
                    return vValue.CompareTo(value);

                var cValue = (double) this;

                if(cValue is IComparable coValue)
                    return coValue.CompareTo(value);
                
                return this.Value.GetHashCode().CompareTo(value.GetHashCode());
            }

        
            public static implicit operator byte (AValue v) => v.Convert< byte >();
            public static implicit operator byte[] (AValue v) => v.Convert<byte[] >();
            
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

            public bool Equals(byte value) => (this.Value is string
                                                                || this.UnderlyingType.IsValueType
                                                                || !Helpers.IsSubclassOfInterface(typeof(IEnumerable<>), this.UnderlyingType))
                                                           && (this.DigestRequired()
                                                                ? this.CompareDigest(value)
                                                                : (byte)this == value);

            public bool Equals(byte v1, byte v2) => v1 == v2;
            public int GetHashCode(byte value) => value.GetHashCode();

            public int CompareTo(byte value)
            {
                                if(this.Value is null) return -1;
                                if(this.Value is IComparable< byte > vValue)
                    return vValue.CompareTo(value);

                var cValue = (byte) this;

                if(cValue is IComparable coValue)
                    return coValue.CompareTo(value);
                
                return this.Value.GetHashCode().CompareTo(value.GetHashCode());
            }

        
            public static implicit operator sbyte (AValue v) => v.Convert< sbyte >();
            public static implicit operator sbyte[] (AValue v) => v.Convert<sbyte[] >();
            
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

            public bool Equals(sbyte value) => (this.Value is string
                                                                || this.UnderlyingType.IsValueType
                                                                || !Helpers.IsSubclassOfInterface(typeof(IEnumerable<>), this.UnderlyingType))
                                                           && (this.DigestRequired()
                                                                ? this.CompareDigest(value)
                                                                : (sbyte)this == value);

            public bool Equals(sbyte v1, sbyte v2) => v1 == v2;
            public int GetHashCode(sbyte value) => value.GetHashCode();

            public int CompareTo(sbyte value)
            {
                                if(this.Value is null) return -1;
                                if(this.Value is IComparable< sbyte > vValue)
                    return vValue.CompareTo(value);

                var cValue = (sbyte) this;

                if(cValue is IComparable coValue)
                    return coValue.CompareTo(value);
                
                return this.Value.GetHashCode().CompareTo(value.GetHashCode());
            }

        
            public static implicit operator DateTime (AValue v) => v.Convert< DateTime >();
            public static implicit operator DateTime[] (AValue v) => v.Convert<DateTime[] >();
            
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

            public bool Equals(DateTime value) => (this.Value is string
                                                                || this.UnderlyingType.IsValueType
                                                                || !Helpers.IsSubclassOfInterface(typeof(IEnumerable<>), this.UnderlyingType))
                                                           && (this.DigestRequired()
                                                                ? this.CompareDigest(value)
                                                                : (DateTime)this == value);

            public bool Equals(DateTime v1, DateTime v2) => v1 == v2;
            public int GetHashCode(DateTime value) => value.GetHashCode();

            public int CompareTo(DateTime value)
            {
                                if(this.Value is null) return -1;
                                if(this.Value is IComparable< DateTime > vValue)
                    return vValue.CompareTo(value);

                var cValue = (DateTime) this;

                if(cValue is IComparable coValue)
                    return coValue.CompareTo(value);
                
                return this.Value.GetHashCode().CompareTo(value.GetHashCode());
            }

        
            public static implicit operator DateTimeOffset (AValue v) => v.Convert< DateTimeOffset >();
            public static implicit operator DateTimeOffset[] (AValue v) => v.Convert<DateTimeOffset[] >();
            
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

            public bool Equals(DateTimeOffset value) => (this.Value is string
                                                                || this.UnderlyingType.IsValueType
                                                                || !Helpers.IsSubclassOfInterface(typeof(IEnumerable<>), this.UnderlyingType))
                                                           && (this.DigestRequired()
                                                                ? this.CompareDigest(value)
                                                                : (DateTimeOffset)this == value);

            public bool Equals(DateTimeOffset v1, DateTimeOffset v2) => v1 == v2;
            public int GetHashCode(DateTimeOffset value) => value.GetHashCode();

            public int CompareTo(DateTimeOffset value)
            {
                                if(this.Value is null) return -1;
                                if(this.Value is IComparable< DateTimeOffset > vValue)
                    return vValue.CompareTo(value);

                var cValue = (DateTimeOffset) this;

                if(cValue is IComparable coValue)
                    return coValue.CompareTo(value);
                
                return this.Value.GetHashCode().CompareTo(value.GetHashCode());
            }

        
            public static implicit operator TimeSpan (AValue v) => v.Convert< TimeSpan >();
            public static implicit operator TimeSpan[] (AValue v) => v.Convert<TimeSpan[] >();
            
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

            public bool Equals(TimeSpan value) => (this.Value is string
                                                                || this.UnderlyingType.IsValueType
                                                                || !Helpers.IsSubclassOfInterface(typeof(IEnumerable<>), this.UnderlyingType))
                                                           && (this.DigestRequired()
                                                                ? this.CompareDigest(value)
                                                                : (TimeSpan)this == value);

            public bool Equals(TimeSpan v1, TimeSpan v2) => v1 == v2;
            public int GetHashCode(TimeSpan value) => value.GetHashCode();

            public int CompareTo(TimeSpan value)
            {
                                if(this.Value is null) return -1;
                                if(this.Value is IComparable< TimeSpan > vValue)
                    return vValue.CompareTo(value);

                var cValue = (TimeSpan) this;

                if(cValue is IComparable coValue)
                    return coValue.CompareTo(value);
                
                return this.Value.GetHashCode().CompareTo(value.GetHashCode());
            }

        
        
            public static implicit operator JsonDocument (AValue key) => key is null ? null : (JsonDocument) key.Convert< JsonDocument >();
            public static implicit operator JsonDocument[] (AValue key) => (JsonDocument[]) key.Convert<JsonDocument[]>();
            
            public bool Equals(JsonDocument value) => this.DigestRequired()
                                                            ? this.CompareDigest(value)
                                                            : ((JsonDocument) this).Equals(value);
            public bool Equals(JsonDocument v1, JsonDocument v2)
            {
                if(ReferenceEquals(v1,v2)) return true;
                if(v1 is null) return v2 is null;

                return v1.Equals(v2);
            }
            public int GetHashCode(JsonDocument value) => value?.GetHashCode() ?? 0;

        
            public static implicit operator JObject (AValue key) => key is null ? null : (JObject) key.Convert< JObject >();
            public static implicit operator JObject[] (AValue key) => (JObject[]) key.Convert<JObject[]>();
            
            public bool Equals(JObject value) => this.DigestRequired()
                                                            ? this.CompareDigest(value)
                                                            : ((JObject) this).Equals(value);
            public bool Equals(JObject v1, JObject v2)
            {
                if(ReferenceEquals(v1,v2)) return true;
                if(v1 is null) return v2 is null;

                return v1.Equals(v2);
            }
            public int GetHashCode(JObject value) => value?.GetHashCode() ?? 0;

                
        /// <summary>
        /// Determines if <paramref name="value"/> is contained in <see cref="Value"/>.
        /// If <see cref="Value"/> is a collection, each element is compared. 
        /// If <see cref="Value"/> is an instance, the Equals method is applied.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="value"/></typeparam>
        /// <param name="value">The value used to determined if it exists</param>
        /// <returns>
        /// True if it is contained within a collection or is equal to an instance.
        /// </returns>
        public bool Contains<T>(T value)
        {
            return this.Value switch
            {
                IEnumerable<T> iValue => iValue.Contains(value),
                IEnumerable<object> oValue => oValue.Any(i => Helpers.Equals(i, value)),
                IEnumerable<KeyValuePair<string, object>> iKeyValuePair
                    => iKeyValuePair.Any(kvp => Helpers.Equals(kvp.Value, value)),
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
        public bool Contains<K, T>(K key, T value)
        {
            return this.Value switch
            {
                JObject jObj
                    => key is string sKey
                            && jObj.ContainsKey(sKey)
                            && Helpers.Equals(jObj[sKey], value),
                IDictionary<string, object> sDict
                    => key is string sKey
                            && sDict.ContainsKey(sKey)
                            && Helpers.Equals(sDict[sKey], value),
                IDictionary<object, object> oDict
                    => oDict.ContainsKey(key)
                            && Helpers.Equals(oDict[key], value),
                _ => false
            };
        }
    }
}
