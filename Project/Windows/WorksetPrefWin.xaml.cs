using System.Windows;


namespace AbimToolsMine
{
    public partial class WorksetPrefWin : Window
    {
        public string DefaultPrefix { get; set; }
        public string DWGPrefix { get; set; }

        public WorksetPrefWin()
        {
            InitializeComponent();
            // Загружаем значение из Default.Settings и устанавливаем его в TextBox
            DefaultPrefix = Properties.Settings.Default.LinkPrefix;
            DWGPrefix = Properties.Settings.Default.DWGWorksetName; ;
            PrefixTextBox.Text = DefaultPrefix;
            DWGStringBox.Text = DWGPrefix;
            DataContext = this;
        }
        private void PrefixTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Обновляем значение ConnectionPrefix в Settings
            Properties.Settings.Default.LinkPrefix = PrefixTextBox.Text;
            Properties.Settings.Default.DWGWorksetName = DWGStringBox.Text;
            Properties.Settings.Default.Save();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
        }
    }
}