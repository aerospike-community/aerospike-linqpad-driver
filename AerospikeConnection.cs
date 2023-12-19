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

namespace Aerospike.Database.LINQPadDriver
{
    /// <summary>
    /// This class is a wrapper around the Aerospike connection class (<see cref="Aerospike.Client.AerospikeClient"/>)
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("{ConnectionString}")]
    public sealed partial class AerospikeConnection : IDbConnection, IEquatable<AerospikeConnection>
    {
        public static readonly Version NoNSBinsRequest = new Version(7, 0, 0, 0);

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

            this.DBType = connectionInfo.DBType;
            this.CloudNamespace = connectionInfo.NamespaceCloud;

            this.UsePasswordManager = connectionInfo.UsePasswordManager;
            this.PasswordManagerName = connectionInfo.PasswordManagerName?.Trim();
            this.Debug = connectionInfo.Debug;
            this.ConnectionTimeout = connectionInfo.ConnectionTimeout;
            this.TotalTimeout = connectionInfo.TotalTimeout;
            this.SocketTimeout = connectionInfo.SocketTimeout;
            this.SendPK = connectionInfo.SendKey;
            this.ShortQuery = connectionInfo.ShortQuery;
            this.DriverLogging = connectionInfo.DriverLogging;
            this.RespondAllOps = connectionInfo.RespondAllOps;
            this.RecordView = connectionInfo.RecordView;
            this.TLSCertName = connectionInfo.TLSCertName?.Trim();
            if (this.TLSCertName == string.Empty)
                this.TLSCertName = null;

            if (this.DBType == DBTypes.Cloud)
            {
                this.UseExternalIP = false;
                if (this.RecordView == ARecord.DumpTypes.Record)
                    this.RecordView = ARecord.DumpTypes.Dynamic;
                this.DBRecordSampleSet = 0;
                this.DocumentAPI = false;
                this.AlwaysUseAValues = true;
                this.NetworkCompression = false;
                encryptTraffic = false;

                if (string.IsNullOrEmpty(this.TLSCertName))
                    this.TLSCertName = connectionInfo.SeedHosts.FirstOrDefault();
            }
            else
            {
                this.UseExternalIP = connectionInfo.UseExternalIP;                
                this.DBRecordSampleSet = connectionInfo.DBRecordSampleSet;
                this.DBRecordSampleSetMin = (int)Math.Ceiling(this.DBRecordSampleSet * connectionInfo.DBRecordSampleSetPercent);
                this.DocumentAPI = connectionInfo.DocumentAPI;
                this.AlwaysUseAValues = connectionInfo.AlwaysUseAValues;                
                this.NetworkCompression = connectionInfo.NetworkCompression;
            }            

            this.DBVersion = new Version();

            cxInfo.DatabaseInfo.EncryptTraffic = !string.IsNullOrEmpty(connectionInfo.TLSProtocols);

            this.ConnectionString = string.Format("hosts='{0}',user={1}{2},externalIP={3},TLS='{4}',timeout={5},totaltimeout={6},sockettimeout={7},compression={8},{9}IsProduction={10}",
                                                    string.Join(",", connectionInfo.SeedHosts
                                                                        .Select(s => String.Format("{0}:{1}", s, dbPort))),
                                                    cxInfo.DatabaseInfo.UserName,
                                                    this.UsePasswordManager
                                                        ? $",passwordmgrname={this.PasswordManagerName}"
                                                        : (cxInfo.DatabaseInfo.Password is null
                                                                ? string.Empty
                                                                : ",password=" + new string('*', cxInfo.DatabaseInfo.Password.Length)),
                                                    this.UseExternalIP,
                                                    cxInfo.DatabaseInfo.EncryptTraffic
                                                        ? connectionInfo.TLSProtocols
                                                        : string.Empty,
                                                    this.ConnectionTimeout,
                                                    this.TotalTimeout,
                                                    this.SocketTimeout,
                                                    this.NetworkCompression,
                                                    string.IsNullOrEmpty(this.TLSCertName) 
                                                        ? string.Empty 
                                                        : $"TLSCertName={this.TLSCertName},",
                                                    this.CXInfo.IsProduction);

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

        public Node[] Nodes { get => this.DBType == DBTypes.Cloud ? null : this.AerospikeClient?.Nodes; }

        public string CloudNamespace { get; }
        public DBTypes DBType { get; }

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

        public IAerospikeClient AerospikeClient
        {
            get;
            private set;
        }

        public int ConnectionTimeout { get; }
        public int SocketTimeout { get; }
        public int TotalTimeout { get; }
        public bool NetworkCompression { get; }
        public int SleepBetweenRetries { get; }

        public bool SendPK { get; }
        public bool ShortQuery { get; }

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

        public string TLSCertName {  get; }

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
        public void ObtainMetaDate(bool obtainBinsInSet = true, bool closeUponClompletion = true)
        {
            bool performedOpen = false;
            
            //System.Diagnostics.Debugger.Launch();
            try
            {
                if (this.State != ConnectionState.Open)
                {
                    this.Open();
                    performedOpen = true;
                }

                try
                {
                    if (this.DBType == DBTypes.Cloud)
                    {
                        var hostName = this.SeedHosts.FirstOrDefault()?.name ?? "<Unknown>";
                        
                        if (this.AerospikeClient.Connected)
                        {
                            var dbIDIdx = hostName.IndexOf('.');

                            this.Database = dbIDIdx <=0 ? hostName : hostName[0..dbIDIdx];
                            this.Namespaces = LPNamespace.Create(this.CloudNamespace);

                            this.UDFModules = Array.Empty<LPModule>();
                        }
                        else
                        {
                            throw new AerospikeException(11, $"Connection to Cloud Host \"{hostName}\" failed or timed out.");
                        }
                    }
                    else
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

                                    #region Bins in Sets
                                    if (obtainBinsInSet)
                                    {
                                        var getBins = new GetSetBins(this.AerospikeClient,
                                                                        this.SocketTimeout,
                                                                        this.NetworkCompression);

                                        foreach (var ns in this.Namespaces)
                                        {
                                            Parallel.ForEach(ns.Sets,
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
                                                });

                                            if (!ns.Bins.Any())
                                                ns.DetermineUpdateBinsBasedOnSets();
                                        }

                                    }
                                    #endregion

                                    #region Secondary Indexes
                                    {
                                        var sIdxGrp = LPSecondaryIndex.Create(this.Connection, this.Namespaces)
                                                                .GroupBy(idx => new { ns = idx.Namespace?.Name, set = idx.Set?.Name });

                                        foreach (var setIdxs in sIdxGrp)
                                        {
                                            var aIdx = setIdxs.FirstOrDefault();

                                            if (aIdx?.Set != null)
                                                aIdx.Set.SIndexes = setIdxs.ToArray();
                                        }
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
                if (performedOpen && closeUponClompletion)
                    try
                    {
                        this.Close();
                    }
                    catch 
                    {
                        this.State = ConnectionState.Broken;
                    }
            }
        }

        public void Open()
        {            
#if DEBUG
            if (this.Debug)
                System.Diagnostics.Debugger.Launch();
#endif

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
                    writePolicyDefault = new WritePolicy()
                    {
                        compress = this.NetworkCompression,
                        socketTimeout = this.SocketTimeout,
                        totalTimeout = this.ConnectionTimeout,
                        sendKey = this.SendPK,
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
                        totalTimeout = 0,
                        socketTimeout = this.SocketTimeout,
                        compress = this.NetworkCompression,
                        sendKey = this.SendPK,
                        failOnClusterChange = false,
                        sleepBetweenRetries = this.SleepBetweenRetries,
                        shortQuery = this.ShortQuery
                    },
                    readPolicyDefault = new Policy()
                    {
                        compress = this.NetworkCompression,
                        socketTimeout = this.SocketTimeout,
                        totalTimeout = this.ConnectionTimeout,
                        sendKey = this.SendPK,
                        sleepBetweenRetries = this.SleepBetweenRetries
                    }
                };

                if (this.DBType == DBTypes.Cloud)
                {
                    this.AerospikeClient = new AerospikeClientProxy(policy, this.SeedHosts);
                }
                else
                {
                    this.AerospikeClient = new AerospikeClient(policy, this.SeedHosts);

                    var connectionNode = this.AerospikeClient.Nodes.FirstOrDefault(n => n.Active);

                    if (connectionNode != null)
                    {
                        this.Connection = connectionNode.GetConnection(this.ConnectionTimeout);

                        if (this.Connection == null)
                        {
                            throw new AerospikeException(11, $"Connection to {connectionNode.Name} failed or timed out. Cannot obtain meta-data for cluster.");
                        }
                    }
                }

                this.State = ConnectionState.Open;
            }
            catch(AerospikeException.Connection ex)
            {
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
            catch
            {
                this.Close();
                this.State = ConnectionState.Broken;
                throw;
            }
        }
        
        private void DriverLogCallback(Client.Log.Context context, Client.Log.Level level, String message)
        {
            // Put log messages to the appropriate place.
            if (level == Log.Level.ERROR)
                Console.Write(LINQPad.Util.WithStyle(level.ToString(), "color:black;background-color:red"));
            else if(level == Log.Level.WARN)
                Console.Write(LINQPad.Util.WithStyle(level.ToString(), "color:black;background-color:orange"));
            else
                Console.Write(LINQPad.Util.WithStyle(level.ToString(), "color:black;background-color:green"));

            Console.Write(": ");
            Console.WriteLine(LINQPad.Util.WithStyle(message, "color:darkgreen"));
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
                    && this.SleepBetweenRetries == other.SleepBetweenRetries
                    && this.ShortQuery == other.ShortQuery
                    && this.SendPK == other.SendPK
                    && this.RespondAllOps == other.RespondAllOps
                    && this.NetworkCompression == other.NetworkCompression
                    && this.DBRecordSampleSet == other.DBRecordSampleSet
                    && this.DBRecordSampleSetMin == other.DBRecordSampleSetMin;
                            
        }
    }
}
