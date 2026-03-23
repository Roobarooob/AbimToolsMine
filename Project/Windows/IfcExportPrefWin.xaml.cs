using System.Windows;

namespace AbimToolsMine
{
    public partial class IfcExportPrefWin : Window
 {
  public string ConfigName { get; set; }
   public string ViewNameSubstring { get; set; }

        public IfcExportPrefWin()
   {
 InitializeComponent();
       // Load saved configuration name and view name substring
     ConfigName = Properties.Settings.Default.IFCExportConfig;
     ViewNameSubstring = Properties.Settings.Default.IFCViewName;
     IfcConfigTextBox.Text = ConfigName;
     IfcViewNameTextBox.Text = ViewNameSubstring;
   }

 private void IfcConfigTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
   {
// Update configuration name in Settings
     Properties.Settings.Default.IFCExportConfig = IfcConfigTextBox.Text;
     Properties.Settings.Default.Save();
  }

   private void IfcViewNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
{
 // Update view name substring in Settings
     Properties.Settings.Default.IFCViewName = IfcViewNameTextBox.Text;
     Properties.Settings.Default.Save();
 }

  private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
{
     Properties.Settings.Default.Save();
 }
}
}
