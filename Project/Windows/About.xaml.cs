using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Settings = AbimToolsMine.Properties.Settings;

namespace AbimToolsMine
{

    [Transaction(TransactionMode.Manual)]
    public class ToggleAbimPanels : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var window = new AboutWindow(commandData.Application);
            window.ShowDialog();
            return Result.Succeeded;
        }
    }

    public partial class AboutWindow : Window
    {
        private const string SuperPanel = "Плагин";
        private readonly UIApplication _uiApp;
        private readonly Dictionary<string, RibbonPanel> _panelMap = new Dictionary<string, RibbonPanel>();
        private bool _isUpdatingPanelSelection;

        public AboutWindow(UIApplication uiApp)
        {
            InitializeComponent();
            _uiApp = uiApp;

            LoadPanelVisibilitySettings();

            Org.Text = Settings.Default.Access_Org;
            Code.Text = Settings.Default.Access_Code;

            // ⛔ Проверка лицензии при открытии окна
            bool licenseValid = LicenseChecker.IsLicenseValid(Org.Text, Code.Text);
            if (licenseValid)
            {
                EnableCheckboxesAndApplyPanelVisibility();
                Check_Label.Content = "Лицензия активна";
                Check_Label.Foreground = Brushes.Green;
                SetLicenseStatusBackground(true);
            }
            else
            {
                DisableCheckboxesAndHidePanels();
                Check_Label.Content = "Лицензия не активна";
                Check_Label.Foreground = Brushes.Red;
                SetLicenseStatusBackground(false);
            }
        }

        private void CheckboxChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.Content is string panelName && _panelMap.TryGetValue(panelName, out var panel))
            {
                bool isSuperPanel = panelName == SuperPanel;
                SetRibbonPanelVisibility(panel, isSuperPanel || cb.IsChecked == true);

                if (isSuperPanel && cb.IsChecked != true)
                {
                    cb.IsChecked = true;
                }

                if (!_isUpdatingPanelSelection)
                {
                    SavePanelVisibilitySettings();
                }
            }
        }
        public static List<RibbonPanel> FindAbimPanels(UIApplication app)
        {
            var allPanels = app.GetRibbonPanels("АБИМ-ПРО");
            return allPanels;
        }

        public static void SetRibbonPanelVisibility(RibbonPanel panel, bool visible)
        {
            panel.Visible = visible;
        }

        private void LoadPanelVisibilitySettings()
        {
            var hiddenPanels = Settings.Default.HiddenPanels ?? new System.Collections.Specialized.StringCollection();

            foreach (var panel in FindAbimPanels(_uiApp))
            {
                bool isSuperPanel = panel.Name == SuperPanel;
                bool isVisible = isSuperPanel || !hiddenPanels.Contains(panel.Name);
                panel.Visible = isVisible;

                var checkbox = new CheckBox
                {
                    Content = panel.Name,
                    IsChecked = isVisible
                };
                if (isSuperPanel)
                {
                    checkbox.IsChecked = true;
                    checkbox.IsEnabled = false;
                }
                checkbox.Checked += CheckboxChanged;
                checkbox.Unchecked += CheckboxChanged;

                _panelMap[panel.Name] = panel;
                CheckboxContainer.Items.Add(checkbox);
            }
        }
        private void SavePanelVisibilitySettings()
        {
            var hiddenPanels = new System.Collections.Specialized.StringCollection();
            foreach (var item in CheckboxContainer.Items)
            {
                if (!(item is CheckBox cb) || !(cb.Content is string panelName))
                {
                    continue;
                }

                if (panelName == SuperPanel)
                {
                    cb.IsChecked = true;
                    if (_panelMap.TryGetValue(panelName, out var superPanel))
                    {
                        superPanel.Visible = true;
                    }
                    continue;
                }

                if (cb.IsChecked != true)
                {
                    hiddenPanels.Add(panelName);
                }
            }
            Settings.Default.Access_Org = Org.Text;
            Settings.Default.Access_Code = Code.Text;
            Settings.Default.HiddenPanels = hiddenPanels;
            Settings.Default.Save();
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            SavePanelVisibilitySettings();
        }

        private void LCheck_Click(object sender, RoutedEventArgs e)
        {
            bool licenseValid = LicenseChecker.IsLicenseValid(Org.Text, Code.Text);
            if (licenseValid)
            {
                Check_Label.Content = "Лицензия активна";
                Check_Label.Foreground = Brushes.Green;
                SetLicenseStatusBackground(true);
                EnableCheckboxesAndApplyPanelVisibility();
            }
            else
            {
                Check_Label.Content = "Лицензия не активна";
                Check_Label.Foreground = Brushes.Red;
                SetLicenseStatusBackground(false);
                DisableCheckboxesAndHidePanels();
            }

            SavePanelVisibilitySettings();
        }

        private void EnableAllButton_Click(object sender, RoutedEventArgs e)
        {
            SetAllOptionalPanelsVisibility(true);
        }

        private void DisableAllButton_Click(object sender, RoutedEventArgs e)
        {
            SetAllOptionalPanelsVisibility(false);
        }

        private void SetAllOptionalPanelsVisibility(bool visible)
        {
            _isUpdatingPanelSelection = true;
            try
            {
                foreach (var item in CheckboxContainer.Items)
                {
                    if (item is CheckBox cb &&
                        cb.Content is string panelName &&
                        panelName != SuperPanel &&
                        cb.IsEnabled)
                    {
                        cb.IsChecked = visible;
                    }
                }
            }
            finally
            {
                _isUpdatingPanelSelection = false;
            }

            SavePanelVisibilitySettings();
        }

        private void SetLicenseStatusBackground(bool isValid)
        {
            if (Check_Label.Parent is Border statusBorder)
            {
                statusBorder.Background = new SolidColorBrush(
                    (System.Windows.Media.Color)ColorConverter.ConvertFromString(isValid ? "#EFFAF3" : "#FFF3F3"));
                statusBorder.BorderBrush = new SolidColorBrush(
                    (System.Windows.Media.Color)ColorConverter.ConvertFromString(isValid ? "#B9E5C8" : "#FFC9C9"));
            }
        }

        private void DisableCheckboxesAndHidePanels()
        {
            EnableAllButton.IsEnabled = false;
            DisableAllButton.IsEnabled = false;

            foreach (var item in CheckboxContainer.Items)
            {
                if (item is CheckBox cb)
                {
                    string name = cb.Content?.ToString();
                    if (name != SuperPanel)
                    {
                        cb.IsEnabled = false;

                        if (_panelMap.TryGetValue(name, out var panel))
                        {
                            SetRibbonPanelVisibility(panel, false);
                        }
                    }
                    else if (_panelMap.TryGetValue(name, out var superPanel))
                    {
                        cb.IsChecked = true;
                        SetRibbonPanelVisibility(superPanel, true);
                    }
                }
            }
        }
        private void EnableCheckboxesAndApplyPanelVisibility()
        {
            EnableAllButton.IsEnabled = true;
            DisableAllButton.IsEnabled = true;

            foreach (var item in CheckboxContainer.Items)
            {
                if (item is CheckBox cb)
                {
                    string name = cb.Content?.ToString();
                    if (name == SuperPanel)
                    {
                        cb.IsChecked = true;
                        cb.IsEnabled = false;
                        if (_panelMap.TryGetValue(name, out var superPanel))
                        {
                            SetRibbonPanelVisibility(superPanel, true);
                        }
                        continue;
                    }

                    cb.IsEnabled = true;

                    if (_panelMap.TryGetValue(name, out var panel))
                    {
                        SetRibbonPanelVisibility(panel, cb.IsChecked == true);
                    }
                }
            }
        }

    }
}
