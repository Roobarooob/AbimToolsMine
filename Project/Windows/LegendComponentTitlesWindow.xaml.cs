using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace AbimToolsMine
{
    public partial class LegendComponentTitlesWindow : Window
    {
        public string ParameterName => ParameterNameBox.Text.Trim();
        public string OffsetText => OffsetBox.Text.Trim();
        public ElementId SelectedTextNoteTypeId
        {
            get
            {
                TextNoteTypeItem item = TextNoteTypeBox.SelectedItem as TextNoteTypeItem;
                return item?.Id ?? ElementId.InvalidElementId;
            }
        }

        public LegendComponentTitlesWindow(
            IEnumerable<TextNoteType> textNoteTypes,
            string parameterName,
            double offsetMillimeters,
            ElementId selectedTextNoteTypeId)
        {
            InitializeComponent();
            ParameterNameBox.Text = parameterName ?? string.Empty;
            OffsetBox.Text = offsetMillimeters.ToString(CultureInfo.InvariantCulture);
            List<TextNoteTypeItem> textNoteTypeItems = textNoteTypes
                .Select(t => new TextNoteTypeItem { Id = t.Id, Name = t.Name })
                .ToList();
            TextNoteTypeBox.ItemsSource = textNoteTypeItems;
            TextNoteTypeBox.SelectedItem = textNoteTypeItems
                .FirstOrDefault(item => item.Id == selectedTextNoteTypeId);

            if (TextNoteTypeBox.SelectedIndex < 0 && TextNoteTypeBox.Items.Count > 0)
                TextNoteTypeBox.SelectedIndex = 0;

            ParameterNameBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private class TextNoteTypeItem
        {
            public ElementId Id { get; set; }
            public string Name { get; set; }
        }
    }
}
