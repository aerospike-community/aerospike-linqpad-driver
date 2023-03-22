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
                if (recordBins.Length < this.Aerospike.BinNames?.Length
                        && recordBins.All(n => this.Aerospike.BinNames.Contains(n)))
                { }
                else
                {
                    this.DumpType = DumpTypes.Dynamic;
                    this.HasExtendedBins= true;
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
                this.DumpType = DumpTypes.Dynamic;
                this.HasExtendedBins = true;                
            }
        }

        public ARecord([NotNull] ARecord cloneRecord)
        {
            this.Aerospike = new AerospikeAPI(cloneRecord.Aerospike);
            this.DumpType = cloneRecord.DumpType;
            this.SetBinsHashCode = cloneRecord.SetBinsHashCode;
            this.BinsHashCode = cloneRecord.BinsHashCode;
            this._hasExtendedBins = cloneRecord._hasExtendedBins;
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

        private bool _hasExtendedBins;

        /// <summary>
        /// Returns true to indicated that this record had bins that were not detected by the associated Set.
        /// </summary>
        public bool HasExtendedBins
        {
            get => this._hasExtendedBins;
            set
            {
                this._hasExtendedBins = value;
            }
        }
        
        /// <summary>
        /// This is the Set's BinName Names Hash Code
        /// </summary>
        private int SetBinsHashCode { get; }
        /// <summary>
        /// This is the Record's BinName Name Hash Code
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
                if (keyValue is byte[] digest)
                    this.Key = new Client.Key(ns, set, digest);
                else if (keyValue is Client.Key valueKey)
                    this.Key = new Client.Key(ns, set, valueKey.userKey);
                else if (keyValue is Value value)
                    this.Key = new Client.Key(ns, set, value);
                else
                    this.Key = new Client.Key(ns, set, Value.Get(keyValue));

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
        /// </summary>
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
        public T Cast<T>(Func<string, Type, string, object, object> transform = null)
        {            
            return (T)Helpers.Transform<T>(this.Aerospike.Record.bins, transform);
        }

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
        public string ToJson(bool indented = false, Newtonsoft.Json.JsonSerializerSettings jsonSettings = null)
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
        /// Created an <see cref="ARecord"/> from a JSON string based on <see cref="JsonExportStructure"/> or generated from <see cref="ToJson(bool, JsonSerializerSettings)"/>
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
        public static ARecord FromJson(string jsonString,
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

            var pkKVP = dict.FirstOrDefault(kvp => kvp.Key == DefaultASPIKeyName);
            var exceptionFld = dict.FirstOrDefault(kvp => kvp.Key == nameof(RecordException));

            if (pkKVP.Key is null && exceptionFld.Key is null) return expando;

            if(pkKVP.Key != null && pkKVP.Value is AValue pKey)
            {
                dict[DefaultASPIKeyName] = pKey.ToDump();
            }

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
