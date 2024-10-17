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
using static LINQPad.Util.ActiveDirectory;

namespace Aerospike.Database.LINQPadDriver
{
    [System.Diagnostics.DebuggerDisplay("{Name}")]
    public sealed class LPSet : IGenerateCode
    {        
        public class BinType : ILPExplorer, IEqualityComparer<BinType>
        {            
            public BinType(string name, Type type, bool dup, bool allRecs, 
                                bool detected = false,
                                bool isFK = false)
            {
                this.BinName = name;
                this.FndAllRecs = allRecs;
                this.DataType = type;
                this.Duplicate = dup;
                this.Detected = detected;
                this.IsFK = isFK;
			}

			internal BinType(BinType binType,
								string fkSetName)
			{
				this.BinName = binType.BinName;
				this.FndAllRecs = binType.FndAllRecs;
				this.DataType = binType.DataType;
				this.Duplicate = binType.Duplicate;
				this.Detected = binType.Detected;
				this.IsFK = binType.IsFK;
                this.FKSetNameBin = binType.FKSetNameBin;
				this.FKSetname = fkSetName;
			}

			public readonly string BinName;
            public readonly Type DataType;
            public readonly bool Duplicate;
            public readonly bool FndAllRecs;
            
            /// <summary>
            /// True if the bin was found after the initial scan of the set.
            /// </summary>            
            public bool Detected { get; set; }

			public bool IsFK { get; internal set; }
            public string FKSetname { get; set; }
            public string FKSetNameBin {  get; set; }

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
                    => new ExplorerItem(this.GenerateExplorerName(),
                                            ExplorerItemKind.Schema,
										    this.IsFK
                                                ? ExplorerIcon.OneToOne
												: ExplorerIcon.Column)
                        {
                            IsEnumerable = false,
                            DragText = this.BinName
                        };            

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

            public string GenerateCode()
                => $"new Aerospike.Database.LINQPadDriver.LPSet.BinType(\"{this.BinName}\",typeof({Helpers.GetRealTypeName(this.DataType)}),{this.Duplicate.ToString().ToLower()},{this.FndAllRecs.ToString().ToLower()},{this.Detected.ToString().ToLower()},{this.IsFK.ToString().ToLower()}){{FKSetname=\"{this.FKSetname}\", FKSetNameBin=\"{this.FKSetNameBin}\"}}";

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
			if(this.SafeName == NullSetName)
				this.SafeName += "_User";
			this.IsNullSet = this.Name == NullSetName;
            SetsBag.Add(this);
        }

        public LPSet(LPNamespace aNamespace, string name, IEnumerable<BinType> binTypes)
        {
            this.LPnamespace = aNamespace;
            this.Name = name;
            this.SafeName = Helpers.CheckName(name, "Set");
            this.binTypes = binTypes?.Where(b => b.DataType != null).ToList() ?? new List<BinType>();
            this.IsNullSet = this.Name == NullSetName;
            SetsBag.Add(this);
			this.DetermineIsVectorIdx();
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
		    this.DetermineIsVectorIdx();
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

		#region Vector
		static readonly internal string VectorFKSetBin = null;
        static readonly internal string[] VectorFKBins = Array.Empty<string>(); // new string[] { "vectorDigest", "neighbors" };
		static readonly private string[] VectorBins = new string[] { "vectorDigest", "vector", "neighbors", "indexIdWithTs" };
        
        private void DetermineIsVectorIdx()
        {
            if(this.IsNullSet)
            {
				this.IsVectorIdx = false;
			}
            else
            {
                this.IsVectorIdx = VectorBins.All(vb => this.binTypes.Any(b => b.BinName == vb));
                if(this.IsVectorIdx && VectorFKBins.Length > 0)
                {
                    foreach(var binType in this.binTypes)
                    {
                        binType.IsFK = VectorFKBins.Any(f => f == binType.BinName);                        
					}
                }
            }            
		}

        public IEnumerable<BinType> GetFKBins() => this.BinTypes.Where(b => b.IsFK);

		public bool IsVectorIdx
        {
            get;
            private set;
		}
		#endregion

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

        public Exception LastException { get; internal set; }

        internal void GetRecordBins(GetSetBins getBins,
                                        bool determineDocType,
                                        int maxRecords,
                                        int minRecs,
                                        bool updateCntd = true)
        {
            lock (binTypes)
            {
                this.binTypes = getBins.Get(this.LPnamespace.Name, this.Name, determineDocType, maxRecords, minRecs);
                this.DetermineIsVectorIdx();

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
                this.DetermineIsVectorIdx();                
            }
        }

        public bool AddBin(string binName, Type dataType)
        {
            bool dup;

            lock(binTypes)
            {
                dup = binTypes.Any(b => b.BinName == binName && b.DataType == dataType);

                this.binTypes.Add(new BinType(binName, dataType, dup, false, true));
                this.DetermineIsVectorIdx();

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

            public {SafeName}_SetCls({SafeName}_SetCls clone,
							            Aerospike.Client.Policy readPolicy = null,
							            Aerospike.Client.WritePolicy writePolicy = null,
							            Aerospike.Client.QueryPolicy queryPolicy = null,
							            Aerospike.Client.ScanPolicy scanPolicy = null)
                : base(clone, readPolicy, writePolicy, queryPolicy, scanPolicy)
            {{ }}

		    /// <summary>
		    /// Clones the specified instance providing new policies, if provided.
		    /// </summary>
		    /// <param name=""newReadPolicy"">The new read policy.</param>
		    /// <param name=""newWritePolicy"">The new write policy.</param>
		    /// <param name=""newQueryPolicy"">The new query policy.</param>
		    /// <param name=""newScanPolicy"">The new scan policy.</param>
		    /// <returns>New clone of <see cref=""{SafeName}_SetCls""/> instance.</returns>
		    new public {SafeName}_SetCls Clone(Aerospike.Client.Policy newReadPolicy = null,
								                Aerospike.Client.WritePolicy newWritePolicy = null,
								                Aerospike.Client.QueryPolicy newQueryPolicy = null,
								                Aerospike.Client.ScanPolicy newScanPolicy = null)
			    => new {SafeName}_SetCls(this,
								            newReadPolicy,
								            newWritePolicy,
								            newQueryPolicy,
								            newScanPolicy);

            public {this.SafeName}_SetCls ({this.SafeName}_SetCls clone)
			: base(clone)
		    {{ }}

            /// <summary>
		    /// Initializes a new instance of <see cref=""{this.SafeName}_SetCls""/> as an Aerospike transactional unit.
		    /// If <see cref=""SetRecords.Commit""/> method is not called the server will abort (rollback) this transaction.
		    /// </summary>
		    /// <param name=""baseSet"">Base Aerospike Set instance</param>
		    /// <param name=""txn"">
		    /// The Aerospike <see cref=""Txn""/> instance or null to create a new transactional unit.
		    /// </param>
		    /// <seealso cref=""SetRecords.CreateTransaction""/>
		    /// <seealso cref=""SetRecords.Commit""/>
		    /// <seealso cref=""SetRecords.Abort""/>
		    public {this.SafeName}_SetCls(SetRecords baseSet, Aerospike.Client.Txn txn)
                : base(baseSet, txn)
            {{ }}

            /// <summary>
		    /// Creates an Aerospike transaction where all operations will be included in this transactional unit.
		    /// </summary>
		    /// <param name=""txn"">
		    /// If provided, <see cref=""Aerospike.Client.Txn""> instance is used instead of creating a new transaction instance.
		    /// </param>
		    /// <returns>Transaction Set instance</returns>
		    /// <seealso cref=""SetRecords.Commit""/>
		    /// <seealso cref=""SetRecords.Abort""/>
		    public new {this.SafeName}_SetCls CreateTransaction(Aerospike.Client.Txn txn = null)
                => new (this, txn);
			
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
			var propValuesUpdateRec = new StringBuilder();
			var binIEnums = new StringBuilder();
            var fldSeen = new List<string>();            

            setClassFlds.AppendLine($"\t\t\tpublic APrimaryKey {ARecord.DefaultASPIKeyName} {{ get; private set; }}");
            setClassFldsConst.AppendLine($"\t\t\t\t\t{ARecord.DefaultASPIKeyName} = new APrimaryKey(this.Aerospike.Key);");

            paramsNewRec.AppendLine($"\t\t\tdynamic {ARecord.DefaultASPIKeyName},");
            
            void ObtainConvertBinValue(StringBuilder statement, 
                                            BinType setBinType,
                                            string getStmt, 
                                            string fldName, 
                                            string fldType, 
                                            string jsonType,
                                            string aRecord,
                                            bool makeNullable = false)
            {
                statement.Append(" = (");
                statement.Append(fldType);
                if (makeNullable)
                    statement.Append('?');
                statement.Append(") ");

                if (setBinType.Duplicate || alwaysUseAValues)
                {
                    if (!string.IsNullOrEmpty(jsonType))
                    {
                        statement.Append($" new AValue(");
                        statement.Append($" Helpers.CastToNativeType({aRecord}, \"{fldName}\", typeof({jsonType}), \"{setBinType.BinName}\", {getStmt}.GetValue(\"");
                        statement.Append(setBinType.BinName);
                        statement.Append("\"))");
                        statement.Append($", \"{setBinType.BinName}\",  \"{fldName}\" );");
                    }
                    else
                    {
                        statement.Append($" new AValue({getStmt}.GetValue(\"");
                        statement.Append(setBinType.BinName);
                        statement.Append($"\"), \"{setBinType.BinName}\",  \"{fldName}\" );");
                    }
                }
                else if (setBinType.DataType.IsValueType && !makeNullable)
                {
                    statement.Append($" (Helpers.CastToNativeType({aRecord}, \"{fldName}\", typeof({fldType}), \"{setBinType.BinName}\", {getStmt}.GetValue(\"");
                    statement.Append(setBinType.BinName);
                    statement.Append("\"))");
                    statement.Append($" ?? default({fldType}));");
                }
                else
                {
                    statement.Append($" Helpers.CastToNativeType({aRecord}, \"{fldName}\", typeof({fldType}), \"{setBinType.BinName}\",  {getStmt} .GetValue(\"");
                    statement.Append(setBinType.BinName);
                    statement.Append("\"));");
                }
            }

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
                    setClassFlds.Append("{ get; private set; }");
                    setClassFlds.AppendLine();

                    setClassFldsConst.Append("\t\t\t\t\t");
                    setClassFldsConst.Append(fldName);
                    
                    ObtainConvertBinValue(setClassFldsConst, setBinType, "this.Aerospike", fldName, fldType, jsonType, "this");
                    
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
                    
                    {
                        var useFldType = fldType.StartsWith("Nullable<")
                                            ? paramType
                                            : fldType;
                        binIEnums.Append($@"
        /// <summary>
        /// Returns a collection of existing values for this set&apos;s Bin.
        /// </summary>
        /// <return>
        /// A collection of bin values.
        /// </return>
        public IEnumerable<{useFldType}> Get{fldName}Values()
        {{
            var queryPolicy = new QueryPolicy(this.DefaultQueryPolicy);
            var stmt = new Statement();

            stmt.SetNamespace(this.Namespace);
            stmt.SetSetName(this.SetName);
            stmt.SetBinNames(""{setBinType.BinName}"");

            using var recordset = this.SetAccess.AerospikeConnection
                                   .AerospikeClient
                                   .Query(queryPolicy, stmt);
            {{
                while (recordset.Next())
                {{
                    var value "

                        );

                        ObtainConvertBinValue(binIEnums,
                                                setBinType,
                                                "recordset.Record",
                                                fldName,
                                                useFldType,
                                                jsonType,
                                                "null",
                                                makeNullable: true);

                        if (useFldType == "AValue")
                        {
                            binIEnums.AppendLine($@"
                    if (value is not null && !value.IsEmpty)
                        yield return value;"
                            );
                        }
                        else if (setBinType.DataType.IsValueType)
                        {
                            binIEnums.AppendLine($@"
                    if (value.HasValue)
                        yield return value.Value;"
                            );
                        }
                        else
                        {
                            binIEnums.AppendLine($@"
                    if (value is not null)
                        yield return value;"
                            );
                        }

                    binIEnums.AppendLine(@"
                }
            }
        }"
                        );

                    }

                    dictValuesNewRec.AppendLine($"\t\t\t\tif({fldName} is not null) dictRec.Add(\"{setBinType.BinName}\",{fldName});");
                    propValuesUpdateRec.AppendLine($"\t\t\t\tif({fldName} is not null) base.SetValue(\"{setBinType.BinName}\", {fldName});");
                
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

            string setValuesParams = paramsNewRec.ToString().Trim();

			{
                var eopComma = setValuesParams.IndexOf(',');

                setValuesParams = setValuesParams[(eopComma+1)..].Trim();

                eopComma = setValuesParams.LastIndexOf(",");
                setValuesParams = setValuesParams[..eopComma];
			}

            var fkDefs = new StringBuilder();

            if(this.IsVectorIdx)
            {
                var binTypeDefs = new StringBuilder();
                foreach(var fkBin in this.GetFKBins())
                {
					binTypeDefs.Append(fkBin.GenerateCode());
					binTypeDefs.AppendLine(",");
				}
                if(binTypeDefs.Length > 0)
                {
                    fkDefs.AppendLine("this.FKBins = new Aerospike.Database.LINQPadDriver.LPSet.BinType[] {");
                    fkDefs.AppendLine(binTypeDefs.ToString());
                    fkDefs.AppendLine("};");
				}
            }


            var settClasses = $@"
	public class {this.SafeName}_SetCls : Aerospike.Database.LINQPadDriver.Extensions.SetRecords<{this.SafeName}_SetCls.RecordCls>
	{{
		public {this.SafeName}_SetCls (Aerospike.Database.LINQPadDriver.Extensions.ANamespaceAccess setAccess)
			: base(Aerospike.Database.LINQPadDriver.LPSet.GetSet(""{this.LPnamespace.Name}"", ""{this.Name}""),
						setAccess, 
						""{this.Name}"",
						bins: new string[] {{ {binsString} }})
		{{
            {fkDefs}
        }}

		public {SafeName}_SetCls({SafeName}_SetCls clone,
							        Aerospike.Client.Policy readPolicy = null,
							        Aerospike.Client.WritePolicy writePolicy = null,
							        Aerospike.Client.QueryPolicy queryPolicy = null,
							        Aerospike.Client.ScanPolicy scanPolicy = null)
                : base(clone, readPolicy, writePolicy, queryPolicy, scanPolicy)
        {{ }}

		/// <summary>
		/// Clones the specified instance providing new policies, if provided.
		/// </summary>
		/// <param name=""newReadPolicy"">The new read policy.</param>
		/// <param name=""newWritePolicy"">The new write policy.</param>
		/// <param name=""newQueryPolicy"">The new query policy.</param>
		/// <param name=""newScanPolicy"">The new scan policy.</param>
		/// <returns>New clone of <see cref=""{SafeName}_SetCls""/> instance.</returns>
		new public {SafeName}_SetCls Clone(Aerospike.Client.Policy newReadPolicy = null,
								            Aerospike.Client.WritePolicy newWritePolicy = null,
								            Aerospike.Client.QueryPolicy newQueryPolicy = null,
								            Aerospike.Client.ScanPolicy newScanPolicy = null)
			=> new {SafeName}_SetCls(this,
								        newReadPolicy,
								        newWritePolicy,
								        newQueryPolicy,
								        newScanPolicy);

        /// <summary>
		/// Initializes a new instance of <see cref=""{this.SafeName}_SetCls""/> as an Aerospike transactional unit.
		/// If <see cref=""SetRecords.Commit""/> method is not called the server will abort (rollback) this transaction.
		/// </summary>
		/// <param name=""baseSet"">Base Aerospike Set instance</param>
		/// <param name=""txn"">
		/// The Aerospike <see cref=""Txn""/> instance or null to create a new transactional unit.
		/// </param>
		/// <seealso cref=""SetRecords.CreateTransaction""/>
		/// <seealso cref=""SetRecords.Commit""/>
		/// <seealso cref=""SetRecords.Abort""/>
		public {this.SafeName}_SetCls(SetRecords baseSet, Aerospike.Client.Txn txn)
            : base(baseSet, txn)
        {{ }}

        /// <summary>
		/// Creates an Aerospike transaction where all operations will be included in this transactional unit.
		/// </summary>
		/// <param name=""txn"">
		/// If provided, <see cref=""Aerospike.Client.Txn""> instance is used instead of creating a new transaction instance.
		/// </param>
		/// <returns>Transaction Set instance</returns>
		/// <seealso cref=""SetRecords.Commit""/>
		/// <seealso cref=""SetRecords.Abort""/>
		public new {this.SafeName}_SetCls CreateTransaction(Aerospike.Client.Txn txn = null)
            => new (this, txn);

		protected override Aerospike.Database.LINQPadDriver.Extensions.ARecord CreateRecord(Aerospike.Database.LINQPadDriver.Extensions.ANamespaceAccess setAccess,
														Aerospike.Client.Key key,
														Aerospike.Client.Record record,
														string[] binNames,
														int binsHashCode,
														Aerospike.Database.LINQPadDriver.Extensions.ARecord.DumpTypes recordView = global::Aerospike.Database.LINQPadDriver.Extensions.ARecord.DumpTypes.Record,
                                                        IEnumerable<Aerospike.Database.LINQPadDriver.LPSet.BinType> fkBins = null)
                            => new RecordCls(setAccess, key, record, binNames, binsHashCode, recordView, fkBins: fkBins);

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
        /// <param name=""additionalBinValues"">
        /// Additional bins and values as a dictionary where the key is the bin name and the value is the bin&apos;s value.
        /// </param>
        /// <param name=""writePolicy"">
        /// The write policy. If not provided, the default policy is used.
        /// <seealso cref=""Aerospike.Client.WritePolicy""/>
        /// </param>
        /// <param name=""ttl"">Time-to-live of the record</param>
        public new void PutRec(
{paramsNewRec}				IEnumerable<KeyValuePair<string,object>> additionalBinValues = null,
				global::Aerospike.Client.WritePolicy writePolicy = null,
				TimeSpan? ttl = null
        )
        {{
            var dictRec = new Dictionary<string,object>( additionalBinValues
                                                            ?? Enumerable.Empty<KeyValuePair<string,object>>() );

{dictValuesNewRec}

            this.SetAccess.Put(this.SetName, 
                                {ARecord.DefaultASPIKeyName}, 
                                dictRec,                                
                                writePolicy: writePolicy,
                                ttl: ttl);                                                               
        }}

        {binIEnums}

		public new {this.SafeName}_SetCls Clone() => new {this.SafeName}_SetCls(this);

		public class RecordCls : Aerospike.Database.LINQPadDriver.Extensions.ARecord
		{{
			public RecordCls(Aerospike.Database.LINQPadDriver.Extensions.ANamespaceAccess setAccess,
								Aerospike.Client.Key key,
								Aerospike.Client.Record record,
								string[] binNames,
								int binsHashCode,
								Aerospike.Database.LINQPadDriver.Extensions.ARecord.DumpTypes recordView = global::Aerospike.Database.LINQPadDriver.Extensions.ARecord.DumpTypes.Record,
                                IEnumerable<Aerospike.Database.LINQPadDriver.LPSet.BinType> fkBins = null)
				:base(setAccess, key, record, binNames, recordView, binsHashCode, fkBins: fkBins)
			{{
				RefreshFromDBRecord();
			}}
            
            public RecordCls(RecordCls clone)
                :base(clone)
            {{
				RefreshFromDBRecord();
			}}

            /// <summary>
            /// Updates this instance based on the DB Record. 
            /// This does not re-query the DB.
            /// To perform this, call <see cref=""Refresh(Policy)""/> 
            /// </summary>
            /// <returns>
            /// Returns the updated instance. 
            /// </returns>            
            /// <seealso cref=""Refresh(Policy)""/>            
            public RecordCls RefreshFromDBRecord()
            {{
                try {{
{setClassFldsConst}
				}} catch (System.Exception ex) {{
					this.SetException(ex);
					this.SetDumpType(ARecord.DumpTypes.Dynamic);
				}}
                return this;
            }}

{setClassFlds}

            /// <summary>
            /// Sets values based on current record bin properties.
            /// </summary>
            /// <returns>
            /// Returns the updated record. 
            /// </returns>
            /// <seealso cref=""this[string]""/>
            /// <seealso cref=""BinExists(string)""/>
            /// <seealso cref=""GetValue(string)""/>
            /// <seealso cref=""Refresh(Policy)""/>
            /// <seealso cref=""Update(WritePolicy)""/>
            public RecordCls SetValues(
{setValuesParams}
)
            {{
{propValuesUpdateRec}

                return RefreshFromDBRecord();                
            }}

            /// <inheritdoc/>
            public new RecordCls Refresh(Policy policy = null) {{
                var record = this.SetAccess
			                    .AerospikeConnection
				                .AerospikeClient
                                .Get(policy, this.Aerospike.Key);
                return new RecordCls(this.SetAccess,
                                            this.Aerospike.Key,
                                            record,
                                            this.Aerospike.BinNames,
                                            this.BinsHashCode,
                                            this.DumpType);
            }}

            /// <inheritdoc/>
            public new RecordCls Update(WritePolicy writePolicy = null) => (RecordCls) base.Update(writePolicy);

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
            var name = this.Name;

            if(this.LastException is not null)
            {
                name += " (Exception)";
            }

            return new ExplorerItem(name,
                                        ExplorerItemKind.QueryableObject,
                                        this.IsVectorIdx ? ExplorerIcon.LinkedDatabase : ExplorerIcon.Schema)
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
