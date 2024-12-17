using System.Windows;

namespace AbimToolsMine
{
    public partial class TypeInstanceWindow : Window
    {
        public string SelectedTypeInstance { get; private set; }

        public TypeInstanceWindow()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedTypeInstance = TypeRadioButton.IsChecked == true ? "Тип" : "Экземпляр";
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
