using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AbimToolsMine
{
    public class SheetSelectionItem : INotifyPropertyChanged
    {
        public ViewSheet Sheet { get; set; }
        public string DisplayName { get; set; }

        private bool _isChecked;
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked == value)
                    return;

                _isChecked = value;
                OnPropertyChanged("IsChecked");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class AlignBySheetsWindow : Window
    {
        private readonly List<SheetSelectionItem> _allSheets;
        private readonly ObservableCollection<SheetSelectionItem> _visibleSheets = new ObservableCollection<SheetSelectionItem>();

        public List<ViewSheet> SelectedSheets { get; private set; } = new List<ViewSheet>();
        public ViewSheet ReferenceSheet { get; private set; }
        public string FirstGridName { get; private set; }
        public string SecondGridName { get; private set; }
        public List<ViewType> SelectedViewTypes { get; private set; } = new List<ViewType>();

        public AlignBySheetsWindow(List<ViewSheet> sheets)
        {
            InitializeComponent();

            _allSheets = sheets
                .Select(s => new SheetSelectionItem
                {
                    Sheet = s,
                    DisplayName = $"{s.SheetNumber} - {s.Name}"
                })
                .ToList();

            foreach (var item in _allSheets)
                item.PropertyChanged += SheetItem_PropertyChanged;

            SheetsListBox.ItemsSource = _visibleSheets;
            ReferenceSheetComboBox.ItemsSource = _allSheets;
            ReferenceSheetComboBox.SelectedIndex = _allSheets.Count > 0 ? 0 : -1;

            ApplyFilter();
            UpdateSelectedCount();
        }

        private void SheetFilterBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (SheetSelectionItem item in _visibleSheets)
                item.IsChecked = true;

            UpdateSelectedCount();
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (SheetSelectionItem item in _visibleSheets)
                item.IsChecked = false;

            UpdateSelectedCount();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            SheetSelectionItem referenceItem = ReferenceSheetComboBox.SelectedItem as SheetSelectionItem;
            ReferenceSheet = referenceItem?.Sheet;
            FirstGridName = FirstGridBox.Text.Trim();
            SecondGridName = SecondGridBox.Text.Trim();

            SelectedViewTypes = new List<ViewType>();
            if (FloorPlansCheckBox.IsChecked == true)
                SelectedViewTypes.Add(ViewType.FloorPlan);
            if (CeilingPlansCheckBox.IsChecked == true)
                SelectedViewTypes.Add(ViewType.CeilingPlan);

            SelectedSheets = _allSheets
                .Where(s => s.IsChecked)
                .Select(s => s.Sheet)
                .ToList();

            if (ReferenceSheet != null && !SelectedSheets.Any(s => s.Id == ReferenceSheet.Id))
                SelectedSheets.Add(ReferenceSheet);

            if (SelectedSheets.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один лист.", "Выравнивание по листам", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ReferenceSheet == null)
            {
                MessageBox.Show("Выберите эталонный лист.", "Выравнивание по листам", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedViewTypes.Count == 0)
            {
                MessageBox.Show("Выберите планы этажей и/или планы потолков.", "Выравнивание по листам", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(FirstGridName) || string.IsNullOrWhiteSpace(SecondGridName))
            {
                MessageBox.Show("Укажите две опорные оси.", "Выравнивание по листам", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.Equals(FirstGridName, SecondGridName, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Опорные оси должны быть разными.", "Выравнивание по листам", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ApplyFilter()
        {
            string filter = SheetFilterBox.Text ?? string.Empty;
            _visibleSheets.Clear();

            foreach (SheetSelectionItem item in _allSheets)
            {
                if (string.IsNullOrWhiteSpace(filter) ||
                    item.DisplayName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _visibleSheets.Add(item);
                }
            }
        }

        private void SheetItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsChecked")
                UpdateSelectedCount();
        }

        private void UpdateSelectedCount()
        {
            if (SelectedCountText == null)
                return;

            int checkedCount = _allSheets.Count(s => s.IsChecked);
            SelectedCountText.Text = $"Выбрано листов: {checkedCount}";
        }
    }
}
