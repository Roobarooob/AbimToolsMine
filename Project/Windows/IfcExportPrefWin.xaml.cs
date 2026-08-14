using Microsoft.Win32;
using System.IO;
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
            ConfigName = Properties.Settings.Default.IFCExportConfig;
            ViewNameSubstring = Properties.Settings.Default.IFCViewName;
            IfcConfigTextBox.Text = ConfigName;
            IfcViewNameTextBox.Text = ViewNameSubstring;
        }

        private void SelectIfcConfig_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите JSON-конфигурацию экспорта IFC",
                Filter = "Конфигурация IFC (*.json)|*.json|Все файлы (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            string currentPath = IfcConfigTextBox.Text.Trim();
            if (File.Exists(currentPath))
            {
                dialog.InitialDirectory = Path.GetDirectoryName(currentPath);
                dialog.FileName = Path.GetFileName(currentPath);
            }

            if (dialog.ShowDialog(this) == true)
            {
                IfcConfigTextBox.Text = dialog.FileName;
            }
        }

        private void IfcConfigTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Properties.Settings.Default.IFCExportConfig = IfcConfigTextBox.Text.Trim();
            Properties.Settings.Default.Save();
        }

        private void IfcViewNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Properties.Settings.Default.IFCViewName = IfcViewNameTextBox.Text;
            Properties.Settings.Default.Save();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
        }
    }
}
