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
#if DEBUG
			AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
            {
                if (args.Exception.StackTrace.Contains(typeof(DynamicDriver).Namespace))
                    Debugger.Launch();
            };
#else
            AppDomain.CurrentDomain.FirstChanceException += (sender, args) =>
            {
                if(Client.Log.InfoEnabled() && args.Exception.StackTrace.Contains(typeof(DynamicDriver).Namespace)) 
                {
                    Client.Log.Error($"First Chance Exception in LinqPad Driver. Exception \"{args.Exception.GetType().Name}\" ({args.Exception.Message}) Stack: {args.Exception.StackTrace}");
                }
            };        
#endif

		}

		public override string Name => "Aerospike DB";

		public override string Author => "Richard Andersen";

		public override string GetConnectionDescription (IConnectionInfo cxInfo)
		{
			if(string.IsNullOrEmpty(cxInfo?.DatabaseInfo?.Database))
			{
				return $"Aerospike Cluster {cxInfo.DatabaseInfo.Database}";
            }

			return "Aerospike Cluster";
        }

		public override bool ShowConnectionDialog (IConnectionInfo cxInfo, ConnectionDialogOptions dialogOptions)
			=> new ConnectionDialog (cxInfo).ShowDialog () == true;

		static AerospikeConnection ObtainConnection(IConnectionInfo cxInfo, bool openIfClosed)
		{
            if (Client.Log.InfoEnabled())
            {
                Client.Log.Info("ObtainConnection");
            }

            AerospikeConnection connection;

            lock (ConnectionLock)
            {
                connection = _Connection;

				connection ??= _Connection = new AerospikeConnection(cxInfo);

                if (openIfClosed && connection.State != ConnectionState.Open)
                {
                    connection.Open();
                }
            }

			return connection;
        }

        internal static AerospikeConnection GetConnection()
        {
            lock (ConnectionLock)
            {
                return _Connection;
            }
        }

		/// <summary>This virtual method is called after a data context object has been instantiated, in
		/// preparation for a query. You can use this hook to perform additional initialization work.
		///
		/// In overriding InitializeContext, you can access properties on the QueryExecutionManager object that�s passed in as a parameter. One of these properties is called SqlTranslationWriter (type TextWriter) and it allows you to send data to the SQL translation tab.
		/// Although this tab is intended primary for SQL translations, you can use it for other things as well.For example, with WCF Data Services, it makes sense to write HTTP requests here:
		///
		/// var dsContext = (DataServiceContext)context;
		///        dsContext.SendingRequest += (sender, e) =>
		///					executionManager.SqlTranslationWriter.WriteLine(e.Request.RequestUri);
		/// </summary>
		public override void InitializeContext(IConnectionInfo cxInfo, object context, QueryExecutionManager executionManager)
		{
            if(Client.Log.InfoEnabled()) 
            {
                Client.Log.Info("InitializeContext");
            }

			//updates the static field _Connection
			ObtainConnection(cxInfo, true);

            base.InitializeContext (cxInfo, context, executionManager);
        }

        /// <summary>This virtual method is called after a query has completed. You can use this hook to
        /// perform cleanup activities such as disposing of the context or other objects.</summary>
        public override void TearDownContext(IConnectionInfo cxInfo,
                                                object context,
                                                QueryExecutionManager executionManager,
                                                object[] constructorArguments)
        {
            if (Client.Log.InfoEnabled())
            {
                Client.Log.Info("TearDownContext");
            }

            base.TearDownContext(cxInfo, context, executionManager, constructorArguments);
        }

        public override void ClearConnectionPools(IConnectionInfo cxInfo)
        {
            if(Client.Log.InfoEnabled())
            {
                Client.Log.Info("ClearConnectionPools");
            }

            GetConnection()?.Dispose();
			_Connection = null;

			base.ClearConnectionPools (cxInfo);
        }

        /// <summary>This method is called after the query's main thread has finished running the user's code,
        /// but before the query has stopped. If you've spun up threads that are still writing results, you can 
        /// use this method to wait out those threads.</summary>
        public override void OnQueryFinishing(IConnectionInfo cxInfo, object context,
												QueryExecutionManager executionManager)
        {
            //System.Diagnostics.Debugger.Launch();
            if (_Connection?.Namespaces?.Any(n => n.CodeNeedsUpdating) ?? Interlocked.Read(ref ANamespaceAccess.ForceExplorerRefresh) > 0) 
			{
                if(Client.Log.InfoEnabled())
                {
                    Client.Log.Info("OnQueryFinishing Refresh Namespaces");
                }

                Interlocked.Exchange(ref ANamespaceAccess.ForceExplorerRefresh, 0);
				cxInfo.ForceRefresh();
			}
            else if (Client.Log.InfoEnabled())
            {
                Client.Log.Info("OnQueryFinishing");
            }
            
			base.OnQueryFinishing (cxInfo, context, executionManager);
		}

        /*
		 * LINQPad calls this after the user executes an old-fashioned SQL query. 
		 * If it returns a non-null value that�s later than its last value, it automatically refreshes the Schema Explorer. 
		 * This is useful in that quite often, the reason for users running a SQL query is to create a new table or perform some other DDL.
		 */
        /// <summary>Returns the time that the schema was last modified. If unknown, return null.</summary>
        public override DateTime? GetLastSchemaUpdate(IConnectionInfo cxInfo) => null;
        
        public override ParameterDescriptor[] GetContextConstructorParameters(IConnectionInfo cxInfo) => new[] { new ParameterDescriptor("dbConnection", "IDbConnection") };

		public override object[] GetContextConstructorArguments(IConnectionInfo cxInfo) => new[] { ObtainConnection(cxInfo, true) };
		
		public override bool AreRepositoriesEquivalent(IConnectionInfo c1, IConnectionInfo c2) => AerospikeConnection.CXInfoEquals(c1, c2); 
		
		public override List<ExplorerItem> GetSchemaAndBuildAssembly (
			IConnectionInfo cxInfo, AssemblyName assemblyToBuild, ref string nameSpace, ref string typeName)
		{
            //System.Diagnostics.Debugger.Launch();

            if (Client.Log.InfoEnabled())
            {
                Client.Log.Info("GetSchemaAndBuildAssembly Start");
            }

            var connection = ObtainConnection(cxInfo, false);

#if DEBUG
			if(connection.Debug)
                System.Diagnostics.Debugger.Launch();
#endif

            connection.ObtainMetaData();

			var buildNamespaces = BuildNamespaces(connection, connection.AlwaysUseAValues);
			var namespaceClasses = buildNamespaces.Item1;
			var namespaceProps = buildNamespaces.Item2;
			var namespaceConstruct = buildNamespaces.Item3;

			var buildModules = BuildModules(connection);
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

	public IAerospikeClient ASClient => AerospikeConnection.AerospikeClient;
		
	{namespaceProps}
	{moduleProps}
}}

{namespaceClasses}
{moduleClasses}
}}";

#pragma warning disable SYSLIB0044 // Type or member is obsolete
			Compile(source,
					assemblyToBuild.CodeBase,
					cxInfo,
					debug: connection.Debug);
#pragma warning restore SYSLIB0044 // Type or member is obsolete

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

			items.AddRange(CreateNamespaceExploreItems(connection));

            items.Add(new ExplorerItem("UDFs", ExplorerItemKind.Category, ExplorerIcon.Box)
            {
                IsEnumerable = false,
                DragText = null,
                ToolTipText = "User Defined Functions",
                Children = CreateModuleExploreItems(connection).ToList()
            });

			items.Add(CreateInformationalExploreItem(cxInfo, connection));

            if (Client.Log.InfoEnabled())
            {
                Client.Log.Info("GetSchemaAndBuildAssembly End");
            }

            return items;			
		}
		
		public override IEnumerable<string> GetAssembliesToAdd(IConnectionInfo cxInfo)
		{
			return new[] { typeof(Aerospike.Client.Connection).Assembly.Location,
                            typeof(Newtonsoft.Json.Linq.JObject).Assembly.Location,
                            typeof(GeoJSON.Net.IGeoJSONObject).Assembly.Location};
		}

        public override IEnumerable<string> GetNamespacesToAdd(IConnectionInfo cxInfo)
        {
			var connection = ObtainConnection(cxInfo, false);
            
			if(connection.DocumentAPI)
				return new[] { "Aerospike.Database.LINQPadDriver.Extensions",
										"Aerospike.Client",
										"Newtonsoft.Json.Linq",
                                        "GeoJSON.Net"};

            return new[] { "Aerospike.Database.LINQPadDriver.Extensions",
                                        "Aerospike.Client"};
        }
        
		static void Compile(string cSharpSourceCode, string outputFile, IConnectionInfo cxInfo, bool debug = false)
        {
            if (Client.Log.InfoEnabled())
            {
                Client.Log.Info("Compile Start");
            }

            string[] assembliesToReference =

			// GetCoreFxReferenceAssemblies is helper method that returns the full set of .NET Core reference assemblies.
			// (There are more than 100 of them.)
			GetCoreFxReferenceAssemblies(cxInfo)
				.Append(typeof(Aerospike.Client.Connection).Assembly.Location)
				.Append(typeof(AClusterAccess).Assembly.Location)
                .Append(typeof(GeoJSON.Net.IGeoJSONObject).Assembly.Location)
                .Append(typeof(Newtonsoft.Json.Linq.JObject).Assembly.Location)
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

            if (Client.Log.InfoEnabled())
            {
                Client.Log.Info("Compile End");
            }
        }

        #region Namespace/Set/Bin Creation Code/Explorer Items

        /// <summary>
        /// Creates namespace/set/bin C# code strings
        /// </summary>
		/// <param name="alwaysUseAValues"></param>
		/// <param name="connection"></param>
        /// <returns>
        /// Item1 -- Namespace classes
        /// Item2 -- Namespace Properties
        /// Item3 -- Namespace constructors 
        /// </returns>
        public static Tuple<StringBuilder, StringBuilder, StringBuilder> BuildNamespaces(AerospikeConnection connection, bool alwaysUseAValues)
		{
			var nsStack = new ConcurrentStack<(string classes, string props, string constructs)>();

			Parallel.ForEach(connection.Namespaces, ns =>
			{
				var (nsClass, nsProp, nsInstance) = ns.CodeGeneration(alwaysUseAValues);

				nsStack.Push( (nsClass, nsProp, nsInstance) );
			});

            var namespaceClasses = new StringBuilder();
            var namespaceProps = new StringBuilder();
            var namespaceConstruct = new StringBuilder();

			foreach((string classes, string props, string constructs) in nsStack)
			{
				namespaceClasses.AppendLine(classes);
				namespaceProps.AppendLine(props);
				namespaceConstruct.AppendLine(constructs);
			}

            return new Tuple<StringBuilder, StringBuilder, StringBuilder>(namespaceClasses, namespaceProps, namespaceConstruct);
        }
		
        public static IEnumerable<ExplorerItem> CreateNamespaceExploreItems(AerospikeConnection connection)
			=> connection.Namespaces
							.AsParallel()
							.OrderBy(ns => ns.Name)
                            .Select(ns => ns.CreateExplorerItem());

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
        public static Tuple<StringBuilder, StringBuilder, StringBuilder> BuildModules(AerospikeConnection connection)
        {			
            var moduleClasses = new StringBuilder();
            var moduleProps = new StringBuilder();
            var moduleConstruct = new StringBuilder();
            
            foreach (var mod in connection.UDFModules)
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

                    var udfName = char.ToUpper(udf.SafeName[0]) + udf.SafeName[1..] + "_UDFCls";
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

					udfProps.AppendLine($@"			public {udfName} {udf.SafeName} {{ get; private set;}}"
						);

					udfConstructors.AppendLine($@"				{udf.SafeName} = new {udfName}(dbConnection);"
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

        public static List<ExplorerItem> AddUDFChildrenExplorerItems(LPModule mod)
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

        public static IEnumerable<ExplorerItem> CreateModuleExploreItems(AerospikeConnection connection)
        {
            List<ExplorerItem> items = new List<ExplorerItem>();

            foreach (var mod in connection.UDFModules.OrderBy(u => u.Name))
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

        public static ExplorerItem CreateInformationalExploreItem(IConnectionInfo cxInfo, AerospikeConnection connection)
		{
			List<ExplorerItem> children;

            children = new List<ExplorerItem>()
                            {
                                new ExplorerItem($"Cluster Name \"{cxInfo.DatabaseInfo.Database}\"",
                                                    ExplorerItemKind.Parameter,
                                                    ExplorerIcon.ScalarFunction)
                                                {
                                                    IsEnumerable = false,
                                                    DragText = null,
                                                    ToolTipText= cxInfo.DatabaseInfo.Database
                                                },
                                new ExplorerItem($"DB Version {cxInfo.DatabaseInfo.DbVersion}",
                                                    ExplorerItemKind.Parameter,
                                                    ExplorerIcon.ScalarFunction)
                                                {
                                                    IsEnumerable = false,
                                                    DragText = null,
                                                    ToolTipText= cxInfo.DatabaseInfo.DbVersion
                                                },
                                new ExplorerItem($"Nodes ({connection.Nodes.Length})",
                                                    ExplorerItemKind.Category,
                                                    ExplorerIcon.OneToMany)
                                                {
                                                    IsEnumerable = false,
                                                    DragText = null,
                                                    Children = CreateClusterNodesExploreItems(connection).ToList(),
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
                                                        new ExplorerItem($"Total Namespaces: {connection.Namespaces.Count()}",
                                                                            ExplorerItemKind.Parameter,
                                                                            ExplorerIcon.ScalarFunction),
                                                        new ExplorerItem($"Total Sets: {connection.Namespaces.Sum(n => n.Sets.Count())}",
                                                                            ExplorerItemKind.Parameter,
                                                                            ExplorerIcon.ScalarFunction),
                                                        new ExplorerItem($"Total Bins: {connection.Namespaces.Sum(n => n.Bins.Count())}",
                                                                            ExplorerItemKind.Parameter,
                                                                            ExplorerIcon.ScalarFunction),
                                                        new ExplorerItem($"Total SIdx: {connection.Namespaces.Sum(n => n.SIndexes.Count())}",
                                                                            ExplorerItemKind.Parameter,
                                                                            ExplorerIcon.ScalarFunction),
                                                        new ExplorerItem($"Total UDFs: {connection.UDFModules.Count()}",
                                                                            ExplorerItemKind.Parameter,
                                                                            ExplorerIcon.ScalarFunction)
                                                    }
                                                },
                                new ExplorerItem("Features",
									                ExplorerItemKind.Category,
													ExplorerIcon.Box)
                                {
									IsEnumerable = false,
									DragText = null,
									Children = connection.DBFeatures
                                                .Select(f => new ExplorerItem(f, ExplorerItemKind.Parameter, ExplorerIcon.ScalarFunction))
                                                .ToList()
								},
								new ExplorerItem("Configuration",
													ExplorerItemKind.Category,
													ExplorerIcon.Box)
								{
									IsEnumerable = false,
									DragText = null,
									Children = connection.DBConfig
												.Select(f => new ExplorerItem(f, ExplorerItemKind.Parameter, ExplorerIcon.ScalarFunction))
												.ToList()
								}
							};

            var infoItem = new ExplorerItem("Information", ExplorerItemKind.Category, ExplorerIcon.Box)
            {
                IsEnumerable = false,
                DragText = null,
                ToolTipText = "Cluster/Source Code Information",
                Children = children
			};

			return infoItem;
        }

        public static IEnumerable<ExplorerItem> CreateClusterNodesExploreItems(AerospikeConnection connection)
        {
            List<ExplorerItem> items = new List<ExplorerItem>();

            foreach (var node in _Connection.Nodes)
            {
				string nodeState = string.Empty;
                (string hostName, bool? pinged) = Helpers.GetHostName(node.Host.name);

                if(pinged.HasValue && !pinged.Value)
                    nodeState = " Cannot Ping";

                if(connection.UseExternalIP)
                    nodeState += " (External/Alternate IP)";
                //else
                //    nodeState = node.Active ? "Active" : "Inactive";

				items.Add(
                            new ExplorerItem($"{hostName}{nodeState}",
												ExplorerItemKind.Property,
												ExplorerIcon.LinkedDatabase)
										{
											IsEnumerable = false,
											DragText = $"\"{node.Host.name}\"",
											ToolTipText = $"Node \"{node.Host.name}\" ({node.Name}) in Cluster \"{connection.Database}\""
										}
                        );
            }

            return items;
        }

        public static void WriteToLog(string message) => DataContextDriver.WriteToLog(message, "AerospikeLINQPadDriver.log");
        
        public static void WriteToLog(Exception ex, string additionalInfo = "") => DataContextDriver.WriteToLog(ex, "AerospikeLINQPadDriver.log", additionalInfo);
       
        #region Data Grid

        /*
        public class SaveChangesAdapterTest : SaveChangesAdapter
        {
			public SaveChangesAdapterTest()
				: base()
			{ 
			}

            public override void DeleteRows<T>(T[] rows)
            {
                
            }

            public override string[] GetEditableMembers()
            {
				return null;
            }

            public override void SaveEditedRow(object row)
            {
                
            }

            public override void SaveNewRow(object originalCollection, object row)
            {
                
            }

            public override bool StartEditSession(object owningControl)
            {
				return true;
            }
        }

        //
        // Summary:
        //     Override this is you want to allow the user to edit data in the DataGrid and
        //     save the changes (usually to a database).
        public override SaveChangesAdapter CreateSaveChangesAdapter(Type elementType, object dataSource, object elementSample, object parent)
		{
            Debugger.Launch();
			return new SaveChangesAdapterTest();
        }
        //
        // Summary:
        //     Override this is you want to customize how an object is displayed in the DataGrid.
        public override void DisplayObjectInGrid(object objectToDisplay, GridOptions options)
		{
			Debugger.Launch();

            var x = (SetRecords)objectToDisplay.GetType().GetField("<>4__this").GetValue(objectToDisplay);

            options.MembersToInclude = x.BinNames;
			options.PanelTitle = x.SetFullName;

            base.DisplayObjectInGrid(objectToDisplay, options);
		}
		*/

        #endregion


#if NETCORE
        // Put stuff here that's just for LINQPad 6+ (.NET Core and .NET 5+).
#else
        // Put stuff here that's just for LINQPad 5 (.NET Framework)
#endif
    }
}