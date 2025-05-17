using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using LINQPad.Extensibility.DataContext;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Input;
using Aerospike.Database.LINQPadDriver.Extensions;
using static Aerospike.Database.LINQPadDriver.ConnectionProperties;
using Aerospike.Client;
using Microsoft.Win32;

namespace Aerospike.Database.LINQPadDriver
{
	public partial class ConnectionDialog : Window
	{
		readonly IConnectionInfo _cxInfo;
        readonly ConnectionProperties _connectionProps;

        public ConnectionDialog (IConnectionInfo cxInfo)
		{
			_cxInfo = cxInfo;

            // ConnectionProperties is your view-model.
            DataContext = new ConnectionProperties (cxInfo);
            _connectionProps = (ConnectionProperties)DataContext;

            InitializeComponent ();

            if(!string.IsNullOrEmpty(_connectionProps.TLSProtocols))
            {
                cbTLSOnlyLogin.IsEnabled = true;
                txtCertFile.IsEnabled = true;
                txtRejectCerts.IsEnabled = true;
                btnCertFile.IsEnabled = true;
                txtTLSCertName.IsEnabled = true;
                TLSGrpBox.IsExpanded = true;
			}

            {
                var nameFnd = false;

                this.comboPasswordNames.Items.Clear();
                var pwdMgrNames = PasswordManagerNames;

                if(pwdMgrNames is null)
                {
					this.spPassword.IsEnabled = true;
					this.spPassword.Visibility = Visibility.Visible;
					this.spPasswordNames.IsEnabled = false;
					this.spPasswordNames.Visibility = Visibility.Hidden;
					this.cbUsePassMgr.IsChecked = false;
					this.cbUsePassMgr.IsEnabled = false;
                    this.cbUsePassMgr.Visibility = Visibility.Hidden;
                    this._connectionProps.UsePasswordManager = false;
				}
                else
                {
                    foreach(var name in PasswordManagerNames)
                    {
                        if(!this.comboPasswordNames.Items.Contains(name))
                            this.comboPasswordNames.Items.Add(name);
                        if(name == _connectionProps.PasswordManagerName)
                            nameFnd = true;
                    }

                    if(!string.IsNullOrEmpty(_connectionProps.PasswordManagerName))
                    {
                        if(!nameFnd)
                            this.comboPasswordNames.Items.Add(_connectionProps.PasswordManagerName);
                        this.comboPasswordNames.SelectedItem = _connectionProps.PasswordManagerName;
                    }

                    cbUsePassMgr_Click(this.cbUsePassMgr, new RoutedEventArgs());
                }
            }

            DBTypeCache.Save(this._connectionProps);
            DBTypeCache.Update(this._connectionProps.DBType, this, this._connectionProps);            
        }

        void btnOK_Click (object sender, RoutedEventArgs e)
		{
            //Debugger.Launch ();
            DialogResult = true;
		}

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {			
			try
			{
				Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
				e.Handled = true;
			}
			catch { }
        }

        private void btnCertFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "Cert files (*.cer;*.crt;*.cert;*.pem)|*.cer;*.crt;*.cert;*.pem|All files (*.*)|*.*",
                Title = "Select Certificate File",
                CheckPathExists = true,
                CheckFileExists = true,
                AddExtension = true
            };

            if (string.IsNullOrEmpty(txtCertFile.Text))
			{				
				openFileDialog.DefaultExt = ".cer";
			}
			else
			{
				//openFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(openFileDialog.FileName);
				openFileDialog.FileName = txtCertFile.Text;
                openFileDialog.DefaultExt = System.IO.Path.GetExtension(txtCertFile.Text);                
            }
            if(openFileDialog.ShowDialog() == true)
            {
                txtCertFile.Text = openFileDialog.FileName;
            }
        }
        
        private void cbAllDTNumeric_Checked(object sender, RoutedEventArgs e)
        {
            var cb = (CheckBox)sender;

            if(cb.IsChecked.HasValue && cb.IsChecked.Value)
            {
                cbUseUnixEpoch.IsEnabled = false;
                txtDateTimeFmt.IsEnabled = false;
                txtDateTimeOffsetFmt.IsEnabled = false;
                txtTimeSpanFmt.IsEnabled = false;
            }
            else
            {
                cbUseUnixEpoch.IsEnabled = true;
                txtDateTimeFmt.IsEnabled = true;
                txtDateTimeOffsetFmt.IsEnabled = true;
                txtTimeSpanFmt.IsEnabled = true;
            }
        }


        private void lbProtocols_Checked(object sender, RoutedEventArgs e)
        {
            var cb = (CheckBox)sender;
            var protocolItems = lbProtocols.ItemsSource.Cast<TLSProtocolItem>();
            var cbDisabled = protocolItems.First(c => c.Name == "Disabled");
            var cbDetect = protocolItems.First(cb=> cb.Name == "None");

            if ((string)cb.Tag == "Disabled")
            {
                if (cb.IsChecked.HasValue && cb.IsChecked.Value)
                {
                    foreach (var item in protocolItems)
                    {
                        if (item.Name != "Disabled")
                            item.IsChecked = false;
                    }
                    cbTLSOnlyLogin.IsEnabled = false;
                    txtCertFile.IsEnabled = false;
                    txtRejectCerts.IsEnabled = false;
                    btnCertFile.IsEnabled = false;
					txtTLSCertName.IsEnabled = false;

				}
            }
            else if ((string)cb.Tag == "None")
            {
                if (cb.IsChecked.HasValue)
                {
                    if (cb.IsChecked.Value)
                    {
                        foreach (var item in protocolItems)
                        {
                            if (item.Name != "None")
                                item.IsChecked = false;
                        }
                        cbTLSOnlyLogin.IsEnabled = true;
                        txtCertFile.IsEnabled = true;
                        txtRejectCerts.IsEnabled = true;
                        btnCertFile.IsEnabled = true;
                        txtTLSCertName.IsEnabled = true;
					}
                    else if (protocolItems.All(c => !c.IsChecked))
                    {
                        cbTLSOnlyLogin.IsEnabled = false;
                        txtCertFile.IsEnabled = false;
                        txtRejectCerts.IsEnabled = false;
                        btnCertFile.IsEnabled = false;
						txtTLSCertName.IsEnabled = false;
						cbDisabled.IsChecked = true;
                    }
                }
            }
            else
            {
                foreach (var c in protocolItems)
                {
                    if (c.IsChecked)
                    {
                        cbDisabled.IsChecked = false;
                        cbDetect.IsChecked = false;
                        cbTLSOnlyLogin.IsEnabled = true;
                        txtCertFile.IsEnabled = true;
                        txtRejectCerts.IsEnabled = true;
                        btnCertFile.IsEnabled = true;
                        txtTLSCertName.IsEnabled = true;
					}
                    else if (protocolItems.All(c => !c.IsChecked))
                    {
                        cbTLSOnlyLogin.IsEnabled = false;
                        txtCertFile.IsEnabled = false;
                        txtRejectCerts.IsEnabled = false;
                        btnCertFile.IsEnabled = false;
						txtTLSCertName.IsEnabled = false;
						cbDisabled.IsChecked = true;
                    }
                }                
            }

            
        }

        private void lbRecordViews_Checked(object sender, RoutedEventArgs e)
        {
            var rb = (RadioButton)sender;
            var recViewItems = lbRecordViews.ItemsSource.Cast<RecordViewItem>();
            
            foreach(var item in recViewItems)
            {
                if(item.Name == (string) rb.Tag)
                {
                    item.IsChecked = true;
                }
                else
                { item.IsChecked = false; }
            }      
        }

		private void lbEnsureDuration_Checked(object sender, RoutedEventArgs e)
		{
			var rb = (RadioButton) sender;
			var recViewItems = lbExprctedDuration.ItemsSource.Cast<ExpectedDurationItem>();

			foreach(var item in recViewItems)
			{
				if(item.Name == (string) rb.Tag)
				{
					item.IsChecked = true;
				}
				else
				{ item.IsChecked = false; }
			}
		}

		private void btnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            //System.Diagnostics.Debugger.Launch();
			var localHost = txtSeedNodes.Text;
            string messageBoxText = "Trying to Connect...";
			string additionalInfo = string.Empty;
			string caption = $"Testing Connection to \"{localHost}\"";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Information;
            AerospikeConnection connection = null;

            var waitCursor = new WaitCursor();

            try
            {                
                try
                {
                    var orgCertName = txtTLSCertName.Text;

                    var testTask = System.Threading.Tasks.Task.Run(() =>
                    {
                        connection = new AerospikeConnection(_cxInfo);
                        connection.ObtainMetaData(false);
                    });
                    testTask.Wait();

                                            
                    if(_cxInfo.DatabaseInfo.EncryptTraffic)
                    {
						additionalInfo += $@"
TLS Certification Verification Status: {connection.TLSCertVerification}
";

						if(connection.TLSCertName != orgCertName)
                        {                                
                            txtTLSCertName.Text = connection.TLSCertName;
                            additionalInfo += $@"

Warning: TLS Common Name was Missing. 
Using TLS Common Name '{connection.TLSCertName}' from within the Certification.
";
                        }
                    }

                    messageBoxText = $@"
Cluster Name: ""{_cxInfo.DatabaseInfo.Database}""
DB Version: {_cxInfo.DatabaseInfo.DbVersion}
Nodes: {connection.Nodes.Length}
Namespaces: {connection.Namespaces?.Count() ?? 0}
Sets: {connection.Namespaces?.Sum(n => n.Sets.Count()) ?? 0}
Bins: {connection.Namespaces?.Sum(n => n.Bins.Count()) ?? 0}
Secondary Indexes: {connection.Namespaces?.Sum(n => n.SIndexes.Count()) ?? 0}
UDFs: {connection.UDFModules?.Count() ?? 0}{additionalInfo}";

                }
                catch(Exception ex)
                {
                    messageBoxText = "\n";

                    icon = MessageBoxImage.Error;
					if(connection is not null
                            && connection.TLSCertVerification != CertHelpers.ResultCodes.Success
                            && connection.TLSCertVerification != CertHelpers.ResultCodes.Unknown)
					{
						messageBoxText += $@"
WARNING: TLS Certificate Validation Failed with {connection.TLSCertVerification}!";
						switch(connection.TLSCertVerification)
                        {
                            case CertHelpers.ResultCodes.WrongTLSCommonName:
								messageBoxText += $@"
    Is the TLS Common Name Correct?
        Try without a TLS Name and Re-Test to obtain the name from the cert...
    Do you have the correct Certificate File?";
                                break;
							case CertHelpers.ResultCodes.Premature:
								messageBoxText += $@"
    This Certificate is Premature... Do you have the correct cert file?";
                                break;
							case CertHelpers.ResultCodes.Expired:
								messageBoxText += $@"
    This Certificate is Expired... Do you have the correct cert file?";
                                break;
                            case CertHelpers.ResultCodes.InvalidChain:
								messageBoxText += $@"
    Is this the correct Certificate?
    Does this Certificate's Root CA trusted and in the proper Cert Store?
    See https://learn.microsoft.com/en-us/skype-sdk/sdn/articles/installing-the-trusted-root-certificate
        or https://learn.microsoft.com/en-us/windows-hardware/drivers/install/viewing-test-certificates
    PowerShell Cmd to Import the Root CA: Import-Certificate –FilePath  '.\{{CA File}}' –CertStoreLocation 'Cert:\CurrentUser\Root'";
								break;
                            default:
                                break;
                        }
                        messageBoxText += "\n";
					}
                    else
                        messageBoxText = string.Empty;

					messageBoxText += $@"
An Exception ""{ex.GetType().Name}"" occurred.
{ex.Message}
Source: ""{ex.Source}"" Help Link: ""{ex.HelpLink}"" HResult: ""{ex.HResult}"" TargetSite: ""{ex.TargetSite}""
";
                    if(ex.InnerException!= null)
                    {
                        messageBoxText += $@"
Inner Exception is ""{ex.InnerException.GetType().Name}"",
{ex.InnerException.Message}
Source: ""{ex.InnerException.Source}"" Help Link: ""{ex.InnerException.HelpLink}"" HResult: ""{ex.InnerException.HResult}"" TargetSite: ""{ex.InnerException.TargetSite}""
";
                    }
                    if (connection != null)
                    {
                        if (Helpers.IsPrivateAddress(connection.SeedHosts.FirstOrDefault().name))
                        {
                            if (connection.UseExternalIP)
                            {
                                messageBoxText += $@"

Note: The DB seems to be on a private network
    and ""Public Address"" option for this connection is enabled! Should this be disabled?
";
                            }
                        }
                        else if (!connection.UseExternalIP)
                        {
                            messageBoxText += $@"

Note: If the DB has Public/NATted/Alternate Addresses,
    you may need to enable ""Public Address"" option for this connection!
";
                        }
                    }
                }
                
                waitCursor?.Dispose();
                waitCursor = null;
                MessageBox.Show(this, messageBoxText, caption, button, icon, MessageBoxResult.OK);
            }
            finally
            {                
                waitCursor?.Dispose();
                connection?.Dispose();
            }
        }

        private void txtSampleRecs_TextChanged(object sender, TextChangedEventArgs e)
        {            
            static bool TryGetNbrRecs(string txtValue, out int nbrRecs)
            {                
                if(!string.IsNullOrEmpty(txtValue)
                    && int.TryParse(txtValue, out nbrRecs)) { return true; }

                nbrRecs = 0;
                return false;
            }

            var recViewItems = lbRecordViews?.ItemsSource.Cast<RecordViewItem>();

            if (recViewItems != null)
            {
                if (TryGetNbrRecs(txtSampleRecs.Text, out int nbrRecs)
                        && nbrRecs > 0)
                {
                    recViewItems.First(i => i.Name == "Record").IsEnabled = true;
                    txtSampleRecsPercent.IsEnabled = true;
                }
                else
                {
                    var item = recViewItems.First(i => i.Name == "Record");
                    item.IsEnabled = false;
                    if (item.IsChecked)
                    {
                        item.IsChecked = false;
                        recViewItems.First(i => i.Name == "Dynamic").IsChecked = true;
                    }

                    txtSampleRecsPercent.IsEnabled = false;
                }
            }
        }

        static private string[] PasswordManagerNames
        {
            get
            {
                try
                {
                    return (string[])typeof(LINQPad.Util).Assembly.GetType("LINQPad.PasswordManager")
                            .GetMethod("GetAllPasswordNames").Invoke(null, null);
                }
                catch (Exception ex)
                {
                    LINQPad.Extensions.Dump(ex, "Exception obtaining LINQPad.PasswordManager.GetAllPasswordNames");
                }
                return null;
            }
        }

        private void cbUsePassMgr_Click(object sender, RoutedEventArgs e)
        {
            //Debugger.Launch ();

            var cb = (CheckBox)sender;
           

            if (cb.IsChecked == true)
            {                
                this.spPassword.IsEnabled = false;
                this.spPassword.Visibility = Visibility.Hidden;
                this.spPasswordNames.IsEnabled = true;
                this.spPasswordNames.Visibility = Visibility.Visible;
            }
            else
            {
                this.spPassword.IsEnabled = true;
                this.spPassword.Visibility = Visibility.Visible;
                this.spPasswordNames.IsEnabled = false;
                this.spPasswordNames.Visibility = Visibility.Hidden;                
            }
        }

        private void cbShowPasswordChars_Checked(object sender, RoutedEventArgs e)
        {
            passwordBox.Visibility = Visibility.Collapsed;
            txtPassword.Visibility = Visibility.Visible;

            txtPassword.Focus();
        }

        private void cbShowPasswordChars_Unchecked(object sender, RoutedEventArgs e)
        {
            txtPassword.Visibility = Visibility.Collapsed;
            passwordBox.Visibility = Visibility.Visible;

            passwordBox.Focus();
        }
    }

    public class NumericValidationRule : ValidationRule
    {
        public int Min { get; set; } = 0;
        public int Max { get; set; } = int.MaxValue;
        public bool HasPercent { get; set; } = false;
        public bool AllowEmpty { get; set; } = false;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int nValue;

            if(value  is null) 
            {
                if (AllowEmpty)
                {
                    return new ValidationResult(true, null);
                }
                else
                {
                    return new ValidationResult(false, "Required Field");
                }
            }

            try
            {
                var strValue = ((string)value).Trim(' ');

                if (HasPercent)
                    strValue = strValue.TrimEnd(' ', '%');

                if (strValue.Length > 0)
                {
                    nValue = int.Parse(strValue);
                }
                else if (AllowEmpty)
                {
                    return new ValidationResult(true, null);
                }
                else
                {
                    return new ValidationResult(false, "Required Field");
                }
            }
            catch
            {
                return new ValidationResult(false, "Illegal characters for a Numeric Value");
            }

            if ((nValue < Min) || (nValue > Max))
            {
                if(Min == int.MinValue)
                    return new ValidationResult(false,
                                    $"Please enter a Number <= {Max}.");
                else if(Max == int.MaxValue)
                    return new ValidationResult(false,
                                    $"Please enter a Number >= {Min}.");

                return new ValidationResult(false,
                                    $"Please enter a Numeric Range [{Min} - {Max}].");
            }
            return new ValidationResult(true, null);
        }
    }

    public sealed class WaitCursor : IDisposable
    {
        private readonly Cursor previousCursor;

        public WaitCursor()
        {
            previousCursor = Mouse.OverrideCursor;

            Mouse.OverrideCursor = Cursors.Wait;
        }

        #region IDisposable Members

        public void Dispose()
        {
            Mouse.OverrideCursor = previousCursor;
        }

        #endregion
    }

    public static class PasswordHelper
    {
        public static readonly DependencyProperty PasswordProperty =
            DependencyProperty.RegisterAttached("Password",
            typeof(string), typeof(PasswordHelper),
            new FrameworkPropertyMetadata(string.Empty, OnPasswordPropertyChanged));

        public static readonly DependencyProperty AttachProperty =
            DependencyProperty.RegisterAttached("Attach",
            typeof(bool), typeof(PasswordHelper), new PropertyMetadata(false, Attach));

        private static readonly DependencyProperty IsUpdatingProperty =
           DependencyProperty.RegisterAttached("IsUpdating", typeof(bool),
           typeof(PasswordHelper));


        public static void SetAttach(DependencyObject dp, bool value)
        {
            dp.SetValue(AttachProperty, value);
        }

        public static bool GetAttach(DependencyObject dp)
        {
            return (bool)dp.GetValue(AttachProperty);
        }

        public static string GetPassword(DependencyObject dp)
        {
            return (string)dp.GetValue(PasswordProperty);
        }

        public static void SetPassword(DependencyObject dp, string value)
        {
            dp.SetValue(PasswordProperty, value);
        }
        
        private static bool GetIsUpdating(DependencyObject dp)
        {
            return (bool)dp.GetValue(IsUpdatingProperty);
        }

        private static void SetIsUpdating(DependencyObject dp, bool value)
        {
            dp.SetValue(IsUpdatingProperty, value);
        }

        private static void OnPasswordPropertyChanged(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            passwordBox.PasswordChanged -= PasswordChanged;

            if (!(bool)GetIsUpdating(passwordBox))
            {
                passwordBox.Password = (string)e.NewValue;
            }
            passwordBox.PasswordChanged += PasswordChanged;
        }

        private static void Attach(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {           
            if(sender is PasswordBox passwordBox)
            {
                if ((bool)e.OldValue)
                {
                    passwordBox.PasswordChanged -= PasswordChanged;
                }

                if ((bool)e.NewValue)
                {
                    passwordBox.PasswordChanged += PasswordChanged;
                }
            }
        }

        private static void PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox passwordBox = sender as PasswordBox;
            SetIsUpdating(passwordBox, true);
            SetPassword(passwordBox, passwordBox.Password);
            SetIsUpdating(passwordBox, false);            
        }
    }

    public class DBTypeCache
    {
        static readonly List<DBTypeCache> cache = new();

        public DBPlatforms DBType { get; }
        public string Host { get; private set; }
        public string Port { get; private set; }
        public string UserName { get; private set; }
        public string Password { get; private set; }
        public bool ShowPassword { get; private set; }
        public bool? UsePasswordManager { get; private set; }
        public string PasswordManagerName { get; private set; }
        public string TLSCertName { get; private set; }
        
        private DBTypeCache(DBPlatforms dbType)
        {
            this.DBType = dbType;
        }

        static DBTypeCache DBTypeCacheNative()
            => new DBTypeCache(DBPlatforms.Native)
            {
                Host = "localhost",
                Port = "3000",
                UserName = null,
                Password = null,
                UsePasswordManager = false,
                PasswordManagerName = null,
                ShowPassword = false,
                TLSCertName = null
            };
        
        internal static bool Update(DBPlatforms dbType, ConnectionDialog dialog, ConnectionProperties props)
        {
            bool result = false;

            var fndDBType = cache.FirstOrDefault(c => c.DBType == dbType);

            if (fndDBType is null)
            {
                fndDBType = DBTypeCacheNative();
                result = true;
                cache.Add(fndDBType);
            }

			dialog.txtSeedNodes.Text = fndDBType.Host;
			dialog.txtPort.Text = fndDBType.Port;
			dialog.comboPasswordNames.SelectedItem = fndDBType.PasswordManagerName;
			dialog.cbUsePassMgr.IsChecked = fndDBType.UsePasswordManager;
			dialog.txtUserName.Text = fndDBType.UserName;
			dialog.txtPassword.Text = fndDBType.Password;
			dialog.cbShowPasswordChars.IsChecked = fndDBType.ShowPassword;
			dialog.txtTLSCertName.Text = fndDBType.TLSCertName;
			
            props.ConnectionInfo.DatabaseInfo.Server = fndDBType.Host;
            if(!string.IsNullOrEmpty(fndDBType.Port))
                props.Port = int.Parse(fndDBType.Port);
            props.ConnectionInfo.DatabaseInfo.UserName = fndDBType.UserName;
            props.ConnectionInfo.DatabaseInfo.Password = fndDBType.Password;
            props.UsePasswordManager = fndDBType.UsePasswordManager ?? false;
            props.PasswordManagerName = fndDBType.PasswordManagerName;
            props.TLSCertName = fndDBType.TLSCertName;
            
            return result;
        }

        public static bool Save(DBPlatforms dbType, ConnectionDialog dialog)
        {
            bool result = false;

            var fndDBType = cache.FirstOrDefault(c => c.DBType == dbType);

            if (fndDBType is null)
            {
                fndDBType = new DBTypeCache(dbType);
                result = true;
                cache.Add(fndDBType);
            }

			fndDBType.Host = dialog.txtSeedNodes.Text;
			fndDBType.Port = dialog.txtPort.Text;
			fndDBType.PasswordManagerName = (string) dialog.comboPasswordNames.SelectedItem;
			fndDBType.UsePasswordManager = dialog.cbUsePassMgr.IsChecked;
			fndDBType.UserName = dialog.txtUserName.Text;
			fndDBType.Password = dialog.txtPassword.Text;
			fndDBType.ShowPassword = dialog.cbShowPasswordChars.IsChecked ?? false;
			
            return result;
        }

        internal static void Save(ConnectionProperties props)
        {
            var cacheType = new DBTypeCache(props.DBType)
            {
                Host = props.ConnectionInfo.DatabaseInfo.Server,
                Port = props.Port.ToString(),
                UserName = props.ConnectionInfo.DatabaseInfo.UserName,
                Password = props.ConnectionInfo.DatabaseInfo.Password,
                UsePasswordManager = props.UsePasswordManager,
                PasswordManagerName = props.PasswordManagerName,
                TLSCertName = props.TLSCertName                
            };

            cache.Add(cacheType);
        }
    }

}