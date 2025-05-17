using LINQPad.Extensibility.DataContext;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Aerospike.Database.LINQPadDriver.Extensions;
using System.Data;
using Aerospike.Client;

namespace Aerospike.Database.LINQPadDriver
{
    
    /// <summary>
    /// Wrapper to read/write connection properties. This acts as our ViewModel - we will bind to it in ConnectionDialog.xaml.
    /// </summary>
    internal class ConnectionProperties
	{
		public IConnectionInfo ConnectionInfo { get; private set; }

		XElement DriverData => ConnectionInfo.DriverData;
        
		public ConnectionProperties (IConnectionInfo cxInfo)
		{
            //Debugger.Launch();

            ConnectionInfo = cxInfo;

            if (string.IsNullOrEmpty(cxInfo.DatabaseInfo.Server))
                cxInfo.DatabaseInfo.Server = "localhost";

            InitializeTLSProtocols();
            InitializeRecordViews();
            InitializeExpectedDuration();

            ARecord.DefaultASPIKeyName = this.PKName;

        }

        // This is how to create custom connection properties.

        public DBPlatforms DBType
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("DBType", "Native");
                    return DBPlatforms.Native;
                }

                var elementValue = DriverData.Element("DBType")?.Value;

                if (string.IsNullOrEmpty(elementValue))
                {
                    DriverData.SetElementValue("DBType", "Native");
                    return DBPlatforms.Native;
                }
                else if (elementValue == "0")
                {
                    DriverData.SetElementValue("DBType", "Native");
                    return DBPlatforms.Native;
                }
                
                if (Enum.TryParse<DBPlatforms>(elementValue, true, out DBPlatforms result))
                {
                    return result;
                }

                return DBPlatforms.Native;
            }
            set
            {
                DriverData.SetElementValue("DBType", value.ToString());                
            }
        }

        public int DBTypeIdx
        {
            get => (int)this.DBType;
            set => this.DBType = (DBPlatforms)value;
        }


        public IEnumerable<string> SeedHosts
		{
			get
			{
				var hostStr = this.ConnectionInfo.DatabaseInfo.Server;
				var hostLst = (hostStr ?? "localhost")
								.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
								.Select(h => h.Trim());

                return hostLst.Any() ? hostLst : new string[] { "localhost" };
			}
			set
			{
				var hostStr = "localhost";
				if(value?.Any() == true)
				{
					hostStr = String.Join(',', value);
				}
				
                this.ConnectionInfo.DatabaseInfo.Server = hostStr;
            }
		}

        public int Port
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("Port", 3000);
                    return 3000;
                }

                return (int?) DriverData.Element("Port") ?? 3000;                
            }
            set
            {                
                DriverData.SetElementValue("Port", value);
            }
        }

        public bool UseExternalIP
        {
            get
            {
				if(DriverData.IsEmpty)
				{
                    DriverData.SetElementValue("UseExternalIP", false);
					return	false;
                }

                return (bool?)DriverData.Element("UseExternalIP") ?? false;
            }
            set
            {                
                DriverData.SetElementValue("UseExternalIP", value);
            }
        }

        public bool UsePasswordManager
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("UsePasswordManager", false);
                    return false;
                }

                return (bool?)DriverData.Element("UsePasswordManager") ?? false;
            }
            set
            {
                DriverData.SetElementValue("UsePasswordManager", value);
            }
        }

        public string PasswordManagerName
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("PasswordManagerName", null);
                    return null;
                }

                return DriverData.Element("PasswordManagerName")?.Value;
            }
            set
            {
                if (value == string.Empty) value = null;
                DriverData.SetElementValue("PasswordManagerName", value);
            }
        }        

        public bool Debug
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("Debug", false);
                    return false;
                }

                return (bool?)DriverData.Element("Debug") ?? false;
            }
            set
            {
                DriverData.SetElementValue("Debug", value);
            }
        }

        public string NamespaceCloud
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("NamespaceCloud", "aerospike_cloud");
                    return "aerospike_cloud";
                }

                return (string)DriverData.Element("NamespaceCloud") ?? "aerospike_cloud";
            }
            set
            {
                if (value == string.Empty) value = null;
                DriverData.SetElementValue("NamespaceCloud", value);
            }
        }

        public string SetNamesCloud
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    return null;
                }

                var value = (string)DriverData.Element("SetNamesCloud");

                return value == string.Empty ? null : value;
            }
            set
            {
                if (value == string.Empty) value = null;
                DriverData.SetElementValue("SetNamesCloud", value);
            }
        }

        public int DBRecordSampleSet
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("DBRecordSampleSet", 10);
                    return 10;
                }

                return (int?)DriverData.Element("DBRecordSampleSet") ?? 10;
            }
            set
            {
                DriverData.SetElementValue("DBRecordSampleSet", value);
            }
        }

        public decimal DBRecordSampleSetPercent
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("DBRecordSampleSetPercent", 0.50m);
                    return 0.50m;
                }

                return (decimal?)DriverData.Element("DBRecordSampleSetPercent") ?? 0.50m;
            }
            set
            {
                DriverData.SetElementValue("DBRecordSampleSetPercent", value);
            }
        }

        public string DBRecordSampleSetPercentStr
        {
            get
            {
                return this.DBRecordSampleSetPercent.ToString("P0");
            }
            set
            {
                if(string.IsNullOrEmpty(value))
                {
                    this.DBRecordSampleSetPercent = 0.5m;
                    return;
                }

                var valueWithoutPercentage = value.TrimEnd(' ', '%');
                this.DBRecordSampleSetPercent = decimal.Parse(valueWithoutPercentage) / 100;
            }
        }

        public bool NetworkCompression
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("NetworkCompression", false);
                    return false;
                }

                return (bool?)DriverData.Element("NetworkCompression") ?? false;
            }
            set
            {
                DriverData.SetElementValue("NetworkCompression", value);
            }
        }

        public int TotalTimeout
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("TotalTimeout", 1000);
                    return 1000;
                }

                return (int?)DriverData.Element("TotalTimeout") ?? 1000;
            }
            set
            {                
                DriverData.SetElementValue("TotalTimeout", value);
            }
        }

        public int SocketTimeout
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("SocketTimeout", 1000);
                    return 1000;
                }

                return (int?)DriverData.Element("SocketTimeout") ?? 1000;
            }
            set
            {
                DriverData.SetElementValue("SocketTimeout", value);
            }
        }

        public int ConnectionTimeout
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("ConnectionTimeout", 1000);
                    return 1000;
                }

                return (int?)DriverData.Element("ConnectionTimeout") ?? 1000;
            }
            set
            {
                DriverData.SetElementValue("ConnectionTimeout", value);
            }
        }

		public int Retries
		{
			get
			{
				if(DriverData.IsEmpty)
				{
					DriverData.SetElementValue("Retries", 2);
					return 2;
				}

				return (int?) DriverData.Element("Retries") ?? 2;
			}
			set
			{
				DriverData.SetElementValue("Retries", value);
			}
		}

		public int SleepBetweenRetries
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("SleepBetweenRetries", 0);
                    return 0;
                }

                return (int?)DriverData.Element("SleepBetweenRetries") ?? 0;
            }
            set
            {
                DriverData.SetElementValue("SleepBetweenRetries", value);
            }
        }

        public int ConnectionsPerNode
        {
			get
			{
				if(DriverData.IsEmpty)
				{
					DriverData.SetElementValue("ConnectionsPerNode", 1);
					return 1;
				}

				return (int?) DriverData.Element("ConnectionsPerNode") ?? 1;
			}
			set
			{
				DriverData.SetElementValue("ConnectionsPerNode", value);
			}
		}

		public bool SendKey
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("SendKey", true);
                    return true;
                }

                return (bool?)DriverData.Element("SendKey") ?? true;
            }
            set
            {
                DriverData.SetElementValue("SendKey", value);
            }
        }

		#region QueryDuration Options

		public QueryDuration ExpectedDuration
		{
			get
			{
				if(DriverData.IsEmpty)
				{
					DriverData.SetElementValue("ExpectedDuration", "Long");
					return QueryDuration.LONG;
				}

				var elementValue = DriverData.Element("ExpectedDuration")?.Value;
				
				if(Enum.TryParse<QueryDuration>(elementValue, true, out QueryDuration result))
				{
					return result;
				}

				return QueryDuration.LONG;
			}
			set
			{
				DriverData.SetElementValue("ExpectedDuration", value.ToString());
			}
		}

		public class ExpectedDurationItem : INotifyPropertyChanged
		{

			internal ExpectedDurationItem(ConnectionProperties connectionProperties)
			{
				ConnectionProperties = connectionProperties;
			}

			private ConnectionProperties ConnectionProperties { get; }

			public string Content { get; set; }
			public bool UpdateExpectedDuration { get; set; }

			private bool _isChecked;
			public bool IsChecked
			{
				get { return this._isChecked; }
				set
				{
					if(this._isChecked != value)
					{
						this._isChecked = value;
						this.NotifyIsCheckedProperty();

						if(this._isChecked
								&& this.UpdateExpectedDuration
								&& Enum.TryParse<QueryDuration>(this.Name, true, out QueryDuration result))
						{
							this.ConnectionProperties.ExpectedDuration = result;
						}
					}
				}
			}

			private bool _isEnabled = true;
			public bool IsEnabled
			{
				get { return this._isEnabled; }
				set
				{
					if(this._isEnabled != value)
					{
						this._isEnabled = value;
						this.NotifyIsEnabledProperty();
					}
				}
			}
			public string ToolTip { get; set; }
			public string Name { get; set; }

			public event PropertyChangedEventHandler PropertyChanged;

			private void NotifyPropertyChanged(String info)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
			}

			public void NotifyIsCheckedProperty()
			{
				this.NotifyPropertyChanged("IsChecked");
			}

			public void NotifyIsEnabledProperty()
			{
				this.NotifyPropertyChanged("IsEnabled");
			}
		}

		private List<ExpectedDurationItem> _expectedDurationItemList = null;

		private void InitializeExpectedDuration()
		{
			_expectedDurationItemList = new List<ExpectedDurationItem>();

            foreach(var item in Enum.GetValues(typeof(QueryDuration)).Cast<QueryDuration>())
            {
                var itemstr = item.ToString();

                _expectedDurationItemList.Add(new ExpectedDurationItem(this)
                                                {
                                                    Name = itemstr,
                                                    Content = itemstr[0].ToString().ToUpper() + itemstr.Substring(1).ToLower(),
                                                    IsEnabled = true
                                                });
            }

			var expectedDuration = this.ExpectedDuration.ToString();

			foreach(var item in _expectedDurationItemList)
			{
				item.IsChecked = item.Name == expectedDuration;
				item.UpdateExpectedDuration = true;
			}
		}

		public List<ExpectedDurationItem> ExpectedDurationList
		{
			get
			{
				return _expectedDurationItemList;
			}

			set
			{
				var selectedItem = value.FirstOrDefault(c => c.IsChecked)?.Name;

				if(string.IsNullOrEmpty(selectedItem))
				{
					this.ExpectedDuration = QueryDuration.LONG;
				}
				else if(Enum.TryParse<QueryDuration>(selectedItem, true, out QueryDuration result))
				{
					this.ExpectedDuration = result;
				}
			}
		}

		#endregion

		public bool DocumentAPI
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("DocumentAPI", true);
                    return true;
                }

                return (bool?)DriverData.Element("DocumentAPI") ?? true;
            }
            set
            {
                DriverData.SetElementValue("DocumentAPI", value);
            }
        }

        public bool AlwaysUseAValues
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("AlwaysUseAValues", true);
                    return true;
                }

                return (bool?)DriverData.Element("AlwaysUseAValues") ?? true;
            }
            set
            {
                DriverData.SetElementValue("AlwaysUseAValues", value);
            }
        }

        public bool RespondAllOps
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("RespondAllOps", true);
                    return true;
                }

                return (bool?)DriverData.Element("RespondAllOps") ?? true;
            }
            set
            {
                DriverData.SetElementValue("RespondAllOps", value);
            }
        }

        public string TLSCertName
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("TLSCertName", null);
                    return null;
                }

                return DriverData.Element("TLSCertName")?.Value;
            }
            set
            {
                if (value == string.Empty) value = null;
                DriverData.SetElementValue("TLSCertName", value);
            }
        }

        public bool TLSOnlyLogin
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("TLSOnlyLogin", false);
                    return false;
                }

                return (bool?)DriverData.Element("TLSOnlyLogin") ?? false;
            }
            set
            {
                DriverData.SetElementValue("TLSOnlyLogin", value);
            }
        }

        public string TLSRevokeCerts
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("TLSRevokeCerts", null);
                    return null;
                }

                return (string)DriverData.Element("TLSRevokeCerts");
            }
            set
            {
                if (value == string.Empty) value = null;
                DriverData.SetElementValue("TLSRevokeCerts", value);
            }
        }

        public string TLSClientCertFile
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("TLSClientCertFile", null);
                    return null;
                }

                return (string)DriverData.Element("TLSClientCertFile");
            }
            set
            {
                if (value == string.Empty) value = null;
                DriverData.SetElementValue("TLSClientCertFile", value);
            }
        }

        #region TLS Options
        public string TLSProtocols
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("TLSProtocols", null);
                    return null;
                }

                return (string)DriverData.Element("TLSProtocols");
            }
            set
            {
                if (value == string.Empty) value = null;
                DriverData.SetElementValue("TLSProtocols", value);
            }
        }

        public class TLSProtocolItem : INotifyPropertyChanged
        {

            internal TLSProtocolItem(ConnectionProperties connectionProperties)
            {
                ConnectionProperties= connectionProperties;
            }

            private ConnectionProperties ConnectionProperties { get;}

            public string Content { get; set; }
            public bool UpdateTLSProtocols { get; set; }

            private bool _isChecked;
            public bool IsChecked
            {
                get { return this._isChecked; }
                set
                {
                    if (this._isChecked != value)
                    {
                        this._isChecked = value;
                        this.NotifyIsCheckedProperty();

                        if (UpdateTLSProtocols)
                        {
                            if (this.ConnectionProperties.TLSProtocolsList[0].IsChecked)
                            {
                                this.ConnectionProperties.TLSProtocols = null;
                            }
                            else
                            {
                                this.ConnectionProperties.TLSProtocols = null;
                                this.ConnectionProperties.TLSProtocols = string.Join(",", this.ConnectionProperties.TLSProtocolsList.Where(c => c.IsChecked).Select(c => (string)c.Name));
                            }
                        }
                    }
                }
            }
            public string ToolTip { get; set; }
            public string Name { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged(String info)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
            }

            public void NotifyIsCheckedProperty()
            {
                this.NotifyPropertyChanged("IsChecked");
            }
        }

        private List<TLSProtocolItem> _TLSProtocolsList = null;

        private void InitializeTLSProtocols()
        {
           _TLSProtocolsList = new List<TLSProtocolItem>()
            {
                new TLSProtocolItem(this) { Content = "Disabled", Name = "Disabled", UpdateTLSProtocols=false },
                new TLSProtocolItem(this) { Content = "Detect", Name = "None", UpdateTLSProtocols = false, ToolTip="Determines the best protocol to use."},
                new TLSProtocolItem(this) { Content = "SSL 2", Name = "Ssl2", UpdateTLSProtocols = false },
                new TLSProtocolItem(this) { Content = "SSL 3", Name = "Ssl3", UpdateTLSProtocols = false },
                new TLSProtocolItem(this) { Content = "TLS 1.0", Name = "Tls", UpdateTLSProtocols = false },
                new TLSProtocolItem(this) { Content = "TLS 1.1", Name = "Tls11", UpdateTLSProtocols = false },
                new TLSProtocolItem(this) { Content = "TLS 1.2", Name = "Tls12", UpdateTLSProtocols = false },
                new TLSProtocolItem(this) { Content = "TLS 1.3", Name = "Tls13", UpdateTLSProtocols = false },
            };            

            var split = this.TLSProtocols?
                                .Split(',') ?? Array.Empty<string>();

            foreach (var checkItem in split)
            {
                var boxItem = _TLSProtocolsList.Find(c => c.Name == checkItem);


                if (boxItem == null)
                {
                    if (checkItem == "Default")
                    {
                        _TLSProtocolsList[3].IsChecked = true;
                        _TLSProtocolsList[4].IsChecked = true;
                    }
                }
                else
                {
                    boxItem.IsChecked = true;
                }
            }

            if (!_TLSProtocolsList.Any(c => c.IsChecked))
            {
                _TLSProtocolsList[0].IsChecked = true;
            }

            foreach(var item in _TLSProtocolsList)
            {
                item.UpdateTLSProtocols = true;                
            }
        }

        public List<TLSProtocolItem> TLSProtocolsList
        {
            get
            {               
                return _TLSProtocolsList;
            }

            set
            {
                if (value[0].IsChecked)
                {
                    this.TLSProtocols = null;
                }                
                else
                {
                    this.TLSProtocols = string.Join(",", value.Where(c => c.IsChecked).Select(c => (string) c.Name));
                }
            }
        }

        #endregion

        #region Record Display Options

        public ARecord.DumpTypes RecordView
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("RecordView", "Record");
                    return ARecord.DumpTypes.Record;
                }

                var elementValue = DriverData.Element("RecordView")?.Value;

                if(string.IsNullOrEmpty(elementValue))
                {
                    DriverData.SetElementValue("RecordView", "Record");
                    return ARecord.DumpTypes.Record;
                }
                else if(elementValue == "0")
                {
                    DriverData.SetElementValue("RecordView", "Detail");
                    return ARecord.DumpTypes.Detail;
                }
                else if (elementValue == "1")
                {
                    DriverData.SetElementValue("RecordView", "Record");
                    return ARecord.DumpTypes.Record;
                }

                if (Enum.TryParse<ARecord.DumpTypes>(elementValue, true, out ARecord.DumpTypes result))
                {
                    return result;
                }

                return ARecord.DumpTypes.Record;
            }
            set
            {
                DriverData.SetElementValue("RecordView", value.ToString());
            }
        }

        public class RecordViewItem : INotifyPropertyChanged
        {

            internal RecordViewItem(ConnectionProperties connectionProperties)
            {
                ConnectionProperties = connectionProperties;
            }

            private ConnectionProperties ConnectionProperties { get; }

            public string Content { get; set; }
            public bool UpdateRecordView { get; set; }

            private bool _isChecked;
            public bool IsChecked
            {
                get { return this._isChecked; }
                set
                {
                    if (this._isChecked != value)
                    {
                        this._isChecked = value;
                        this.NotifyIsCheckedProperty();

                        if (this._isChecked
                                && this.UpdateRecordView
                                && Enum.TryParse<ARecord.DumpTypes>(this.Name, true, out ARecord.DumpTypes result))
                        {
                            this.ConnectionProperties.RecordView = result;
                        }                        
                    }
                }
            }
            
            private bool _isEnabled = true;
            public bool IsEnabled
            {
                get { return this._isEnabled; }
                set
                {
                    if (this._isEnabled != value)
                    {
                        this._isEnabled = value;
                        this.NotifyIsEnabledProperty();                        
                    }
                }
            }
            public string ToolTip { get; set; }
            public string Name { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged(String info)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
            }

            public void NotifyIsCheckedProperty()
            {
                this.NotifyPropertyChanged("IsChecked");
            }

            public void NotifyIsEnabledProperty()
            {
                this.NotifyPropertyChanged("IsEnabled");
            }
        }

        private List<RecordViewItem> _RecordViewItemList = null;

        private void InitializeRecordViews()
        {            
            _RecordViewItemList = new List<RecordViewItem>()
            {
                new RecordViewItem(this) { Content = "Record", 
                                            Name = "Record",
                                            ToolTip="Displays the record based on the detected bins of the set.",
                                            IsEnabled = this.DBRecordSampleSet > 0},
                new RecordViewItem(this) { Content = "Dynamic", Name = "Dynamic", ToolTip="Similar to \"Record\" except all bins associated to this record are displayed regardless of the set's defined bins."},
                new RecordViewItem(this) { Content = "Detail", Name = "Detail", ToolTip= "Displays all properties/fields of this instance like the \"LINQPad Dump\" method." }
            };

            var recordView = this.RecordView.ToString();

            foreach (var item in _RecordViewItemList)
            {
                item.IsChecked = item.Name == recordView;
                item.UpdateRecordView = true;
            }
        }

        public List<RecordViewItem> RecordViewList
        {
            get
            {
                return _RecordViewItemList;
            }

            set
            {
                var selectedItem = value.FirstOrDefault(c => c.IsChecked)?.Name;

                if (string.IsNullOrEmpty(selectedItem))
                {
                    this.RecordView = ARecord.DumpTypes.Record;
                }
                else if (Enum.TryParse<ARecord.DumpTypes>(selectedItem, true, out ARecord.DumpTypes result))
                {
                    this.RecordView = result;
                }                
            }
        }

        #endregion

        public bool DriverLogging
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("DriverLogging", false);
                    return false;
                }

                return (bool?)DriverData.Element("DriverLogging") ?? false;
            }
            set
            {
                DriverData.SetElementValue("DriverLogging", value);
            }
        }

        public string DateTimeFmt
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("DateTimeFmt", Helpers.DateTimeFormat);
                    return Helpers.DateTimeFormat;
                }

                return (string)DriverData.Element("DateTimeFmt") ?? Helpers.DateTimeFormat;
            }
            set
            {
                if (value == string.Empty) value = null;
                DriverData.SetElementValue("DateTimeFmt", value);
                Helpers.DateTimeFormat = value ?? Helpers.defaultDateTimeFormat;
            }
        }

        public string DateTimeOffsetFmt
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("DateTimeOffsetFmt", Helpers.DateTimeOffsetFormat);
                    return Helpers.DateTimeOffsetFormat;
                }

                return (string)DriverData.Element("DateTimeOffsetFmt") ?? Helpers.DateTimeOffsetFormat;
            }
            set
            {
                if (value == string.Empty) value = null;
                DriverData.SetElementValue("DateTimeOffsetFmt", value);
                Helpers.DateTimeOffsetFormat= value ?? Helpers.defaultDateTimeOffsetFormat;
            }
        }

        public string TimespanFmt
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("TimespanFmt", Helpers.TimeSpanFormat);
                    return Helpers.TimeSpanFormat;
                }

                return (string)DriverData.Element("TimespanFmt") ?? Helpers.TimeSpanFormat;
            }
            set
            {
                if (value == string.Empty) value = null;
                DriverData.SetElementValue("TimespanFmt", value);
                Helpers.TimeSpanFormat= value ?? Helpers.defaultTimeSpanFormat;
            }
        }

        public bool UseUnixEpochNanoForNumericDateTime
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("UseUnixEpochNanoForNumericDateTime", Helpers.UseUnixEpochNanoForNumericDateTime);
                    return Helpers.UseUnixEpochNanoForNumericDateTime;
                }

                return (bool?) DriverData.Element("UseUnixEpochNanoForNumericDateTime") 
                                ?? Helpers.UseUnixEpochNanoForNumericDateTime;
            }
            set
            {
                DriverData.SetElementValue("UseUnixEpochNanoForNumericDateTime", value);
                Helpers.UseUnixEpochNanoForNumericDateTime = value;
            }
        }

        public bool AllDateTimeUseUnixEpochNano
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("AllDateTimeUseUnixEpochNano", Helpers.AllDateTimeUseUnixEpochNano);
                    return Helpers.AllDateTimeUseUnixEpochNano;
                }

                return (bool?)DriverData.Element("AllDateTimeUseUnixEpochNano")
                                ?? Helpers.AllDateTimeUseUnixEpochNano;
            }
            set
            {
                DriverData.SetElementValue("AllDateTimeUseUnixEpochNano", value);
                Helpers.AllDateTimeUseUnixEpochNano = value;
            }
        }

        public string PKName
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("PKName", ARecord.DefaultASPIKeyName);
                    return ARecord.DefaultASPIKeyName;
                }

                return (string)DriverData.Element("PKName") ?? ARecord.DefaultASPIKeyName;
            }
            set
            {
                if (value == string.Empty) value = null;

                DriverData.SetElementValue("PKName", value ?? "PK");
                ARecord.DefaultASPIKeyName = value ?? "PK";
            }
        }

    }
}