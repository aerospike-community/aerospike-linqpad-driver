using LINQPad;
using LINQPad.Extensibility.DataContext;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Aerospike.Database.LINQPadDriver.Extensions;

namespace Aerospike.Database.LINQPadDriver
{
	public class DynamicDriver : DynamicDataContextDriver
	{
		static DynamicDriver()
		{
            // Uncomment the following code to attach to Visual Studio's debugger when an exception is thrown:

            /*AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
			{
				if (args.Exception.StackTrace.Contains ("Aerospike.Database.LINQPadDriver"))
					Debugger.Launch ();
			};*/
            
        }		

        public override string Name => "Aerospike DB";

		public override string Author => "Richard Andersen";

		public override string GetConnectionDescription (IConnectionInfo cxInfo)
		{

			if(_Connection != null)
			{
				return $"Aerospike Cluster {cxInfo.DatabaseInfo.Database}";
            }

			return "Aerospike Cluster";
        }

		public override bool ShowConnectionDialog (IConnectionInfo cxInfo, ConnectionDialogOptions dialogOptions)
			=> new ConnectionDialog (cxInfo).ShowDialog () == true;

		public override void InitializeContext(IConnectionInfo cxInfo, object context, QueryExecutionManager executionManager)
		{           
        }		

        /*
		 * LINQPad calls this after the user executes an old-fashioned SQL query. 
		 * If it returns a non-null value that’s later than its last value, it automatically refreshes the Schema Explorer. 
		 * This is useful in that quite often, the reason for users running a SQL query is to create a new table or perform some other DDL.
		 */
        /// <summary>Returns the time that the schema was last modified. If unknown, return null.</summary>
        public override DateTime? GetLastSchemaUpdate(IConnectionInfo cxInfo) 
		{
            return null;
		}


        public override void TearDownContext(IConnectionInfo cxInfo,
												object context,
												QueryExecutionManager executionManager,
												object[] constructorArguments)
		{           
            var connection = constructorArguments == null
								? null
								: constructorArguments.FirstOrDefault() as AerospikeConnection;


			if(connection != null)
			{
				lock(ConnectionLock)
				{
                    connection.Dispose();
					_Connection = null;
                }
			}
        }

		public override ParameterDescriptor[] GetContextConstructorParameters(IConnectionInfo cxInfo)
		{
			return new[] { new ParameterDescriptor("dbConnection", "IDbConnection") };
		}

		public override object[] GetContextConstructorArguments(IConnectionInfo cxInfo)
		{
			AerospikeConnection connection;

			lock (ConnectionLock)
			{
				connection = _Connection;

				if (connection == null)
				{					
					connection = _Connection = new AerospikeConnection(cxInfo);
					connection.Open();
				}
			}
			return new[] { connection };
		}

		public override bool AreRepositoriesEquivalent(IConnectionInfo c1, IConnectionInfo c2)
		{
			return AerospikeConnection.CXInfoEquals(c1, c2); 
		}

		static internal AerospikeConnection _Connection;
		static internal object ConnectionLock = new object();

		public override List<ExplorerItem> GetSchemaAndBuildAssembly (
			IConnectionInfo cxInfo, AssemblyName assemblyToBuild, ref string nameSpace, ref string typeName)
		{
            //Debugger.Launch();

            lock (ConnectionLock)
			{
				var connection = _Connection;

				if (connection == null)
				{
					_Connection = new AerospikeConnection(cxInfo);
					_Connection.Open();
				}
				else if(_Connection.State != ConnectionState.Open)
				{
					_Connection.Open();
				}
            }
            
            var buildNamespaces = this.BuildNamespaces();
			var namespaceClasses = buildNamespaces.Item1;
            var namespaceProps = buildNamespaces.Item2;
            var namespaceConstruct = buildNamespaces.Item3;

			var buildModules = this.BuildModules();
			var moduleClasses = buildModules.Item1;
			var moduleProps = buildModules.Item2;
			var moduleConstruct = buildModules.Item3;

			string source = $@"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;
using Aerospike.Client;
using Aerospike.Database.LINQPadDriver;
using Aerospike.Database.LINQPadDriver.Extensions;

namespace {nameSpace}
{{	
	public class {typeName} : Aerospike.Database.LINQPadDriver.Extensions.AClusterAccess
	{{
		public {typeName}(System.Data.IDbConnection dbConnection)
			: base(dbConnection)
			{{
				{namespaceConstruct}
				{moduleConstruct}
			}}

		public AerospikeClient ASClient => AerospikeConnection.AerospikeClient;
		
		{namespaceProps}
		{moduleProps}
	}}

	{namespaceClasses}
	{moduleClasses}
}}";

			Compile(source,
				#pragma warning disable SYSLIB0044
                    assemblyToBuild.CodeBase,
				#pragma warning restore SYSLIB0044
                    cxInfo,
					debug: _Connection.Debug);

			List<ExplorerItem> items = new List<ExplorerItem>();

			{
                var asyncClient = typeof(Aerospike.Client.Connection).Assembly.GetName();
				items.Add(new ExplorerItem("Client Connection",
														ExplorerItemKind.Property,
														ExplorerIcon.TableFunction)
				{
					IsEnumerable = false,
					DragText = "ASClient",
					ToolTipText = $"{asyncClient?.Name} Driver Version: {asyncClient?.Version}"
				});
            }

            items.AddRange(CreateNamespaceExploreItems());

			items.Add(new ExplorerItem("UDFs", ExplorerItemKind.Category, ExplorerIcon.Box)
            {
                IsEnumerable = false,
                DragText = null,
                ToolTipText = "User Defined Functions",
                Children = CreateModuleExploreItems().ToList()
            });

            items.Add(CreateInformationalExploreItem(cxInfo, source));

            return items;
		}

		public override IDbConnection GetIDbConnection(IConnectionInfo cxInfo)
		{            
            return new AerospikeConnection(cxInfo);
		}

		public override IEnumerable<string> GetAssembliesToAdd(IConnectionInfo cxInfo)
		{
			return new[] { typeof(Aerospike.Client.Connection).Assembly.Location, typeof(Newtonsoft.Json.Linq.JObject).Assembly.Location };
		}

        public override IEnumerable<string> GetNamespacesToAdd(IConnectionInfo cxInfo)
        {
            lock (ConnectionLock)
            {
                var connection = _Connection;

                if (connection == null)
                {
                    _Connection = new AerospikeConnection(cxInfo);                    
                }                
            }
            
			if(_Connection.DocumentAPI)
				return new[] { "Aerospike.Database.LINQPadDriver.Extensions",
										"Aerospike.Client",
										"Newtonsoft.Json.Linq"};

            return new[] { "Aerospike.Database.LINQPadDriver.Extensions",
                                        "Aerospike.Client"};
        }

        public override void ClearConnectionPools(IConnectionInfo cxInfo)
		{
            lock (ConnectionLock)
            {
				if (_Connection != null)
				{
					_Connection.Dispose();
					_Connection = null;
				}
            }
        }

		static void Compile(string cSharpSourceCode, string outputFile, IConnectionInfo cxInfo, bool debug = false)
        {           
            string[] assembliesToReference =

			// GetCoreFxReferenceAssemblies is helper method that returns the full set of .NET Core reference assemblies.
			// (There are more than 100 of them.)
			GetCoreFxReferenceAssemblies(cxInfo)
				.Append(typeof(Aerospike.Client.Connection).Assembly.Location)
				.Append(typeof(AClusterAccess).Assembly.Location)
				.ToArray();

            // CompileSource is a static helper method to compile C# source code using LINQPad's built-in Roslyn libraries.
            // If you prefer, you can add a NuGet reference to the Roslyn libraries and use them directly.
            var compileResult = CompileSource(new CompilationInput
            {
                FilePathsToReference = assembliesToReference,
                OutputPath = outputFile,
                SourceCode = new[] { cSharpSourceCode }
            });

			if (compileResult.Errors.Length > 0)
			{
				DumpCompilerError.ToLinqPadFile(cSharpSourceCode, compileResult.Errors);
				throw new Exception("Cannot compile typed context: " + compileResult.Errors[0]);
			}
			else if(debug)
			{
                DumpCompilerError.ToLinqPadFile(cSharpSourceCode, compileResult.Errors);
            }
        }

        #region Namespace/Set/Bin Creation Code/Explorer Items

        /// <summary>
        /// Creates namespace/set/bin C# code strings
        /// </summary>
        /// <returns>
        /// Item1 -- Namespace classes
        /// Item2 -- Namespace Properties
        /// Item3 -- Namespace constructors 
        /// </returns>
        public Tuple<StringBuilder, StringBuilder, StringBuilder> BuildNamespaces()
		{
			var namespaceClasses = new StringBuilder();
            var namespaceProps = new StringBuilder();
            var namespaceConstruct = new StringBuilder();

			foreach (var ns in _Connection.Namespaces)
			{
                var setProps = new StringBuilder();
				var setClasses = new StringBuilder();
                var binNames = new StringBuilder();
				
				void GenerateNoRecSet(string safeSetName, 
										string setName,
										IEnumerable<ASecondaryIndex> indexes)
				{
                    var idxProps = new StringBuilder();

                    foreach (var sidx in indexes)
                    {

                        idxProps.AppendLine($@"
			public Aerospike.Database.LINQPadDriver.Extensions.ASecondaryIndexAccess {sidx.SafeName} 
								{{ get => new Aerospike.Database.LINQPadDriver.Extensions.ASecondaryIndexAccess(this, ""{sidx.Name}"", ""{sidx.Bin}"", ""{sidx.Type}"", ""{sidx.IndexType}""); }}
");
                    }

                    setClasses.AppendLine($@"
		public class {safeSetName}_SetCls : Aerospike.Database.LINQPadDriver.Extensions.SetRecords
		{{
			public {safeSetName}_SetCls (Aerospike.Database.LINQPadDriver.Extensions.ANamespaceAccess setAccess)
				: base(setAccess, ""{setName}"")
			{{ }}
			
{idxProps}
		}}"
                            );

                    setProps.AppendLine($@"
		public {safeSetName}_SetCls {safeSetName} {{ get => new {safeSetName}_SetCls(this); }}"
                        );
                }


                //Code for getting RecordSets for Set. 
                foreach (var set in ns.Sets)
                {
					if (_Connection.SetBins == null)
					{
						GenerateNoRecSet(set.SafeName, set.Name, set.SIndexes);
                    }
					else
					{
						var setBins = _Connection.SetBins.FirstOrDefault(s => s.nsName == ns.Name && s.setname == set.Name);

						if (setBins.setname != null && setBins.Item3.Any())
						{
                            var idxProps = new StringBuilder();
                            var binsString = string.Join(',', setBins.Item3.Select(b => string.Format("\"{0}\"", b.bin)));
							var flds = new List<string>();

							var setClassFlds = new StringBuilder();
							var setClassFldsConst = new StringBuilder();
							var fldSeen = new List<string>();

							setClassFlds.AppendLine($"\t\t\tpublic APrimaryKey {ARecord.DefaultASPIKeyName} {{ get; }}");
							setClassFldsConst.AppendLine($"\t\t\t\t\t{ARecord.DefaultASPIKeyName} = new APrimaryKey(this.Aerospike.Key);");

							foreach (var setBinType in setBins.Item3)
							{
								if (fldSeen.Contains(setBinType.bin))
								{
									continue;
								}

								var fldName = Helpers.CheckName(setBinType.bin, "Bin");
								var fldType = setBinType.dup ? "AValue" : Helpers.GetRealTypeName(setBinType.type, !setBinType.inAllRecs);

								flds.Add(fldName);

								//if (fldType.Contains("JsonDocument"))
								//{
								//	fldType = fldType.Replace("JsonDocument", "Dictionary<object,object>");
								//}

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

								if (setBinType.dup)
								{
									setClassFldsConst.Append($" new AValue(this.Aerospike.GetValue(\"");
									setClassFldsConst.Append(setBinType.bin);
									setClassFldsConst.Append($"\"), \"{setBinType.bin}\",  \"{fldName}\" );");
								}
								else if (setBinType.type.IsValueType)
								{
									setClassFldsConst.Append($" (Helpers.CastToNativeType(this, \"{fldName}\", typeof({fldType}), \"{setBinType.bin}\", this.Aerospike.GetValue(\"");
									setClassFldsConst.Append(setBinType.bin);
									setClassFldsConst.Append("\"))");
									setClassFldsConst.Append($" ?? default({fldType}));");
								}
								else 
								{
                                    setClassFldsConst.Append($" Helpers.CastToNativeType(this, \"{fldName}\", typeof({fldType}), \"{setBinType.bin}\", this.Aerospike.GetValue(\"");
                                    setClassFldsConst.Append(setBinType.bin);
                                    setClassFldsConst.Append("\"));");
                                }								

								if (setBinType.dup)
									setClassFldsConst.Append("\t//Multiple Type Bin");
                                if (!setBinType.inAllRecs)
                                    setClassFldsConst.Append("\t//Bin not found in all records");

                                setClassFldsConst.AppendLine();

								fldSeen.Add(setBinType.bin);
							}

                            foreach (var sidx in set.SIndexes)
                            {
								var idxDataType = setBins.Item3.FirstOrDefault(b => b.bin == sidx.Bin).type;

								if (idxDataType is null) idxDataType = typeof(object);

                                idxProps.AppendLine($@"
			public Aerospike.Database.LINQPadDriver.Extensions.ASecondaryIndexAccess<RecordCls> {sidx.SafeName} 
								{{ get => new Aerospike.Database.LINQPadDriver.Extensions.ASecondaryIndexAccess<RecordCls>(this, ""{sidx.Name}"", ""{sidx.Bin}"", ""{sidx.Type}"", ""{sidx.IndexType}"", typeof({Helpers.GetRealTypeName(idxDataType)})); }}
");
                            }


                            setClasses.AppendLine($@"
		public class {set.SafeName}_SetCls : Aerospike.Database.LINQPadDriver.Extensions.SetRecords<{set.SafeName}_SetCls.RecordCls>
		{{
			public {set.SafeName}_SetCls (Aerospike.Database.LINQPadDriver.Extensions.ANamespaceAccess setAccess)
				: base(setAccess, ""{set.Name}"", bins: new string[] {{ {binsString} }})
			{{ }}

			public {set.SafeName}_SetCls ({set.SafeName}_SetCls clone)
				: base(clone)
			{{ }}

			protected override Aerospike.Database.LINQPadDriver.Extensions.ARecord CreateRecord(Aerospike.Database.LINQPadDriver.Extensions.ANamespaceAccess setAccess,
															Aerospike.Client.Key key,
															Aerospike.Client.Record record,
															string[] binNames,
															int binsHashCode,
															Aerospike.Database.LINQPadDriver.Extensions.ARecord.DumpTypes recordView = global::Aerospike.Database.LINQPadDriver.Extensions.ARecord.DumpTypes.Record) => new RecordCls(setAccess, key, record, binNames, binsHashCode, recordView);

			public new {set.SafeName}_SetCls Clone() => new {set.SafeName}_SetCls(this);

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
							);

							setProps.AppendLine($@"
		public {set.SafeName}_SetCls {set.SafeName} {{ get => new {set.SafeName}_SetCls(this); }}"
							);
						}
						else
						{
                            GenerateNoRecSet(set.SafeName, set.Name, set.SIndexes);
                        }
					}
                }

                foreach (var binName in ns.Bins)
                {					
                    binNames.Append("\"");
                    binNames.Append(binName);
                    binNames.Append("\", ");
                }

                //Code to generate namespace class
                namespaceClasses.AppendLine($@"
	public class {ns.SafeName}_NamespaceCls : Aerospike.Database.LINQPadDriver.Extensions.ANamespaceAccess
	{{

		public {ns.SafeName}_NamespaceCls(System.Data.IDbConnection dbConnection)
			: base(dbConnection, ""{ns.Name}"", new string[] {{{binNames}}})
		{{ }}

		public {ns.SafeName}_NamespaceCls(Aerospike.Database.LINQPadDriver.Extensions.ANamespaceAccess clone, Aerospike.Client.Expression expression)
			: base(clone, expression)
		{{ }}

		public {ns.SafeName}_NamespaceCls FilterExpression(Aerospike.Client.Expression expression)
        {{
            return new {ns.SafeName}_NamespaceCls(this, expression);
        }}

		public {ns.SafeName}_NamespaceCls FilterExpression(Aerospike.Client.Exp exp)
        {{
            return new {ns.SafeName}_NamespaceCls(this, Aerospike.Client.Exp.Build(exp));
        }}

		public static implicit operator AerospikeClient({ns.SafeName}_NamespaceCls ns) => ns.AerospikeConnection.AerospikeClient;
		        
		{setClasses}
		{setProps}
	}}"
                );

                //Code to access namespace instance. 
                namespaceProps.AppendLine($@"
		public {ns.SafeName}_NamespaceCls {ns.SafeName} {{get; }}"
                );

                //Code to construct namespace instance
                namespaceConstruct.AppendLine($@"
			this.{ns.SafeName} = new {ns.SafeName}_NamespaceCls(dbConnection);"
                );
            }

			return new Tuple<StringBuilder, StringBuilder, StringBuilder>(namespaceClasses, namespaceProps, namespaceConstruct);
        }

		public List<ExplorerItem> AddSetBinItems(ANamespace ns, ASet set)
		{

			static string GetBinTypeDupIndicator(Type type, bool dup, bool notInAllRecs)
			{
				var binName = new StringBuilder(Helpers.GetRealTypeName(type));

				if (dup) binName.Append('*');
				if(notInAllRecs) binName.Append('?');

				return binName.ToString();
            }

            var items = new List<ExplorerItem>();

			if (set.IsNullSet)
			{               
                items.AddRange(ns.Bins.OrderBy(b => b).Select(b => new ExplorerItem(b,
																					ExplorerItemKind.Schema,
																					ExplorerIcon.Column)));                
            }
            else
			{
				var binsSet = _Connection.SetBins?
									.FirstOrDefault(b => b.nsName == ns.Name && b.setname == set.Name)
									.Item3;

				if (binsSet == null) return items;
				
				items.AddRange(binsSet
									.OrderBy(b => b.bin)
									.Select(b => new ExplorerItem($"{b.bin} ({GetBinTypeDupIndicator(b.type, b.dup, !b.inAllRecs)})",
																	ExplorerItemKind.Schema,
																	ExplorerIcon.Column)
													{
														IsEnumerable = false,
														DragText = b.bin
													}
				));				
			}

            if (set.SIndexes.Any())
            {
                items.Add(new ExplorerItem($"Secondary Indexes",
                                                ExplorerItemKind.Category,
                                                ExplorerIcon.Key)
                {
                    IsEnumerable = false,
                    Children = set.SIndexes
									.OrderBy(i => i.Name)
									.Select(i => new ExplorerItem($"{i.Name} ({i.Bin})",
                                                                    ExplorerItemKind.Schema,
                                                                    ExplorerIcon.Key)
													{
														DragText = $"{i.Namespace.SafeName}.{i.Set.SafeName}.{i.SafeName}",
														Children = new List<ExplorerItem>() { new ExplorerItem($"{i.Bin} ({i.Type}:{i.IndexType})",
																											ExplorerItemKind.Schema,
																											ExplorerIcon.Column)
																									{ DragText = i.Bin }
																							}
													})
									.ToList()
                });
            }

            return items;
        }

        public List<ExplorerItem> AddSetBinExplorerItems(ANamespace ns)
        {
            var items = new List<ExplorerItem>();

            items.AddRange(ns.Sets
								.Where(s => !s.IsNullSet)
								.OrderBy(s => s.Name).Select(b => new ExplorerItem(b.Name,
																						ExplorerItemKind.QueryableObject,
																						ExplorerIcon.Schema)
																		{
																			IsEnumerable = true,
																			DragText = $"{ns.SafeName}.{b.SafeName}",
																			Children = AddSetBinItems(ns, b)
																		}
				));
            items.AddRange(ns.Sets
                                .Where(s => s.IsNullSet).Select(b => new ExplorerItem(b.Name,
                                                                                        ExplorerItemKind.QueryableObject,
                                                                                        ExplorerIcon.Schema)
																		{
																			IsEnumerable = true,
																			DragText = $"{ns.SafeName}.{b.SafeName}",
																			Children = AddSetBinItems(ns, b)
																		}
                ));
            return items;
        }

        public IEnumerable<ExplorerItem> CreateNamespaceExploreItems()
		{
            List<ExplorerItem> items = new List<ExplorerItem>();
            
            foreach (var ns in _Connection.Namespaces.OrderBy(n => n.Name))
            {
                items.Add(
                            new ExplorerItem($"{ns.Name} ({ns.Sets.Count()})", ExplorerItemKind.Property, ExplorerIcon.Table)
                            {
                                IsEnumerable = false,
                                DragText = $"{ns.SafeName}",
                                Children = AddSetBinExplorerItems(ns),
                                ToolTipText = $"Sets associated with namespace \"{ns.Name}\""
                            }
                        );
            }

			return items;
        }

        #endregion

        #region Module Creation Code/Explorer Items

        /// <summary>
        /// Creates module/UDF C# code strings
        /// </summary>
        /// <returns>
        /// Item1 -- Module classes
        /// Item2 -- Module Properties
        /// Item3 -- Module constructors 
        /// </returns>
        public Tuple<StringBuilder, StringBuilder, StringBuilder> BuildModules()
        {			
            var moduleClasses = new StringBuilder();
            var moduleProps = new StringBuilder();
            var moduleConstruct = new StringBuilder();
            
            foreach (var mod in _Connection.UDFModules)
            {
                var udfProps = new StringBuilder();
                var udfClasses = new StringBuilder();
                var udfConstructors = new StringBuilder();
				
				//Code for getting UDF classes 
				foreach (var udf in mod.UDFs)
				{
					if (udf.IsLocal)
					{
						continue;
					}

                    var udfName = char.ToUpper(udf.SafeName[0]) + udf.SafeName.Substring(1) + "_UDFCls";
					var udfParams = udf.Params?.Split(',', StringSplitOptions.RemoveEmptyEntries);
					var udfCode = udf.Code?.Replace("\"", "\"\"");
                    var udfFuncs = new StringBuilder();

                    if (udfParams != null && udfParams.Length > 1)
                    {
                        var paramStr = string.Join(',', udfParams.Skip(1).Select(p => "object " + p.Trim()));
                        var valueStr = string.Join(',', udfParams.Skip(1).Select(p => p.Trim()));

                        udfFuncs.AppendLine($@"				public object Execute (Aerospike.Client.Key key, {paramStr}) => base.Execute(key, {valueStr}); ");
                        udfFuncs.AppendLine($@"				public object Execute (Aerospike.Database.LINQPadDriver.Extensions.SetRecords set, object primaryKey, {paramStr}) => base.Execute(set, primaryKey, {valueStr}); ");
                        udfFuncs.AppendLine($@"				public object Execute (Aerospike.Database.LINQPadDriver.Extensions.ARecord asRecord, {paramStr}) => base.Execute(asRecord, {valueStr}); ");

                        udfFuncs.AppendLine($@"				public IEnumerable<object> QueryAggregate (Aerospike.Client.Statement statement, {paramStr}) => base.QueryAggregate(statement, {valueStr}); ");
                        udfFuncs.AppendLine($@"				public IEnumerable<object> QueryAggregate (Aerospike.Client.Statement statement, Aerospike.Client.Exp filterExpression, {paramStr}) => base.QueryAggregate(statement, filterExpression, {valueStr}); ");
                        udfFuncs.AppendLine($@"				public IEnumerable<object> QueryAggregate (Aerospike.Database.LINQPadDriver.Extensions.SetRecords set, {paramStr}) => base.QueryAggregate(set, {valueStr}); ");
                        udfFuncs.AppendLine($@"				public IEnumerable<object> QueryAggregate (Aerospike.Database.LINQPadDriver.Extensions.SetRecords set, Aerospike.Client.Exp filterExpression, {paramStr}) => base.QueryAggregate(set, filterExpression, {valueStr}); ");
                    }

                    udfClasses.AppendLine($@"
			public class {udfName} : Aerospike.Database.LINQPadDriver.Extensions.AUDFAccess
			{{

				public {udfName}(System.Data.IDbConnection dbConnection)
					: base(dbConnection, ""{mod.Name}"", ""{udf.Name}"",
@""{udfCode}""
)
				{{ }}

{udfFuncs}
		
			}}"
					);

					udfProps.AppendLine($@"			public {udfName} {udf.SafeName} {{ get; }}"
						);

					udfConstructors.AppendLine($@"				this.{udf.SafeName} = new {udfName}(dbConnection);"
					);					
                }

                
                //Code to generate module class
                moduleClasses.AppendLine($@"
	public class {mod.SafePackageName}_ModuleCls : Aerospike.Database.LINQPadDriver.Extensions.AModuleAccess
	{{

		public {mod.SafePackageName}_ModuleCls(System.Data.IDbConnection dbConnection)
			: base(dbConnection)
		{{ 
{udfConstructors}
		}}

{udfClasses}

{udfProps}

	}}"
                );

                //Code to access namespace instance. 
                moduleProps.AppendLine($@"
		public {mod.SafePackageName}_ModuleCls {mod.SafePackageName} {{get; }}"
                );

                //Code to construct namespace instance
                moduleConstruct.AppendLine($@"
			this.{mod.SafePackageName} = new {mod.SafePackageName}_ModuleCls(dbConnection);"
                );
            }

            return new Tuple<StringBuilder, StringBuilder, StringBuilder>(moduleClasses, moduleProps, moduleConstruct);
        }

        public List<ExplorerItem> AddUDFChildrenExplorerItems(AModule mod)
        {
            var items = new List<ExplorerItem>();

            items.AddRange(mod.UDFs
								.OrderBy(s => s.Name)
								.Select(b => new ExplorerItem(b.IsLocal
																? $"{b.Name}({b.Params}) Local"
                                                                : $"{b.Name}({b.Params})",
															ExplorerItemKind.Property,
															ExplorerIcon.StoredProc)
													{
														IsEnumerable = false,
														DragText = b.IsLocal ? null : $"{mod.SafePackageName}.{b.SafeName}",
														ToolTipText = b.Code
													}
                                            ));
            
            return items;
        }

        public IEnumerable<ExplorerItem> CreateModuleExploreItems()
        {
            List<ExplorerItem> items = new List<ExplorerItem>();

            foreach (var mod in _Connection.UDFModules.OrderBy(u => u.Name))
            {
                items.Add(
                            new ExplorerItem($"{mod.Name} ({mod.UDFs.Count()})", ExplorerItemKind.Property, ExplorerIcon.Schema)
                            {
                                IsEnumerable = false,
                                DragText = $"{mod.SafePackageName}",
                                Children = AddUDFChildrenExplorerItems(mod),
                                ToolTipText = $"UDFs associated with module \"{mod.Name}\""
                            }
                        );
            }

            return items;
        }

        #endregion

        public ExplorerItem CreateInformationalExploreItem(IConnectionInfo cxInfo, string souceCode)
		{            
            var infoItem = new ExplorerItem("Information", ExplorerItemKind.Category, ExplorerIcon.Box)
			{
				IsEnumerable = false,
				DragText = null,
				ToolTipText = "Cluster/Source Code Information",
				Children = new List<ExplorerItem>()
								{									
                                    new ExplorerItem($"Cluster Name \"{cxInfo.DatabaseInfo.Database}\"",
														ExplorerItemKind.Parameter,
														ExplorerIcon.ScalarFunction)
													{
														IsEnumerable = false,
														DragText = null,
														ToolTipText= cxInfo.DatabaseInfo.Database
													},
									new ExplorerItem($"DB Version {cxInfo.DatabaseInfo.Provider}",
														ExplorerItemKind.Parameter,
														ExplorerIcon.ScalarFunction)
													{
														IsEnumerable = false,
														DragText = null,
														ToolTipText= cxInfo.DatabaseInfo.Provider
													},
                                    new ExplorerItem($"Nodes ({_Connection.Nodes.Length})",
                                                        ExplorerItemKind.Category,
                                                        ExplorerIcon.OneToMany)
                                                    {
                                                        IsEnumerable = false,
                                                        DragText = null,
														Children = CreateClusterNodesExploreItems().ToList(),
                                                        ToolTipText= "Nodes within the Cluster"
                                                    },
                                    new ExplorerItem("Stats",
														ExplorerItemKind.Category,
														ExplorerIcon.Box)
													{
														IsEnumerable = false,
														DragText = null,
														Children = new List<ExplorerItem>()
														{
															new ExplorerItem($"Total Namespaces: {_Connection.Namespaces.Count()}",
																				ExplorerItemKind.Parameter,
																				ExplorerIcon.ScalarFunction),
															new ExplorerItem($"Total Sets: {_Connection.Namespaces.Sum(n => n.Sets.Count())}",
																				ExplorerItemKind.Parameter,
																				ExplorerIcon.ScalarFunction),
															new ExplorerItem($"Total Bins: {_Connection.Namespaces.Sum(n => n.Bins.Count())}",
																				ExplorerItemKind.Parameter,
																				ExplorerIcon.ScalarFunction),
                                                            new ExplorerItem($"Total SIdx: {_Connection.Namespaces.Sum(n => n.SIndexes.Count())}",
                                                                                ExplorerItemKind.Parameter,
                                                                                ExplorerIcon.ScalarFunction),
                                                            new ExplorerItem($"Total UDFs: {_Connection.UDFModules.Count()}",
                                                                                ExplorerItemKind.Parameter,
                                                                                ExplorerIcon.ScalarFunction)
                                                        }
													}
								}
			};

			return infoItem;
        }

        public IEnumerable<ExplorerItem> CreateClusterNodesExploreItems()
        {
            List<ExplorerItem> items = new List<ExplorerItem>();

            foreach (var node in _Connection.Nodes)
            {
				var nodeState = node.Active ? "Active" : "Inactive";
                items.Add(
                            new ExplorerItem($"{node.Host.name} ({nodeState})",
												ExplorerItemKind.Property,
												ExplorerIcon.LinkedDatabase)
										{
											IsEnumerable = false,
											DragText = $"\"{node.Host.name}\"",
											ToolTipText = $"Node \"{node.Host.name}\" ({node.Name}) in Cluster \"{_Connection.Database}\""
										}
                        );
            }

            return items;
        }


        #region Data Grid
       
        //
        // Summary:
        //     Override this is you want to allow the user to edit data in the DataGrid and
        //     save the changes (usually to a database).
        //public override SaveChangesAdapter CreateSaveChangesAdapter(BinType elementType, object dataSource, object elementSample, object parent)
		//{
         //   Debugger.Launch();
         //   return new sca();
		//}
        //
        // Summary:
        //     Override this is you want to customize how an object is displayed in the DataGrid.
        //public override void DisplayObjectInGrid(object objectToDisplay, GridOptions options)
		//{
		//	Debugger.Launch();
		//}


        #endregion


#if NETCORE
        // Put stuff here that's just for LINQPad 6+ (.NET Core and .NET 5+).
#else
		// Put stuff here that's just for LINQPad 5 (.NET Framework)
#endif
    }
}