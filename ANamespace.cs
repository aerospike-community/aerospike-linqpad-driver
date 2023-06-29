using Aerospike.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Aerospike.Database.LINQPadDriver
{
    /// <summary>
    /// 
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay}")]
    public sealed class ANamespace
    {
        public ANamespace(string name)
        {
            this.Name = name;
            this.SafeName = Helpers.CheckName(name, "Namespace");
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

            this.Sets = nsSets.ToArray();
        }

        public ANamespace(string name, IEnumerable<string> setAttribs, string binNames)
            : this(name, setAttribs ?? Enumerable.Empty<string>())
        {
            /*
             * bin_names=62,bin_names_quota=65535,addbin,appendbin,prependbin,bbin,lbin,lbin2,lbin3,bbin3,bbin2,putgetbin,asqbin,name,age,B5,audfbin1,bin5,A,listmapbin,bin1,bin2,expirebin,D,B,C,H,E,genbin,hllbin_1,hllbin_2,hllbin_3,testbin,listbin2,listbin1,mapbin2,mapbin1,optintbin,optstringbin,optintbin1,optintbin2,opbbin,ophbin,ophbinother,ophbino,oplistbin,otherbin,opmapbin,bin3,bin4,l2,l1,map_bin,list,tqebin1,tqebin2,foo,password,fltint,listbin,mapbin,blob_data_1,catalog,diffbin
             */
            var binNameSplit = binNames.Split(',', StringSplitOptions.RemoveEmptyEntries);

            this.Bins = binNameSplit.Where(s => !s.Contains('=')).ToArray();
            this.SafeBins = this.Bins.Select(s => Helpers.CheckName(s, "Bin")).ToArray();
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
        /// The Actual DB Bin Names
        /// </summary>
        public IEnumerable<string> Bins { get; } = Enumerable.Empty<string>();
        /// <summary>
        /// Bin names that are safe to use as C# class name or properties.
        /// </summary>
        public IEnumerable<string> SafeBins { get; } = Enumerable.Empty<string>();
        /// <summary>
        /// DB Sets
        /// </summary>
        public IEnumerable<ASet> Sets { get; } = Enumerable.Empty<ASet>();
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


        #region Code Generation

        public (string nsClass, string nsPropAccess, string nsConstructInstance) 
            GenerateCode(bool alwaysUseAValues)
        {
            var setProps = new StringBuilder();
            var setClasses = new StringBuilder();
            var binNames = new StringBuilder();

            //Code for getting RecordSets for Set. 
            foreach (var set in this.Sets)
            {
                var (setClass, setProp) = set.GenerateCode(alwaysUseAValues);
                setClasses.AppendLine(setClass);
                setProps.AppendLine(setProp);
            }

            foreach (var binName in this.Bins)
            {
                binNames.Append('"');
                binNames.Append(binName);
                binNames.Append("\", ");
            }

            return ($@"
	public class {this.SafeName}_NamespaceCls : Aerospike.Database.LINQPadDriver.Extensions.ANamespaceAccess
	{{

		public {this.SafeName}_NamespaceCls(System.Data.IDbConnection dbConnection)
			: base(dbConnection, ""{this.Name}"", new string[] {{{binNames}}})
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

        private string DebuggerDisplay => $"{Name} {Sets.Count()} {Bins.Count()}";

    }
}
