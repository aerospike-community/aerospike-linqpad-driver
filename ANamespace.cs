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
    public sealed class ANamespace : IGenerateCode
    {

        private readonly static ConcurrentBag<ANamespace> LPNamespacesBag = new ConcurrentBag<ANamespace>();

        public ANamespace(string name)
        {
            this.Name = name;
            this.SafeName = Helpers.CheckName(name, "Namespace");
            LPNamespacesBag.Add(this);
        }

        static private readonly Regex SetNameRegEx = new Regex("set=(?<setname>[^:;]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static private readonly Regex NameSpaceRegEx = new Regex("ns=(?<namespace>[^:;]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public ANamespace(string name, IEnumerable<string> setAttribs)
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
                            .Select(s => new ASet(this, s))
                            .ToList();

            nsSets.Add(new ASet(this));

            this.aSets = nsSets.ToList();
        }

        public ANamespace(string name, IEnumerable<string> setAttribs, string binNames)
            : this(name, setAttribs ?? Enumerable.Empty<string>())
        {
            /*
             * bin_names=62,bin_names_quota=65535,addbin,appendbin,prependbin,bbin,lbin,lbin2,lbin3,bbin3,bbin2,putgetbin,asqbin,name,age,B5,audfbin1,bin5,A,listmapbin,bin1,bin2,expirebin,D,B,C,H,E,genbin,hllbin_1,hllbin_2,hllbin_3,testbin,listbin2,listbin1,mapbin2,mapbin1,optintbin,optstringbin,optintbin1,optintbin2,opbbin,ophbin,ophbinother,ophbino,oplistbin,otherbin,opmapbin,bin3,bin4,l2,l1,map_bin,list,tqebin1,tqebin2,foo,password,fltint,listbin,mapbin,blob_data_1,catalog,diffbin
             */
            var binNameSplit = binNames.Split(',', StringSplitOptions.RemoveEmptyEntries);

            this.bins = binNameSplit.Where(s => !s.Contains('=')).ToList();
            this.safeBins = this.Bins.Select(s => Helpers.CheckName(s, "Bin")).ToList();
        }

        /// <summary>
        /// The name of the namespace
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The namespace name that is safe to use as a C# class name or property
        /// </summary>
        public string SafeName { get; }

        private List<string> bins = new List<string>();
        /// <summary>
        /// The Actual DB Bin Names
        /// </summary>
        public IEnumerable<string> Bins { get => this.bins; }

        private List<string> safeBins = new List<string> ();
        /// <summary>
        /// Bin names that are safe to use as C# class name or properties.
        /// </summary>
        public IEnumerable<string> SafeBins { get => this.safeBins; }

        private readonly List<ASet> aSets = new List<ASet>();
        /// <summary>
        /// DB Sets
        /// </summary>
        public IEnumerable<ASet> Sets { get => this.aSets; }

        /// <summary>
        /// DB Secondary Indexes  
        /// </summary>
        public IEnumerable<ASecondaryIndex> SIndexes { get; internal set; } = Enumerable.Empty<ASecondaryIndex>();

        public static IEnumerable<ANamespace> Create(Client.Connection asConnection)
        {
            var setsAttrib = Info.Request(asConnection, "sets");

            var asNamespaces = (from nsSets in setsAttrib.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                let ns = NameSpaceRegEx.Match(nsSets).Groups["namespace"].Value
                                group nsSets by ns into nsGrp
                                let nsBins = Info.Request(asConnection, $"bins/{nsGrp.Key}")
                                select new ANamespace(nsGrp.Key, nsGrp.ToList(), nsBins)).ToList();

            var namespaces = Info.Request(asConnection, "namespaces")?.Split(';');

            if(namespaces != null)
            {
                foreach(var ns in namespaces)
                {
                    if(!asNamespaces.Any(ans => ans.Name == ns))
                        asNamespaces.Add(new ANamespace(ns));
                }
            }


            return asNamespaces.ToArray();
        }

        public ASet TryAddSet(string setName, IEnumerable<ASet.BinType> binTypes)
        {
            var fndASet = this.Sets.FirstOrDefault(s => s.Name == setName);
            var updateCnt = true;

            if(fndASet is null)
            {
                fndASet = new ASet(this, setName, binTypes);
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
                    Aerospike.Database.LINQPadDriver.ANamespace.GetNamepsace(""{this.Name}""), 
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

        public static ANamespace GetNamepsace(string namespaceName) => LPNamespacesBag.FirstOrDefault(n => n.Name == namespaceName);

        private string DebuggerDisplay => $"{Name} {Sets.Count()} {Bins.Count()}";

    }
}
