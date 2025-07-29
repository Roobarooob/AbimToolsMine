using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Shapes;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;
using View = Autodesk.Revit.DB.View;
using Settings = AbimToolsMine.Properties.Settings;

namespace AbimToolsMine
{
    [Transaction(TransactionMode.Manual)]
    public class FloorLegends : IExternalCommand
    {
        private static readonly string ParamName = Settings.Default.StructureComp;
        private static readonly string SymbolName = Settings.Default.FloorLayerName;
        private static readonly string viewTemplateName = Settings.Default.viewTemplateName;
        private static readonly Dictionary<string, int> LayerTypeToHatchType = new Dictionary<string, int>     
        {
            { "Железобетон", 1 },
            { "Плитка", 2 },
            { "Бетон", 3 },
            { "Сталь", 4 },
            { "Другое", 5 },
            { "Утеплитель", 6 },
            { "Стяжка", 7 },
            { "Пенополистерол", 8 },
            {"Гидроизоляция", 9 }
        };      
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Сбор всех типов полов
            var collector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Floors)
                .WhereElementIsNotElementType();

            HashSet<ElementId> typeIdsSeen = new HashSet<ElementId>();
            List<Element> elementTypes = new List<Element>();

            foreach (var el in collector)
            {
                try
                {
                    ElementId typeId = el.GetTypeId();
                    if (typeId != ElementId.InvalidElementId && !typeIdsSeen.Contains(typeId))
                    {
                        Element typeEl = doc.GetElement(typeId);
                        if (typeEl != null)
                        {
                            elementTypes.Add(typeEl);
                            typeIdsSeen.Add(typeId);
                        }
                    }
                }
                catch { }
            }

            List<(Element Type, List<LayerInfo> Layers)> parsedData = new List<(Element, List<LayerInfo>)>();
            int legendsCreated = 0;
            foreach (var typeEl in elementTypes)
            {
                Parameter param = typeEl.LookupParameter(ParamName);
                if (param == null || string.IsNullOrWhiteSpace(param.AsString()))
                {
                    continue;
                }

                string rawText = param.AsString();
                var result = new List<LayerInfo>();
                int position = 1;
                foreach (var line in rawText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed))
                        continue;

                    string namePart = trimmed;
                    string thicknessPart = "";

                    if (trimmed.Contains(" - "))
                    {
                        var parts = trimmed.Split(new[] { " - " }, StringSplitOptions.None);
                        namePart = string.Join(" - ", parts.Take(parts.Length - 1));
                        thicknessPart = parts.Last();
                    }

                    // Определение типа слоя
                    string lname = namePart.ToLower();
                    string layerType = lname.Contains("стяжка") ? "Стяжка" :
                                       lname.Contains("гидроизол") ? "Гидроизоляция" :
                                       lname.Contains("полист") ? "Пенополистерол" :
                                       lname.Contains("тепл") ? "Утеплитель" :
                                       lname.Contains("звук") ? "Утеплитель" :
                                       lname.Contains("керам") ? "Плитка" :
                                       lname.Contains("ж/б") || lname.Contains("железобетон") ? "Железобетон" :
                                       lname.Contains(" бетон") ? "Бетон" :
                                       lname.Contains("сталь") ? "Сталь" :
                                       "Другое";
                    if (layerType == null)
                    {
                        TaskDialog.Show("Отладка", "layerType == null");
                    }
                    int hatchType = (layerType != null && LayerTypeToHatchType.TryGetValue(layerType, out int code)) ? code : 0;
                    // Определение толщины
                    var matches = Regex.Matches(thicknessPart, @"\d+(\.\d+)?");
                    List<double> thicknessNumbers = matches.Cast<Match>()
                                                           .Select(m => double.Parse(m.Value))
                                                           .ToList();

                    double thickness = 0;
                    bool isVariable = false;

                    if (thicknessNumbers.Count == 2)
                    {
                        thickness = (thicknessNumbers[0] + thicknessNumbers[1]) / 2;
                        isVariable = true;
                    }
                    else if (thicknessNumbers.Count == 1)
                    {
                        thickness = thicknessNumbers[0];
                    }
                    else
                    {
                        // Подставляем значение по типу слоя, если нет чисел
                        if (layerType == "Железобетон")
                            thickness = 50;
                        else
                            thickness = 1;
                    }
                    if (layerType == "Гидроизоляция")
                        thickness = 10;
                    result.Add(new LayerInfo
                    {
                        Name = namePart,
                        LayerType = layerType,
                        Thickness = thickness,
                        IsVariable = isVariable,
                        HatchType = hatchType,
                        Position = position
                    });
                    position += 1;
                }

                parsedData.Add((typeEl, result));
            }

            using (Transaction t = new Transaction(doc, "Создание пирогов для полов"))
            {
                t.Start();

                int legendIndex = 1;
               

                foreach (var (typeEl, layers) in parsedData)
                {
                    string legendName = $"Пирог - {typeEl.Name}";

                    // Проверка: существует ли уже легенда с таким именем?
                    bool legendExists = new FilteredElementCollector(doc)
                        .OfClass(typeof(ViewDrafting))
                        .Cast<ViewDrafting>()
                        .Any(v => v.Name.Equals(legendName, StringComparison.OrdinalIgnoreCase));

                    if (legendExists)
                    {
                        // Пропускаем создание, если вид уже есть
                        continue;
                    }
                    ViewFamilyType legendType = new FilteredElementCollector(doc)
                        .OfClass(typeof(ViewFamilyType))
                        .Cast<ViewFamilyType>()
                        .FirstOrDefault(l => l.ViewFamily == ViewFamily.Drafting);

                    if (legendType == null)
                    {
                        TaskDialog.Show("Ошибка", "Не найден тип вида 'Чертежный вид'");
                        continue;
                    }
                    Autodesk.Revit.DB.View legendView = null;
                    try
                    {
                        legendView = ViewDrafting.Create(doc, legendType.Id);
                        legendView.get_Parameter(BuiltInParameter.VIEW_SCALE_PULLDOWN_METRIC).Set(20);
                        legendView.Name = legendName;

                            // Поиск шаблона вида по имени
                            View viewTemplate = new FilteredElementCollector(doc)
                                .OfClass(typeof(View))
                                .Cast<View>()
                                .FirstOrDefault(v => v.IsTemplate && v.Name.Equals(viewTemplateName));

                            if (viewTemplate != null)
                            {
                                legendView.ViewTemplateId = viewTemplate.Id;
                            }
                    }
                    catch
                    {
                        continue;
                    }
                    double totalThickness = layers.Sum(x => x.Thickness);
                    int totalCount = layers.Count;
                    double y = 0;
                    double tag_h = 100 / 304.8;
                    double tag_offset = (totalCount) * tag_h;
                    //double tag_y = totalThickness/304.8+(totalCount-1)*tag_h;



                    FamilySymbol familySymbol = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_DetailComponents)  // Категория "Обобщенные модели"
                        .WhereElementIsElementType()                   // Выбираем только типоразмеры (ElementType)
                        .FirstOrDefault(e => e.Name == SymbolName) as FamilySymbol;

                    if (familySymbol == null)
                    {
                        TaskDialog.Show("Ошибка", $"Не найден тип слоя {SymbolName}");
                        continue;
                    }

                    foreach (var layer in layers)
                    {

                        XYZ position = new XYZ(0, y, 0);
                        familySymbol.Activate();
                        FamilyInstance layerElement = doc.Create.NewFamilyInstance(position, familySymbol,legendView);
                        layerElement.LookupParameter("Ширина").Set(layer.Thickness/304.8);
                        layerElement.LookupParameter("Тип штриховки").Set(layer.HatchType);
                        layerElement.LookupParameter("Отступ").Set(tag_offset);
                        layerElement.LookupParameter("Номер слоя").Set(layer.Position.ToString());
                        layerElement.LookupParameter("Комментарии").Set($"{layer.Name} - {layer.Thickness.ToString()}");
                        // смещение вниз на толщину слоя
                        double offset = layer.Thickness / 304.8;
                        y -= offset;
                        tag_offset -= tag_h - offset;
                        
                    }                   

                    legendIndex++;
                    legendsCreated++;
                }               

                t.Commit();
            }

            TaskDialog.Show("Готово", $"Создано легенд: {legendsCreated}");
            return Result.Succeeded;
        }



        public class LayerInfo
        {
            public string Name { get; set; }
            public string LayerType { get; set; }
            public double Thickness { get; set; } // в мм
            public bool IsVariable { get; set; }
            public int HatchType { get; set; }
            public int Position { get; set; }
        }
    }
}
