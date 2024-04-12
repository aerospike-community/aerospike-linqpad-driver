
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
    
    partial class AValue : IConvertible,
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
    
    #region To type Methods
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
            catch(FormatException ex)
            {
                if(throwOnFormatException)
                {
                    if (Client.Log.DebugEnabled())
                    {
                        Client.Log.Error($"AValue.ToString Exception {ex.GetType().Name} ({ex.Message})");
                        DynamicDriver.WriteToLog(ex, "AValue.ToString");
                    }
                    throw;
                }
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
            catch(FormatException ex)
            {
                if(throwOnFormatException)
                {
                    if (Client.Log.DebugEnabled())
                    {
                        Client.Log.Error($"AValue.ToString Exception {ex.GetType().Name} ({ex.Message})");
                        DynamicDriver.WriteToLog(ex, "AValue.ToString");
                    }
                    throw;
                }
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
            catch(FormatException ex)
            {
                if(throwOnFormatException)
                {
                    if (Client.Log.DebugEnabled())
                    {
                        Client.Log.Error($"AValue.ToString Exception {ex.GetType().Name} ({ex.Message})");
                        DynamicDriver.WriteToLog(ex, "AValue.ToString");
                    }
                    throw;
                }
            }
            return this.ToString();
        }
#pragma warning restore CS0618 // Type or member is obsolete
        
    #endregion
     
    #region native DataTypes operator/Methods
        
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

            public static bool operator<(string oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
	        public static bool operator>(string oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
            public static bool operator<=(string oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);
            public static bool operator>=(string oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);

            public static bool operator<(AValue aValue, string oValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
	        public static bool operator>(AValue aValue, string oValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
            public static bool operator<=(AValue aValue, string oValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);
            public static bool operator>=(AValue aValue, string oValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);

             
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

            public static bool operator<(bool oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
	        public static bool operator>(bool oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
            public static bool operator<=(bool oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);
            public static bool operator>=(bool oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);

            public static bool operator<(AValue aValue, bool oValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
	        public static bool operator>(AValue aValue, bool oValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
            public static bool operator<=(AValue aValue, bool oValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);
            public static bool operator>=(AValue aValue, bool oValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);

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

            public static bool operator<(Enum oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
	        public static bool operator>(Enum oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
            public static bool operator<=(Enum oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);
            public static bool operator>=(Enum oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);

            public static bool operator<(AValue aValue, Enum oValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
	        public static bool operator>(AValue aValue, Enum oValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
            public static bool operator<=(AValue aValue, Enum oValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);
            public static bool operator>=(AValue aValue, Enum oValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);

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

            public static bool operator<(Guid oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
	        public static bool operator>(Guid oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
            public static bool operator<=(Guid oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);
            public static bool operator>=(Guid oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);

            public static bool operator<(AValue aValue, Guid oValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
	        public static bool operator>(AValue aValue, Guid oValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
            public static bool operator<=(AValue aValue, Guid oValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);
            public static bool operator>=(AValue aValue, Guid oValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);

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

            public static bool operator<(short oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
	        public static bool operator>(short oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
            public static bool operator<=(short oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);
            public static bool operator>=(short oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);

            public static bool operator<(AValue aValue, short oValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
	        public static bool operator>(AValue aValue, short oValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
            public static bool operator<=(AValue aValue, short oValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);
            public static bool operator>=(AValue aValue, short oValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);

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

            public static bool operator<(int oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
	        public static bool operator>(int oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
            public static bool operator<=(int oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);
            public static bool operator>=(int oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);

            public static bool operator<(AValue aValue, int oValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
	        public static bool operator>(AValue aValue, int oValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
            public static bool operator<=(AValue aValue, int oValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);
            public static bool operator>=(AValue aValue, int oValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);

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

            public static bool operator<(long oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
	        public static bool operator>(long oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
            public static bool operator<=(long oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);
            public static bool operator>=(long oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);

            public static bool operator<(AValue aValue, long oValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
	        public static bool operator>(AValue aValue, long oValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
            public static bool operator<=(AValue aValue, long oValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);
            public static bool operator>=(AValue aValue, long oValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);

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

            public static bool operator<(ushort oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
	        public static bool operator>(ushort oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
            public static bool operator<=(ushort oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);
            public static bool operator>=(ushort oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);

            public static bool operator<(AValue aValue, ushort oValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
	        public static bool operator>(AValue aValue, ushort oValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
            public static bool operator<=(AValue aValue, ushort oValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);
            public static bool operator>=(AValue aValue, ushort oValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);

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

            public static bool operator<(uint oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
	        public static bool operator>(uint oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
            public static bool operator<=(uint oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);
            public static bool operator>=(uint oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);

            public static bool operator<(AValue aValue, uint oValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
	        public static bool operator>(AValue aValue, uint oValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
            public static bool operator<=(AValue aValue, uint oValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);
            public static bool operator>=(AValue aValue, uint oValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);

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

            public static bool operator<(ulong oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
	        public static bool operator>(ulong oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
            public static bool operator<=(ulong oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);
            public static bool operator>=(ulong oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);

            public static bool operator<(AValue aValue, ulong oValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
	        public static bool operator>(AValue aValue, ulong oValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
            public static bool operator<=(AValue aValue, ulong oValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);
            public static bool operator>=(AValue aValue, ulong oValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);

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

            public static bool operator<(decimal oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
	        public static bool operator>(decimal oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
            public static bool operator<=(decimal oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);
            public static bool operator>=(decimal oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);

            public static bool operator<(AValue aValue, decimal oValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
	        public static bool operator>(AValue aValue, decimal oValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
            public static bool operator<=(AValue aValue, decimal oValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);
            public static bool operator>=(AValue aValue, decimal oValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);

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

            public static bool operator<(float oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
	        public static bool operator>(float oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
            public static bool operator<=(float oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);
            public static bool operator>=(float oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);

            public static bool operator<(AValue aValue, float oValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
	        public static bool operator>(AValue aValue, float oValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
            public static bool operator<=(AValue aValue, float oValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);
            public static bool operator>=(AValue aValue, float oValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);

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

            public static bool operator<(double oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
	        public static bool operator>(double oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
            public static bool operator<=(double oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);
            public static bool operator>=(double oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);

            public static bool operator<(AValue aValue, double oValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
	        public static bool operator>(AValue aValue, double oValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
            public static bool operator<=(AValue aValue, double oValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);
            public static bool operator>=(AValue aValue, double oValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);

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

            public static bool operator<(byte oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
	        public static bool operator>(byte oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
            public static bool operator<=(byte oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);
            public static bool operator>=(byte oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);

            public static bool operator<(AValue aValue, byte oValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
	        public static bool operator>(AValue aValue, byte oValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
            public static bool operator<=(AValue aValue, byte oValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);
            public static bool operator>=(AValue aValue, byte oValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);

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

            public static bool operator<(sbyte oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
	        public static bool operator>(sbyte oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
            public static bool operator<=(sbyte oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);
            public static bool operator>=(sbyte oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);

            public static bool operator<(AValue aValue, sbyte oValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
	        public static bool operator>(AValue aValue, sbyte oValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
            public static bool operator<=(AValue aValue, sbyte oValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);
            public static bool operator>=(AValue aValue, sbyte oValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);

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

            public static bool operator<(DateTime oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
	        public static bool operator>(DateTime oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
            public static bool operator<=(DateTime oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);
            public static bool operator>=(DateTime oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);

            public static bool operator<(AValue aValue, DateTime oValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
	        public static bool operator>(AValue aValue, DateTime oValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
            public static bool operator<=(AValue aValue, DateTime oValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);
            public static bool operator>=(AValue aValue, DateTime oValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);

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

            public static bool operator<(DateTimeOffset oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
	        public static bool operator>(DateTimeOffset oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
            public static bool operator<=(DateTimeOffset oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);
            public static bool operator>=(DateTimeOffset oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);

            public static bool operator<(AValue aValue, DateTimeOffset oValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
	        public static bool operator>(AValue aValue, DateTimeOffset oValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
            public static bool operator<=(AValue aValue, DateTimeOffset oValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);
            public static bool operator>=(AValue aValue, DateTimeOffset oValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);

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

            public static bool operator<(TimeSpan oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
	        public static bool operator>(TimeSpan oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
            public static bool operator<=(TimeSpan oValue, AValue aValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);
            public static bool operator>=(TimeSpan oValue, AValue aValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);

            public static bool operator<(AValue aValue, TimeSpan oValue) => aValue is null || (aValue.CompareTo(oValue) < 0);
	        public static bool operator>(AValue aValue, TimeSpan oValue) => aValue is not null && (aValue.CompareTo(oValue) > 0);
            public static bool operator<=(AValue aValue, TimeSpan oValue) => aValue is null || (aValue.CompareTo(oValue) <= 0);
            public static bool operator>=(AValue aValue, TimeSpan oValue) => aValue is not null && (aValue.CompareTo(oValue) >= 0);

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

            #endregion
    #region Class Data Types operator/methods
        
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

            #endregion
    
    }
}
