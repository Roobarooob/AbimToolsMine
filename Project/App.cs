#region Namespaces
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using Settings = AbimToolsMine.Properties.Settings;
#endregion

namespace AbimToolsMine
{
    public class App : IExternalApplication
    {
        private UIControlledApplication _application;
        private bool _panelsInitialized = false;
        private string superpanel = "������";

        public RibbonPanel RibbonPanel(UIControlledApplication a, string tab, string ribbonPanelText)
        {
            RibbonPanel ribbonPanel = null;

            try
            {
                a.CreateRibbonTab(tab);
            }
            catch 
            {

            }

            try
            {
                ribbonPanel = a.CreateRibbonPanel(tab, ribbonPanelText);
            }
            catch (Exception ex)
            {
                // ����������� ������
                TaskDialog.Show("Error", ex.Message);
            }

            if (ribbonPanel == null)
            {
                List<RibbonPanel> panels = a.GetRibbonPanels(tab);
                ribbonPanel = panels.FirstOrDefault(p => p.Name == ribbonPanelText);
            }

            return ribbonPanel;
        }

        private PushButton CreateButton(
            RibbonPanel panel,
            string name,
            string text,
            string command,
            string imageUri,
            string toolTip = "",
            string longDescription = "",
            string availabilityClassName = "",
            string dllName = "")
        {
            // ���������� ���� � DLL-�����: ���� ��� DLL ������, ���������� ���, ����� ����� ������� DLL
            string assemblyPath;
            if (!string.IsNullOrWhiteSpace(dllName))
            {
                string thisDllPath = typeof(App).Assembly.Location;
                string folder = Path.GetDirectoryName(thisDllPath);
                assemblyPath = Path.Combine(folder, dllName);
            }
            else
            {
                assemblyPath = typeof(App).Assembly.Location;
            }

            // ������ ������ ������
            PushButtonData buttonData = new PushButtonData(name, text, assemblyPath, command);

            // ��������� �����������
            BitmapImage pbImage = new BitmapImage(new Uri(imageUri));
            buttonData.LargeImage = pbImage;

            // ��������� ������ �� ������
            PushButton button = panel.AddItem(buttonData) as PushButton;
            button.ToolTip = toolTip;
            button.LongDescription = longDescription;

            // ������������� ����� �����������, ���� ������
            if (!string.IsNullOrWhiteSpace(availabilityClassName))
            {
                try
                {
                    button.AvailabilityClassName = availabilityClassName;
                }
                catch (Exception) { }
            }

            return button;
        }

        private void OnButtonCreate(UIControlledApplication application)
        {
            var pan0 = RibbonPanel(application, "����-���", superpanel);
            
            // ������ "���������"

            PushButton ToggleAbimPanels = CreateButton(
                panel: pan0,
                name: "About",
                text: "���������",
                command: "AbimToolsMine.ToggleAbimPanels",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/About.png",
                toolTip: "��������� �������� �������",
                longDescription: "",
                availabilityClassName: "AbimToolsMine.CommandAvailability",
                dllName: "AbimToolsMine.dll"
            );
            
            var pan1 = RibbonPanel(application, "����-���", "����� �������");
            // ������ "�������� ���������"
            PushButton bath_button = CreateButton(
                panel: pan1,
                name: "BatchTools",
                text: "��������\n���������",
                command: "AbimToolsMine.BatchTools",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/BatchTools32.png",
                toolTip: "����������� ������������������ ���������������� ��������� ���������� ������ Revit",
                longDescription: "�������������� �������� ��������, ����������� �������� �� XML, �������� ������ �� �������",
                availabilityClassName: "AbimToolsMine.CommandAvailability",
                dllName: "AbimToolsMine.dll"
            );
            bath_button.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/BatchTools16.png"));

            // ������ "��������� ��������"
            PushButton col_button = CreateButton(
                panel: pan1,
                name: "CollisionTools",
                text: "���������\n��������",
                command: "AbimToolsMine.Collisions",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/Collisions32.png",
                toolTip: "�������� �������� �� XML � ������� ��������\n���� � XML ������ ���� �������� � ��������� ���_���� XML ��������",
                dllName: "AbimToolsMine.dll"
            );
            col_button.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/Collisions16.png"));

            // ������ "������������"
            PushButton selector_button = CreateButton(
                panel: pan1,
                name: "FastFilter",
                text: "������������",
                command: "AbimToolsMine.FastFilter",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/FastFilter32.png",
                toolTip: "���������� ��������� ���������, ����� �������",
                dllName: "AbimToolsMine.dll"
            );
            selector_button.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/FastFilter16.png"));

            // ������ "����������� ������� �������"
            PushButton workset_button = CreateButton(
                panel: pan1,
                name: "SetWorksetForLinks",
                text: "�����������\n������� �������",
                command: "AbimToolsMine.LinksWokset",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/WSName32.png",
                toolTip: "�������� ������� ������� ��� ������, ���������� ������ ������� �������",
                dllName: "AbimToolsMine.dll"
            );
            workset_button.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/WSName16.png"));

            // ������ "������� ������ ������"
            PushButton lookUp_button = CreateButton(
                panel: pan1,
                name: "GetLookupTable",
                text: "������� ������\n������",
                command: "AbimToolsMine.GetLookUpTable",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/LPTableExport32.png",
                toolTip: "������� ������ ������ �� ��������� ��������� ��� ��������� �� �������",
                dllName: "AbimToolsMine.dll"
            );
            lookUp_button.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/LPTableExport16.png"));

            // ������ "�������� ���������� ������"
            PushButton linkChecker_button = CreateButton(
                panel: pan1,
                name: "DuplicateLinkChecker",
                text: "��������\n���������� ������",
                command: "AbimToolsMine.DuplicateLinkChecker",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/linkChecker32.png",
                toolTip: "�������� ������ �� ��������� ������",
                dllName: "AbimToolsMine.dll"
            );
            linkChecker_button.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/linkChecker16.png"));

            // ������ "������ � ��������"
            PushButton CheckLevels = CreateButton(
                panel: pan1,
                name: "CheckLevels",
                text: "������ � ��������",
                command: "AbimToolsMine.LevelTools",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/Levels32.png",
                toolTip: "�������� �������� � ���������� ���������� �������",
                dllName: "AbimToolsMine.dll"
            );
            CheckLevels.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/Levels16.png"));

            // ������ "������ ��� �� ���� ������"
            PushButton hideAxes_button = CreateButton(
                panel: pan1,
                name: "DisableLevelsAndGridsWorksetInLinks",
                text: "������ ���\n�� ����\n������",
                command: "AbimToolsMine.DisableLevelsAndGridsWorksetInLinks",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/Osi32.png",
                toolTip: "������ ��� �� ���� ��������� �������",
                dllName: "AbimToolsMine.dll"
            );
            hideAxes_button.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/Osi16.png"));

            // ������ "��������� ����� ������" (�������� ��������� ����� � C:\a � �������� � C:\a\�����)
            PushButton localBackupBtn = CreateButton(
                panel: pan1,
                name: "MakeLocalTask",
                text: "������\n�������",
                command: "AbimToolsMine.MakeLocalTask",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/MakeLocalTask32.png",
                toolTip: "�������������/���������� � ����� ����� ��� ������� ������� ��������)",
                dllName: "AbimToolsMine.dll"
            );
            localBackupBtn.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/MakeLocalTask16.png"));
            
            var pan2 = RibbonPanel(application, "����-���", "������� � ����");
            
            // ������ "C������� ��������� �������"
            PushButton Pref_ScheduleFinishing = CreateButton(
                panel: pan2,
                name: "Pref_ScheduleFinishing",
                text: "���������",
                command: "AbimToolsMine.Pref_ScheduleFinishingWindow",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/Pref_Finishing32.png",
                toolTip: "��������� �������� ��������� �������",
                dllName: "AbimToolsMine.dll"
            );
            Pref_ScheduleFinishing.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/Pref_Finishing32.png"));

            // ������ "C������� ��������� �������"
            PushButton ScheduleFinishing = CreateButton(
                panel: pan2,
                name: "ScheduleFinishing",
                text: "��������� �������",
                command: "AbimToolsMine.ScheduleFinishing",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/Finishing32.png",
                toolTip: "�������� ��������� ������� �� ����",
                dllName: "AbimToolsMine.dll"
            );
            ScheduleFinishing.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/Finishing32.png"));
           
            // ������ "C������� ��������� �������"
            PushButton FloorLegends = CreateButton(
                panel: pan2,
                name: "FloorLegends",
                text: "����\n�������� �������",
                command: "AbimToolsMine.FloorLegends",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/FloorLegends32.png",
                toolTip: "�������� ������� ��� ������� ���� ����. (���� ����� ��������� ��������� ������)",
                dllName: "AbimToolsMine.dll"
            );
            ScheduleFinishing.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/FloorLegends16.png"));

            // ������ "C������� ��������� �������"
            PushButton LegendsToParameters = CreateButton(
                panel: pan2,
                name: "LegendsToParameter",
                text: "����\n������ � ��������",
                command: "AbimToolsMine.LegendsToParameter",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/LegendsToParameter32.png",
                toolTip: "������ ��������� � �������� ���� ����",
                dllName: "AbimToolsMine.dll"
            );
            ScheduleFinishing.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/LegendsToParameter16.png"));

            // ������ "��������"
            /*PushButton LintelsPlacing = CreateButton(
                panel: pan2,
                name: "LintelsPlacing",
                text: "����\n������ � ��������",
                command: "AbimToolsMine.LintelsPlacing",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/LegendsToParameter32.png",
                toolTip: "������ ��������� � �������� ���� ����",
                dllName: "AbimToolsMine.dll"
            );
            LintelsPlacing.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/LegendsToParameter16.png"));*/
        }

        public Result OnStartup(UIControlledApplication application)
        {
            _application = application;
            OnButtonCreate(application);
            application.ControlledApplication.DocumentOpened += OnDocumentOpened;
            // ������� ���������� ����������:
            application.Idling += OnIdling_ApplyPanelVisibility;
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            application.ControlledApplication.DocumentOpened -= OnDocumentOpened;
            return Result.Succeeded;
        }

        private void OnDocumentOpened(object sender, Autodesk.Revit.DB.Events.DocumentOpenedEventArgs e)
        {
            Document doc = e.Document;

            if (!doc.IsFamilyDocument)
            {
                PinLink(doc);
            }
        }

        private void PinLink(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            var elementsToPin = collector.OfClass(typeof(RevitLinkInstance))
                                        .UnionWith(new FilteredElementCollector(doc).OfClass(typeof(Grid)))
                                        .UnionWith(new FilteredElementCollector(doc).OfClass(typeof(Level)))
                                        .Cast<Element>();

            using (Transaction trans = new Transaction(doc, "Pin"))
            {
                trans.Start();

                foreach (var element in elementsToPin)
                {
                    element.Pinned = true;
                }

                trans.Commit();
            }
        }

        public static void ApplyPanelVisibilityFromSettings(UIControlledApplication app)
        {
            var hiddenPanels = Settings.Default.HiddenPanels ?? new System.Collections.Specialized.StringCollection();

            // ������� �������� ������ �� �������
            var panels = app.GetRibbonPanels("����-���");

            foreach (var panel in panels)
            {
                panel.Visible = !hiddenPanels.Contains(panel.Name);
            }
        }
        private void OnIdling_ApplyPanelVisibility(object sender, Autodesk.Revit.UI.Events.IdlingEventArgs e)
        {
            if (_panelsInitialized)
                return;

            _panelsInitialized = true;

            var panels = _application.GetRibbonPanels("����-���");
            var hiddenPanels = Settings.Default.HiddenPanels ?? new System.Collections.Specialized.StringCollection();
            string org = Settings.Default.Access_Org;
            string code = Settings.Default.Access_Code;

            bool licenseValid = LicenseChecker.IsLicenseValid(org, code);

            foreach (var panel in panels)
            {
                if (panel.Name == superpanel)
                {
                    panel.Visible = true;
                    continue;
                }

                if (licenseValid)
                {
                    // ��������������� ��������� �� ����������
                    panel.Visible = !hiddenPanels.Contains(panel.Name);
                }
                else
                {
                    // �������� �� ����� "������"
                    panel.Visible = false;
                }
            }
        }
    }

    public class CommandAvailability : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
        {
            return true;
        }
    }
}