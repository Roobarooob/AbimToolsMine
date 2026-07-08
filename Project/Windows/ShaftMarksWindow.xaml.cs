using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AbimToolsMine
{
    public partial class ShaftMarksWindow : Window
    {
        private readonly List<ViewItem> _views;

        public string ParameterName => ParameterNameBox.Text.Trim();
        public bool UseCurrentView => CurrentViewRadio.IsChecked == true;
        public ElementId TextTypeId => (TextTypeBox.SelectedItem as TextTypeItem)?.Id ?? ElementId.InvalidElementId;
        public IList<string> SelectedViewUniqueIds => _views.Where(x => x.IsSelected).Select(x => x.UniqueId).ToList();

        public ShaftMarksWindow(IEnumerable<ViewPlan> views, IEnumerable<TextNoteType> textTypes,
            string parameterName, bool useCurrentView, IEnumerable<string> selectedViewIds, ElementId textTypeId)
        {
            InitializeComponent();
            var selected = new HashSet<string>(selectedViewIds ?? Enumerable.Empty<string>());
            _views = views.OrderBy(v => v.Name).Select(v => new ViewItem
            {
                UniqueId = v.UniqueId,
                DisplayName = v.Name + "  [" + GetViewKind(v.ViewType) + "]",
                IsSelected = selected.Contains(v.UniqueId)
            }).ToList();

            ParameterNameBox.Text = parameterName ?? string.Empty;
            var types = textTypes.OrderBy(t => t.Name).Select(t => new TextTypeItem { Id = t.Id, Name = t.Name }).ToList();
            TextTypeBox.ItemsSource = types;
            TextTypeBox.SelectedItem = types.FirstOrDefault(x => x.Id == textTypeId) ?? types.FirstOrDefault();
            CurrentViewRadio.IsChecked = useCurrentView;
            SelectedViewsRadio.IsChecked = !useCurrentView;
            ApplyFilter();
        }

        private static string GetViewKind(ViewType type) =>
            type == ViewType.CeilingPlan ? "план потолка" : "план этажа";

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            if (ViewsList == null) return;
            string query = SearchBox?.Text?.Trim() ?? string.Empty;
            ViewsList.ItemsSource = _views.Where(x => x.DisplayName.IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();
        }

        private void Mode_Changed(object sender, RoutedEventArgs e)
        {
            if (SearchBox == null || ViewsList == null) return;
            bool enabled = SelectedViewsRadio.IsChecked == true;
            SearchBox.IsEnabled = enabled;
            ViewsList.IsEnabled = enabled;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ParameterName))
            {
                MessageBox.Show(this, "Укажите имя параметра.", "Марки по шахтам", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!UseCurrentView && !SelectedViewUniqueIds.Any())
            {
                MessageBox.Show(this, "Выберите хотя бы один вид.", "Марки по шахтам", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
        }

        private sealed class TextTypeItem { public ElementId Id { get; set; } public string Name { get; set; } }

        private sealed class ViewItem : INotifyPropertyChanged
        {
            private bool _isSelected;
            public string UniqueId { get; set; }
            public string DisplayName { get; set; }
            public bool IsSelected { get => _isSelected; set { _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); } }
            public event PropertyChangedEventHandler PropertyChanged;
        }
    }
}
