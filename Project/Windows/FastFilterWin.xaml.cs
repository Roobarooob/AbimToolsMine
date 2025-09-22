using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;


namespace AbimToolsMine
{
    /// <summary>
    /// Логика взаимодействия для UserControl1.xaml
    /// </summary>
    public partial class FastFilterWin : Window
    {
        private Key _lastKeyPressed = Key.None;
        private readonly ExternalEvent _exEvent;
        public Category SelectedCategory = null;
        public Dictionary<string, Category> category_dict = new Dictionary<string, Category>();
        public FastFilterWin(FastFilterHandler ExHandler, ExternalEvent ExEvent, ExternalCommandData commandData)
        {

            InitializeComponent();
            _exEvent = ExEvent;
            LoadCategories(commandData);
            Closing += WinClosing;
            PreviewKeyDown += MainWindow_PreviewKeyDown;

        }

        private void LoadCategories(ExternalCommandData commandData)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            List<string> categoryNames = new List<string>();

            foreach (Category category in doc.Settings.Categories)
            {
                category_dict.Add(category.Name, category);
                categoryNames.Add(category.Name);
            }

            categoryNames.Sort();

            foreach (string categoryName in categoryNames)
            {
                CategoryComboBox.Items.Add(categoryName);
            }
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_lastKeyPressed == Key.F && e.Key == Key.F)
            {
                // Последовательное нажатие клавиши F обнаружено
                Activate();// Сделать окно активным
                filter();
                _lastKeyPressed = Key.None;
                //MessageBox.Show("Последовательное нажатие клавиш F-F обнаружено!");
            }
            else
            {
                _lastKeyPressed = e.Key;
            }
        }

        void filter()
        {
            {
                if (_exEvent != null && CategoryComboBox.SelectedItem != null)
                {
                    SelectedCategory = category_dict[CategoryComboBox.SelectedItem.ToString()];
                    _exEvent.Raise();
                }
                else
                    MessageBox.Show("external event handler is null");
            }
        }
        private void OnSelectButtonClick(object sender, RoutedEventArgs e)
        {
            filter();
        }
        private void WinClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FastFilter.win = null;
        }
    }
}
