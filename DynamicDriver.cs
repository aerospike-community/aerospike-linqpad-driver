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
using System.Runtime.Intrinsics.X86;
using System.Windows.Markup;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;

namespace Aerospike.Database.LINQPadDriver
{
	public class DynamicDriver : DynamicDataContextDriver
	{
		static internal volatile AerospikeConnection _Connection;
        static internal object ConnectionLock = new object();

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

        /// <summary>This virtual method is called after a data context object has been instantiated, in
        /// preparation for a query. You can use this hook to perform additional initialization work.
        ///
        /// In overriding InitializeContext, you can access properties on the QueryExecutionManager object that’s passed in as a parameter. One of these properties is called SqlTranslationWriter (type TextWriter) and it allows you to send data to the SQL translation tab.
        /// Although this tab is intended primary for SQL translations, you can use it for other things as well.For example, with WCF Data Services, it makes sense to write HTTP requests here:
        ///
		/// var dsContext = (DataServiceContext)context;
		///        dsContext.SendingRequest += (sender, e) =>
		///					executionManager.SqlTranslationWriter.WriteLine(e.Request.RequestUri);
        /// </summary>
        public override void InitializeContext(IConnectionInfo cxInfo, object context, QueryExecutionManager executionManager)
		{           
        }

        /// <summary>This virtual method is called after a query has completed. You can use this hook to
        /// perform cleanup activities such as disposing of the context or other objects.</summary>
        public override void TearDownContext(IConnectionInfo cxInfo,
                                                object context,
                                                QueryExecutionManager executionManager,
                                                object[] constructorArguments)
        {
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

        /// <summary>This method is called after the query's main thread has finished running the user's code,
        /// but before the query has stopped. If you've spun up threads that are still writing results, you can 
        /// use this method to wait out those threads.</summary>
        public override void OnQueryFinishing(IConnectionInfo cxInfo, object context,
												QueryExecutionManager executionManager)
        { }

		static DateTime RefreshExplorerLastTime = DateTime.MinValue;
        static long RefreshingExplorer = 0;
		public static async Task UpdateSchemaExplorerVersion()
		{
			
			if (Interlocked.Read(ref RefreshingExplorer) == 0
					&& DateTime.Now >= RefreshExplorerLastTime.AddMinutes(1))
            {
				Interlocked.Increment(ref RefreshingExplorer);
				try
				{
					await _Connection.CXInfo.ForceRefresh();
				}
				catch { }
				finally
				{
					Interlocked.Decrement(ref RefreshingExplorer);
				}
			}
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

            Interlocked.Increment(ref RefreshingExplorer);
			try
			{
				var buildNamespaces = BuildNamespaces(_Connection.AlwaysUseAValues);
				var namespaceClasses = buildNamespaces.Item1;
				var namespaceProps = buildNamespaces.Item2;
				var namespaceConstruct = buildNamespaces.Item3;

				var buildModules = BuildModules();
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
			finally
			{
                RefreshExplorerLastTime = DateTime.Now;
                Interlocked.Decrement(ref RefreshingExplorer);				
            }
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
		/// <param name="alwaysUseAValues"></param>
        /// <returns>
        /// Item1 -- Namespace classes
        /// Item2 -- Namespace Properties
        /// Item3 -- Namespace constructors 
        /// </returns>
        public static Tuple<StringBuilder, StringBuilder, StringBuilder> BuildNamespaces(bool alwaysUseAValues)
		{
			var nsStack = new ConcurrentStack<(string classes, string props, string constructs)>();

			Parallel.ForEach(_Connection.Namespaces, ns =>
			{
				var (nsClass, nsProp, nsInstance) = ns.GenerateCode(alwaysUseAValues);

				nsStack.Push( (nsClass, nsProp, nsInstance) );
			});

            var namespaceClasses = new StringBuilder();
            var namespaceProps = new StringBuilder();
            var namespaceConstruct = new StringBuilder();

			foreach((string classes, string props, string constructs) nsItem in nsStack)
			{
				namespaceClasses.AppendLine(nsItem.classes);
				namespaceProps.AppendLine(nsItem.props);
				namespaceConstruct.AppendLine(nsItem.constructs);
			}

            return new Tuple<StringBuilder, StringBuilder, StringBuilder>(namespaceClasses, namespaceProps, namespaceConstruct);
        }

		public static List<ExplorerItem> AddSetBinItems(ANamespace ns, ASet set)
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
				static string DetermineContext(string context) => string.IsNullOrEmpty(context) ? string.Empty : ":" + context;

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
														Children = new List<ExplorerItem>() { new ExplorerItem($"{i.Bin} ({i.Type}:{i.IndexType}{DetermineContext(i.Context)})",
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

        public static List<ExplorerItem> AddSetBinExplorerItems(ANamespace ns)
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

        public static IEnumerable<ExplorerItem> CreateNamespaceExploreItems()
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
        public static Tuple<StringBuilder, StringBuilder, StringBuilder> BuildModules()
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

        public static List<ExplorerItem> AddUDFChildrenExplorerItems(AModule mod)
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

        public static IEnumerable<ExplorerItem> CreateModuleExploreItems()
        {
            List<ExplorerItem> items = new List<ExplorerItem>();

            foreach (var mod in _Connection.UDFModules.OrderBy(u => u.Name))
            {
                items.Add(
                            new ExplorerItem($"{mod.Name} ({mod.UDFs.Length})", ExplorerItemKind.Property, ExplorerIcon.Schema)
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

        public static ExplorerItem CreateInformationalExploreItem(IConnectionInfo cxInfo, string souceCode)
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

        public static IEnumerable<ExplorerItem> CreateClusterNodesExploreItems()
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