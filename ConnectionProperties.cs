using LINQPad.Extensibility.DataContext;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Aerospike.Database.LINQPadDriver.Extensions;

namespace Aerospike.Database.LINQPadDriver
{
	/// <summary>
	/// Wrapper to read/write connection properties. This acts as our ViewModel - we will bind to it in ConnectionDialog.xaml.
	/// </summary>
	class ConnectionProperties
	{
		public IConnectionInfo ConnectionInfo { get; private set; }

		XElement DriverData => ConnectionInfo.DriverData;

		public ConnectionProperties (IConnectionInfo cxInfo)
		{
			ConnectionInfo = cxInfo;

            if (string.IsNullOrEmpty(cxInfo.DatabaseInfo.Server))
                cxInfo.DatabaseInfo.Server = "localhost";

            InitializeTLSProtocols();
            InitializeRecordViews();

            ARecord.DefaultASPIKeyName = this.PKName;
        }

		// This is how to create custom connection properties.

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
                    DriverData.SetElementValue("SocketTimeout", 30000);
                    return 30000;
                }

                return (int?)DriverData.Element("SocketTimeout") ?? 30000;
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

        public bool ShortQuery
        {
            get
            {
                if (DriverData.IsEmpty)
                {
                    DriverData.SetElementValue("ShortQuery", true);
                    return true;
                }

                return (bool?)DriverData.Element("ShortQuery") ?? true;
            }
            set
            {
                DriverData.SetElementValue("ShortQuery", value);
            }
        }

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
                    DriverData.SetElementValue("AlwaysUseAValues", false);
                    return false;
                }

                return (bool?)DriverData.Element("AlwaysUseAValues") ?? false;
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
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(info));
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
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs(info));
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
                DriverData.SetElementValue("DateTimeFmt", value);
                Helpers.DateTimeFormat = value;
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
                DriverData.SetElementValue("DateTimeOffsetFmt", value);
                Helpers.DateTimeOffsetFormat= value;
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
                DriverData.SetElementValue("TimespanFmt", value);
                Helpers.TimeSpanFormat= value;
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
                DriverData.SetElementValue("PKName", value ?? "PK");
                ARecord.DefaultASPIKeyName = value ?? "PK";
            }
        }
    }
}