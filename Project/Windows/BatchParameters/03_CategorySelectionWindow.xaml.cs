using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;

namespace AbimTools
{
    public partial class CategorySelectionWindow : Window
    {
        public List<string> SelectedCategories { get; private set; }
        private List<CategoryInfo> allItems = new List<CategoryInfo>(); // Список для хранения всех элементов
        public CategorySelectionWindow()
        {
            InitializeComponent();
            LoadCategories();
        }

        private void LoadCategories()
        {
            // Загрузка категорий из документа
            string filePath = Path.GetDirectoryName(typeof(App).Assembly.Location)+ "/Categories.csv";

            // Список для хранения строк из файла
            List<string> lines = new List<string>();

            // Чтение строк из файла и добавление их в список
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }
            allItems = lines.Select(line => new CategoryInfo { Name = line }).ToList();
            CategoryListBox.ItemsSource = allItems;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedCategories = allItems.Where(c => c.IsSelected).Select(c => c.Name).ToList();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchBox.Text.ToLower();

            // Фильтруем элементы на основе текста поиска, сохраняя состояние IsSelected
            CategoryListBox.ItemsSource = allItems
                .Where(c => c.Name.ToLower().Contains(searchText))
                .ToList();
        }
    }

    public class CategoryInfo
    {
        public string Name { get; set; }
        public bool IsSelected { get; set; }
    }
}
