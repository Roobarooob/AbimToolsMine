using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AbimToolsMine
{
    public partial class CategorySelectionWindow : Window
    {
        public List<string> SelectedCategories { get; private set; }
        private List<CategoryInfo> allItems = new List<CategoryInfo>();

        public CategorySelectionWindow(List<string> preselectedCategories)
        {
            InitializeComponent();
            LoadCategories(preselectedCategories);
        }

        private void LoadCategories(List<string> preselectedCategories)
        {
            // Загрузка категорий из документа
            string filePath = Path.GetDirectoryName(typeof(App).Assembly.Location) + "/Categories.csv";

            // Чтение строк из файла
            List<string> lines = new List<string>();
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            // Инициализация списка категорий с учетом предварительно выбранных
            allItems = lines.Select(line => new CategoryInfo
            {
                Name = line,
                IsSelected = preselectedCategories.Contains(line)
            }).ToList();

            // Привязка списка к ListBox
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
