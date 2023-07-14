using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Win32;
using LINQPad.Extensibility.DataContext;
using System.Diagnostics;
using System.Collections;
using static Aerospike.Database.LINQPadDriver.ConnectionProperties;
using System.Globalization;
using System.Windows.Input;

namespace Aerospike.Database.LINQPadDriver
{
	public partial class ConnectionDialog : Window
	{
		readonly IConnectionInfo _cxInfo;

		public ConnectionDialog (IConnectionInfo cxInfo)
		{
			_cxInfo = cxInfo;

            // ConnectionProperties is your view-model.
            DataContext = new ConnectionProperties (cxInfo);

			InitializeComponent ();

            if(!string.IsNullOrEmpty(((ConnectionProperties)DataContext).TLSProtocols))
            {
                cbTLSOnlyLogin.IsEnabled = true;
                txtCertFile.IsEnabled = true;
                txtRejectCerts.IsEnabled = true;
                btnCertFile.IsEnabled = true;
            }
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
                Filter = "Cert files (*.cer;*.crt;*.cert)|*.cer;*.crt;*.cert|All files (*.*)|*.*",
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
            if (openFileDialog.ShowDialog() == true)
                txtCertFile.Text = openFileDialog.FileName;
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
                    }
                    else if (protocolItems.All(c => !c.IsChecked))
                    {
                        cbTLSOnlyLogin.IsEnabled = false;
                        txtCertFile.IsEnabled = false;
                        txtRejectCerts.IsEnabled = false;
                        btnCertFile.IsEnabled = false;
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
                    }
                    else if (protocolItems.All(c => !c.IsChecked))
                    {
                        cbTLSOnlyLogin.IsEnabled = false;
                        txtCertFile.IsEnabled = false;
                        txtRejectCerts.IsEnabled = false;
                        btnCertFile.IsEnabled = false;
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

        private void btnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            var localHost = txtSeedNodes.Text;
            string messageBoxText = "Trying to Connect...";
            string caption = $"Testing Connection to \"{localHost}\"";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.Information;
            AerospikeConnection connection = null;

            var waitCursor = new WaitCursor();

            try
            {                
                try
                {
                    connection = new AerospikeConnection(_cxInfo);
                    connection.ObtainMetaDate(false);

                    messageBoxText = $@"
Cluster Name: ""{_cxInfo.DatabaseInfo.Database}""
DB Version: {_cxInfo.DatabaseInfo.DbVersion}
Nodes: {connection.Nodes.Length}
Namespaces: {connection.Namespaces.Count()}
Sets: {connection.Namespaces.Sum(n => n.Sets.Count())}
Bins: {connection.Namespaces.Sum(n => n.Bins.Count())}
Secondary Indexes: {connection.Namespaces.Sum(n => n.SIndexes.Count())}
UDFs: {connection.UDFModules.Count()}";

                }
                catch(Exception ex)
                {
                    icon = MessageBoxImage.Error;
                    messageBoxText = $@"
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
                }
                
                waitCursor?.Dispose();
                waitCursor = null;
                MessageBox.Show(this, messageBoxText, caption, button, icon, MessageBoxResult.OK);
            }
            finally
            {                
                waitCursor?.Dispose();
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
}