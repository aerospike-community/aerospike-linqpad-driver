using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;
using LINQPad.Extensibility.DataContext;
using System.Diagnostics;
using System.Collections;
using static Aerospike.Database.LINQPadDriver.ConnectionProperties;

namespace Aerospike.Database.LINQPadDriver
{
	public partial class ConnectionDialog : Window
	{
		IConnectionInfo _cxInfo;

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
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Cert files (*.cer;*.crt;*.cert)|*.cer;*.crt;*.cert|All files (*.*)|*.*";
			openFileDialog.Title = "Select Certificate File";
			openFileDialog.CheckPathExists = true;
			openFileDialog.CheckFileExists= true;
			openFileDialog.AddExtension = true;	
			
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
            var cb = (CheckBox)sender;
            var recViewItems = lbRecordViews.ItemsSource.Cast<RecordViewItem>();
            
            foreach(var item in recViewItems)
            {
                if(item.Name == (string) cb.Tag)
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

            try
            {
                try
                {
                    connection = new AerospikeConnection(_cxInfo);
                    connection.Open(false);

                    messageBoxText = $@"
Cluster Name: ""{_cxInfo.DatabaseInfo.Database}""
DB Version: {_cxInfo.DatabaseInfo.Provider}
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
                finally
                {
                    if (connection != null)
                    {
                        try
                        {
                            connection.Close();
                        }
                        catch { }
                    }
                }

                MessageBox.Show(this, messageBoxText, caption, button, icon, MessageBoxResult.OK);
            }
            finally
            {
                if (connection != null)
                {
                    try
                    {
                        connection.Dispose();
                        connection = null;
                    }
                    catch { }
                }
            }
        }
    }
}