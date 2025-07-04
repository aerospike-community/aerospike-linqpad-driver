﻿// Ignore Spelling: TLS

using System;
using System.Collections.Generic;
using LINQPad;
using Aerospike.Client;
using System.Data;
using LINQPad.Extensibility.DataContext;
using System.Linq;
using System.Diagnostics;
using Aerospike.Database.LINQPadDriver.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;

namespace Aerospike.Database.LINQPadDriver
{
    /// <summary>
    /// This class is a wrapper around the Aerospike connection class (<see cref="Aerospike.Client.AerospikeClient"/>)
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{ConnectionString}")]
    public sealed partial class AerospikeConnection : IDbConnection, IEquatable<AerospikeConnection>
    {
        public static readonly Version NoNSBinsRequest = new Version(7, 0, 0, 0);
		public static readonly Version NoRosterRequest = new Version(4, 0, 0, 0);
		public static readonly Version NoConfigRequest = new Version(3, 3, 0, 0);

		private bool disposedValue = false;

        public AerospikeConnection(IConnectionInfo cxInfo)
        {
#if DEBUG
            if (this.Debug)
                System.Diagnostics.Debugger.Launch();
#endif
            this.CXInfo = cxInfo;

            var connectionInfo = new ConnectionProperties(cxInfo);
            
            var dbPort = connectionInfo.Port;
            var encryptTraffic = cxInfo.DatabaseInfo.EncryptTraffic;

            this.DBPlatform = connectionInfo.DBType;
            
            this.UsePasswordManager = connectionInfo.UsePasswordManager;
            this.PasswordManagerName = connectionInfo.PasswordManagerName?.Trim();
            this.Debug = connectionInfo.Debug;
            this.ConnectionTimeout = connectionInfo.ConnectionTimeout;
            this.ConnectionsPerNode = connectionInfo.ConnectionsPerNode;
            this.ExpectedDuration = connectionInfo.ExpectedDuration;
            this.TotalTimeout = connectionInfo.TotalTimeout;
            this.Retries = connectionInfo.Retries;
            this.SleepBetweenRetries = connectionInfo.SleepBetweenRetries;
            this.SocketTimeout = connectionInfo.SocketTimeout;
            this.SendPK = connectionInfo.SendKey;
            this.ExpectedDuration = connectionInfo.ExpectedDuration;
            this.DriverLogging = connectionInfo.DriverLogging;
            this.RespondAllOps = connectionInfo.RespondAllOps;
            this.RecordView = connectionInfo.RecordView;
            this.TLSCertName = connectionInfo.TLSCertName?.Trim();
            if (this.TLSCertName == string.Empty)
                this.TLSCertName = null;

            this.DBRecordSampleSet = connectionInfo.DBRecordSampleSet;
            this.DBRecordSampleSetMin = (int)Math.Ceiling(this.DBRecordSampleSet * connectionInfo.DBRecordSampleSetPercent);
            this.DocumentAPI = connectionInfo.DocumentAPI;
            this.AlwaysUseAValues = connectionInfo.AlwaysUseAValues;

            this.UseExternalIP = connectionInfo.UseExternalIP;                
            this.NetworkCompression = connectionInfo.NetworkCompression;

            if(encryptTraffic)
            {
                this.TLSClientCertFile = connectionInfo.TLSClientCertFile;
                (this.TLSCertVerification, this.TLSCertName)
                    = AerospikeConnection.ValidateCert(this.TLSClientCertFile, this.TLSCertName);
            }
            else
            {
                this.TLSClientCertFile = null;
                this.TLSCertName = null;
            }

                this.DBVersion = new Version();

            cxInfo.DatabaseInfo.EncryptTraffic = !string.IsNullOrEmpty(connectionInfo.TLSProtocols);

            {
                var passwordStr = string.Empty;

                if(cxInfo.DatabaseInfo.Password is not null)
                {
                    if(cxInfo.DatabaseInfo.Password == string.Empty)
                    {
                        passwordStr = "password=;";
                    }
                    else
                    {
                        var encrypted = Helpers.Encrypt(cxInfo.DatabaseInfo.Password).ToString();

                        passwordStr = $"password=<encrypted>{encrypted}</encrypted>;";
                    }
                }

                this.ConnectionString = string.Format("hosts={0};user={1};{2}externalIP={3};{4}timeout={5};totaltimeout={6};sockettimeout={7};retries={8},sleepretries={9},connpool={10};compression={11};{12}IsProduction={13}{14}",
                                                        string.Join(",", connectionInfo.SeedHosts
                                                                            .Select(s => String.Format("{0}:{1}", s, dbPort))),
                                                        cxInfo.DatabaseInfo.UserName,
                                                        this.UsePasswordManager
                                                            ? $"passwordmgrname={this.PasswordManagerName};"
                                                            : passwordStr,
                                                        this.UseExternalIP,
                                                        cxInfo.DatabaseInfo.EncryptTraffic
                                                            ? $"TLS={connectionInfo.TLSProtocols};"
                                                            : string.Empty,
                                                        this.ConnectionTimeout,
                                                        this.TotalTimeout,
                                                        this.SocketTimeout,
                                                        this.Retries,
                                                        this.SleepBetweenRetries,
                                                        this.ConnectionsPerNode,
                                                        this.NetworkCompression,
                                                        string.IsNullOrEmpty(this.TLSCertName)
                                                            ? string.Empty
                                                            : $"TLSCertName={this.TLSCertName};",
                                                        this.CXInfo.IsProduction,
                                                        this.DBPlatform == DBPlatforms.Native || this.DBPlatform == DBPlatforms.None
                                                            ? string.Empty
                                                            : $"DBPlatform={this.DBPlatform};");
            }
            cxInfo.DatabaseInfo.CustomCxString = this.ConnectionString;
            cxInfo.DatabaseInfo.Provider = "Aerospike";
            
            this.SeedHosts = connectionInfo.SeedHosts
                                .Select(s => s?.Trim())
                                .Where(s => !string.IsNullOrEmpty(s))
                                .Select(s => new Host(s, this.TLSCertName, dbPort))
                                .ToArray();

            if (encryptTraffic)
            {
                try
                {
                    this.TLS = new TlsPolicy(connectionInfo.TLSProtocols,
                                                connectionInfo.TLSRevokeCerts,
                                                connectionInfo.TLSClientCertFile,
                                                connectionInfo.TLSOnlyLogin);                    
				}
                catch (Exception ex)
                {
                    throw new AerospikeException($"Exception Occurred while creating the TLS Policy. Error is \"{ex.Message}\"");
                }
            }
            else if(!string.IsNullOrEmpty(this.TLSCertName))
            {
                try
                {
                    this.TLS = new TlsPolicy()
                                    {
                                        forLoginOnly = true
                                    };
                    connectionInfo.TLSOnlyLogin = true;
                }
                catch (Exception ex)
                {
                    throw new AerospikeException($"Exception Occurred while creating the TLS Login Only Policy for Cert Name {this.TLSCertName}. Error is \"{ex.Message}\"");
                }
            }

        }

        public IConnectionInfo CXInfo { get; }

        public string ConnectionString { get; set; }

        public Host[] SeedHosts { get; }

        public Node[] Nodes => this.AerospikeClient?.Nodes;

        public DBPlatforms DBPlatform { get; }

        public bool UseExternalIP { get; }

        public bool UsePasswordManager { get; }
        public string PasswordManagerName { get; }

        public Version DBVersion { get; private set; }

        public bool Debug { get; }

        public ARecord.DumpTypes RecordView { get; }

        public int DBRecordSampleSet
        {
            get;
        }

        public int DBRecordSampleSetMin
        {
            get;
        }

        public IEnumerable<LPNamespace> Namespaces { get; private set; }

        public IEnumerable<LPModule> UDFModules { get; private set; }

        public AerospikeClient AerospikeClient
        {
            get;
            private set;
        }

        public int ConnectionTimeout { get; }
        public int SocketTimeout { get; }
        public int TotalTimeout { get; }
        public bool NetworkCompression { get; }
        public int Retries { get; }
        public int SleepBetweenRetries { get; }
        public int ConnectionsPerNode { get; }
        public bool SendPK { get; }
        public QueryDuration ExpectedDuration { get; }

        public bool DocumentAPI { get; }
        public bool AlwaysUseAValues { get; }

        public bool RespondAllOps { get; }

        private bool _driverLogging;

        /// <summary>
        /// Enables the Aerospike Driver&apos;s logging
        /// </summary>
        public bool DriverLogging
        {
            get => this._driverLogging;
            set
            {
                if (this._driverLogging && !value)
                {
                    Client.Log.Disable();
                }
                else if (!this._driverLogging && value)
                {
                    Client.Log.SetContextCallback(DriverLogCallback);
                    Client.Log.DebugEnabled();
                    Client.Log.SetLevel(Log.Level.DEBUG);

                    {
                        var lqDriver = typeof(Aerospike.Database.LINQPadDriver.AerospikeConnection).Assembly.GetName();
                        var asyncClient = typeof(Aerospike.Client.Connection).Assembly.GetName();

                        Client.Log.LogMessage(Log.Level.INFO, $"{lqDriver?.Name} Driver Version: {lqDriver?.Version}");
                        Client.Log.LogMessage(Log.Level.INFO, $"{asyncClient?.Name} Driver Version: {asyncClient?.Version}");
                    }
                }

                this._driverLogging = value;
            }
        }

        public TlsPolicy TLS { get; }

        public string TLSCertName { get; }
        public string TLSClientCertFile { get; }
        public CertHelpers.ResultCodes TLSCertVerification { get; }

		public ClientPolicy ClientPolicy { get; private set; }

        /// <summary>
        /// Cluster Name
        /// </summary>
        public string Database
        {
            get { return this.CXInfo.DatabaseInfo.Database; }
            private set
            {
                this.CXInfo.DatabaseInfo.Database = value;
            }
        }

        public ConnectionState State
        {
            get;
            private set;
        }

        public Connection Connection { get; private set; }

        public IDbTransaction BeginTransaction()
        {
            throw new NotImplementedException();
        }

        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            throw new NotImplementedException();
        }

        public void ChangeDatabase(string databaseName)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {            
            if(this.State == ConnectionState.Open
                    || this.State == ConnectionState.Connecting
                    || this.State == ConnectionState.Broken)
            {
                this.Connection?.Close();
                this.AerospikeClient?.Close();
                this.State = ConnectionState.Closed;
            }
        }

        public IDbCommand CreateCommand()
        {
            throw new NotImplementedException();
        }

#if NET7_0_OR_GREATER
        [GeneratedRegex(@"(?<major>\d+)\.(?<minor>\d+)\.?(?<build>\d*)\.?(?<revision>\d*)",
                        RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        private static partial Regex VersionRegEx();
#else
        private static readonly Regex versionRegex = new Regex(@"(?<major>\d+)\.(?<minor>\d+)\.?(?<build>\d*)\.?(?<revision>\d*)",
                                                                RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex VersionRegEx() => versionRegex;
#endif

		/// <summary>
		/// Returns the LINQPad&apos;s internal structure by querying the DB is required.
        /// The returned instances can be used to obtain meta data about the DB and even the ability to generate code.
        /// You can, also, use the instance to obtain schema information about namespaces, sets, bins, UDF, etc.
        /// 
        /// Note: this will update this instance with the returned information.
		/// </summary>
		/// <param name="forceRefresh">if set to <c>true</c>, the data is refreshed from the DB.</param>
		/// <returns>Returns IEnumerable&lt;LPNamespace&gt; instances or <seealso cref="Namespaces"/>.</returns>
		public IEnumerable<LPNamespace> GetDBInfo(bool forceRefresh = false)
        {
            if (this.Namespaces is null || forceRefresh)
                this.ObtainMetaData();

            return this.Namespaces;
        }

		/// <summary>
		/// Obtains the meta data from the DB including namespaces, sets, bins, and other DB information.
		/// </summary>
		/// <param name="obtainBinsInSet">if set to <c>true</c> obtain bins in set.</param>
		/// <param name="closeUponCompletion">if set to <c>true</c> and if the connection is created, it will be closed..</param>
		/// <exception cref="Aerospike.Client.AerospikeException">11 - Connection to Cloud Host \"{hostName}\" failed.</exception>
		/// <exception cref="Aerospike.Client.AerospikeException">11 - Connection to Cloud Host \"{hostName}\" failed or timed out.</exception>
		/// <exception cref="Aerospike.Client.AerospikeException">11 - Connection to {connectionNode.Name} failed or timed out. Cannot obtain meta-data for cluster.</exception>
		/// <exception cref="Aerospike.Client.AerospikeException">11 - No active node found in cluster \"{this.Database}\". Tried: {nodes}</exception>
		public void ObtainMetaData(bool obtainBinsInSet = true, bool closeUponCompletion = true)
        {
            
            bool performedOpen = false;

			if(Client.Log.InfoEnabled())
			{
				Client.Log.Info($"ObtainMetaDate Start {this.ConnectionString}");
			}

			//System.Diagnostics.Debugger.Launch();
			try
            {
                if (this.State != ConnectionState.Open)
                {
                    this.Open();
                    performedOpen = true;
                }

                #region Bins in Sets
                void DetermineBins(bool noInfoRequestBins)
                {
                    var getBins = new GetSetBins(this.AerospikeClient,
                                                    this.SocketTimeout,
                                                    this.NetworkCompression);

                    var parallelOptions = new ParallelOptions()
					{
                        MaxDegreeOfParallelism = 3
					};

					foreach (var ns in this.Namespaces)
                    {                        
                        Parallel.ForEach(ns.Sets,
							parallelOptions,
							(set, cancelationToken) =>
                            {
                                if (set.IsNullSet && ns.Bins.Any())
                                    set.UpdateTypeBins(ns.Bins, false);
                                else
                                    set.GetRecordBins(getBins,
                                                        this.DocumentAPI,
                                                        noInfoRequestBins && this.DBRecordSampleSet <= 0 ? 10 : this.DBRecordSampleSet,
                                                        noInfoRequestBins && this.DBRecordSampleSetMin <= 0 ? 1 : this.DBRecordSampleSetMin,
                                                        false);
								if(Client.Log.DebugEnabled())
								{
									Client.Log.Info($"ObtainMetaDate Set {set}");
								}
							});

                        if (!ns.Bins.Any())
                            ns.DetermineUpdateBinsBasedOnSets();

						if(Client.Log.DebugEnabled())
						{
							Client.Log.Info($"ObtainMetaDate NS {ns}");
						}
					}

                }
                #endregion

                try
                {
                    if (this.AerospikeClient.Connected)
                    {
                        var connectionNode = this.AerospikeClient.Nodes.FirstOrDefault(n => n.Active);

                        if (connectionNode != null)
                        {
                            if (this.Connection == null)
                            {
                                throw new AerospikeException(11, $"Connection to {connectionNode.Name} failed or timed out. Cannot obtain meta-data for cluster.");
                            }
                            else
                            {
                                this.Database = Info.Request(this.Connection, "cluster-name");

                                if (string.IsNullOrEmpty(this.Database) || this.Database == "null")
                                {
                                    this.Database = this.AerospikeClient.Cluster.GetStats().ToString();
                                }

                                bool noInfoRequestBins = false;

                                this.CXInfo.DatabaseInfo.DbVersion = Info.Request(this.Connection, "version");

                                if (!string.IsNullOrEmpty(this.CXInfo.DatabaseInfo.DbVersion))
                                {
                                    try
                                    {
                                        var versionMatch = VersionRegEx().Match(this.CXInfo.DatabaseInfo.DbVersion);

                                        if (versionMatch.Success)
                                        {
                                            int major = 0;
                                            int minor = 0;
                                            int build = 0;
                                            int revision = 0;

                                            if (versionMatch.Groups["major"].Success)
                                                major = int.Parse(versionMatch.Groups["major"].Value);
                                            if (versionMatch.Groups["minor"].Success)
                                                minor = int.Parse(versionMatch.Groups["minor"].Value);
                                            if (versionMatch.Groups["build"].Success)
                                                build = int.Parse(versionMatch.Groups["build"].Value);
                                            if (versionMatch.Groups["revision"].Success)
                                                revision = int.Parse(versionMatch.Groups["revision"].Value);

                                            this.DBVersion = new Version(major, minor, build, revision);
                                            noInfoRequestBins = this.DBVersion >= AerospikeConnection.NoNSBinsRequest;
                                        }
                                    }
                                    catch { }
                                }

                                this.Namespaces = LPNamespace.Create(this.Connection, this.DBVersion);

                                this.UDFModules = LPModule.Create(this.Connection);
                                   
                                if (obtainBinsInSet)
                                {
                                    DetermineBins(noInfoRequestBins);
                                }
                                    
                                #region Secondary Indexes
                                {
                                    var sIdxGrp = LPSecondaryIndex.Create(this.Connection, this.Namespaces)
                                                            .GroupBy(idx => new { ns = idx.Namespace?.Name, set = idx.Set?.Name });

                                    foreach (var setIdxs in sIdxGrp)
                                    {
                                        var aIdx = setIdxs.FirstOrDefault();

                                        if (aIdx?.Set != null)
                                            aIdx.Set.SIndexes = setIdxs.ToArray();

										if(Client.Log.DebugEnabled())
										{
											Client.Log.Info($"ObtainMetaDate SIdx {setIdxs}");
										}
									}
                                }
                                #endregion

                                #region Features
                                {
                                    var features = Info.Request(this.Connection, "features");
                                    if(string.IsNullOrEmpty(features))
                                        this.DBFeatures = Enumerable.Empty<string>();
                                    else
                                        this.DBFeatures = features.Split(';')
															.OrderBy(c => c.Split('=')[0])
                                                            .ToArray();
                                }
								#endregion
								#region Configuration
								{
									var configs = Info.Request(this.Connection, "get-config");
									if(string.IsNullOrEmpty(configs))
                                        this.DBConfig = Enumerable.Empty<string>();
                                    else
                                        this.DBConfig = configs.Split(';')
															.OrderBy(c => c.Split('=')[0])
                                                            .ToArray();
								}
								#endregion
							}
						}
                        else
                        {
                            var nodes = string.Join(',', this.AerospikeClient.Nodes.Select(n => n.Name));
                            throw new AerospikeException(11, $"No active node found in cluster \"{this.Database}\". Tried: {nodes}");
                        }
                    }
                }
                catch
                {
                    this.Close();
                    this.State = ConnectionState.Broken;
                    throw;
                }
            }
            finally
            {
                if (performedOpen && closeUponCompletion)
                    try
                    {
                        this.Close();
                    }
                    catch 
                    {
                        this.State = ConnectionState.Broken;
                    }
				if(Client.Log.InfoEnabled())
				{
					Client.Log.Info($"ObtainMetaDate Ended {this.ConnectionString}");
				}
			}
        }

        static readonly Dictionary<string, AerospikeClient> Connections = new();

		public AerospikeClient GetConnectionNative(ClientPolicy policy)
        {
			if(Client.Log.InfoEnabled())
			{
				Client.Log.Info($"GetConnectionNative Start {this.ConnectionString}");
			}

			lock(Connections)
            {
                if(Connections.TryGetValue(this.ConnectionString, out var conn))
                {
					if(Client.Log.InfoEnabled())
					{
						Client.Log.Info($"GetConnectionNative Fnd Connection Pool {this.ConnectionString}");
					}
					return conn;
                }                

				var newconn = new AerospikeClient(policy, this.SeedHosts);
				Connections.Add(this.ConnectionString, newconn);
				if(Client.Log.InfoEnabled())
				{
					Client.Log.Info($"GetConnectionNative Created {this.ConnectionString}");
				}
				return newconn;
			}
        }

		public void Open()
        {            
#if DEBUG
            if (this.Debug)
                System.Diagnostics.Debugger.Launch();
#endif
            if (Client.Log.InfoEnabled())
            {
                Client.Log.Info($"Open Start {this.ConnectionString}");
            }

            static string GetPasswordName(string name)
            {
                return (string)typeof(LINQPad.Util).Assembly.GetType("LINQPad.PasswordManager")
                            ?.GetMethod("GetPassword")
                            ?.Invoke(null, new object[] {name});
            }

            this.State = ConnectionState.Connecting;

            try
            {
                var password = this.CXInfo.DatabaseInfo.Password;

                if(this.UsePasswordManager)
                {
                    var newPassword = GetPasswordName(this.PasswordManagerName)
                                        ?? throw new KeyNotFoundException($"A LINQPad Password Manager Name of \"{this.PasswordManagerName}\" was provided but was not found in LINQPAD. You need to define the password association with name or disable Password Manager use!");
                    password = newPassword;
                }

                var userName = this.CXInfo.DatabaseInfo.UserName;

                if(string.IsNullOrEmpty(userName) )
                {
                    userName = null;
                    password = null;
                }

                var policy = new ClientPolicy()
                {
                    timeout = this.SocketTimeout,
                    loginTimeout = this.ConnectionTimeout,
                    useServicesAlternate = this.UseExternalIP,
                    tlsPolicy = this.TLS,
                    user = userName,
                    password = password,
                    connPoolsPerNode = this.ConnectionsPerNode <= 0
                                        ? (int) Math.Round(Environment.ProcessorCount / 8m, MidpointRounding.AwayFromZero)
                                        : this.ConnectionsPerNode,
                    
                    writePolicyDefault = new WritePolicy()
                    {
                        compress = this.NetworkCompression,
                        socketTimeout = this.SocketTimeout,
                        totalTimeout = this.TotalTimeout,
                        sendKey = this.SendPK,
                        maxRetries = this.Retries,
                        sleepBetweenRetries = this.SleepBetweenRetries,
                        recordExistsAction = RecordExistsAction.UPDATE,
                        respondAllOps = this.RespondAllOps,
                    },
                    infoPolicyDefault = new InfoPolicy()
                    {
                        timeout = this.SocketTimeout
                    },
                    queryPolicyDefault = new QueryPolicy()
                    {
                        totalTimeout = this.TotalTimeout,
                        socketTimeout = this.SocketTimeout,
                        compress = this.NetworkCompression,
                        sendKey = this.SendPK,
                        failOnClusterChange = false,
                        maxRetries = this.Retries,
                        sleepBetweenRetries = this.SleepBetweenRetries,
                        expectedDuration = this.ExpectedDuration
                    },
                    readPolicyDefault = new Policy()
                    {
                        compress = this.NetworkCompression,
                        socketTimeout = this.SocketTimeout,
                        totalTimeout = this.TotalTimeout,
                        sendKey = this.SendPK,
                        maxRetries = this.Retries,
                        sleepBetweenRetries = this.SleepBetweenRetries
                    },
                    scanPolicyDefault = new ScanPolicy()
                    {
						compress = this.NetworkCompression,
						sleepBetweenRetries = this.SleepBetweenRetries,						
						sendKey = this.SendPK,
						socketTimeout = this.SocketTimeout,
						totalTimeout = this.TotalTimeout,
						failOnFilteredOut = false,
                        includeBinData = true,
                        maxRetries = this.Retries,
                        recordsPerSecond = 0
					}
                };
                this.ClientPolicy = policy;

                this.AerospikeClient = this.GetConnectionNative(policy);

                var connectionNode = this.AerospikeClient.Nodes.FirstOrDefault(n => n.Active);

                if (connectionNode != null)
                {
                    this.Connection = connectionNode.GetConnection(this.ConnectionTimeout);

                    if (this.Connection == null)
                    {
                        throw new AerospikeException(11, $"Connection to {connectionNode.Name} failed or timed out. Cannot obtain meta-data for cluster.");
                    }
                }

                this.State = ConnectionState.Open;
            }
            catch(AerospikeException.Connection ex)
            {
                if(this.Debug || Client.Log.DebugEnabled())
                    DynamicDriver.WriteToLog(ex, "AerospikeConnection.Open");
                
                this.Close();
                this.State = ConnectionState.Broken;

                if (ex.Result == -8
                        && ex.Message.StartsWith("Error -8: Failed to connect to host(s):")
                        && ex.Message.EndsWith("Error -3: Node name is null\r\n")
                        && this.SeedHosts.All(h => !string.IsNullOrEmpty(h.name)))                        
                {
                    throw new AerospikeException.Connection(ex.Result,
                                                            "Authentication Error. Invalid or Missing User Name or Password?");
                }

                throw;
            }
            catch(Exception ex)
            {
                if (this.Debug || Client.Log.DebugEnabled())
                    DynamicDriver.WriteToLog(ex, "AerospikeConnection.Open");

                this.Close();
                this.State = ConnectionState.Broken;
                throw;
            }

            if (Client.Log.InfoEnabled())
            {
                Client.Log.Info($"Open Exit {this}");
            }
        }

		/// <summary>
		/// Attempt to commit the given multi-record transaction. First, the expected record versions are
		/// sent to the server nodes for verification.If all nodes return success, the command is
		/// committed. Otherwise, the transaction is aborted.
		/// <p>
		/// Requires server version 8.0+
		/// </p>
		/// </summary>
		/// <param name="txn">multi-record transaction</param>
		public CommitStatus.CommitStatusType Commit(Txn txn)
            => this.AerospikeClient?.Commit(txn) ?? CommitStatus.CommitStatusType.CLOSE_ABANDONED;

        private static (CertHelpers.ResultCodes, string) ValidateCert(string certFilePath, string certName)
        {
			if(Client.Log.InfoEnabled())
			{
				Client.Log.Info($"ValidateCert Testing Cert from path '{certFilePath}' issue to {certName}");
			}
				
            var certResult = CertHelpers.Validate(certFilePath);
            var issueto = CertHelpers.ToIssuer(certResult.Item2);
            var result = certResult.Item1;
            
			if(string.IsNullOrEmpty(certName))
			{
				if(Client.Log.InfoEnabled())
				{
					Client.Log.Info($"ValidateCert Cert Using CN {issueto} from path '{certFilePath}' issue to {certName}");
				}
				certName = issueto;
			}
			else if(!string.IsNullOrEmpty(issueto) && certName != issueto)
			{
				if(result == CertHelpers.ResultCodes.Success
                        || result == CertHelpers.ResultCodes.Unknown)
					result = CertHelpers.ResultCodes.WrongTLSCommonName;

				if(Client.Log.InfoEnabled())
				{
					Client.Log.Info($"ValidateCert Cert CN Provided {certName} but found CN {issueto} from path '{certFilePath}' issue to {certName}");
				}
			}

			if(Client.Log.InfoEnabled())
			{
				Client.Log.Info($"ValidateCert Testing Cert Verified {result} Subject '{certResult.Item2}' from path '{certFilePath}' issue to {certName}");
			}

            return (result, certName);
		}

		/// <summary>
		/// Abort and rollback the given multi-record transaction.
		/// <p>
		/// Requires server version 8.0+
		/// </p>
		/// </summary>
		/// <param name="txn">multi-record transaction</param>
		public AbortStatus.AbortStatusType Abort(Txn txn)
			=> this.AerospikeClient?.Abort(txn) ?? AbortStatus.AbortStatusType.ROLL_BACK_ABANDONED;

		private void DriverLogCallback(Client.Log.Context context, Client.Log.Level level, String message)
        {
            // Put log messages to the appropriate place.
            if(!Thread.CurrentThread.IsBackground)
            {                
                if(level == Log.Level.ERROR)
                    Console.Write(LINQPad.Util.WithStyle(level.ToString(), "color:black;background-color:red"));
                else if(level == Log.Level.WARN)
                    Console.Write(LINQPad.Util.WithStyle(level.ToString(), "color:black;background-color:orange"));
                else
                    Console.Write(LINQPad.Util.WithStyle(level.ToString(), "color:black;background-color:green"));

                Console.Write(": ");
                Console.WriteLine(LINQPad.Util.WithStyle(message, "color:darkgreen"));
            }
            DynamicDriver.WriteToLog($"{level} - {message}");
        }

		/// <summary>
		/// Returns the Aerospike features that are enabled or null.
		/// </summary>
		public IEnumerable<string> DBFeatures
        {
            get;
            private set;
        }

		/// <summary>
		/// Returns running configuration for this cluster or null.
		/// </summary>
		public IEnumerable<string> DBConfig
        {
            get;
            private set;
		}


        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {                    
                    this.Close();
                }
               
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public override bool Equals(object obj)
        {
            if(obj is null) return false;
            if(ReferenceEquals(this, obj)) return true;
            if(obj is AerospikeConnection aConnection) return this.Equals(aConnection);
            if(obj is string connectionString) return this.ConnectionString == connectionString;

            return false;
        }

        public override string ToString()
        {
            return this.ConnectionString;
        }

        public override int GetHashCode()
        {
            return this.ConnectionString?.GetHashCode() ?? 0;
        }

        const string ConnectionStringRegExStr = @"(?<kvp>(?<type>[^=]+)\s*=\s*('\s*(?<value>[^']+)\s*')?(?<value>[^,]+)?\s*,?)+";
        const string ConnectionStringKVPRegExStr = @"(?<type>[^=]+)\s*=\s*('\s*(?<value>[^']+)\s*')?(?<value>[^,]+)?\s*,?";

#if NET7_0_OR_GREATER
        //hosts='a,b,c,d',user={1},password={2},externalIP={3},TLS='{4}',timeout={5},totaltimeout={6},sockettimeout={7},compression={8},IsProduction={9}
        [GeneratedRegex(ConnectionStringRegExStr,
                            RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex ConnectionStringRegEx();

        [GeneratedRegex(ConnectionStringKVPRegExStr,
                            RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex ConnectionStringKVPRegEx();
#else
        readonly static Regex ConnectionStringRegExVar = new Regex(ConnectionStringRegExStr,
                                                                        RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static Regex ConnectionStringRegEx() => ConnectionStringRegExVar;

        readonly static Regex ConnectionStringKVPRegExVar = new Regex(ConnectionStringKVPRegExStr,
                                                                                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static Regex ConnectionStringKVPRegEx() => ConnectionStringKVPRegExVar;
#endif
        public static bool CXInfoEquals(IConnectionInfo c1, IConnectionInfo c2)
        {
            if (ReferenceEquals(c1, c2)) return true;
            if (c1.DisplayName == c2.DisplayName) return true;

            if (string.IsNullOrEmpty(c1.DatabaseInfo.CustomCxString)
                    || string.IsNullOrEmpty(c2.DatabaseInfo.CustomCxString))
            {
                return c1.DatabaseInfo.Provider == c2.DatabaseInfo.Provider
                        && c1.DatabaseInfo.DbVersion == c2.DatabaseInfo.DbVersion
                        && c1.DatabaseInfo.Server == c2.DatabaseInfo.Server
                        && c1.DatabaseInfo.UserName == c2.DatabaseInfo.UserName
                        && c1.IsProduction == c2.IsProduction
                        && c1.DatabaseInfo.EncryptTraffic == c2.DatabaseInfo.EncryptTraffic;
            }

            if (c1.DatabaseInfo.CustomCxString == c2.DatabaseInfo.CustomCxString) return true;
            
            var c1Hosts = ConnectionStringRegEx().Match(c1.DatabaseInfo.CustomCxString.ToUpperInvariant());
            var c2Hosts = ConnectionStringRegEx().Match(c2.DatabaseInfo.CustomCxString.ToUpperInvariant());

            if(!c1Hosts.Success 
                || !c2Hosts.Success) return false;

            static IEnumerable<KeyValuePair<string, string>> MergeIntoKVP(CaptureCollection kvpCaptures)
            {
                var kvpLst = new List<KeyValuePair<string, string>>();

                foreach (var kvpStr in kvpCaptures.Select(c => c.Value))
                {
                    var kvpMatch = ConnectionStringKVPRegEx().Match(kvpStr);

                    if(kvpMatch.Success)
                        kvpLst.Add(new KeyValuePair<string, string>(kvpMatch.Groups["type"].Captures
                                                                        .FirstOrDefault()
                                                                        .Value,
                                                                    kvpMatch.Groups["value"].Captures
                                                                        .FirstOrDefault()
                                                                        ?.Value));
                }

                return kvpLst;
            }

            var connectionKVP1 = MergeIntoKVP(c1Hosts.Groups["kvp"].Captures)
                                    .OrderBy(kvp => kvp.Key)
                                    .ToArray();
            var connectionKVP2 = MergeIntoKVP(c2Hosts.Groups["kvp"].Captures)
                                    .OrderBy(kvp => kvp.Key)
                                    .ToArray();

            if (connectionKVP1.Length != connectionKVP2.Length) return false;

            if (connectionKVP1.Length == 0 && connectionKVP2.Length == 0) return false;            

            for (var idx = 0; idx < connectionKVP1.Length; idx++)
            {
                if(connectionKVP1[idx].Key != connectionKVP2[idx].Key) return false;

                var value1 = connectionKVP1[idx].Value?.Split(',').OrderBy(v => v);
                var value2 = connectionKVP2[idx].Value?.Split(',').OrderBy(v => v);

                if (value1 is null && value2 is null) continue;
                if (value1 is null) return false;
                if (value2 is null) return false;

                if (!value1.Any())
                {
                    if (value2.Any())
                        return false;

                    continue;
                }
                if (!value2.Any())
                {
                    return false;
                }

                if (connectionKVP1[idx].Key == "HOSTS")
                {
                    if(!value1.Any(v1 => value2.Any(v2 => v1 == v2))) return false;
                }
                else if(!value1.SequenceEqual(value2)) return false;                
            }

            return true;            
        }

        private bool TLSEqual(AerospikeConnection other)
        {
            if(ReferenceEquals(this.TLS, other.TLS)) return true;

            if(this.CXInfo.DatabaseInfo.EncryptTraffic != other.CXInfo.DatabaseInfo.EncryptTraffic) return false;
            if (!this.CXInfo.DatabaseInfo.EncryptTraffic && !other.CXInfo.DatabaseInfo.EncryptTraffic) return true;

            if (this.TLS.forLoginOnly != other.TLS.forLoginOnly) return false;

            if (this.TLS.revokeCertificates is null) return other.TLS.revokeCertificates is null;
            if (other.TLS.revokeCertificates is null) return false;
            if (this.TLS.revokeCertificates.SequenceEqual(other.TLS.revokeCertificates)) return true;

            if (this.TLS.clientCertificates is null) return other.TLS.clientCertificates is null;            
            if (this.TLS.clientCertificates.Equals(other.TLS.clientCertificates)) return true;

            return this.TLS.protocols.Equals(other.TLS.protocols);
        }

        public bool Equals([AllowNull] AerospikeConnection other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return this.SeedHosts.Any(x => other.SeedHosts.Any(y => y.Equals(x)))
                    && this.CXInfo.DatabaseInfo.UserName == other.CXInfo.DatabaseInfo.UserName
                    && this.UsePasswordManager == other.UsePasswordManager
                    && (this.UsePasswordManager
                            ? this.PasswordManagerName == other.PasswordManagerName
                            : this.CXInfo.DatabaseInfo.Password == other.CXInfo.DatabaseInfo.Password)
                    && this.TLSEqual(other)
                    && this.CXInfo.IsProduction == other.CXInfo.IsProduction
                    //&& this.CXInfo.DynamicSchemaOptions == other.CXInfo.DynamicSchemaOptions
                    && this.CXInfo.Persist == other.CXInfo.Persist
                    && this.CXInfo.PopulateChildrenOnStartup == other.CXInfo.PopulateChildrenOnStartup
                    && this.DocumentAPI == other.DocumentAPI
                    && this.RecordView == other.RecordView
                    && this.AlwaysUseAValues == other.AlwaysUseAValues
                    && this.TotalTimeout == other.TotalTimeout
                    && this.UseExternalIP == other.UseExternalIP
                    && this.SocketTimeout == other.SocketTimeout
                    && this.ConnectionsPerNode == other.ConnectionsPerNode
                    && this.SleepBetweenRetries == other.SleepBetweenRetries
                    && this.Retries == other.Retries
                    && this.ExpectedDuration == other.ExpectedDuration
                    && this.SendPK == other.SendPK
                    && this.RespondAllOps == other.RespondAllOps
                    && this.NetworkCompression == other.NetworkCompression
                    && this.DBRecordSampleSet == other.DBRecordSampleSet
                    && this.DBRecordSampleSetMin == other.DBRecordSampleSetMin;
                            
        }
    }
}
