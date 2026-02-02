using System.Windows;

namespace AbimToolsMine
{
    public partial class RvtExportPrefWin : Window
    {
public string ServerName { get; set; }

public RvtExportPrefWin()
      {
       InitializeComponent();
      // Load value from Settings
            ServerName = Properties.Settings.Default.RevitServer_ip;
         ServerNameTextBox.Text = ServerName;
   DataContext = this;
        }

 private void ServerNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
  {
            // Update value in Settings
            Properties.Settings.Default.RevitServer_ip = ServerNameTextBox.Text;
Properties.Settings.Default.Save();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
 {
            Properties.Settings.Default.Save();
   }
    }
}
