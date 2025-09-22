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
        private string superpanel = "Плагин";

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
                // Логирование ошибки
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
            // Определяем путь к DLL-файлу: если имя DLL задано, используем его, иначе берем текущую DLL
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

            // Создаём данные кнопки
            PushButtonData buttonData = new PushButtonData(name, text, assemblyPath, command);

            // Загружаем изображение
            BitmapImage pbImage = new BitmapImage(new Uri(imageUri));
            buttonData.LargeImage = pbImage;

            // Добавляем кнопку на панель
            PushButton button = panel.AddItem(buttonData) as PushButton;
            button.ToolTip = toolTip;
            button.LongDescription = longDescription;

            // Устанавливаем класс доступности, если указан
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
            var pan0 = RibbonPanel(application, "АБИМ-ПРО", superpanel);
            
            // Кнопка "Программа"

            PushButton ToggleAbimPanels = CreateButton(
                panel: pan0,
                name: "About",
                text: "Программа",
                command: "AbimToolsMine.ToggleAbimPanels",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/About.png",
                toolTip: "Настройки лицензий панелей",
                longDescription: "",
                availabilityClassName: "AbimToolsMine.CommandAvailability",
                dllName: "AbimToolsMine.dll"
            );
            
            var pan1 = RibbonPanel(application, "АБИМ-ПРО", "Общие утилиты");
            // Кнопка "Пакетная обработка"
            PushButton bath_button = CreateButton(
                panel: pan1,
                name: "BatchTools",
                text: "Пакетная\nобработка",
                command: "AbimToolsMine.BatchTools",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/BatchTools32.png",
                toolTip: "Инструменты автоматизированной последовательной обработки нескольких файлов Revit",
                longDescription: "Автоматическая загрузка семейств, расстановка коллизий по XML, удаление связей из моделей",
                availabilityClassName: "AbimToolsMine.CommandAvailability",
                dllName: "AbimToolsMine.dll"
            );
            bath_button.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/BatchTools16.png"));

            // Кнопка "Загрузить коллизии"
            PushButton col_button = CreateButton(
                panel: pan1,
                name: "CollisionTools",
                text: "Загрузить\nколлизии",
                command: "AbimToolsMine.Collisions",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/Collisions32.png",
                toolTip: "Загрузка коллизий по XML в текущий документ\nПуть к XML должен быть прописан в параметре ПРО_Путь XML коллизий",
                dllName: "AbimToolsMine.dll"
            );
            col_button.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/Collisions16.png"));

            // Кнопка "БыстроФильтр"
            PushButton selector_button = CreateButton(
                panel: pan1,
                name: "FastFilter",
                text: "БыстроФильтр",
                command: "AbimToolsMine.FastFilter",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/FastFilter32.png",
                toolTip: "Фильтрация выбранных категорий, одной кнопкой",
                dllName: "AbimToolsMine.dll"
            );
            selector_button.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/FastFilter16.png"));

            // Кнопка "Инструменты рабочих наборов"
            PushButton workset_button = CreateButton(
                panel: pan1,
                name: "SetWorksetForLinks",
                text: "Инструменты\nрабочих наборов",
                command: "AbimToolsMine.LinksWokset",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/WSName32.png",
                toolTip: "Создание рабочих наборов для связей, фильтрация пустых рабочих наборов",
                dllName: "AbimToolsMine.dll"
            );
            workset_button.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/WSName16.png"));

            // Кнопка "Экспорт таблиц выбора"
            PushButton lookUp_button = CreateButton(
                panel: pan1,
                name: "GetLookupTable",
                text: "Экспорт таблиц\nвыбора",
                command: "AbimToolsMine.GetLookUpTable",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/LPTableExport32.png",
                toolTip: "Экспорт таблиц выбора из документа семейства или семейства из проекта",
                dllName: "AbimToolsMine.dll"
            );
            lookUp_button.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/LPTableExport16.png"));

            // Кнопка "Проверка дубликатов связей"
            PushButton linkChecker_button = CreateButton(
                panel: pan1,
                name: "DuplicateLinkChecker",
                text: "Проверка\nдубликатов связей",
                command: "AbimToolsMine.DuplicateLinkChecker",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/linkChecker32.png",
                toolTip: "Проверка модели на дубликаты связей",
                dllName: "AbimToolsMine.dll"
            );
            linkChecker_button.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/linkChecker16.png"));

            // Кнопка "Работа с уровнями"
            PushButton CheckLevels = CreateButton(
                panel: pan1,
                name: "CheckLevels",
                text: "Работа с уровнями",
                command: "AbimToolsMine.LevelTools",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/Levels32.png",
                toolTip: "Проверка привязки и заполнение параметров уровням",
                dllName: "AbimToolsMine.dll"
            );
            CheckLevels.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/Levels16.png"));

            // Кнопка "Скрыть оси во всех связях"
            PushButton hideAxes_button = CreateButton(
                panel: pan1,
                name: "DisableLevelsAndGridsWorksetInLinks",
                text: "Скрыть оси\nво всех\nсвязях",
                command: "AbimToolsMine.DisableLevelsAndGridsWorksetInLinks",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/Osi32.png",
                toolTip: "Скрыть оси во всех связанных моделях",
                dllName: "AbimToolsMine.dll"
            );
            hideAxes_button.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/Osi16.png"));

            // Кнопка "Локальная копия модели" (создание резервной копии в C:\a и архивной в C:\a\архив)
            PushButton localBackupBtn = CreateButton(
                panel: pan1,
                name: "MakeLocalTask",
                text: "Выдать\nзадание",
                command: "AbimToolsMine.MakeLocalTask",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/MakeLocalTask32.png",
                toolTip: "Синхронизация/сохранение и копии файла для задания смежным разделам)",
                dllName: "AbimToolsMine.dll"
            );
            localBackupBtn.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/MakeLocalTask16.png"));
            
            var pan2 = RibbonPanel(application, "АБИМ-ПРО", "Отделка и полы");
            
            // Кнопка "Cоздание ведомости отделки"
            PushButton Pref_ScheduleFinishing = CreateButton(
                panel: pan2,
                name: "Pref_ScheduleFinishing",
                text: "Параметры",
                command: "AbimToolsMine.Pref_ScheduleFinishingWindow",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/Pref_Finishing32.png",
                toolTip: "Настройки создания ведомости отделки",
                dllName: "AbimToolsMine.dll"
            );
            Pref_ScheduleFinishing.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/Pref_Finishing32.png"));

            // Кнопка "Cоздание ведомости отделки"
            PushButton ScheduleFinishing = CreateButton(
                panel: pan2,
                name: "ScheduleFinishing",
                text: "Ведомость отделки",
                command: "AbimToolsMine.ScheduleFinishing",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/Finishing32.png",
                toolTip: "Создание ведомости отделки по ГОСТ",
                dllName: "AbimToolsMine.dll"
            );
            ScheduleFinishing.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/Finishing32.png"));
           
            // Кнопка "Cоздание ведомости отделки"
            PushButton FloorLegends = CreateButton(
                panel: pan2,
                name: "FloorLegends",
                text: "Полы\nСоздание эскизов",
                command: "AbimToolsMine.FloorLegends",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/FloorLegends32.png",
                toolTip: "Создание эскизов для каждого типа пола. (типы полов создаются чертёжными видами)",
                dllName: "AbimToolsMine.dll"
            );
            ScheduleFinishing.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/FloorLegends16.png"));

            // Кнопка "Cоздание ведомости отделки"
            PushButton LegendsToParameters = CreateButton(
                panel: pan2,
                name: "LegendsToParameter",
                text: "Полы\nЭскизы в параметр",
                command: "AbimToolsMine.LegendsToParameter",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/LegendsToParameter32.png",
                toolTip: "Эскизы переходят в параметр типа пола",
                dllName: "AbimToolsMine.dll"
            );
            ScheduleFinishing.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/LegendsToParameter16.png"));

            // Кнопка "Пермычки"
            /*PushButton LintelsPlacing = CreateButton(
                panel: pan2,
                name: "LintelsPlacing",
                text: "Полы\nЭскизы в параметр",
                command: "AbimToolsMine.LintelsPlacing",
                imageUri: "pack://application:,,,/AbimToolsMine;component/Resources/LegendsToParameter32.png",
                toolTip: "Эскизы переходят в параметр типа пола",
                dllName: "AbimToolsMine.dll"
            );
            LintelsPlacing.Image = new BitmapImage(new Uri("pack://application:,,,/AbimToolsMine;component/Resources/LegendsToParameter16.png"));*/
        }

        public Result OnStartup(UIControlledApplication application)
        {
            _application = application;
            OnButtonCreate(application);
            application.ControlledApplication.DocumentOpened += OnDocumentOpened;
            // Поставь отложенное применение:
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

            // Пробуем получить панели из вкладки
            var panels = app.GetRibbonPanels("АБИМ-ПРО");

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

            var panels = _application.GetRibbonPanels("АБИМ-ПРО");
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
                    // Восстанавливаем видимость по настройкам
                    panel.Visible = !hiddenPanels.Contains(panel.Name);
                }
                else
                {
                    // Скрываем всё кроме "Плагин"
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