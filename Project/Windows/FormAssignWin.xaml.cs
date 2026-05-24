using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Settings = AbimToolsMine.Properties.Settings;

namespace AbimToolsMine
{
    public partial class FormAssignWin : Window
    {
        // --- Result properties ---
        public bool Confirmed { get; private set; }
        public Element SelectedFormElement { get; private set; }
        public RevitLinkInstance SelectedLinkInstance { get; private set; }
        public string FormParam { get; private set; }

        private readonly Document _doc;

        // Helper wrappers for ComboBox display
        private class LinkItem
        {
            public string Name { get; set; }
            public RevitLinkInstance LinkInstance { get; set; }
        }

        private class FormItem
        {
            public string Name { get; set; }
            public Element Element { get; set; }
        }

        public FormAssignWin(Document doc)
        {
            InitializeComponent();
            _doc = doc;

            // Load settings value into textbox
            TbFormParam.Text = Settings.Default.FormParamName;

            // Load linked files
            var links = new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>()
                .Where(li => li.GetLinkDocument() != null)
                .Select(li => new LinkItem { Name = li.Name, LinkInstance = li })
                .ToList();

            foreach (var item in links)
                CbLinks.Items.Add(item);

            // Load forms from current doc by default
            LoadForms(doc, null);
        }

        private void OnSourceChanged(object sender, RoutedEventArgs e)
        {
            if (CbLinks == null) return;

            bool isLinked = RbLinkedFile.IsChecked == true;
            CbLinks.IsEnabled = isLinked;

            if (!isLinked)
            {
                LoadForms(_doc, null);
            }
            else
            {
                // Clear forms until link is selected
                CbForms.Items.Clear();

                if (CbLinks.SelectedItem is LinkItem selected)
                    LoadForms(selected.LinkInstance.GetLinkDocument(), selected.LinkInstance);
            }
        }

        private void OnLinkSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CbLinks.SelectedItem is LinkItem item && item.LinkInstance.GetLinkDocument() != null)
                LoadForms(item.LinkInstance.GetLinkDocument(), item.LinkInstance);
        }

        private void LoadForms(Document sourceDoc, RevitLinkInstance linkInstance)
        {
            CbForms.Items.Clear();

            if (sourceDoc == null) return;

            string paramName = TbFormParam.Text.Trim();

            // Collect all mass elements (OST_Mass covers conceptual masses and in-place masses)
            var massElements = new FilteredElementCollector(sourceDoc)
                .OfCategory(BuiltInCategory.OST_Mass)
                .WhereElementIsNotElementType()
                .ToElements();

            var forms = new List<FormItem>();

            foreach (var el in massElements)
            {
                // Build display name: element name + param value if available
                string displayName = el.Name;

                if (!string.IsNullOrEmpty(paramName))
                {
                    Parameter p = el.LookupParameter(paramName);
                    string val = null;

                    if (p != null)
                        val = p.StorageType == StorageType.String ? p.AsString() : p.AsValueString();

                    if (!string.IsNullOrWhiteSpace(val))
                        displayName = $"{el.Name}  [{val}]";
                }

                forms.Add(new FormItem { Name = displayName, Element = el });
            }

            if (forms.Count == 0)
            {
                CbForms.Items.Add(new FormItem
                {
                    Name = "В данном файле нет формообразующих",
                    Element = null
                });

                CbForms.IsEnabled = false;
            }
            else
            {
                foreach (var f in forms)
                    CbForms.Items.Add(f);

                CbForms.IsEnabled = true;
                CbForms.SelectedIndex = 0;
            }
        }

        private void OnFormParamChanged(object sender, TextChangedEventArgs e)
        {
            string val = TbFormParam.Text;
            Settings.Default.FormParamName = val;
            Settings.Default.Save();

            // Refresh forms list with new param name
            if (RbLinkedFile != null && RbLinkedFile.IsChecked == true && CbLinks.SelectedItem is LinkItem li)
                LoadForms(li.LinkInstance.GetLinkDocument(), li.LinkInstance);
            else
                LoadForms(_doc, null);
        }

        private void OnRunClick(object sender, RoutedEventArgs e)
        {
            if (!(CbForms.SelectedItem is FormItem fi) || fi.Element == null)
            {
                MessageBox.Show(
                    "Выберите формообразующий элемент.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            if (string.IsNullOrWhiteSpace(TbFormParam.Text))
            {
                MessageBox.Show(
                    "Укажите имя параметра формообразующего.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            // Verify the selected element actually has the parameter filled
            string paramName = TbFormParam.Text.Trim();
            Parameter fp = fi.Element.LookupParameter(paramName);

            string val = fp == null
                ? null
                : (fp.StorageType == StorageType.String ? fp.AsString() : fp.AsValueString());

            if (string.IsNullOrWhiteSpace(val))
            {
                MessageBox.Show(
                    $"У выбранного элемента параметр \"{paramName}\" не заполнен или отсутствует.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            SelectedFormElement = fi.Element;
            SelectedLinkInstance = (RbLinkedFile.IsChecked == true && CbLinks.SelectedItem is LinkItem l)
                ? l.LinkInstance
                : null;

            FormParam = paramName;
            Confirmed = true;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            Close();
        }
    }
}