using Aerospike.Database.LINQPadDriver.Extensions;
using GeoJSON.Net;
using LINQPad.Extensibility.DataContext;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aerospike.Database.LINQPadDriver
{
    [System.Diagnostics.DebuggerDisplay("{Name}")]
    public sealed class LPSet : IGenerateCode
    {        
        public class BinType : ILPExplorer, IEqualityComparer<BinType>
        {            
            internal BinType(string name, Type type, bool dup, bool allRecs, bool detected = false)
            {
                this.BinName = name;
                this.FndAllRecs = allRecs;
                this.DataType = type;
                this.Duplicate = dup;
                this.Detected = detected;
            }

            public readonly string BinName;
            public readonly Type DataType;
            public readonly bool Duplicate;
            public readonly bool FndAllRecs;
            /// <summary>
            /// True if the bin was found after the initial scan of the set.
            /// </summary>            
            public bool Detected { get; set; }

            public string GenerateExplorerName()
            {
                var binName = new StringBuilder(this.BinName);

                if (this.DataType != null)
                {
                    binName.Append(" (");
                    binName.Append(Helpers.GetRealTypeName(this.DataType));

                    if (this.Duplicate) binName.Append('*');
                    if (!this.FndAllRecs) binName.Append('?');

                    binName.Append(')');
                }

                return binName.ToString();
            }

            public ExplorerItem CreateExplorerItem()
            {
                return new ExplorerItem(this.GenerateExplorerName(),
                                        ExplorerItemKind.Schema,
                                        ExplorerIcon.Column)
                {
                    IsEnumerable = false,
                    DragText = this.BinName
                };
            }

            public override string ToString()
                => $"{this.BinName}({this.DataType.Name})";

            public override int GetHashCode()
                => this.ToString().GetHashCode();

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(this, obj)) return true;
                if (obj is null) return false;

                if (obj is BinType xbinType) return this.Equals(this, xbinType);
                if (obj is string binName) return this.BinName == binName;

                return false;
            }

            public bool Equals([AllowNull] BinType x, [AllowNull] BinType y)
            {
                if(ReferenceEquals(x, y)) return true;
                if (x is null || y is null) return false;
                if (x.DataType is null  || y.DataType is null) return false;

                return x.BinName == y.BinName && x.DataType.Equals(y.DataType);
            }

            public int GetHashCode([DisallowNull] BinType obj)
                => obj?.GetHashCode() ?? 0;

            public sealed class NameEqualityComparer : IEqualityComparer<BinType>
            {
                public bool Equals([AllowNull] BinType x, [AllowNull] BinType y)
                    => x?.BinName == y?.BinName;

                public int GetHashCode([DisallowNull] BinType obj)
                    => obj?.GetHashCode() ?? 0;
            }

            public static readonly NameEqualityComparer DefaultNameComparer = new NameEqualityComparer();
        }

        static readonly ConcurrentBag<LPSet> SetsBag = new ConcurrentBag<LPSet>();

        public const string NullSetName = "NullSet";

        public LPSet(LPNamespace aNamespace, string name)
        {
            this.LPnamespace = aNamespace;
            this.Name = name;
            this.SafeName = Helpers.CheckName(name, "Set");
            SetsBag.Add(this);
        }

        public LPSet(LPNamespace aNamespace, string name, IEnumerable<BinType> binTypes)
        {
            this.LPnamespace = aNamespace;
            this.Name = name;
            this.SafeName = Helpers.CheckName(name, "Set");
            this.binTypes = binTypes?.Where(b => b.DataType != null).ToList() ?? new List<BinType>();
            SetsBag.Add(this);
        }

        public LPSet(LPNamespace aNamespace)
        {
            this.LPnamespace = aNamespace;
            this.Name = NullSetName;
            this.SafeName = Helpers.CheckName(this.Name, "Set");

            this.IsNullSet = true;
            SetsBag.Add(this);
        }
        
        public LPSet(LPNamespace anamespace,
                        string name,
                        string safename,
                        bool isnullset,
                        IEnumerable<BinType> binTypes,
                        IEnumerable<LPSecondaryIndex> sindexes)
        {
            this.LPnamespace = anamespace;
            this.Name = name;
            this.SafeName = safename;
            this.IsNullSet = isnullset;
            this.binTypes = binTypes.ToList();
            this.SIndexes = sindexes;
            SetsBag.Add(this);
        }

        public LPNamespace LPnamespace { get; }

        /// <summary>
        /// The DB Name of the Set
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The safe name of the set that can be used by a C# class name or property.
        /// </summary>
        public string SafeName { get; }

        /// <summary>
        /// Returns true t indicate a Null Set.
        /// </summary>
        public bool IsNullSet { get; }

        private List<BinType> binTypes = new List<BinType>(0);
        public IEnumerable<BinType> BinTypes
        {
            get
            {
                lock (binTypes) { return binTypes; }
            }            
        }

        /// <summary>
        /// Returns the Secondary Indexes associated with this set.
        /// </summary>
        public IEnumerable<LPSecondaryIndex> SIndexes { get; internal set; } = Enumerable.Empty<LPSecondaryIndex>();

        internal void GetRecordBins(GetSetBins getBins,
                                        bool determineDocType,
                                        int maxRecords,
                                        int minRecs,
                                        bool updateCntd = true)
        {
            lock (binTypes)
            {
                this.binTypes = getBins.Get(this.LPnamespace.Name, this.Name, determineDocType, maxRecords, minRecs);

                if (updateCntd)
                {
                    Interlocked.Increment(ref nbrCodeUpdates);
                    Interlocked.Increment(ref LPnamespace.nbrCodeUpdates);
                }
            }
        }

        internal void UpdateTypeBins(IEnumerable<string> bins, bool updateCnts = true)
        {
            lock (binTypes)
            {
                this.binTypes = bins.Select(b => new BinType(b, null, false, false)).ToList();
                if (updateCnts)
                {
                    Interlocked.Increment(ref nbrCodeUpdates);
                    Interlocked.Increment(ref LPnamespace.nbrCodeUpdates);
                }
            }
        }

        public bool AddBin(string binName, Type dataType)
        {
            bool dup;

            lock(binTypes)
            {
                dup = binTypes.Any(b => b.BinName == binName && b.DataType == dataType);

                this.binTypes.Add(new BinType(binName, dataType, dup, false, true));
                Interlocked.Increment(ref nbrCodeUpdates);
                if(this.LPnamespace != null)
                    Interlocked.Increment(ref this.LPnamespace.nbrCodeUpdates);
            }

            return dup; 
        }

        public bool RemoveBin(string removeBinName)
        {            
            lock (binTypes)
            {
                if (this.binTypes.Any(b => b.BinName == removeBinName))
                {
                    this.binTypes.Remove(this.binTypes.First(b => b.BinName == removeBinName));
                    Interlocked.Increment(ref nbrCodeUpdates);
                    if (this.LPnamespace != null)
                        Interlocked.Increment(ref this.LPnamespace.nbrCodeUpdates);
                }               
            }

            return false;
        }

        #region Code Generation

        public (string classCode, string definePropCode, string createInstanceCode)
            CodeCache
        { get; private set; }

        private long nbrCodeUpdates = 0;
        
        public bool CodeNeedsUpdating { get => Interlocked.Read(ref nbrCodeUpdates) > 0; }

        public (string setClassCode, string setDefinePropCode, string ignore) GenerateNoRecSet()
        {
            var idxProps = new StringBuilder();
            
            foreach (var sidx in this.SIndexes)
            {
                idxProps.AppendLine(sidx.GenerateCode(null, true));
            }

            Interlocked.Exchange(ref nbrCodeUpdates, 0);
            Interlocked.Decrement(ref LPnamespace.nbrCodeUpdates);

            return this.CodeCache = ($@"
        public class {SafeName}_SetCls : Aerospike.Database.LINQPadDriver.Extensions.SetRecords
		{{
			public {SafeName}_SetCls (Aerospike.Database.LINQPadDriver.Extensions.ANamespaceAccess setAccess)
				: base(setAccess, ""{Name}"")
			{{ }}
			
{idxProps}
		}}", //End of Class string
        ///////////////////////////////////////////////////////////////////////
        $@"
		public {SafeName}_SetCls {SafeName} {{ get => new {SafeName}_SetCls(this); }}",//End of property string
        null
            ); 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="alwaysUseAValues"></param>
        /// <param name="forceGeneration"></param>
        /// <returns>
        /// classCode -- Code used to define this set&apos;s class
        /// definePropCode -- Code used to define the property used to reference this Set
        /// createInstanceCode -- ignored, always null
        /// </returns>
        public (string classCode, string definePropCode, string createInstanceCode) 
            CodeGeneration(bool alwaysUseAValues, bool forceGeneration = false)
        {
            if (!this.CodeNeedsUpdating && !forceGeneration && this.CodeCache.classCode != null)
                return this.CodeCache;

            var bins = this.BinTypes.ToArray();

            if (this.IsNullSet || !bins.Any())
                return GenerateNoRecSet();

            var idxProps = new StringBuilder();
            var binsString = string.Join(',', bins.Select(b => string.Format("\"{0}\"", b.BinName)));
            var flds = new List<string>();

            var setClassFlds = new StringBuilder();
            var setClassFldsConst = new StringBuilder();
            var paramsNewRec = new StringBuilder();
            var dictValuesNewRec = new StringBuilder();
            var fldSeen = new List<string>();

            setClassFlds.AppendLine($"\t\t\tpublic APrimaryKey {ARecord.DefaultASPIKeyName} {{ get; }}");
            setClassFldsConst.AppendLine($"\t\t\t\t\t{ARecord.DefaultASPIKeyName} = new APrimaryKey(this.Aerospike.Key);");

            paramsNewRec.AppendLine($"\t\t\tdynamic {ARecord.DefaultASPIKeyName},");
            
            var generateBinsTask = Task.Run(() =>
            {
                foreach (var setBinType in bins)
                {
                    if (fldSeen.Contains(setBinType.BinName))
                    {
                        continue;
                    }

                    var fldName = Helpers.CheckName(setBinType.BinName, "Bin");
                    var fldType = setBinType.Duplicate || alwaysUseAValues
                                        ? "AValue"
                                        : Helpers.GetRealTypeName(setBinType.DataType, !setBinType.FndAllRecs);
                    var paramType = setBinType.Duplicate
                                        ? "object"
                                        : Helpers.GetRealTypeName(setBinType.DataType);
                    var jsonType = setBinType.DataType == typeof(JsonDocument)
                                        || setBinType.DataType == typeof(List<JsonDocument>)
                                        || setBinType.DataType == typeof(JArray)
                                        || setBinType.DataType == typeof(List<JArray>)
                                        || setBinType.DataType == typeof(JToken)
                                        || setBinType.DataType == typeof(List<JToken>)
                                        || setBinType.DataType == typeof(JValue)
                                        || setBinType.DataType == typeof(List<JValue>)
                                        || Helpers.IsSubclassOfInterface(typeof(IGeoJSONObject), setBinType.DataType)
                                        ? Helpers.GetRealTypeName(setBinType.DataType, !setBinType.FndAllRecs)
                                        : null;

                    flds.Add(fldName);

                    setClassFlds.Append("\t\t\tpublic ");
                    setClassFlds.Append(fldType);
                    setClassFlds.Append(' ');
                    setClassFlds.Append(fldName);
                    setClassFlds.Append("{ get; }");
                    setClassFlds.AppendLine();

                    setClassFldsConst.Append("\t\t\t\t\t");
                    setClassFldsConst.Append(fldName);
                    setClassFldsConst.Append(" = (");
                    setClassFldsConst.Append(fldType);
                    setClassFldsConst.Append(") ");

                    if (setBinType.Duplicate || alwaysUseAValues)
                    {                        
                        if (!string.IsNullOrEmpty(jsonType))
                        {
                            setClassFldsConst.Append($" new AValue(");
                            setClassFldsConst.Append($" Helpers.CastToNativeType(this, \"{fldName}\", typeof({jsonType}), \"{setBinType.BinName}\", this.Aerospike.GetValue(\"");
                            setClassFldsConst.Append(setBinType.BinName);
                            setClassFldsConst.Append("\"))");
                            setClassFldsConst.Append($", \"{setBinType.BinName}\",  \"{fldName}\" );");
                        }
                        else
                        {
                            setClassFldsConst.Append($" new AValue(this.Aerospike.GetValue(\"");
                            setClassFldsConst.Append(setBinType.BinName);
                            setClassFldsConst.Append($"\"), \"{setBinType.BinName}\",  \"{fldName}\" );");
                        }
                    }
                    else if (setBinType.DataType.IsValueType)
                    {
                        setClassFldsConst.Append($" (Helpers.CastToNativeType(this, \"{fldName}\", typeof({fldType}), \"{setBinType.BinName}\", this.Aerospike.GetValue(\"");
                        setClassFldsConst.Append(setBinType.BinName);
                        setClassFldsConst.Append("\"))");
                        setClassFldsConst.Append($" ?? default({fldType}));");
                    }
                    else
                    {
                        setClassFldsConst.Append($" Helpers.CastToNativeType(this, \"{fldName}\", typeof({fldType}), \"{setBinType.BinName}\", this.Aerospike.GetValue(\"");
                        setClassFldsConst.Append(setBinType.BinName);
                        setClassFldsConst.Append("\"));");
                    }

                    if (setBinType.DataType.IsValueType)
                    {
                        paramsNewRec.Append($"\t\t\t\t{paramType}? ");
                    }
                    else
                    {
                        paramsNewRec.Append($"\t\t\t\t{paramType} ");
                    }

                    if (setBinType.Duplicate)
                        setClassFldsConst.Append("\t//Multiple Type Bin");
                    if (!setBinType.FndAllRecs)
                        setClassFldsConst.Append("\t//Bin not found in all records");

                    if (setBinType.Duplicate)
                    {
                        paramsNewRec.AppendLine($"{fldName} = null,\t//Multiple Type Bin");
                    }
                    else
                    {
                        paramsNewRec.AppendLine($"{fldName} = null,");
                    }
                    
                    dictValuesNewRec.AppendLine($"\t\t\t\tif(!({fldName} is null)) dictRec.Add(\"{setBinType.BinName}\",{fldName});");

                    setClassFldsConst.AppendLine();

                    fldSeen.Add(setBinType.BinName);

                    setBinType.Detected = false;
                }
            });

            var generateSIdxTask = Task.Run(() =>
            {
                foreach (var sidx in this.SIndexes)
                {
                    var idxDataType = alwaysUseAValues
                                        ? typeof(AValue)
                                        : bins.FirstOrDefault(b => b.BinName == sidx.Bin && !b.Duplicate)
                                            ?.DataType ?? typeof(AValue);

                    idxProps.AppendLine(sidx.GenerateCode(idxDataType, false));
                }
            });

            Task.WaitAll(generateBinsTask, generateSIdxTask);

            var settClasses = $@"
	public class {this.SafeName}_SetCls : Aerospike.Database.LINQPadDriver.Extensions.SetRecords<{this.SafeName}_SetCls.RecordCls>
	{{
		public {this.SafeName}_SetCls (Aerospike.Database.LINQPadDriver.Extensions.ANamespaceAccess setAccess)
			: base(Aerospike.Database.LINQPadDriver.LPSet.GetSet(""{this.LPnamespace.Name}"", ""{this.Name}""),
						setAccess, 
						""{this.Name}"",
						bins: new string[] {{ {binsString} }})
		{{ }}

		public {this.SafeName}_SetCls ({this.SafeName}_SetCls clone)
			: base(clone)
		{{ }}

		protected override Aerospike.Database.LINQPadDriver.Extensions.ARecord CreateRecord(Aerospike.Database.LINQPadDriver.Extensions.ANamespaceAccess setAccess,
														Aerospike.Client.Key key,
														Aerospike.Client.Record record,
														string[] binNames,
														int binsHashCode,
														Aerospike.Database.LINQPadDriver.Extensions.ARecord.DumpTypes recordView = global::Aerospike.Database.LINQPadDriver.Extensions.ARecord.DumpTypes.Record) => new RecordCls(setAccess, key, record, binNames, binsHashCode, recordView);

        /// <summary>
        /// Puts (Writes) a DB record based on the provided key and bin values.
        /// If a parameter is not provided, the associated bin is not inserted or updated. A parameter value of null is not allowed.
        /// 
        /// Note that if the namespace and/or set is different, this instances&apos;s values are used.
        /// </summary>
        /// <param name=""primaryKey"">
        /// Primary AerospikeKey.
        /// This can be a <see cref=""Client.Key""/>, <see cref=""Value""/>, or <see cref=""Bin""/> object besides a native, collection, etc. value/object.
        /// </param>
        /// <param name=""additionalValues"">
        /// Additional values as a dictionary where the key is the bin and the value is the bin&apos;s value.
        /// </param>
        /// <param name=""writePolicy"">
        /// The write policy. If not provided, the default policy is used.
        /// <seealso cref=""Aerospike.Client.WritePolicy""/>
        /// </param>
        /// <param name=""ttl"">Time-to-live of the record</param>
        public new void PutRec(
{paramsNewRec}				IEnumerable<KeyValuePair<string,object>> additionalValues = null,
				global::Aerospike.Client.WritePolicy writePolicy = null,
				TimeSpan? ttl = null
        )
        {{
            var dictRec = new Dictionary<string,object>( additionalValues
                                                            ?? Enumerable.Empty<KeyValuePair<string,object>>() );

{dictValuesNewRec}

            this.SetAccess.Put(this.SetName, 
                                {ARecord.DefaultASPIKeyName}, 
                                dictRec,                                
                                writePolicy: writePolicy,
                                ttl: ttl);                                                               
        }}

		public new {this.SafeName}_SetCls Clone() => new {this.SafeName}_SetCls(this);

		public class RecordCls : Aerospike.Database.LINQPadDriver.Extensions.ARecord
		{{
			public RecordCls(Aerospike.Database.LINQPadDriver.Extensions.ANamespaceAccess setAccess,
								Aerospike.Client.Key key,
								Aerospike.Client.Record record,
								string[] binNames,
								int binsHashCode,
								Aerospike.Database.LINQPadDriver.Extensions.ARecord.DumpTypes recordView = global::Aerospike.Database.LINQPadDriver.Extensions.ARecord.DumpTypes.Record)
				:base(setAccess, key, record, binNames, recordView, binsHashCode)
			{{
				try {{
{setClassFldsConst}
				}} catch (System.Exception ex) {{
					this.SetException(ex);
					this.SetDumpType(ARecord.DumpTypes.Dynamic);
				}}
			}}           

{setClassFlds}
			override public object ToDump() => this.ToDump( new string[] {{ ""{ARecord.DefaultASPIKeyName}"", {string.Join(',', flds.Select(s => "\"" + s + "\""))} }} );
		}}

{idxProps}
	}}"
    ;//End of Class String

            var setProps = $@"
	public {this.SafeName}_SetCls {this.SafeName} {{ get => new {this.SafeName}_SetCls(this); }}"
            ; //End of property string

            this.CodeCache = (settClasses, setProps, null);
            Interlocked.Exchange(ref nbrCodeUpdates, 0);
            Interlocked.Decrement(ref LPnamespace.nbrCodeUpdates);

            return CodeCache;
        }

        #endregion

        #region Explorer 

        public ExplorerItem CreateExplorerItem()
        {
            var bins = this.BinTypes
                        .OrderBy(b => b.BinName)
                        .Select(b => b.CreateExplorerItem());
            var sIdxs = this.SIndexes
                            .OrderBy(sIdx => sIdx.Name)
                            .Select(sIdx => sIdx.CreateExplorerItem());

            return new ExplorerItem(this.Name,
                                        ExplorerItemKind.QueryableObject,
                                        ExplorerIcon.Schema)
            {
                IsEnumerable = true,
                DragText = $"{this.LPnamespace.SafeName}.{this.SafeName}",
                Children = bins.Concat(sIdxs).ToList()
            };
        }

        #endregion

        public override string ToString()
        {
            return this.LPnamespace?.Name + '.' + this.Name;
        }

        public static LPSet GetSet(string namespaceName, string setName) => SetsBag.FirstOrDefault(s => s.LPnamespace?.Name == namespaceName && s.Name == setName);
    }
}
