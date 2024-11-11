using Aerospike.Client;
using LINQPad.Extensibility.DataContext;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Aerospike.Database.LINQPadDriver
{
    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay}")]
    public sealed partial class LPNamespace : IGenerateCode, ILPExplorer
    {

        private readonly static ConcurrentBag<LPNamespace> LPNamespacesBag = new ConcurrentBag<LPNamespace>();

        public LPNamespace(string name)
        {			
			this.Name = name;
            this.SafeName = Helpers.CheckName(name, "Namespace");
            LPNamespacesBag.Add(this);            
        }

#if NET7_0_OR_GREATER
        [GeneratedRegex("set=(?<setname>[^:;]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        static private partial Regex SetNameRegEx();
        [GeneratedRegex("ns=(?<namespace>[^:;]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        static private partial Regex NameSpaceRegEx();
        [GeneratedRegex(@"^roster=(?<roster>\d+|(?:null)):pending_roster=(?<pending>\d+|(?:null)):observed_nodes=(?<observed>\d+|(?:null))", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
		static private partial Regex NameSpaceRosterRegEx();
#else
        static private readonly Regex setNameRegEx = new Regex("set=(?<setname>[^:;]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static private readonly Regex nameSpaceRegEx = new Regex("ns=(?<namespace>[^:;]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static private readonly Regex nameSpaceRosterRegEx = new Regex(@"^roster=(?<roster>\d+|(?:null)):pending_roster=(?<pending>\d+|(?:null)):observed_nodes=(?<observed>\d+|(?:null))", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        static private Regex SetNameRegEx() => setNameRegEx;
        static private Regex NameSpaceRegEx() => nameSpaceRegEx;
        static private Regex NameSpaceRosterRegEx() => nameSpaceRosterRegEx;
#endif

		public LPNamespace(string name, IEnumerable<string> setAttribs)
            : this(name)
        {			
			/*
             * ns=test:set=demo:objects=4:tombstones=0:memory_data_bytes=0:device_data_bytes=368:truncate_lut=0:sindexes=0:index_populating=false:disable-eviction=false:enable-index=false:stop-writes-count=0
             */
			if(Client.Log.DebugEnabled())
            {
                if (setAttribs is null)
                    Client.Log.Debug($"NS {name} Set Attributes is null");
                else if (setAttribs.Any())
                {
                    foreach(var setAttr in setAttribs)
                    {
                        Client.Log.Debug($"NS {name} {setAttr}");
                    }
                }
                else
                    Client.Log.Debug($"NS {name} Set Attributes is Empty");
            }

            var setNames = from setAttrib in setAttribs
                           let matches = SetNameRegEx().Match(setAttrib)
                           select matches.Groups["setname"].Value;

            var nsSets = setNames
                            .Where(s => !string.IsNullOrEmpty(s))
                            .Select(s => new LPSet(this, s))
                            .ToList();

            nsSets.Add(new LPSet(this));

            this.aSets = nsSets.ToList();
        }

        public LPNamespace(string name, 
                                IEnumerable<string> setAttribs,
                                string binNames,
                                string rosterInfo,
                                string nsConfig)
            : this(name, setAttribs ?? Enumerable.Empty<string>())
        {
			/*
             * bin_names=62,bin_names_quota=65535,addbin,appendbin,prependbin,bbin,lbin,lbin2,lbin3,bbin3,bbin2,putgetbin,asqbin,name,age,B5,audfbin1,bin5,A,listmapbin,bin1,bin2,expirebin,D,B,C,H,E,genbin,hllbin_1,hllbin_2,hllbin_3,testbin,listbin2,listbin1,mapbin2,mapbin1,optintbin,optstringbin,optintbin1,optintbin2,opbbin,ophbin,ophbinother,ophbino,oplistbin,otherbin,opmapbin,bin3,bin4,l2,l1,map_bin,list,tqebin1,tqebin2,foo,password,fltint,listbin,mapbin,blob_data_1,catalog,diffbin
             */
			if(Client.Log.DebugEnabled())
            {
                if (binNames is null)
                    Client.Log.Debug($"NS {name} Bins is null");
                else if(binNames == string.Empty)
                    Client.Log.Debug($"NS {name} Bins is Empty");
                else
                {
                    Client.Log.Debug($"NS {name}: {binNames}");
                }
            }

            var binNameSplit = binNames.Split(',', StringSplitOptions.RemoveEmptyEntries);

            this.bins = binNameSplit.Where(s => !s.Contains('=')).ToList();
            this.safeBins = this.Bins.Select(s => Helpers.CheckName(s, "Bin")).ToList();

			//roster=2887538337:pending_roster=2887538337:observed_nodes=2887538337
			//roster=null:pending_roster=null:observed_nodes=null
			if(Client.Log.DebugEnabled())
			{
				if(rosterInfo is null)
					Client.Log.Debug($"NS {name} Roster Info is null");
				else if(rosterInfo == string.Empty)
					Client.Log.Debug($"NS {name} Roster Info is Empty");
				else
				{
					Client.Log.Debug($"NS Roster {name}: {rosterInfo}");
				}
			}
            if(!string.IsNullOrEmpty(rosterInfo))
            {
                var rosterMatches = NameSpaceRosterRegEx().Match(rosterInfo);

                this.IsStrongConsistencyMode = rosterMatches.Success
                                                && rosterMatches.Groups.ContainsKey("roster")
                                                && !rosterMatches.Groups["roster"].Value.Equals("null");
            }

			if(Client.Log.DebugEnabled())
			{
				if(nsConfig is null)
					Client.Log.Debug($"NS {name} Config is null");
				else if(nsConfig == string.Empty)
					Client.Log.Debug($"NS {name} Config is Empty");
				else
				{
					Client.Log.Debug($"NS Config {name}: {nsConfig}");
				}
			}
			if(!string.IsNullOrEmpty(nsConfig))
			{
				var nsConfigLU = nsConfig.Split(';')
                                    .Select(c => c.Split('='))
                                    .OrderBy(ca => ca[0])
                                    .ToLookup(ca => ca[0]?.Trim(), ca => ca[1].Trim());

                if(nsConfigLU.Contains("strong-consistency"))
                {
                    this.IsStrongConsistencyMode = nsConfigLU["strong-consistency"].Contains("true");
                }
                this.ConfigParams = nsConfigLU;
			}
		}
       
        public LPNamespace(string name,
                            string safename,
                            IEnumerable<string> bins,
                            IEnumerable<string> safebins,
                            IEnumerable<LPSet> sets,
                            IEnumerable<LPSecondaryIndex> sindexes)
        {
			System.Diagnostics.Debugger.Break();

			this.Name = name;
            this.SafeName = safename;
            this.bins = bins.ToList();
            this.safeBins = safebins.ToList();
            this.aSets = sets.ToList();
            this.SIndexes = sindexes;
            LPNamespacesBag.Add(this);            
        }

        /// <summary>
        /// The name of the namespace
        /// </summary>
        public string Name { get; }
		/// <summary>
		/// The namespace name that is safe to use as a C# class name or property
		/// </summary>
		public string SafeName { get; }

		/// <summary>
		/// Gets a value indicating whether this namespace is in strong consistency mode.
		/// </summary>
		/// <value><c>true</c> if this instance is strong consistency mode; otherwise, <c>false</c>.</value>
		public bool IsStrongConsistencyMode { get; }

		/// <summary>
		/// Gets the configuration parameters for this namespace.
		/// </summary>
		/// <value>The configuration parameters or null.</value>
		public ILookup<string,string> ConfigParams {  get; }

		private readonly List<string> bins = new List<string>();
        /// <summary>
        /// The Actual DB Bin Names
        /// </summary>
        public IEnumerable<string> Bins { get => this.bins; }
        
        private readonly List<string> safeBins = new List<string> ();
        /// <summary>
        /// Bin names that are safe to use as C# class name or properties.
        /// </summary>
        public IEnumerable<string> SafeBins { get => this.safeBins; }

        private readonly List<LPSet> aSets = new List<LPSet>();
        /// <summary>
        /// DB Sets
        /// </summary>
        public IEnumerable<LPSet> Sets { get => this.aSets; }

        /// <summary>
        /// DB Secondary Indexes  
        /// </summary>
        public IEnumerable<LPSecondaryIndex> SIndexes { get; internal set; } = Enumerable.Empty<LPSecondaryIndex>();

        public static IEnumerable<LPNamespace> Create(string staticNamespace, IEnumerable<string> staticSets)
        {
            var ns = new LPNamespace(staticNamespace);

            foreach(var setName in staticSets)
            {
                var lpSet = new LPSet(ns, setName);
                ns.aSets.Add(lpSet);
            }
            ns.aSets.Add(new LPSet(ns));

            return new List<LPNamespace> { ns };
        }

        public static IEnumerable<LPNamespace> Create(Client.Connection asConnection, Version dbVersion)
        {
            var setsAttrib = Info.Request(asConnection, "sets");

            string GetNSBins(string nsName)
            {
                if (dbVersion < AerospikeConnection.NoNSBinsRequest)
                    return Info.Request(asConnection, $"bins/{nsName}");

                return string.Empty;
            }

			string GetNSRoster(string nsName)
			{
				//roster=2887538337:pending_roster=2887538337:observed_nodes=2887538337
				//roster=null:pending_roster=null:observed_nodes=null
				if(dbVersion >= AerospikeConnection.NoRosterRequest)
					return Info.Request(asConnection, $"roster:namespace={nsName}");

				return string.Empty;
			}

			string GetNSConfig(string nsName)
			{
				//active-rack=0;allow-ttl-without-nsup=false;auto-revive=false;background-query-max-rps=10000;conflict-resolution-policy=undefined;conflict-resolve-writes=false;default-read-touch-ttl-pct=0;default-ttl=0;disable-cold-start-eviction=false;disable-write-dup-res=false;disallow-expunge=false;disallow-null-setname=false;enable-benchmarks-batch-sub=false;enable-benchmarks-ops-sub=false;enable-benchmarks-read=false;enable-benchmarks-udf=false;enable-benchmarks-udf-sub=false;enable-benchmarks-write=false;enable-hist-proxy=false;evict-hist-buckets=10000;evict-indexes-memory-pct=0;evict-tenths-pct=5;force-long-queries=false;ignore-migrate-fill-d
				if(dbVersion >= AerospikeConnection.NoConfigRequest)
					return Info.Request(asConnection, $"get-config:context=namespace;namespace={nsName}");

				return string.Empty;
			}

			var asNamespaces = (from nsSets in setsAttrib.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                let ns = NameSpaceRegEx().Match(nsSets).Groups["namespace"].Value
                                group nsSets by ns into nsGrp
                                let nsBins = GetNSBins(nsGrp.Key)
                                let nsRoster = GetNSRoster(nsGrp.Key)
                                let nsConfig = GetNSConfig(nsGrp.Key)
                                select new LPNamespace(nsGrp.Key, nsGrp.ToList(), nsBins, nsRoster, nsConfig)).ToList();

            var namespaces = Info.Request(asConnection, "namespaces")?.Split(';');

            if(namespaces != null)
            {
                foreach(var ns in namespaces)
                {
                    if(!asNamespaces.Any(ans => ans.Name == ns))
                        asNamespaces.Add(new LPNamespace(ns, Enumerable.Empty<string>()));
                }
            }


            return asNamespaces.ToArray();
        }

        public LPSet TryAddSet(string setName, IEnumerable<LPSet.BinType> binTypes)
        {
            var fndASet = this.Sets.FirstOrDefault(s => s.Name == setName);
            var updateCnt = true;

            if(fndASet is null)
            {
                fndASet = new LPSet(this, setName, binTypes);
                this.aSets.Add(fndASet);
                Interlocked.Increment(ref nbrCodeUpdates);
                updateCnt = false;
            }

            foreach (var binType in binTypes)
            {
                this.TryAddBin(binType.BinName, updateCnt);
            }
            
            return fndASet;
        }

        public bool TryAddBin(string binName, bool incUpdateCnt = true)
        {
            var fndBin = this.Bins.FirstOrDefault(b => b == binName);

            if (fndBin is null)
            {
                this.bins.Add(binName);
                this.safeBins.Add(Helpers.CheckName(binName, "Bin"));
                if (incUpdateCnt) Interlocked.Increment(ref nbrCodeUpdates);
                return true;
            }

            return false;
        }

        public LPSet TryRemoveSet(string setName, IEnumerable<LPSet.BinType> removeBinTypes)
        {
            var fndASet = this.Sets.FirstOrDefault(s => s.Name == setName);
            
            if (fndASet != null)
            {
                foreach (var removeBinType in removeBinTypes)
                {
                    this.TryRemoveBin(removeBinType.BinName);
                }
            }

            return fndASet;
        }

        public bool TryRemoveBin(string removeBinName, bool incUpdateCnt = true)
        {
            var br = this.bins.Remove(removeBinName);
            var sb = this.safeBins.Remove(Helpers.CheckName(removeBinName, "Bin"));

            if(br || sb)
            {                
                if (incUpdateCnt) Interlocked.Increment(ref nbrCodeUpdates);
                return true;
            }

            return false;
        }

        internal void DetermineUpdateBinsBasedOnSets()
        {            
            var binTypes = this.Sets
                            .SelectMany(s => s.BinTypes)
                            .Distinct(LPSet.BinType.DefaultNameComparer);

            if (binTypes.Any())
            {
                this.bins.Clear();
                this.bins.AddRange(binTypes.Select(b => b.BinName));

                this.safeBins.Clear();
                this.safeBins.AddRange(binTypes.Select(b => Helpers.CheckName(b.BinName, "Bin")));

                var nullSet = this.Sets.First(s => s.IsNullSet);

                nullSet.UpdateTypeBins(this.bins, false);
            }
        }


        #region Code Generation

        public (string classCode, string definePropCode, string createInstanceCode)
           CodeCache
        { get; private set; }

        
        internal long nbrCodeUpdates = 0;
        public bool CodeNeedsUpdating { get => Interlocked.Read(ref nbrCodeUpdates) > 0; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="alwaysUseAValues"></param>
        /// <param name="forceGeneration"></param>
        /// <returns>
        /// classCode -- Code used to define this Namespace&apos;s class
        /// definePropCode -- Code used to define the property used to reference this Namespace
        /// createInstanceCode -- Code used to create an instance of this Namespace
        /// </returns>
        public (string classCode, string definePropCode, string createInstanceCode)
            CodeGeneration(bool alwaysUseAValues, bool forceGeneration = false)
        {           
            if (!this.CodeNeedsUpdating && !forceGeneration && this.CodeCache.classCode != null)
                return this.CodeCache;

            var setProps = new StringBuilder();
            var setClasses = new StringBuilder();
            var binNames = new StringBuilder();

            var generateSetsTask = Task.Run(() =>
            {
                //Code for getting RecordSets for Set. 
                foreach (var set in this.Sets)
                {
                    var (setClass, setProp, ignore) = set.CodeGeneration(alwaysUseAValues);
                    setClasses.AppendLine(setClass);
                    setProps.AppendLine(setProp);
                }
            });

            var generateBinsTask = Task.Run(() =>
            {
                foreach (var binName in this.Bins)
                {
                    binNames.Append('"');
                    binNames.Append(binName);
                    binNames.Append("\", ");
                }
            });

            Task.WaitAll(generateSetsTask, generateBinsTask);

            Interlocked.Exchange(ref nbrCodeUpdates, 0);
            var sc = this.IsStrongConsistencyMode ? "true" : "false";

            return this.CodeCache = ($@"
	public class {this.SafeName}_NamespaceCls : Aerospike.Database.LINQPadDriver.Extensions.ANamespaceAccess
	{{

		public {this.SafeName}_NamespaceCls(System.Data.IDbConnection dbConnection)
			: base(dbConnection,
                    Aerospike.Database.LINQPadDriver.LPNamespace.GetNamepsace(""{this.Name}""), 
                    ""{this.Name}"", 
                    new string[] {{{binNames}}},
                    {sc})
		{{ }}

		public {this.SafeName}_NamespaceCls(Aerospike.Database.LINQPadDriver.Extensions.ANamespaceAccess clone, Aerospike.Client.Expression expression)
			: base(clone, expression)
		{{ }}

        public {this.SafeName}_NamespaceCls(Aerospike.Database.LINQPadDriver.Extensions.ANamespaceAccess clone,
                                            Aerospike.Client.Policy readPolicy = null,
								            Aerospike.Client.WritePolicy writePolicy = null,
								            Aerospike.Client.QueryPolicy queryPolicy = null,
								            Aerospike.Client.ScanPolicy scanPolicy = null)
			: base(clone,
                    readPolicy,
					writePolicy,
					queryPolicy,
					scanPolicy)
		{{ }}

		/// <summary>
		/// Clones the specified instance providing new policies, if provided.
		/// </summary>
		/// <param name=""newReadPolicy"">The new read policy.</param>
		/// <param name=""newWritePolicy"">The new write policy.</param>
		/// <param name=""newQueryPolicy"">The new query policy.</param>
		/// <param name=""newScanPolicy"">The new scan policy.</param>
		/// <returns>New clone of <see cref=""{this.SafeName}_NamespaceCls""/> instance.</returns>
		new public {this.SafeName}_NamespaceCls Clone(Aerospike.Client.Policy newReadPolicy = null,
                                                        Aerospike.Client.WritePolicy newWritePolicy = null,
                                                        Aerospike.Client.QueryPolicy newQueryPolicy = null,
                                                        Aerospike.Client.ScanPolicy newScanPolicy = null)
            => new {this.SafeName}_NamespaceCls(this,
                                                newReadPolicy,
                                                newWritePolicy,
                                                newQueryPolicy,
                                                newScanPolicy);

		public {this.SafeName}_NamespaceCls FilterExpression(Aerospike.Client.Expression expression)
        {{
            return new {this.SafeName}_NamespaceCls(this, expression);
        }}

		public {this.SafeName}_NamespaceCls FilterExpression(Aerospike.Client.Exp exp)
        {{
            return new {this.SafeName}_NamespaceCls(this, Aerospike.Client.Exp.Build(exp));
        }}

		public IAerospikeClient ASClient() => this.AerospikeConnection.AerospikeClient;
		        
		{setClasses}
		{setProps}
	}}",

            //Code to access namespace properties. 
            $@"
		public static {this.SafeName}_NamespaceCls {this.SafeName} {{get; private set; }}",

            //Code to construct namespace instance
            $@"
			{this.SafeName} = new {this.SafeName}_NamespaceCls(dbConnection);"
            );
        }

        #endregion

        #region Explorer 

        public ExplorerItem CreateExplorerItem()
        {
            var isSC = this.IsStrongConsistencyMode ? "-SC" : string.Empty;
            var sc = this.IsStrongConsistencyMode ? " Strong Consistency " : string.Empty;
            var children = this.Sets.Select(s => s.CreateExplorerItem()).ToList();

            if(this.ConfigParams is not null && this.ConfigParams.Any())
            {
                children.Add(new ExplorerItem("Configuration",
                                                ExplorerItemKind.Category,
                                                ExplorerIcon.Box)
                {
                    IsEnumerable = false,
                    DragText = null,
                    Children = this.ConfigParams
                                .Select(c => c.Key + '=' + string.Join(",", c))
                                .Select(s => new ExplorerItem(s, ExplorerItemKind.Parameter, ExplorerIcon.ScalarFunction))
                                .ToList()
                });
			}

			return new ExplorerItem($"{this.Name} ({this.Sets.Count()}{isSC})",
                                    ExplorerItemKind.Property,
                                    ExplorerIcon.Table)
            {
                IsEnumerable = false,
                DragText = this.SafeName,
                Children = children,
                ToolTipText = $"Sets associated with{sc}namespace \"{this.Name}\""
            };
        }

        #endregion

        public override string ToString()
        {
            return this.Name;
        }

        public static LPNamespace GetNamepsace(string namespaceName) => LPNamespacesBag.FirstOrDefault(n => n.Name == namespaceName);

        private string DebuggerDisplay => $"{Name} {Sets.Count()} {Bins.Count()} {this.IsStrongConsistencyMode}";

    }
}
