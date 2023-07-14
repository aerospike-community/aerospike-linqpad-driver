using Aerospike.Client;
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
    public sealed class LPNamespace : IGenerateCode
    {

        private readonly static ConcurrentBag<LPNamespace> LPNamespacesBag = new ConcurrentBag<LPNamespace>();

        public LPNamespace(string name)
        {
            this.Name = name;
            this.SafeName = Helpers.CheckName(name, "Namespace");
            LPNamespacesBag.Add(this);
        }
       
        static private readonly Regex SetNameRegEx = new Regex("set=(?<setname>[^:;]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static private readonly Regex NameSpaceRegEx = new Regex("ns=(?<namespace>[^:;]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public LPNamespace(string name, IEnumerable<string> setAttribs)
            : this(name)
        {
            /*
             * ns=test:set=demo:objects=4:tombstones=0:memory_data_bytes=0:device_data_bytes=368:truncate_lut=0:sindexes=0:index_populating=false:disable-eviction=false:enable-index=false:stop-writes-count=0
             */
            var setNames = from setAttrib in setAttribs
                           let matches = SetNameRegEx.Match(setAttrib)
                           select matches.Groups["setname"].Value;

            var nsSets = setNames
                            .Where(s => !string.IsNullOrEmpty(s))
                            .Select(s => new LPSet(this, s))
                            .ToList();

            nsSets.Add(new LPSet(this));

            this.aSets = nsSets.ToList();
        }

        public LPNamespace(string name, IEnumerable<string> setAttribs, string binNames)
            : this(name, setAttribs ?? Enumerable.Empty<string>())
        {
            /*
             * bin_names=62,bin_names_quota=65535,addbin,appendbin,prependbin,bbin,lbin,lbin2,lbin3,bbin3,bbin2,putgetbin,asqbin,name,age,B5,audfbin1,bin5,A,listmapbin,bin1,bin2,expirebin,D,B,C,H,E,genbin,hllbin_1,hllbin_2,hllbin_3,testbin,listbin2,listbin1,mapbin2,mapbin1,optintbin,optstringbin,optintbin1,optintbin2,opbbin,ophbin,ophbinother,ophbino,oplistbin,otherbin,opmapbin,bin3,bin4,l2,l1,map_bin,list,tqebin1,tqebin2,foo,password,fltint,listbin,mapbin,blob_data_1,catalog,diffbin
             */
            var binNameSplit = binNames.Split(',', StringSplitOptions.RemoveEmptyEntries);

            this.bins = binNameSplit.Where(s => !s.Contains('=')).ToList();
            this.safeBins = this.Bins.Select(s => Helpers.CheckName(s, "Bin")).ToList();
        }
       
        public LPNamespace(string name,
                            string safename,
                            IEnumerable<string> bins,
                            IEnumerable<string> safebins,
                            IEnumerable<LPSet> sets,
                            IEnumerable<LPSecondaryIndex> sindexes)
        {
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

        public static IEnumerable<LPNamespace> Create(Client.Connection asConnection)
        {
            var setsAttrib = Info.Request(asConnection, "sets");

            var asNamespaces = (from nsSets in setsAttrib.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                let ns = NameSpaceRegEx.Match(nsSets).Groups["namespace"].Value
                                group nsSets by ns into nsGrp
                                let nsBins = Info.Request(asConnection, $"bins/{nsGrp.Key}")
                                select new LPNamespace(nsGrp.Key, nsGrp.ToList(), nsBins)).ToList();

            var namespaces = Info.Request(asConnection, "namespaces")?.Split(';');

            if(namespaces != null)
            {
                foreach(var ns in namespaces)
                {
                    if(!asNamespaces.Any(ans => ans.Name == ns))
                        asNamespaces.Add(new LPNamespace(ns));
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

            return this.CodeCache = ($@"
	public class {this.SafeName}_NamespaceCls : Aerospike.Database.LINQPadDriver.Extensions.ANamespaceAccess
	{{

		public {this.SafeName}_NamespaceCls(System.Data.IDbConnection dbConnection)
			: base(dbConnection,
                    Aerospike.Database.LINQPadDriver.LPNamespace.GetNamepsace(""{this.Name}""), 
                    ""{this.Name}"", 
                    new string[] {{{binNames}}})
		{{ }}

		public {this.SafeName}_NamespaceCls(Aerospike.Database.LINQPadDriver.Extensions.ANamespaceAccess clone, Aerospike.Client.Expression expression)
			: base(clone, expression)
		{{ }}

		public {this.SafeName}_NamespaceCls FilterExpression(Aerospike.Client.Expression expression)
        {{
            return new {this.SafeName}_NamespaceCls(this, expression);
        }}

		public {this.SafeName}_NamespaceCls FilterExpression(Aerospike.Client.Exp exp)
        {{
            return new {this.SafeName}_NamespaceCls(this, Aerospike.Client.Exp.Build(exp));
        }}

		public static implicit operator AerospikeClient({this.SafeName}_NamespaceCls ns) => ns.AerospikeConnection.AerospikeClient;
		        
		{setClasses}
		{setProps}
	}}",

            //Code to access namespace properties. 
            $@"
		public {this.SafeName}_NamespaceCls {this.SafeName} {{get; }}",

            //Code to construct namespace instance
            $@"
			this.{this.SafeName} = new {this.SafeName}_NamespaceCls(dbConnection);"
            );
        }

        #endregion

        public override string ToString()
        {
            return this.Name;
        }

        public static LPNamespace GetNamepsace(string namespaceName) => LPNamespacesBag.FirstOrDefault(n => n.Name == namespaceName);

        private string DebuggerDisplay => $"{Name} {Sets.Count()} {Bins.Count()}";

    }
}
