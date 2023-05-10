using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Dynamic;
using Aerospike.Client;
using LPU = LINQPad.Util;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Input;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
    /// <summary>
    /// An extension to the Aerospike <see cref="Aerospike.Client.Record"/>. 
    /// </summary>
    [DebuggerDisplay("{ToString()}")]
    public class ARecord : //IEnumerable<BinName>,
                                IEqualityComparer<ARecord>,
                                IEquatable<ARecord>,
                                IEquatable<Client.Key>,
                                IEquatable<Value>,
                                IEquatable<string>,
                                IEquatable<AValue>
    {
        /// <summary>
        /// The Aerospike Epoch
        /// </summary>
        public static readonly DateTimeOffset Epoch = new DateTime(2010, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        #region Constructors
        public ARecord([NotNull] ANamespaceAccess setAccess,
                            [NotNull] Client.Key key,
                            [NotNull] Record record,
                            string[] binNames,
                            DumpTypes dumpType = DumpTypes.Record,
                            int setBinsHashCode = 0)
        {            
            this.SetAccess = setAccess;

            this.Aerospike = new AerospikeAPI(key,
                                                record,
                                                binNames == null
                                                    ? Array.Empty<string>()
                                                    : (binNames.Length == 0 ? setAccess.BinNames : binNames));

            this.DumpType = dumpType;
            this.SetBinsHashCode = setBinsHashCode;

            var recordBins = record.bins?.Keys.ToArray();
            this.BinsHashCode = Helpers.GetHashCode(recordBins);
            
            if (this.BinsHashCode != this.SetBinsHashCode && this.DumpType == DumpTypes.Record)
            {                
                if (recordBins.Length <= this.Aerospike.BinNames?.Length
                        && recordBins.All(n => this.Aerospike.BinNames.Contains(n)))
                { }
                else
                {
                    this.DumpType = DumpTypes.Dynamic;
                    this.HasDifferentSchema= true;
                }
            }
        }

        /// <summary>
        /// Creates an AS AerospikeRecord that can be used to add/update an Aerospike DB record.
        /// </summary>
        /// <param name="ns">Namespace Name</param>
        /// <param name="set">Set Name</param>
        /// <param name="keyValue">The primary AerospikeKey</param>
        /// <param name="binValues">A dictionary where the key is the bin name and the value is the bin&apos;s value</param>
        /// <param name="setAccess">The set instance that will be associated to this record.</param>
        /// <param name="expirationDate">
        /// Expiration Date of the record.
        /// Note: If <paramref name="expiration"/> is not null that value is used.
        /// </param>
        /// <param name="expiration">
        /// TTL Epoch in seconds from Jan 01 2010 00:00:00 GMT
        /// Note: if this value is null, <paramref name="expirationDate"/> is used.
        /// <see cref="AerospikeAPI.Expiration"/>
        /// </param>
        /// <param name="generation">record generation</param>
        /// <param name="dumpType"><see cref="DumpTypes"/></param>
        /// <param name="setBinsHashCode">An internal HashCode defined by the associated Set used to determine if this record has dynamic bins</param>
        public ARecord([NotNull] string ns,
                            [NotNull] string set,
                            [NotNull] dynamic keyValue,
                            [NotNull] IDictionary<string, object> binValues,
                            ANamespaceAccess setAccess = null,
                            int? expiration = null,
                            DateTimeOffset? expirationDate = null,
                            int? generation = null,
                            DumpTypes dumpType = DumpTypes.Record,
                            int setBinsHashCode = 0)
        {
            this.SetAccess = setAccess;
            this.Aerospike = new AerospikeAPI(ns,
                                                set,
                                                keyValue,
                                                binValues,
                                                expiration,
                                                expirationDate,
                                                generation);
           
            this.DumpType = dumpType;
            this.SetBinsHashCode = setBinsHashCode;

            this.BinsHashCode = Helpers.GetHashCode(binValues.Keys.ToArray());
            
            if (this.BinsHashCode != this.SetBinsHashCode && this.DumpType == DumpTypes.Record)
            {
                if (binValues.Count <= this.Aerospike.BinNames?.Length
                        && binValues.All(n => this.Aerospike.BinNames.Contains(n.Key)))
                { }
                else
                {
                    this.DumpType = DumpTypes.Dynamic;
                    this.HasDifferentSchema = true;
                }
            }
        }

        public ARecord([NotNull] ARecord cloneRecord)
        {
            this.Aerospike = new AerospikeAPI(cloneRecord.Aerospike);
            this.DumpType = cloneRecord.DumpType;
            this.SetBinsHashCode = cloneRecord.SetBinsHashCode;
            this.BinsHashCode = cloneRecord.BinsHashCode;
            this.HasDifferentSchema = cloneRecord.HasDifferentSchema;
            this.RecordException= cloneRecord.RecordException;
        }

        #endregion

        #region Settings, Record State, etc.

        /// <summary>
        /// How the record will be used by the <see cref="LINQPad.Extensions.Dump{T}(T)"/>.
        /// </summary>
        public enum DumpTypes
        {
            /// <summary>
            /// Displays the record based on the detected bins of the set. 
            /// If this record&apos;s bin are different from the set, Dump BinType <see cref="Dynamic"/> is used.
            /// </summary>
            Record = 0,
            /// <summary>
            /// Similar to <see cref="Record"/> except all bins associated to this record are displayed regardless of the set defined bins.
            /// </summary>
            Dynamic = 1,
            /// <summary>
            /// Displays all properties/fields of this instance like the LinqPad <see cref="LINQPad.Extensions.Dump{T}(T)"/> method.
            /// </summary>
            Detail = 2,
            LinqPad = Detail,
            Normal = Detail
        }

        public static string DefaultASPIKeyName = "PK";
        
        /// <summary>
        /// How this record is displayed when using the LinqPad <see cref="LINQPad.Extensions.Dump{T}(T)"/> method.
        /// The default is <see cref="DumpTypes.Record"/>
        /// <see cref="DumpTypes"/>
        /// </summary>
        /// <seealso cref="DumpTypes"/>
        /// <see cref="SetDumpType(DumpTypes)"/>
        public DumpTypes DumpType { get; set; } = DumpTypes.Record;

        /// <summary>
        /// Changes how the instance&apos;s properties are displayed. See <see cref="DumpType"/>
        /// </summary>       
        /// <param name="newType">The new BinType, see <see cref="DumpTypes"/></param>
        /// <returns>Returns this instance</returns>
        /// <seealso cref="DumpType"/>
        /// <seealso cref="DumpTypes"/>
        public ARecord SetDumpType(DumpTypes newType)
        {
            this.DumpType = newType;
            return this;
        }

        /// <summary>
        /// If not null, this record encountered an exception during procession. 
        /// </summary>
        public Exception RecordException { get; private set; }

        /// <summary>
        /// Places record in exception state.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns>Returns the Record.</returns>
        public ARecord SetException(Exception exception)
        {
            this.RecordException = exception;
            return this;
        }

        /// <summary>
        /// Returns true to indicated that this record&apos;s schema does not match when the set was scanned.
        /// </summary>
        public bool HasDifferentSchema
        {
            get;
        }
        
        /// <summary>
        /// This is the Set's Bin Names Hash Code
        /// </summary>
        private int SetBinsHashCode { get; }
        /// <summary>
        /// This is the Record&apos;s Bin Name Hash Code
        /// </summary>
        private int BinsHashCode { get; }
        
        /// <summary>
        /// The Set Access instance that this record is associated with.
        /// </summary>
        public ANamespaceAccess SetAccess { get; set; }

        #endregion

        #region Aerospike Client Related API Items

        public sealed class AerospikeAPI
        {
            internal AerospikeAPI(Client.Key key, Client.Record record, string[] binNames)
            {
                this.BinNames = binNames;
                this.Record = record;
                this.Key = key;
            }

            internal AerospikeAPI([NotNull] string ns,
                                    [NotNull] string set,
                                    [NotNull] dynamic keyValue,
                                    [NotNull] IDictionary<string, object> binValues,
                                    int? expiration = null,
                                    DateTimeOffset? expirationDate = null,
                                    int? generation = null)
            {
                this.Key = Helpers.DetermineAerospikeKey(keyValue, ns, set);              
                this.Record = new Record((Dictionary<string, object>)binValues,
                                                    generation ?? 0,
                                                    expiration ?? (expirationDate.HasValue
                                                                    ? (int)(expirationDate.Value - Epoch).TotalSeconds
                                                                    : 0));
                this.BinNames = binValues.Keys.ToArray();
                this._bins = this.Record?.bins?.Select(b => new Bin(b.Key, b.Value)).ToArray();
            }

            internal AerospikeAPI(AerospikeAPI cloneRecord)
            {
                this.Key = new Client.Key(cloneRecord.Key.ns,
                                                cloneRecord.Key.setName,
                                                Value.Get(cloneRecord.Key.userKey));
                this.Record = new Record(new Dictionary<string, object>(cloneRecord.Record.bins),
                                                        cloneRecord.Record.generation,
                                                        cloneRecord.Record.expiration);
                this.BinNames = cloneRecord.BinNames;
            }

            private Bin[] _bins;

            /// <summary>
            /// Return all bin names that can be present in the record.
            /// This can be all bins associated with the namespace.
            /// </summary>
            public string[] BinNames { get; }

            /// <summary>
            /// Returns the <seealso cref="Bin"/> actually defined for this record.
            /// </summary>
            public Bin[] Bins
            {
                get
                {
                    if (this._bins == null)
                        return this._bins = this.Record?.bins?.Select(b => new Bin(b.Key, b.Value)).ToArray() ?? Array.Empty<Bin>();
                    return this._bins;
                }
                internal set { this._bins = value; }
            }

            /// <summary>
            /// Returns the Aerospike <see cref="Aerospike.Client.Record"/> associated to this object.
            /// </summary>
            public Aerospike.Client.Record Record { get; }

            /// <summary>
            /// The primary key&apos;s value, if <see cref="Aerospike.Client.Policy.sendKey"/> value is true.
            /// To use a primary key where use <see cref="Equals(string)"/> method.
            /// </summary>
            /// <seealso cref="AerospikeAPI.Key"/>
            /// <seealso cref="AerospikeAPI.KeyValue"/>
            /// <seealso cref="AerospikeAPI.Digest"/>
            /// <seealso cref="Equals(string)"/>
            /// <seealso cref="SetRecords.Get(dynamic, string[])"/>
            public object PrimaryKey { get => this.KeyValue?.Object; }

            /// <summary>
            /// The Records Aerospike <see cref="Aerospike.Client.Key"/>
            /// </summary>
            /// <seealso cref="KeyValue"/>
            /// <seealso cref="Equals(Client.Key)"/>
            /// <seealso cref="Equals(string)"/>
            public Client.Key Key { get; }

            /// <summary>
            /// Returns the key value (<see cref="Aerospike.Client.Key"/> or <see cref="Aerospike.Client.Value"/>) of the record. If null, the key was not saved but only the <see cref="Digest"/>
            /// </summary>
            /// <seealso cref="Digest"/>
            /// <seealso cref="Equals(string)"/>
            /// <seealso cref="Equals(Client.Key)"/>
            /// <seealso cref="Key"/>
            public Value KeyValue { get => this.Key.userKey; }

            /// <summary>
            /// This is the Namespace associated with this record.
            /// </summary>
            /// <seealso cref="SetName"/>
            public string Namespace { get => this.Key.ns; }

            /// <summary>
            /// returns the name of the Set associated with this record
            /// </summary>
            /// <seealso cref="SetName"/>
            public string SetName { get => this.Key.setName; }

            /// <summary>
            /// Returns the AerospikeKey&apos;s Digest
            /// </summary>
            /// <seealso cref="KeyValue"/>
            /// <seealso cref="Equals(Client.Key)"/>
            /// <seealso cref="Equals(string)"/>
            public byte[] Digest { get => this.Key.digest; }

            /// <summary>
            /// Returns the Record&apos;s Aerospike Generation
            /// </summary>
            public int Generation { get => this.Record.generation; }

            /// <summary>
            /// Date record will expire, in seconds from Jan 01 2010 00:00:00 GMT
            /// </summary>
            /// <seealso cref="TTL"/>
            public int Expiration { get => this.Record.expiration; }

            public static TimeSpan? CalcTTLTimeSpan(int? ttl)
            {
                if (!ttl.HasValue) return null;

                if (ttl == -2 || ttl == -1)
                    return null;

                if (ttl <= 1)
                    return TimeSpan.Zero;

                return TimeSpan.FromSeconds((double)ttl);
            }

            /// <summary>
            /// Returns the time span of when this record will expire.
            /// </summary>
            /// <seealso cref="Expiration"/>
            public TimeSpan? TTL
            {
                get => CalcTTLTimeSpan(this.Record.TimeToLive);
            }

            /// <summary>
            /// Returns or sets the bin based on <paramref name="binName"/>
            /// </summary>
            /// <param name="binName">Name of the bin</param>
            /// <returns>A BinName or null indicating the bin name does not exists</returns>
            /// <seealso cref="GetValue(string)"/>
            /// <seealso cref="SetValue(string, object, bool)"/>
            /// <seealso cref="BinExists(string)"/>
            /// <see cref="Values"/>
            public Bin this[string binName]
            {
                get
                {
                    if (this.Record.bins.TryGetValue(binName, out object value))
                    {
                        return new Bin(binName, value);
                    }

                    return null;
                }               
            }

            /// <summary>
            /// The number of bins defined in this record.
            /// </summary>
            public int Count { get => this.Record.bins?.Count ?? 0; }

            private ExpandoObject _definedValuesCache = null;

            /// <summary>
            /// Returns only the defined bin name/value pairs in the record including the primary key defined as <see cref="DefaultASPIKeyName"/>.
            /// The returned object is actually an <see cref="ExpandoObject"/> instance.
            /// </summary>
            /// <seealso cref="this[string]"/>
            /// <see cref="GetValues"/>
            /// <see cref="GetValue(string)"/>
            /// <see cref="ToValue(string)"/>
            /// <see cref="BinExists(string)"/>
            public dynamic Values
            {
                get
                {
                    if (this._definedValuesCache == null)
                    {
                        var record = new ExpandoObject() as IDictionary<string, Object>;

                        record.Add(DefaultASPIKeyName, this.PrimaryKey);

                        if (this.Record.bins != null)
                            foreach (var bin in this.Record.bins)
                            {
                                record.Add(Helpers.CheckName(bin.Key, "Bin"), bin.Value);
                            }
                        this._definedValuesCache = (ExpandoObject)record;
                    }

                    return (dynamic)this._definedValuesCache;
                }
            }


            /// <summary>
            /// Gets the value based on <paramref name="binName"/>
            /// </summary>
            /// <param name="binName">The name of the bin within the record</param>
            /// <returns>
            /// The value of the bin or null indicating that the bin was not found.
            /// </returns>
            /// <seealso cref="this[string]"/>
            /// <seealso cref="BinExists(string)"/>
            /// <seealso cref="SetValue(string, object, bool)"/>
            /// <seealso cref="ToValue(string)"/>
            public object GetValue([NotNull] string binName)
            {
                return this.Record.bins != null
                            && this.Record.bins.TryGetValue(binName, out object value) ? value : null;
            }

            /// <summary>
            /// Gets the <see cref="AValue"/> based on <paramref name="binName"/>
            /// </summary>
            /// <param name="binName">The name of the bin within the record</param>
            /// <returns>
            /// The <see cref="AValue"/> of the bin or null indicating that the bin was not found.
            /// </returns>
            /// <seealso cref="this[string]"/>
            /// <seealso cref="BinExists(string)"/>
            /// <seealso cref="GetValue(string)"/>
            /// <seealso cref="SetValue(string, object, bool)"/>
            public AValue ToValue([NotNull] string binName)
            {
                return this.Record.bins != null
                            && this.Record.bins.TryGetValue(binName, out object value) ? new AValue(value, binName, "Value") : null;
            }

            /// <summary>
            /// Gets all defined values in the record and returns a Dictionary. 
            /// </summary>
            /// <returns>
            /// Returns a Dictionary of items where the key is the bin name and the value is the associated bin value.
            /// </returns>
            /// <seealso cref="Values"/>
            public Dictionary<string, object> GetValues()
            {
                return this.Record.bins;
            }
            
            /// <summary>
            /// Determines if the bin exists within the record.
            /// </summary>
            /// <param name="binName">BinName Name</param>
            /// <returns>
            /// True if it exists.
            /// </returns>
            /// <seealso cref="this[string]"/>
            /// <seealso cref="GetValue(string)"/>
            public bool BinExists(string binName)
            {
                return this.Record.bins.ContainsKey(binName);
            }

        }

        /// <summary>
        /// Container for Aerospike API Properties.
        /// You can obtain <see cref="Aerospike.Client.Record"/> instance, namespace, set name, etc. 
        /// </summary>
        /// <seealso cref="ARecord.AerospikeAPI.Record"/>
        /// <seealso cref="ARecord.AerospikeAPI.Values"/>
        public AerospikeAPI Aerospike { get; }

        #endregion

        /// <summary>
        /// Returns the value associated with <paramref name="pkbinName"/> or null to indicate the bin doesn't exists.
        /// </summary>
        /// <param name="pkbinName">Name of the bin or primary key name defined by <see cref="DefaultASPIKeyName"/>.</param>
        /// <returns>A BinName or null indicating the bin name does not exists</returns>
        /// <seealso cref="AerospikeAPI.GetValue(string)"/>
        /// <seealso cref="AerospikeAPI.ToValue(string)"/>
        /// <seealso cref="SetValue(string, object, bool)"/>
        /// <seealso cref="BinExists(string)"/>
        /// <seealso cref="AerospikeAPI.Values"/>
        public AValue this[string pkbinName]
        {
            get
            {
                if (pkbinName == DefaultASPIKeyName)
                    return new APrimaryKey(this.Aerospike.Key);

                return this.Aerospike.ToValue(pkbinName);
            }
        }

        public static implicit operator ExpandoObject(ARecord r) => (ExpandoObject) r.Aerospike.Values;
        
        /// <summary>
        /// Set a value to a bin within the record. This can be adding a new bin or updating an existing bin.
        /// </summary>
        /// <param name="binName">BinName Name</param>
        /// <param name="value">
        /// Value associated with the bin.
        /// If null, the bin is removed from DB record.
        /// <paramref name="value"/> can be a <see cref="Value"/>, <see cref="AerospikeAPI.Key"/>, <see cref="Bin"/> or a native type/class.
        /// </param>
        /// <param name="cloneRecord">
        /// If true, this instance (record) is cloned and then updated.
        /// </param>
        /// <returns>
        /// Returns the updated record. 
        /// </returns>
        /// <seealso cref="this[string]"/>
        /// <seealso cref="BinExists(string)"/>
        /// <seealso cref="AerospikeAPI.GetValue(string)"/>
        public ARecord SetValue([NotNull] string binName, object value, bool cloneRecord = false)
        {
            ARecord newRec = cloneRecord ? new ARecord(this) : this;
            object binValue = value;

            newRec.Aerospike.Bins = null;

            if (value is Client.Key key)
                binValue = key.userKey?.Object;
            else if (value is Client.Value clientValue)
                binValue = clientValue.Object;
            else if (value is Client.Bin bValue)
                binValue = bValue?.value?.Object;

            if (newRec.Aerospike.Record.bins.ContainsKey(binName))
            {
                newRec.Aerospike.Record.bins[binName] = binValue;
            }
            else
            {
                newRec.Aerospike.Record.bins.Add(binName, binValue);
            }

            return newRec;
        }
        
        /// <summary>
        /// Determines if the bin exists within the record.
        /// </summary>
        /// <param name="binName">BinName Name</param>
        /// <returns>
        /// True if it exists.
        /// </returns>
        /// <seealso cref="this[string]"/>
        /// <seealso cref="AerospikeAPI.GetValue(string)"/>
        public bool BinExists(string binName)
        {
            return this.Aerospike.BinExists(binName);
        }

        /// <summary>
        /// Deletes the record from the DB. 
        /// </summary>
        /// <param name="writePolicy"></param>
        /// <returns>
        /// True if deleted.
        /// </returns>
        /// <exception cref="NullReferenceException">
        /// If <see cref="SetAccess"/> is null, a Null reference exception is thrown. 
        /// </exception>
        public bool Delete(WritePolicy writePolicy = null)
        {
            if (this.SetAccess == null)
                throw new NullReferenceException("No Set Instance associated with this record. As such, it cannot be deleted.");

            return this.SetAccess
                        .AerospikeConnection
                        .AerospikeClient.Delete(writePolicy, this.Aerospike.Key);
        }

        /// <summary>
        /// Updates this record in the DB
        /// </summary>
        /// <param name="writePolicy"></param>
        /// <exception cref="NullReferenceException">
        /// If <see cref="SetAccess"/> is null, a Null reference exception is thrown. 
        /// </exception>
        public void Update(WritePolicy writePolicy = null)
        {
            if (this.SetAccess == null)
                throw new NullReferenceException("No Set Instance associated with this record. As such, it cannot be updated.");

            this.SetAccess
                .AerospikeConnection
                .AerospikeClient.Put(writePolicy, this.Aerospike.Key, this.Aerospike.Bins);
        }

        /// <summary>
        /// Will convert the record into a user defined class were the bin's name is matches the class's field/property name and type.
        /// <seealso cref="Aerospike.Client.BinNameAttribute"/>
        /// <seealso cref="Aerospike.Client.BinIgnoreAttribute"/>
        /// <seealso cref="Aerospike.Client.ConstructorAttribute"/>
        /// </summary>
        /// <typeparam name="T">user defined class</typeparam>
        /// <param name="transform">
        /// A action that is called to perform customized transformation. 
        /// First argument -- the name of the property/field
        /// Second argument -- the property/field type
        /// Third argument -- bin name
        /// Fourth argument -- bin value
        /// Returns the new transformed object or null to indicate that this transformation should be skipped.
        /// </param>
        /// <returns>
        /// An instance of the class or an exception.
        /// </returns>
        /// <exception cref="MissingMethodException">Thrown if the constructor for the type cannot be determined</exception>
        /// <seealso cref="Aerospike.Client.BinNameAttribute"/>
        /// <seealso cref="Aerospike.Client.BinIgnoreAttribute"/>
        /// <seealso cref="Aerospike.Client.ConstructorAttribute"/>
        /// <seealso cref="Cast{T}(object, Func{string, Type, string, object, object})"/>
        public T Cast<T>(Func<string, Type, string, object, object> transform = null)
        {            
            return (T)Helpers.Transform<T>(this.Aerospike.Record.bins, transform);
        }

        /// <summary>
        /// Will convert the record into a user defined class were the bin's name is matches the class's field/property name and type.
        /// <seealso cref="Aerospike.Client.BinNameAttribute"/>
        /// <seealso cref="Aerospike.Client.BinIgnoreAttribute"/>
        /// <seealso cref="Aerospike.Client.ConstructorAttribute"/>
        /// </summary>
        /// <typeparam name="T">user defined class</typeparam>
        /// <param name="primaryKey">
        /// The primary key can be a <see cref="Aerospike.Client.Key"/>, <see cref="Aerospike.Client.Value"/>, digest (byte[]), or a .net type.
        /// </param>  
        /// <param name="transform">
        /// A action that is called to perform customized transformation. 
        /// First argument -- the name of the property/field
        /// Second argument -- the property/field type
        /// Third argument -- bin name
        /// Fourth argument -- bin value
        /// Returns the new transformed object or null to indicate that this transformation should be skipped.
        /// </param>
        /// <returns>
        /// An instance of the class or an exception.
        /// </returns>
        /// <exception cref="MissingMethodException">Thrown if the constructor for the type cannot be determined</exception>
        /// <seealso cref="Aerospike.Client.BinNameAttribute"/>
        /// <seealso cref="Aerospike.Client.BinIgnoreAttribute"/>
        /// <seealso cref="Aerospike.Client.ConstructorAttribute"/>
        /// <seealso cref="Cast{T}(Func{string, Type, string, object, object})"/>
        public T Cast<T>(object primaryKey, Func<string, Type, string, object, object> transform = null)
        {
            Client.Key pk = null;
            
            if (primaryKey != null)
            {
                pk = Helpers.DetermineAerospikeKey(primaryKey, this.Aerospike.Namespace, this.Aerospike.SetName);
            }

            return (T)Helpers.Transform<T>(this.Aerospike.Record.bins, transform, pk);
        }

        /// <summary>
        /// Converts the record into a Dictionary&lt;string, object&gt; where the key is the bin name and the value is the bin&apos;s value.
        /// To obtain the actual Aerospike Record see <see cref="AerospikeAPI.Record"/>.
        /// </summary>
        /// <returns>
        /// An IDictionary where the key is the bin name and the value is the bin&apos;s value.
        /// </returns>
        /// <seealso cref="ARecord.AerospikeAPI.Values"/>
        /// <seealso cref="ARecord.AerospikeAPI.Record"/>
        /// <seealso cref="ARecord.AerospikeAPI.GetValues"/>
        public IDictionary<string, object> ToDictionary() => new Dictionary<string, object>((IDictionary<string, object>)this.Aerospike.GetValues());

        #region JSON

        static readonly Newtonsoft.Json.JsonSerializerSettings JSONSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            NullValueHandling = NullValueHandling.Ignore,
            DateParseHandling = DateParseHandling.DateTimeOffset
        };

        /// <summary>
        /// Generates a JSON string based on <see cref="JsonExportStructure"/>. 
        /// </summary>
        /// <param name="indented">If true, JSON string is formatted for readability</param>
        /// <param name="jsonSettings">
        /// JSON settings for serialization. 
        /// If not provided, <see cref="TypeNameHandling.All"/>, <see cref="DateParseHandling.DateTimeOffset"/>, and <see cref="NullValueHandling.Ignore"/> are defined.
        /// <seealso cref="JsonSerializerSettings"/>
        /// </param>
        /// <returns>A JSON string</returns>
        /// <seealso cref="JsonExportStructure"/>
        /// <seealso cref="JsonSerializerSettings"/>
        /// <seealso cref="JsonConvert.SerializeObject(object?, Formatting, JsonSerializerSettings?)"/>
        public string Export(bool indented = false, Newtonsoft.Json.JsonSerializerSettings jsonSettings = null)
        {
            var values = this.Aerospike.GetValues();

            var jsonStruct = new JsonExportStructure()
            {
                NameSpace = this.Aerospike.Namespace,
                SetName= this.Aerospike.SetName,
                Digest = this.Aerospike.Digest,
                Generation= this.Aerospike.Generation,
                TimeToLive = this.Aerospike.Expiration == 0 ? null : (int?) this.Aerospike.Expiration,
                KeyValue = this.Aerospike.PrimaryKey,
                Values = values
            };

            return Newtonsoft.Json.JsonConvert.SerializeObject(jsonStruct,
                                                                indented
                                                                    ? Newtonsoft.Json.Formatting.Indented
                                                                    : Formatting.None,
                                                                jsonSettings ?? JSONSettings);
        }

        /// <summary>
        /// Created an <see cref="ARecord"/> from a JSON string based on <see cref="JsonExportStructure"/> or generated from <see cref="Export(bool, JsonSerializerSettings)"/>
        /// </summary>
        /// <param name="jsonString">A valid JSON string based on <see cref="JsonExportStructure"/></param>
        /// <param name="ignoreTTL">If true, the TimeToLive field is ignored when creating a record</param>
        /// <param name="ignoreGeneration">if true, the Generation is ignored when creating a record</param>
        /// <param name="jsonSettings">
        /// JSON settings for serialization. 
        /// If not provided, <see cref="TypeNameHandling.All"/> and <see cref="NullValueHandling.Ignore"/> are defined.
        /// <seealso cref="JsonSerializerSettings"/>
        /// </param>
        /// <returns>
        /// An <see cref="ARecord"/> instance
        /// </returns>
        public static ARecord Import(string jsonString,
                                        bool ignoreTTL = false,
                                        bool ignoreGeneration=false,
                                        Newtonsoft.Json.JsonSerializerSettings jsonSettings = null)
        {
            var jsonStruct = Newtonsoft.Json.JsonConvert.DeserializeObject<JsonExportStructure>(jsonString,
                                                                                                    jsonSettings ?? JSONSettings);

            return new ARecord(jsonStruct.NameSpace,
                                jsonStruct.SetName,
                                new Client.Key(jsonStruct.NameSpace,
                                                jsonStruct.Digest,
                                                jsonStruct.SetName,
                                                Client.Value.Get(jsonStruct.KeyValue)),
                                jsonStruct.Values,
                                generation: ignoreGeneration ? null : (int?) jsonStruct.Generation,
                                expiration: ignoreTTL ? null : jsonStruct.TimeToLive);
        }

        /// <summary>
        /// Returns a <see cref="JObject"/> representing the record as JSON. Each property name will be the corresponding bin name. 
        /// The primary key is the PK value or the <see cref="Aerospike.Client.Key.digest"/> if the PK Value is not provided/present.
        /// </summary>
        /// <param name="pkPropertyName">
        /// The property name used for the primary key. The default is &apos;_id&apos;.
        /// If the primary key value is not present, the digest is used. In these cases the property value will be a sub property where that name will be &apos;$oid&apos; and the value is a byte string.
        /// </param>
        /// <param name="useDigest">
        /// If true, always use the PK digest as the primary key.
        /// If false, use the PK value is present, otherwise use the digest. 
        /// Default is false.
        /// </param>
        /// <returns>
        /// Returns a <see cref="JObject"/> representing the record.
        /// </returns>
        /// <seealso cref="FromJson(string, string, string, string, string, ANamespaceAccess)"/>
        /// <seealso cref="FromJson(string, string, dynamic, string, string, ANamespaceAccess)"/>
        /// <seealso cref="SetRecords.FromJson(string, string, string, WritePolicy, TimeSpan?)"/>
        /// <seealso cref="SetRecords.ToJson(Exp, string, bool)"/>
        /// <example>
        /// <code>
        /// {
        ///      &quot;_id&quot;: 522,
        ///      &quot;Tag&quot;: &quot;Player&quot;,
        ///      &quot;PlayerId&quot;: 522,
        ///      &quot;UserName&quot;: &quot;Roberts.Eunice&quot;,
        ///      &quot;FirstName&quot;: &quot;Eunice&quot;,
        ///      &quot;LastName&quot;: &quot;Roberts&quot;,
        ///      &quot;EmailAddress&quot;: &quot;RobertsEunice52@prohaska.name&quot;,
        ///      &quot;CountryCode&quot;: &quot;US&quot;,
        ///      &quot;State&quot;: &quot;NJ&quot;
        /// }
        /// </code>
        /// When <paramref name="useDigest"/> is true:
        /// <code>
        /// {
        ///      &quot;_id&quot;: {
        ///        &quot;$oid&quot;: &quot;0080a245fabe57999707dc41ced60edc4ac7ac40&quot;
        ///      },
        ///      &quot;Tag&quot;: &quot;Player&quot;,
        ///      &quot;PlayerId&quot;: 522,
        ///      &quot;UserName&quot;: &quot;Roberts.Eunice&quot;,
        ///      &quot;FirstName&quot;: &quot;Eunice&quot;,
        ///      &quot;LastName&quot;: &quot;Roberts&quot;,
        ///      &quot;EmailAddress&quot;: &quot;RobertsEunice52@prohaska.name&quot;,
        ///      &quot;CountryCode&quot;: &quot;US&quot;,
        ///      &quot;State&quot;: &quot;NJ&quot;
        /// }
        /// </code>
        /// </example>
        public JObject ToJson(string pkPropertyName = "_id", bool useDigest = false)
        {
            var jsonStruct = new JObject();

            if(!(this.Aerospike.Key is null))
            {
                if(useDigest || this.Aerospike.Key.userKey is null)
                {
                    var jDigest = new JObject()
                    {
                        ["$oid"] = JToken.FromObject(Helpers.ByteArrayToString(this.Aerospike.Key.digest))
                    };
                    
                    jsonStruct.Add(pkPropertyName, jDigest);
                }
                else
                {
                    jsonStruct.Add(pkPropertyName, JToken.FromObject(this.Aerospike.Key.userKey.Object));
                }
            }

            foreach(var bin in this.Aerospike.Bins) 
            {
                if(!(bin.value?.Object is null))
                    jsonStruct.Add(bin.name, JToken.FromObject(bin.value.Object));
            }

            return jsonStruct;
        }

        /// <summary>
        /// Given a Json string, creates a <see cref="ARecord"/> object that can but used to update the DB.
        /// </summary>
        /// <param name="nameSpace">The associated namespace of the set</param>
        /// <param name="setName">The associated Set of the record</param>
        /// <param name="primaryKey">
        /// The primary key which can be:
        ///     a C# value,
        ///     digest byte array,
        ///     <see cref="Aerospike.Client.Key"/>,
        ///     <see cref="APrimaryKey"/>,
        ///     <see cref="AValue"/>,
        ///     <see cref="Aerospike.Client.Value"/>
        /// </param>
        /// <param name="json">A valid Json string</param>
        /// <param name="jsonBinName">
        /// If provided, the Json object is placed into this bin.
        /// If null (default), the each top level Json property will be associated with a bin. Note, if the property name is greater than the bin name limit, an Aerospike exception will occur during the put.
        /// </param>
        /// <param name="setAccess">The set instance that will be associated to this record.</param>
        /// <returns>
        /// Returns ARecord instance.
        /// </returns>
        /// <seealso cref="ToJson(string, bool)"/>
        /// <seealso cref="FromJson(string, string, string, string, string, ANamespaceAccess)"/>
        /// <seealso cref="SetRecords.FromJson(string, string, string, WritePolicy, TimeSpan?)"/>
        /// <seealso cref="SetRecords.ToJson(Exp, string, bool)"/>
        /// <seealso cref="SetRecords.Put(ARecord, WritePolicy, TimeSpan?)"/>
        /// <seealso cref="AerospikeAPI.Bins"/>
        /// <remarks>
        /// The Json string can include Json in-line types. Below are the supported types:
        ///     <code>$date</code> or <code>$datetime</code>,
        ///         This can include an optional sub Json Type.Example:
        ///             <code>&quot;bucket_start_date&quot;: &quot;$date&quot;: { &quot;$numberLong&quot;: &quot;1545886800000&quot;}}</code>
        ///     <code>$datetimeoffset</code>,
        ///         This can include an optional sub Json Type. Example:
        ///             <code>&quot;bucket_start_datetimeoffset&quot;: &quot;$datetimeoffset&quot;: { &quot;$numberLong&quot;: &quot;1545886800000&quot;}}</code>
        ///     <code>$timespan</code>,
        ///         This can include an optional sub Json Type. Example:
        ///             <code>&quot;bucket_start_time&quot;: &quot;$timespan&quot;: { &quot;$numberLong&quot;: &quot;1545886800000&quot;}}</code>
        ///     <code>$timestamp</code>,
        ///     <code>$guid</code> or <code>$uuid</code>,
        ///     &quot;$oid&quot;,
        ///         If the Json string value equals 40 in length it will be treated as a digest and converted into a byte array.
        ///         Example:
        ///             &quot; _id&quot;: { &quot;$oid&quot;:&quot;0080a245fabe57999707dc41ced60edc4ac7ac40&quot; } ==&gt; &quot;_id&quot;:[00 80 A2 45 FA BE 57 99 97 07 DC 41 CE D6 0E DC 4A C7 AC 40]
        ///         This type can also take an optional keyword as a value. They are:
        ///             <code>$guid</code> or <code>$uuid</code> -- If provided, a new guid/uuid is generate as a unique value used
        ///             <code>$numeric</code> -- a sequential number starting at 1 will be used
        ///         Example:
        ///             &quot; _id&quot;: { &quot;$oid&quot;: &quot;$uuid&quot; } ==&gt; Generates a new uuid as the _id value
        ///     <code>$numberint64</code>, <code>$numberlong</code> or <code>$long</code>,
        ///     <code>$numberint32</code>, <code>$numberint</code>, or <code>$int</code>,
        ///     <code>$numberdecimal</code> or  <code>$decimal</code>,
        ///     <code>$numberdouble</code> or  <code>$double</code>,
        ///     <code>$numberfloat</code>, <code>$single</code>, or  <code>$float</code>,
        ///     <code>$numberint16</code>, <code>$numbershort</code> or  <code>$short</code>,
        ///     <code>$numberuint32</code>, <code>$numberuint</code>, or  <code>$uint</code>,
        ///     <code>$numberuint64</code>, <code>$numberulong</code>, or  <code>$ulong</code>,
        ///     <code>$numberuint16</code>, <code>$numberushort</code> or  <code>$ushort</code>,
        ///     <code>$bool</code> or <code>$boolean</code>;
        ///     <code>$type</code>
        ///         This item must be the first property in a JObject where the property&apos;s value is a .NET type.
        ///         All reminding elements will be transformed into that .NET object.
        /// </remarks>
        public static ARecord FromJson(string nameSpace,
                                        string setName,
                                        dynamic primaryKey,
                                        string json,
                                        string jsonBinName = null,
                                        ANamespaceAccess setAccess = null)
        {
            var converter = new CDTConverter();
            var binDict = JsonConvert.DeserializeObject<IDictionary<string, object>>(json, converter);
            
            return new ARecord(nameSpace,
                                setName,
                                primaryKey,
                                string.IsNullOrEmpty(jsonBinName) 
                                    ? binDict
                                    : new Dictionary<string,object>() { {jsonBinName, binDict} },
                                setAccess: setAccess);
        }

        /// <summary>
        /// Given a Json string, creates a <see cref="ARecord"/> object that can but used to update the DB.
        /// </summary>
        /// <param name="nameSpace">The associated namespace of the set</param>
        /// <param name="setName">The associated Set of the record</param>       
        /// <param name="json"></param>
        /// <param name="pkPropertyName">
        /// The property name used to obtain the primary key. This must be a top level field (cannot be nested).
        /// The default is &apos;_id&apos;.
        /// If the pkPropertyName doesn&apos;s exists, a <see cref="KeyNotFoundException"/> is thrown.
        /// </param>
        /// <param name="jsonBinName">
        /// If provided, the Json object is placed into this bin.
        /// If null (default), the each top level Json property will be associated with a bin. Note, if the property name is greater than the bin name limit, an Aerospike exception will occur during the put.
        /// </param>
        /// <param name="setAccess">The set instance that will be associated to this record.</param>
        /// <returns>
        /// Returns ARecord instance.
        /// </returns>
        /// <seealso cref="ToJson(string, bool)"/>
        /// <seealso cref="FromJson(string, string, dynamic, string, string, ANamespaceAccess)"/>
        /// <seealso cref="SetRecords.FromJson(string, string, string, WritePolicy, TimeSpan?)"/>
        /// <seealso cref="SetRecords.ToJson(Exp, string, bool)"/>
        /// <seealso cref="SetRecords.Put(ARecord, WritePolicy, TimeSpan?)"/>
        /// <seealso cref="AerospikeAPI.Bins"/>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the <paramref name="pkPropertyName"/> is not found as a top-level field. 
        /// </exception>
        /// <remarks>
        /// The Json string can include Json in-line types. Below are the supported types:
        ///     <code>$date</code> or <code>$datetime</code>,
        ///         This can include an optional sub Json Type.Example:
        ///             <code>&quot;bucket_start_date&quot;: &quot;$date&quot;: { &quot;$numberLong&quot;: &quot;1545886800000&quot;}}</code>
        ///     <code>$datetimeoffset</code>,
        ///         This can include an optional sub Json Type. Example:
        ///             <code>&quot;bucket_start_datetimeoffset&quot;: &quot;$datetimeoffset&quot;: { &quot;$numberLong&quot;: &quot;1545886800000&quot;}}</code>
        ///     <code>$timespan</code>,
        ///         This can include an optional sub Json Type. Example:
        ///             <code>&quot;bucket_start_time&quot;: &quot;$timespan&quot;: { &quot;$numberLong&quot;: &quot;1545886800000&quot;}}</code>
        ///     <code>$timestamp</code>,
        ///     <code>$guid</code> or <code>$uuid</code>,
        ///     &quot;$oid&quot;,
        ///         If the Json string value equals 40 in length it will be treated as a digest and converted into a byte array.
        ///         Example:
        ///             &quot; _id&quot;: { &quot;$oid&quot;:&quot;0080a245fabe57999707dc41ced60edc4ac7ac40&quot; } ==&gt; &quot;_id&quot;:[00 80 A2 45 FA BE 57 99 97 07 DC 41 CE D6 0E DC 4A C7 AC 40]
        ///         This type can also take an optional keyword as a value. They are:
        ///             <code>$guid</code> or <code>$uuid</code> -- If provided, a new guid/uuid is generate as a unique value used
        ///             <code>$numeric</code> -- a sequential number starting at 1 will be used
        ///         Example:
        ///             &quot; _id&quot;: { &quot;$oid&quot;: &quot;$uuid&quot; } ==&gt; Generates a new uuid as the _id value
        ///     <code>$numberint64</code>, <code>$numberlong</code> or <code>$long</code>,
        ///     <code>$numberint32</code>, <code>$numberint</code>, or <code>$int</code>,
        ///     <code>$numberdecimal</code> or  <code>$decimal</code>,
        ///     <code>$numberdouble</code> or  <code>$double</code>,
        ///     <code>$numberfloat</code>, <code>$single</code>, or  <code>$float</code>,
        ///     <code>$numberint16</code>, <code>$numbershort</code> or  <code>$short</code>,
        ///     <code>$numberuint32</code>, <code>$numberuint</code>, or  <code>$uint</code>,
        ///     <code>$numberuint64</code>, <code>$numberulong</code>, or  <code>$ulong</code>,
        ///     <code>$numberuint16</code>, <code>$numberushort</code> or  <code>$ushort</code>,
        ///     <code>$bool</code> or <code>$boolean</code>;
        ///     <code>$type</code>
        ///         This item must be the first property in a JObject where the property&apos;s value is a .NET type.
        ///         All reminding elements will be transformed into that .NET object.
        /// </remarks>
        public static ARecord FromJson(string nameSpace,
                                        string setName,
                                        string json,
                                        string pkPropertyName = "_id",
                                        string jsonBinName = null,
                                        ANamespaceAccess setAccess = null)
        {            
            var converter = new CDTConverter();
            var binDict = JsonConvert.DeserializeObject<IDictionary<string, object>>(json, converter);

            var primaryKeyValue = binDict[pkPropertyName];
            binDict.Remove(pkPropertyName);

            return new ARecord(nameSpace,
                                setName,
                                primaryKeyValue,
                                string.IsNullOrEmpty(jsonBinName)
                                    ? binDict
                                    : new Dictionary<string, object>() { { jsonBinName, binDict } },
                                setAccess: setAccess);
        }

        #endregion

        #region overrides

        public override int GetHashCode()
        {
            unchecked // Allow arithmetic overflow, numbers will just "wrap around"
            {
                int hashcode = 1430287;
                hashcode = hashcode * 7302013 ^ this.Aerospike.Key.digest.GetHashCode();

                foreach (var bin in this.Aerospike.Record.bins)
                {
                    hashcode = hashcode * 7302013 ^ bin.Key.GetHashCode();
                    hashcode = hashcode * 7302013 ^ (bin.Value?.GetHashCode() ?? 0);
                }

                return hashcode;
            }
        }

        /// <summary>
        /// Return true if object matches this record&apos;s AerospikeKey.
        /// If a string and the key is not present, the string&apos;s digest is calculated and used to match the digest of this object.
        /// Note that if it is an AerospikeRecord it is based on the digest and generation. You can bypass this by just using the <see cref="AerospikeAPI.Key"/>.
        /// </summary>
        /// <param name="obj">
        /// Can be any type of object. 
        /// String, Aerospike AerospikeKey or Value, or an AsRecord instance.
        /// </param>
        /// <returns>
        /// True if <paramref name="obj"/> matches key.
        /// </returns>
        /// <seealso cref="Equals(ARecord)"/>
        /// <seealso cref="Equals(Client.Key)"/>
        /// <seealso cref="Equals(string)"/>
        /// <seealso cref="Equals(Value)"/>
        /// <seealso cref="Equals(AValue)"/>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj is ARecord rec) return this.Equals(rec);
            if (obj is Client.Key key) return this.Equals(key);
            if (obj is Value value) return this.Equals(value);
            if (obj is string strKey) return this.Equals(strKey);
            if (obj is AValue aValue) return this.Equals(aValue);
            if (obj is byte[] digest) return this.Aerospike.Key.digest.SequenceEqual(digest);            

            return this.Equals(Value.Get(obj));
        }

        public override string ToString()
        {
            var keyValue = this.Aerospike.KeyValue?.Object ?? this.Aerospike.Digest;
            return $"ARecord<NS={this.Aerospike.Namespace}, Set={this.Aerospike.SetName}, Key={keyValue}, Gen={this.Aerospike.Generation}, Bins={this.Aerospike.Count}";
        }

        #endregion

        #region IEquatable

        /// <summary>
        /// Determine if the records are equal based on there digest, and generation. 
        /// </summary>
        /// <param name="other">An AerospikeRecord instance</param>
        /// <returns></returns>
        /// <seealso cref="Equals(Object)"/>
        /// <seealso cref="Equals(Client.Key)"/>
        /// <seealso cref="Equals(string)"/>
        /// <seealso cref="Equals(Value)"/>
        /// <seealso cref="Equals(AValue)"/>
        public bool Equals([AllowNull] ARecord other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return this.Aerospike.Key.digest.SequenceEqual(other.Aerospike.Key.digest)
                    && this.Aerospike.Count == other.Aerospike.Count
                    && this.Aerospike.Record.generation == other.Aerospike.Record.generation;
        }

        /// <summary>
        /// Determines if the Aerospike Primary <see cref="Client.Key"/> is the same as this record&apos;s.
        /// </summary>
        /// <param name="keyInstance">The Aerospike <see cref="Client.Key"/>.</param>
        /// <seealso cref="Equals(ARecord)"/>
        /// <seealso cref="Equals(Object)"/>
        /// <seealso cref="Equals(string)"/>
        /// <seealso cref="Equals(Value)"/>
        /// <seealso cref="Equals(AValue)"/>
        public bool Equals([AllowNull] Client.Key keyInstance)
        {
            if(keyInstance is null) return false;

            return this.Aerospike.Key.digest.SequenceEqual(keyInstance.digest);
        }

        /// <summary>
        /// True if the record matches the <paramref name="primaryKey"/>.
        /// This works even if the key is not present in he AerospikeKey instance (<see cref="AerospikeAPI.Key"/>) (KEY_SEND is false). 
        /// </summary>
        /// <param name="primaryKey">The string value used to compare against the key value or <see cref="AerospikeAPI.Digest"/></param>
        /// <returns>
        /// True if a match. 
        /// </returns>
        /// <seealso cref="Equals(ARecord)"/>
        /// <seealso cref="Equals(Client.Key)"/>
        /// <seealso cref="Equals(Object)"/>
        /// <seealso cref="Equals(Value)"/>
        /// <seealso cref="Equals(AValue)"/>
        public bool Equals([AllowNull] string primaryKey)
        {
            if (primaryKey is null) return false;

            if (this.Aerospike.Key == null)
                return this.Aerospike
                            .Digest
                            .SequenceEqual(Client.Key.ComputeDigest(this.Aerospike.SetName,
                                                                    new Value.StringValue(primaryKey)));

            return this.Aerospike.KeyValue.Object.ToString().Equals(primaryKey);
        }

        /// <summary>
        /// Determines if the Aerospike <see cref="Client.Value"/> matches this record&apos;s primary key.
        /// </summary>
        /// <seealso cref="Equals(ARecord)"/>
        /// <seealso cref="Equals(Client.Key)"/>
        /// <seealso cref="Equals(string)"/>
        /// <seealso cref="Equals(Object)"/>
        /// <seealso cref="Equals(AValue)"/>
        public bool Equals([AllowNull] Value valueInstance)
        {            
            if (valueInstance is null) return false;

            if (this.Aerospike.KeyValue == null)
            {
                if (valueInstance.Object is null)
                    return false;

                return this.Aerospike.Digest.SequenceEqual(Client.Key.ComputeDigest(this.Aerospike.SetName, valueInstance));
            }                

            return this.Aerospike.KeyValue.Equals(valueInstance);
        }

        /// <summary>
        /// Determines if the Aerospike <see cref="AValue"/> matches this record&apos;s primary key.
        /// </summary>
        /// <seealso cref="Equals(ARecord)"/>
        /// <seealso cref="Equals(Client.Key)"/>
        /// <seealso cref="Equals(string)"/>
        /// <seealso cref="Equals(Object)"/>
        /// <seealso cref="Equals(Value)"/>
        public bool Equals([AllowNull] AValue valueInstance)
        {            
            if (valueInstance is null) return false;

            return valueInstance.Equals(this.Aerospike.Key);
        }

        #endregion

        #region IEqualityCompare
        public bool Equals([AllowNull] ARecord x, [AllowNull] ARecord y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x == null || y == null) return false;

            return x.Equals(y);            
        }

        public int GetHashCode([DisallowNull] ARecord record)
        {
            return record?.GetHashCode() ?? 0;
        }

        #endregion

        private ExpandoObject CheckForAPrimaryKeyType(ExpandoObject expando, IDictionary<string,object> additionalProperties = null)
        {
            var dict = expando as IDictionary<string, object>;

            if (additionalProperties != null)
            {
                foreach(var kvp in additionalProperties)
                {
                    if (!dict.TryAdd(kvp.Key, kvp.Value))
                        dict[kvp.Key] = kvp.Value;
                }
            }

            foreach(var kvp in dict.ToArray())
            {
                
                if(kvp.Value is AValue aValue)
                {
                    if (kvp.Key == DefaultASPIKeyName)
                    {
                        dict[DefaultASPIKeyName] = aValue.ToDump();
                    }
                    else
                    {
                        dict[kvp.Key] = aValue.ToDump();
                    }
                }                    
            }

           var exceptionFld = dict.FirstOrDefault(kvp => kvp.Key == nameof(RecordException));

            if (exceptionFld.Key != null && exceptionFld.Value is Exception exception)
            {
                dict[nameof(RecordException)] = LPU.WithStyle(exception, "color:Red");
            }
            
            return expando;
        }

        virtual protected object ToDump(string[] dumpFlds)
        {            
            static void AddKVP(ref IDictionary<string,object> additionalProperties, string propName, object value)
            {
                if (additionalProperties == null)
                    additionalProperties = new Dictionary<string, object>() { { propName, value } };
                else
                    additionalProperties.Add(propName, value);
            }

            switch (this.DumpType)
            {
                case DumpTypes.Record:
                    {
                        IDictionary<string, object> additionalProperties = null;
                        var fldsToDump = new StringBuilder(string.Join(',', dumpFlds));

                        if (this.Aerospike.TTL.HasValue)
                        {
                            AddKVP(ref additionalProperties, nameof(AerospikeAPI.TTL), this.Aerospike.TTL);
                        }
                        if (this.RecordException != null)
                        {
                            AddKVP(ref additionalProperties, nameof(RecordException), this.RecordException);
                        }

                        return CheckForAPrimaryKeyType(LPU.ToExpando(this, include: fldsToDump.ToString()), additionalProperties);
                    }
                case DumpTypes.Dynamic:
                    {
                        IDictionary<string, object> additionalProperties = null;
                        var fldsToDump = new StringBuilder(string.Join(',', dumpFlds));

                        AddKVP(ref additionalProperties, nameof(AerospikeAPI.Values), this.Aerospike.Values);

                        if (this.Aerospike.TTL.HasValue)
                        {
                            AddKVP(ref additionalProperties, nameof(AerospikeAPI.TTL), this.Aerospike.TTL);
                        }
                        if (this.RecordException != null)
                        {
                            AddKVP(ref additionalProperties, nameof(RecordException), this.RecordException);
                        }

                        return CheckForAPrimaryKeyType(LPU.ToExpando(this, include: fldsToDump.ToString()), additionalProperties);
                    }

                case DumpTypes.Detail:                                  
                default:
                    return this;
            }
        }

        virtual public object ToDump()
        {            
            static void AddKVP(ref IDictionary<string,object> additionalProperties, string propName, object value)
            {
                if (additionalProperties == null)
                    additionalProperties = new Dictionary<string, object>() { { propName, value } };
                else
                    additionalProperties.Add(propName, value);
            }

            switch (this.DumpType)
            {
                case DumpTypes.Record:
                case DumpTypes.Dynamic:
                    {
                        var expando = new ExpandoObject();
                        var additionalProperties = expando as IDictionary<string, object>;

                        AddKVP(ref additionalProperties, nameof(AerospikeAPI.Namespace), this.Aerospike.Namespace);
                        AddKVP(ref additionalProperties, nameof(AerospikeAPI.SetName), this.Aerospike.SetName);
                        AddKVP(ref additionalProperties, nameof(AerospikeAPI.Values), this.Aerospike.Values);

                        if (this.Aerospike.TTL.HasValue)
                        {
                            AddKVP(ref additionalProperties, nameof(AerospikeAPI.TTL), this.Aerospike.TTL);
                        }
                        if (this.RecordException != null)
                        {
                            AddKVP(ref additionalProperties, nameof(RecordException), this.RecordException);
                        }

                        return CheckForAPrimaryKeyType(expando);
                    }
                case DumpTypes.Detail:
                default:
                    return this;
            }         
        }
    }


}
