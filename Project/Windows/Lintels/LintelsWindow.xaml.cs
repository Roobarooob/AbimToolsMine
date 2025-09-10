using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AbimToolsMine
{
    [Transaction(TransactionMode.Manual)]
    public class LintelsPlacing : IExternalCommand
    {
        public static Document l_doc = null;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;
            l_doc = doc;
            Lintels window = new Lintels(uiapp);
            window.ShowDialog();
            return Result.Succeeded;
        }
    }
    
    public partial class Lintels : Window
    {
        public ObservableCollection<LintelOpening> Openings { get; set; }

        public Lintels(UIApplication uiapp)
        {
            InitializeComponent();
            DataContext = this;

            Openings = new ObservableCollection<LintelOpening>(CollectOpenings(uiapp.ActiveUIDocument.Document));
        }

        private List<LintelOpening> CollectOpenings(Document doc)
        {
            var openings = new List<LintelOpening>();

            var categories = new[] { BuiltInCategory.OST_Doors, BuiltInCategory.OST_Windows };

            foreach (var cat in categories)
            {
                var elements = new FilteredElementCollector(doc)
                    .OfCategory(cat)
                    .WhereElementIsNotElementType()
                    .ToElements();

                foreach (FamilyInstance el in elements)
                {
                    string materialType = GetStringParam(doc.GetElement(el.Host.GetTypeId()), "ПРО_Код основы");
                    if (materialType != "КРП" && materialType != "ГБ" && materialType != "ПГП")
                        continue;

                    double? width = GetDoubleParam(el, "ПРО_Проем_Ширина");
                    if (width == null) continue;

                    var host = doc.GetElement(el.get_Parameter(BuiltInParameter.HOST_ID_PARAM)?.AsElementId());
                    double? wallThickness = null;
                    if (host is Wall wall)
                    {
                        wallThickness = wall.Width;
                    }

                    if (wallThickness == null) continue;

                    openings.Add(new LintelOpening
                    {
                        Width = Math.Round(width.Value * 304.8, 1), // перевод из футов в мм
                        WallThickness = Math.Round(wallThickness.Value * 304.8, 1),
                        MaterialType = materialType
                    });
                }
            }

            // Группировка по уникальным комбинациям
            var distinct = openings
                .GroupBy(o => new { o.Width, o.WallThickness, o.MaterialType })
                .Select(g => g.First())
                .ToList();

            return distinct;
        }

        private double? GetDoubleParam(Element el, string paramName)
        {
            var param = el.LookupParameter(paramName);
            return param != null && param.StorageType == StorageType.Double
                ? param.AsDouble()
                : (double?)null;
        }

        private string GetStringParam(Element el, string paramName)
        {
            var param = el.LookupParameter(paramName);
            return param?.AsString();
        }
        
        private void SelectLintel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is LintelOpening selectedOpening)
            {
                // Передаем текущие выбранные элементы в picker
                var currentElements = selectedOpening.Config?.BrusElements ?? new List<string>();
                var picker = new LintelPickerWindow(currentElements);
                
                if (picker.ShowDialog() == true)
                {
                    selectedOpening.Config = picker.SelectedConfig;
                    OpeningsGrid.Items.Refresh(); // обновить таблицу
                }
            }
        }

        private void PlaceButton_Click(object sender, RoutedEventArgs e)
        {
            Document doc = LintelsPlacing.l_doc;

            using (Transaction t = new Transaction(doc, "Расстановка перемычек"))
            {
                t.Start();

                foreach (var opening in Openings)
                {
                    if (opening.Config == null)
                        continue;

                    var symbol = GetFamilySymbol(doc, opening.Config.LintelType);
                    if (symbol == null)
                    {
                        MessageBox.Show($"Семейство для {opening.Config.LintelType} перемычки не найдено.");
                        continue;
                    }

                    if (!symbol.IsActive) symbol.Activate();

                    var categories = new[] { BuiltInCategory.OST_Doors, BuiltInCategory.OST_Windows };

                    foreach (var cat in categories)
                    {
                        var elements = new FilteredElementCollector(doc)
                            .OfCategory(cat)
                            .WhereElementIsNotElementType()
                            .Cast<FamilyInstance>()
                            .Where(el => MatchesOpening(el, opening, doc))
                            .ToList();

                        foreach (var el in elements)
                        {
                            PlaceLintelForOpening(doc, el, opening, symbol);
                        }
                    }
                }

                t.Commit();
            }

            MessageBox.Show("Перемычки расставлены.");
        }

        private FamilySymbol GetFamilySymbol(Document doc, string lintelType)
        {
            string familyName = "";
            string typeName = "";

            switch (lintelType)
            {
                case "Брусковая":
                    familyName = "ПРО_Обобщенная модель_Перемычка составная";
                    typeName = "Перемычка составная";
                    break;
                case "Уголковая":
                    familyName = "ПРО_Обобщенная модель_Перемычка из уголков";
                    typeName = "Перемычка из уголков";
                    break;
                case "Арматурная":
                    familyName = "ПРО_Обобщенная модель_Арматурная перемычка";
                    typeName = "Арматурная перемычка";
                    break;
                default:
                    return null;
            }

            return new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(fs => fs.Family.Name == familyName && fs.Name == typeName);
        }

        private bool MatchesOpening(FamilyInstance el, LintelOpening opening, Document doc)
        {
            string mat = GetStringParam(doc.GetElement(el.Host.GetTypeId()), "ПРО_Код основы");
            if (mat != opening.MaterialType) return false;

            double? width = GetDoubleParam(el, "ПРО_Проем_Ширина");
            if (width == null) return false;

            double wmm = Math.Round(width.Value * 304.8, 1);
            if (Math.Abs(wmm - opening.Width) > 1.0) return false;

            if (el.Host is Wall wall)
            {
                double tmm = Math.Round(wall.Width * 304.8, 1);
                if (Math.Abs(tmm - opening.WallThickness) > 1.0) return false;
                return true;
            }

            return false;
        }

        private void PlaceLintelForOpening(Document doc, FamilyInstance el, LintelOpening opening, FamilySymbol symbol)
        {
            LocationPoint loc = el.Location as LocationPoint;
            if (loc == null) return;

            double? height = GetDoubleParam(el, "ПРО_Проем_Высота");
            if (height == null) return;

            XYZ insertPoint = new XYZ(loc.Point.X, loc.Point.Y, loc.Point.Z + height.Value);

            var instance = doc.Create.NewFamilyInstance(
                insertPoint,
                symbol,
                el.Host,
                StructuralType.NonStructural);

            // Записываем параметры в зависимости от типа перемычки
            SetLintelParameters(instance, opening.Config);
        }

        private void SetLintelParameters(FamilyInstance instance, LintelConfig config)
        {
            switch (config.LintelType)
            {
                case "Брусковая":
                    SetBruskParameters(instance, config);
                    break;
                case "Уголковая":
                    SetUgolkParameters(instance, config);
                    break;
                case "Арматурная":
                    SetArmaturaParameters(instance, config);
                    break;
            }
        }

        private void SetBruskParameters(FamilyInstance instance, LintelConfig config)
        {
            if (config.BrusElements == null) return;

            var parameters = new[] { "Тип 1-го элемента", "Тип 2-го элемента", "Тип 3-го элемента", 
                                   "Тип 4-го элемента", "Тип 5-го элемента", "Тип 6-го элемента" };

            for (int i = 0; i < config.BrusElements.Count && i < parameters.Length; i++)
            {
                if (string.IsNullOrEmpty(config.BrusElements[i])) continue;

                var param = instance.LookupParameter(parameters[i]);
                if (param != null && param.StorageType == StorageType.String)
                {
                    param.Set(config.BrusElements[i]);
                }
            }
        }

        private void SetUgolkParameters(FamilyInstance instance, LintelConfig config)
        {
            if (config.UgolkConfig == null) return;

            var stepParam = instance.LookupParameter("Полоса_Шаг");
            if (stepParam != null && double.TryParse(config.UgolkConfig.Step, out double step))
            {
                stepParam.Set(step);
            }

            var offsetParam = instance.LookupParameter("Полоса_Отступ");
            if (offsetParam != null && double.TryParse(config.UgolkConfig.Offset, out double offset))
            {
                offsetParam.Set(offset);
            }

            var ugolkParam = instance.LookupParameter("Внутренний уголок");
            if (ugolkParam != null && ugolkParam.StorageType == StorageType.String)
            {
                ugolkParam.Set(config.UgolkConfig.UgolkType);
            }

            var stripParam = instance.LookupParameter("Полоса");
            if (stripParam != null && stripParam.StorageType == StorageType.String)
            {
                stripParam.Set(config.UgolkConfig.StripType);
            }
        }

        private void SetArmaturaParameters(FamilyInstance instance, LintelConfig config)
        {
            if (config.ArmaturaConfig == null) return;

            var countParam = instance.LookupParameter("Арм_N");
            if (countParam != null && countParam.StorageType == StorageType.Integer)
            {
                countParam.Set(config.ArmaturaConfig.Count);
            }

            var diameterParam = instance.LookupParameter("Арм_Диаметр");
            if (diameterParam != null && diameterParam.StorageType == StorageType.Double)
            {
                diameterParam.Set(config.ArmaturaConfig.Diameter);
            }

            var classParam = instance.LookupParameter("Арм_Класс");
            if (classParam != null && classParam.StorageType == StorageType.String)
            {
                classParam.Set(config.ArmaturaConfig.ClassName);
            }
        }
    }

    public class LintelOpening
    {
        public double Width { get; set; }
        public double WallThickness { get; set; }
        public string MaterialType { get; set; }
        public LintelConfig Config { get; set; }

        // Для обратной совместимости
        public List<string> SelectedElements 
        {
            get => Config?.BrusElements ?? new List<string>();
            set 
            {
                if (Config == null)
                    Config = new LintelConfig { LintelType = "Брусковая", BrusElements = new List<string>() };
                Config.BrusElements = value;
            }
        }

        // Удобное представление для кнопки
        public string SelectedElementsText
        {
            get
            {
                if (Config == null) return "Выбрать";

                switch (Config.LintelType)
                {
                    case "Брусковая":
                        var elements = Config.BrusElements?.Where(x => !string.IsNullOrEmpty(x)).ToList();
                        return elements?.Any() == true ?
                            "|" + string.Join("|", elements) + "|" :
                            "Выбрать";

                    case "Уголковая":
                        return Config.UgolkConfig != null ?
                            $"Уголковая: {Config.UgolkConfig.UgolkType}" :
                            "Выбрать";

                    case "Арматурная":
                        return Config.ArmaturaConfig != null ?
                            $"Арматурная: {Config.ArmaturaConfig.Count}ø{Config.ArmaturaConfig.Diameter}" :
                            "Выбрать";

                    default:
                        return "Выбрать";
                }
            }
        }
    }
}