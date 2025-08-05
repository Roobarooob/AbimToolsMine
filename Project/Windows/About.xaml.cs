using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AbimToolsMine.Properties;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
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
        private string superpanel = "Плагин";
        private readonly UIApplication _uiApp;
        private readonly Dictionary<string, RibbonPanel> _panelMap =  new Dictionary<string, RibbonPanel>();

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
                EnableCheckboxesAndApplyPanelVisibility(); // разрешить и синхронизировать
                Check_Label.Content = "Лицензия активна";
                Check_Label.Foreground = Brushes.Green;
            }
            else
            {
                DisableCheckboxesAndHidePanels(); // заблокировать и скрыть панели
                Check_Label.Content = "Лицензия не активна";
                Check_Label.Foreground = Brushes.Red;
            }
        }

        private void CheckboxChanged(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.Content is string panelName && _panelMap.TryGetValue(panelName, out var panel))
            {
                SetRibbonPanelVisibility(panel, cb.IsChecked == true);
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
                bool isVisible = !hiddenPanels.Contains(panel.Name);
                panel.Visible = isVisible;

                var checkbox = new CheckBox
                {
                    Content = panel.Name,
                    IsChecked = isVisible,
                    Margin = new Thickness(0, 2, 0, 2)
                };
                if (checkbox.Content.ToString() == superpanel)
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
            foreach (var kvp in _panelMap)
            {
                if (!kvp.Value.Visible)
                {
                    hiddenPanels.Add(kvp.Key);
                }
            }
            Settings.Default.Access_Org= Org.Text;
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
                EnableCheckboxesAndApplyPanelVisibility();  // ✅
            }
            else
            {
                Check_Label.Content = "Лицензия не активна";
                Check_Label.Foreground = Brushes.Red;
                DisableCheckboxesAndHidePanels();  // ❌
            }
        }
        private void DisableCheckboxesAndHidePanels()
        {
            foreach (var item in CheckboxContainer.Items)
            {
                if (item is CheckBox cb)
                {
                    string name = cb.Content?.ToString();
                    if (name != superpanel)
                    {
                        cb.IsEnabled = false;

                        if (_panelMap.TryGetValue(name, out var panel))
                        {
                            SetRibbonPanelVisibility(panel, false);
                        }
                    }
                }
            }
        }
        private void EnableCheckboxesAndApplyPanelVisibility()
        {
            foreach (var item in CheckboxContainer.Items)
            {
                if (item is CheckBox cb)
                {
                    string name = cb.Content?.ToString();
                    if (name == superpanel)
                        continue; // Пропускаем "Плагин"

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