﻿using Aerospike.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Aerospike.Database.LINQPadDriver
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay}")]
    public sealed class ASecondaryIndex
    {

        public ASecondaryIndex(string name, ANamespace aNamespace, ASet set, string bin, string type, string indexType, string context)
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
        static private readonly Regex IdxRegEx = new Regex("ns=(?<namespace>[^:;]+):indexname=(?<indexname>[^:;]+):set=(?<setname>[^:;]+):bin=(?<binname>[^:;]+):type=(?<type>[^:;]+):indextype=(?<indextype>[^:;]+):context=(?<context>[^:;]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
       
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
        public ANamespace Namespace { get; }
        /// <summary>
        /// Associated Set
        /// </summary>
        public ASet Set { get; }
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

        public static IEnumerable<ASecondaryIndex> Create(Client.Connection asConnection, IEnumerable<ANamespace> namespaces)
        {
            ASet FindSet(ANamespace ns, string set)
            {
                return ns?.Sets?.FirstOrDefault(s => s.Name == set);
            }
            
            var idxsAttrib = Info.Request(asConnection, "sindex");
            
            var idxs = (from nsSetIdx in idxsAttrib.Split(';', StringSplitOptions.RemoveEmptyEntries)
                                let match = IdxRegEx.Match(nsSetIdx)
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
                                                            ? ASet.NullSetName
                                                            : set)
                                select new ASecondaryIndex(idxName,
                                                                aNamespace,
                                                                aSet,
                                                                bin,
                                                                type,
                                                                idxType,
                                                                context));

            return idxs.ToArray();
        }

        public override string ToString()
        {
            return this.Name;
        }

        private string DebuggerDisplay => $"{Namespace}.{Set}.{Bin}.{Name}";

    }
}
