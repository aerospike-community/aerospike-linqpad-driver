using Aerospike.Database.LINQPadDriver.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aerospike.Database.LINQPadDriver
{
    [System.Diagnostics.DebuggerDisplay("{Name}")]
    public sealed class ASet
    {        
        public class BinType
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
            public bool Detected { get; private set; }
        }

        static readonly ConcurrentBag<ASet> SetsBag = new ConcurrentBag<ASet>();

        public const string NullSetName = "NullSet";

        public ASet(ANamespace aNamespace, string name)
        {
            this.ANamespace = aNamespace;
            this.Name = name;
            this.SafeName = Helpers.CheckName(name, "Set");
            SetsBag.Add(this);
        }

        public ASet(ANamespace aNamespace)
        {
            this.ANamespace = aNamespace;
            this.Name = NullSetName;
            this.SafeName = Helpers.CheckName(this.Name, "Set");

            this.IsNullSet = true;
            SetsBag.Add(this);
        }

        public ANamespace ANamespace { get; }

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
        public IEnumerable<ASecondaryIndex> SIndexes { get; internal set; } = Enumerable.Empty<ASecondaryIndex>();

        internal void GetRecordBins(GetSetBins getBins,
                                        bool determineDocType,
                                        int maxRecords,
                                        int minRecs)
        {
            this.binTypes = getBins.Get(this.ANamespace.Name, this.Name, determineDocType, maxRecords, minRecs);
        }

        public bool AddNewlyFndBin(string binName, Type dataType)
        {
            bool dup;

            lock(binTypes)
            {
                dup = binTypes.Any(b => b.BinName == binName && b.DataType == dataType);

                this.binTypes.Add(new BinType(binName, dataType, dup, false, true));
            }

            return dup; 
        }

        #region Code Generation

        public (string setClass, string propInstance) GenerateNoRecSet()
        {
            var idxProps = new StringBuilder();
            var setClasses = new StringBuilder();
            var setProps = new StringBuilder();

            foreach (var sidx in this.SIndexes)
            {
                idxProps.AppendLine(sidx.GenerateCode(null));
            }

            return($@"
        public class {SafeName}_SetCls : Aerospike.Database.LINQPadDriver.Extensions.SetRecords
		{{
			public {SafeName}_SetCls (Aerospike.Database.LINQPadDriver.Extensions.ANamespaceAccess setAccess)
				: base(setAccess, ""{Name}"")
			{{ }}
			
{idxProps}
		}}", //End of Class string
        ///////////////////////////////////////////////////////////////////////
        $@"
		public {SafeName}_SetCls {SafeName} {{ get => new {SafeName}_SetCls(this); }}"
            ); //End of property string
        }


        public (string setClass, string propInstance) GenerateCode(bool alwaysUseAValues)
        {
            var bins = this.BinTypes.ToArray();

            if (this.IsNullSet || !bins.Any())
                return GenerateNoRecSet();

            var idxProps = new StringBuilder();
            var binsString = string.Join(',', bins.Select(b => string.Format("\"{0}\"", b.BinName)));
            var flds = new List<string>();

            var setClassFlds = new StringBuilder();
            var setClassFldsConst = new StringBuilder();
            var fldSeen = new List<string>();

            setClassFlds.AppendLine($"\t\t\tpublic APrimaryKey {ARecord.DefaultASPIKeyName} {{ get; }}");
            setClassFldsConst.AppendLine($"\t\t\t\t\t{ARecord.DefaultASPIKeyName} = new APrimaryKey(this.Aerospike.Key);");

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
                    setClassFldsConst.Append($" new AValue(this.Aerospike.GetValue(\"");
                    setClassFldsConst.Append(setBinType.BinName);
                    setClassFldsConst.Append($"\"), \"{setBinType.BinName}\",  \"{fldName}\" );");
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

                if (setBinType.Duplicate)
                    setClassFldsConst.Append("\t//Multiple Type Bin");
                if (!setBinType.FndAllRecs)
                    setClassFldsConst.Append("\t//Bin not found in all records");

                setClassFldsConst.AppendLine();

                fldSeen.Add(setBinType.BinName);
            }

            foreach (var sidx in this.SIndexes)
            {
                var idxDataType = alwaysUseAValues
                                    ? typeof(AValue)
                                    : bins.FirstOrDefault(b => b.BinName == sidx.Bin && !b.Duplicate).DataType;

                idxProps.AppendLine(sidx.GenerateCode(idxDataType));
            }


            var settClasses = $@"
	public class {this.SafeName}_SetCls : Aerospike.Database.LINQPadDriver.Extensions.SetRecords<{this.SafeName}_SetCls.RecordCls>
	{{
		public {this.SafeName}_SetCls (Aerospike.Database.LINQPadDriver.Extensions.ANamespaceAccess setAccess)
			: base(Aerospike.Database.LINQPadDriver.ASet.GetSet(""{this.ANamespace.Name}"", ""{this.Name}""),
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

            return (settClasses, setProps);
        }

        #endregion


        public override string ToString()
        {
            return this.ANamespace?.Name + '.' + this.Name;
        }

        public static ASet GetSet(string namespaceName, string setName) => SetsBag.FirstOrDefault(s => s.ANamespace.Name == namespaceName && s.Name == setName);
    }
}
