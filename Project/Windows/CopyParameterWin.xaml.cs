using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AbimToolsMine
{
    public class CategoryItem : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public Category Category { get; set; }

        private bool _isChecked;
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked == value) return;
                _isChecked = value;
                OnPropertyChanged("IsChecked");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string prop)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }

    public partial class CopyParameterWin : Window
    {
        // Result filled before window closes
        public bool UseSelection { get; private set; }
        public List<Category> SelectedCategories { get; private set; } = new List<Category>();
        public string FromParam { get; private set; }
        public string ToParam { get; private set; }
        public bool OnlyEmpty { get; private set; }
        // true — копировать из родителя во вложенные субэлементы
        public bool NestedMode { get; private set; }

        private readonly Document _doc;
        private readonly UIDocument _uidoc;

        // Полный список всех категорий
        private readonly List<CategoryItem> _allCategoryItems = new List<CategoryItem>();

        public CopyParameterWin(ExternalCommandData commandData)
        {
            InitializeComponent();
            _uidoc = commandData.Application.ActiveUIDocument;
            _doc = _uidoc.Document;
            LoadCategories();
            LoadSelectionCount();
        }

        private void LoadCategories()
        {
            var names = new List<string>();
            foreach (Category cat in _doc.Settings.Categories)
                names.Add(cat.Name);
            names.Sort();

            foreach (var name in names)
            {
                Category cat = null;
                foreach (Category c in _doc.Settings.Categories)
                    if (c.Name == name) { cat = c; break; }

                _allCategoryItems.Add(new CategoryItem { Name = name, Category = cat, IsChecked = false });
            }

            ApplyFilter("");
        }

        private void ApplyFilter(string filter)
        {
            LbCategories.Items.Clear();
            foreach (var item in _allCategoryItems)
            {
                if (string.IsNullOrEmpty(filter) ||
                    item.Name.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    LbCategories.Items.Add(item);
                }
            }
        }

        private void UpdateSelectedSummary()
        {
            var checked_ = _allCategoryItems
                .Where(i => i.IsChecked)
                .Select(i => i.Name)
                .ToList();

            TbSelectedSummary.Text = checked_.Count == 0
                ? "Выбрано: нет"
                : "Выбрано: " + string.Join(", ", checked_);
        }

        private void OnCategoryFilterChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter(TbCategoryFilter.Text);
        }

        private void OnCategoryCheckedChanged(object sender, RoutedEventArgs e)
        {
            UpdateSelectedSummary();
        }

        private void RbSource_Checked(object sender, RoutedEventArgs e)
        {
            if (LbCategories == null) return;
            LbCategories.IsEnabled = RbAllCategories.IsChecked == true;
            if (TbCategoryFilter != null)
                TbCategoryFilter.IsEnabled = RbAllCategories.IsChecked == true;
        }

        private void OnModeChanged(object sender, RoutedEventArgs e)
        {
            if (LblFromParam == null) return;
            bool nested = RbModeNested.IsChecked == true;

            LblFromParam.Content = nested ? "Параметр:" : "Из параметра:";

            System.Windows.Visibility toVisibility = nested
                ? System.Windows.Visibility.Collapsed
                : System.Windows.Visibility.Visible;
            LblToParam.Visibility = toVisibility;
            TbToParam.Visibility  = toVisibility;
        }

        private void OnExecuteClick(object sender, RoutedEventArgs e)
        {
            NestedMode = RbModeNested.IsChecked == true;
            FromParam  = TbFromParam.Text.Trim();
            ToParam    = NestedMode ? TbFromParam.Text.Trim() : TbToParam.Text.Trim();
 OnlyEmpty  = CbOnlyEmpty.IsChecked == true;
  UseSelection = RbSelection.IsChecked == true;

  if (string.IsNullOrWhiteSpace(FromParam))
            {
                MessageBox.Show("Укажите имя параметра.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
    }

      if (!NestedMode && string.IsNullOrWhiteSpace(ToParam))
            {
        MessageBox.Show("Укажите имена обоих параметров.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
     return;
            }

  if (!UseSelection)
{
       SelectedCategories = _allCategoryItems
      .Where(i => i.IsChecked)
      .Select(i => i.Category)
.ToList();

       if (SelectedCategories.Count == 0)
           {
            MessageBox.Show("Выберите хотя бы одну категорию.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
     return;
       }
   }

            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void LoadSelectionCount()
        {
            int count = _uidoc.Selection.GetElementIds().Count;
            TbSelectionCount.Text = "Выбрано элементов: " + count;
        }
    }
}
