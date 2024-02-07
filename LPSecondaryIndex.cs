using Aerospike.Client;
using Aerospike.Database.LINQPadDriver.Extensions;
using LINQPad.Extensibility.DataContext;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Aerospike.Database.LINQPadDriver
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay}")]
    public sealed partial class LPSecondaryIndex : ILPExplorer
    {
        
        public LPSecondaryIndex(string name, 
                                LPNamespace aNamespace,
                                LPSet set,
                                string bin,
                                string type,
                                string indexType, 
                                string context)
        {
            this.Name = name;
            this.SafeName = Helpers.CheckName(name, "Idx");            
            this.Namespace = aNamespace;
            this.Set = set;
            this.Bin = bin;
            this.Type = type;
            this.IndexType = indexType;
            this.Context = context == "null" ? null : context;
        }


        /// <summary>
        /// ns=test:indexname=State_index:set=players:bin=State:type=string:indextype=default:context=null:state=RW;
        /// ns=test:indexname=idx_list_map_bin_subobj:set=expressionExp:bin=map_bin:type=numeric:indextype=mapvalues:context=[list_value(*):state=RW
        /// </summary>
#if NET7_0_OR_GREATER
        [GeneratedRegex("ns=(?<namespace>[^:;]+):indexname=(?<indexname>[^:;]+):set=(?<setname>[^:;]+):bin=(?<binname>[^:;]+):type=(?<type>[^:;]+):indextype=(?<indextype>[^:;]+):context=(?<context>[^:;]+)",
                            RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        static private partial Regex IdxRegEx();
#else
        static private readonly Regex idxRegEx = new Regex("ns=(?<namespace>[^:;]+):indexname=(?<indexname>[^:;]+):set=(?<setname>[^:;]+):bin=(?<binname>[^:;]+):type=(?<type>[^:;]+):indextype=(?<indextype>[^:;]+):context=(?<context>[^:;]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static private Regex IdxRegEx() => idxRegEx;
#endif

        /// <summary>
        /// The name of the DB Secondary Index name
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The safe name of the index that can be used in a C# class name or property
        /// </summary>
        public string SafeName { get; }   
        /// <summary>
        /// Associated namespace
        /// </summary>
        public LPNamespace Namespace { get; }
        /// <summary>
        /// Associated Set
        /// </summary>
        public LPSet Set { get; }
        /// <summary>
        /// The bin used to maintain the secondary index
        /// </summary>
        public string Bin { get; }
        /// <summary>
        /// The bin DB data type
        /// </summary>
        public string Type { get; }

        public string IndexType { get; }

        public string Context { get; }

        public static IEnumerable<LPSecondaryIndex> Create(Client.Connection asConnection, IEnumerable<LPNamespace> namespaces)
        {
            LPSet FindSet(LPNamespace ns, string set)
            {
                return ns?.Sets?.FirstOrDefault(s => s.Name == set);
            }
            
            var idxsAttrib = Info.Request(asConnection, "sindex");
            
            var idxs = (from nsSetIdx in idxsAttrib.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                let match = IdxRegEx().Match(nsSetIdx)
                                let ns = match.Groups["namespace"].Value
                                let set = match.Groups["setname"].Value
                                let bin = match.Groups["binname"].Value
                                let idxName = match.Groups["indexname"].Value
                                let type = match.Groups["type"].Value
                                let idxType = match.Groups["indextype"].Value
                                let context = match.Groups["context"].Value
                                let aNamespace = namespaces.FirstOrDefault(n => n.Name == ns)
                                let aSet = FindSet(aNamespace,
                                                        set == "NULL" || string.IsNullOrEmpty(set)
                                                            ? LPSet.NullSetName
                                                            : set)
                                select new LPSecondaryIndex(idxName,
                                                                aNamespace,
                                                                aSet,
                                                                bin,
                                                                type,
                                                                idxType,
                                                                context));

            return idxs.ToArray();
        }

        //Aerospike.Database.LINQPadDriver.Extensions.ARecord
        public string GenerateCode(Type idxDataType, bool useARecord)
        {
            if(useARecord)
                return $@"
		public Aerospike.Database.LINQPadDriver.Extensions.ASecondaryIndexAccess {this.SafeName} 
							{{ get => new Aerospike.Database.LINQPadDriver.Extensions.ASecondaryIndexAccess(this, ""{this.Name}"", ""{this.Bin}"", ""{this.Type}"", ""{this.IndexType}""); }}
";

            return $@"
		public Aerospike.Database.LINQPadDriver.Extensions.ASecondaryIndexAccess<RecordCls> {this.SafeName} 
							{{ get => new Aerospike.Database.LINQPadDriver.Extensions.ASecondaryIndexAccess<RecordCls>(this, ""{this.Name}"", ""{this.Bin}"", ""{this.Type}"", ""{this.IndexType}"", typeof({Helpers.GetRealTypeName(idxDataType ?? typeof(AValue))})); }}
";
        }

        public ExplorerItem CreateExplorerItem()
        {
            static string DetermineContext(string context) => string.IsNullOrEmpty(context) ? string.Empty : ":" + context;

            return new ExplorerItem($"{this.Name} ({this.Bin})",
                                    ExplorerItemKind.QueryableObject,
                                    ExplorerIcon.Key)
            {
                IsEnumerable = true,
                DragText = $"{this.Namespace.SafeName}.{this.Set.SafeName}.{this.SafeName}",
                Children = new List<ExplorerItem>() { new ExplorerItem($"{this.Bin} ({this.Type}:{this.IndexType}{DetermineContext(this.Context)})",
                                                                            ExplorerItemKind.Schema,
                                                                            ExplorerIcon.Column)
                                                            { DragText = this.Bin }
                                                    }
            };
        }


        public override string ToString()
        {
            return this.Name;
        }

        private string DebuggerDisplay => $"{Namespace}.{Set}.{Bin}.{Name}";

    }
}
