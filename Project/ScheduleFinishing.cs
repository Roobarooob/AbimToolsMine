using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Settings = AbimToolsMine.Properties.Settings;



namespace AbimToolsMine
{
    [Transaction(TransactionMode.Manual)]
    public class ScheduleFinishing : IExternalCommand
    {
        private static string RoomNumberParam => Settings.Default.RoomNumberParam;
        private static string RoomGroupParam => Settings.Default.RoomGroupParam;
        private static string RoomKeyParam => Settings.Default.RoomKeyParam;
        private static string PlinthString => Settings.Default.PlinthString;
        private static string FlWallString => Settings.Default.FlWallString;
        private static string StructureComp => Settings.Default.StructureComp;
        private static string DimType => Settings.Default.DimType;
        private static bool NeedFloor => Settings.Default.NeedFloor;

        private static readonly Dictionary<string, (string nameParam, string valueParam)> RoomParams = new Dictionary<string, (string, string)>
        {
            { "wall", (Settings.Default.WallNameParam, Settings.Default.WallValueParam) },
            { "floor", (Settings.Default.FloorNameParam, Settings.Default.FloorValueParam) },
            { "ceiling", (Settings.Default.CeilingNameParam, Settings.Default.CeilingValueParam) },
            { "plinth", (Settings.Default.PlinthNameParam, Settings.Default.PlinthValueParam) }
        };

        private const int StructureColWidthMM = 50;
        private const int Divide = 2;
        private static int StructureColChars => (int)(StructureColWidthMM / 1.65);

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            var roomData = new Dictionary<string, Dictionary<string, Dictionary<string, double>>>();
            var rooms = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().ToElements();

            ProcessElements(doc, BuiltInCategory.OST_Walls, "wall", roomData);
            ProcessElements(doc, BuiltInCategory.OST_Floors, "floor", roomData);
            ProcessElements(doc, BuiltInCategory.OST_Ceilings, "ceiling", roomData);
            ProcessElements(doc, BuiltInCategory.OST_Walls, "plinth", roomData);

            var groupingDict = new Dictionary<string, List<string>>();
            var groupAreas = new Dictionary<string, Dictionary<string, Dictionary<string, double>>>();

            foreach (var room in rooms)
            {
                var roomNum = GetParamValue(room, RoomNumberParam);
                if (string.IsNullOrEmpty(roomNum) || !roomData.ContainsKey(roomNum))
                    continue;

                var data = roomData[roomNum];
                string groupKey = GetGroupKey(data);

                if (!groupingDict.ContainsKey(groupKey))
                {
                    groupingDict[groupKey] = new List<string>();
                    groupAreas[groupKey] = InitGroupArea();
                }

                groupingDict[groupKey].Add(roomNum);

                foreach (var part in data)
                {
                    if (!NeedFloor && part.Key == Settings.Default.FloorNameParam)
                        continue;

                    foreach (var entry in part.Value)
                    {
                        if (!groupAreas[groupKey][part.Key].ContainsKey(entry.Key))
                            groupAreas[groupKey][part.Key][entry.Key] = 0;
                        groupAreas[groupKey][part.Key][entry.Key] += entry.Value;
                    }
                }
            }

            using (Transaction trans = new Transaction(doc, "Обновление отделки помещений"))
            {
                trans.Start();
                foreach (var room in rooms)
                {
                    // --- 1. Очистка параметров ---
                    var groupParam = room.LookupParameter(RoomGroupParam);
                    if (groupParam != null && groupParam.StorageType == StorageType.String)
                        groupParam.Set(string.Empty);

                    foreach (var kv in RoomParams)
                    {
                        var nameParam = room.LookupParameter(kv.Value.nameParam);
                        if (nameParam != null && nameParam.StorageType == StorageType.String)
                            nameParam.Set(string.Empty);

                        var valueParam = room.LookupParameter(kv.Value.valueParam);
                        if (valueParam != null && valueParam.StorageType == StorageType.String)
                            valueParam.Set(string.Empty);

                        var groupValueParam = room.LookupParameter(kv.Value.valueParam + ".Гр");
                        if (groupValueParam != null && groupValueParam.StorageType == StorageType.String)
                            groupValueParam.Set(string.Empty);
                    }

                    var roomNum = GetParamValue(room, RoomNumberParam);
                    if (string.IsNullOrEmpty(roomNum) || !roomData.ContainsKey(roomNum))
                        continue;

                    var data = roomData[roomNum];
                    var groupKey = GetGroupKey(data);

                    if (room.LookupParameter(RoomGroupParam)?.StorageType == StorageType.String)
                    {
                        room.LookupParameter(RoomGroupParam)?.Set(string.Join(", ", groupingDict[groupKey].OrderBy(s => s)));
                    }

                    foreach (var kv in RoomParams)
                    {
                        var nameParam = kv.Value.nameParam;
                        var valueParam = kv.Value.valueParam;

                        if (!NeedFloor && nameParam == Settings.Default.FloorNameParam)
                            continue;

                        if (!data.ContainsKey(nameParam))
                            continue;

                        var finishDict = data[nameParam];
                        var lines = new List<string>();
                        var values = new List<string>();
                        var groupValues = new List<string>();
                        var entries = finishDict.OrderBy(e => e.Key).ToList();  // Преобразуем в список для индексации
                        for (int i = 0; i < entries.Count; i++)
                        {
                            var pair = entries[i];
                            var finishLines = new List<string>();
                            var preSplit = pair.Key.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

                            foreach (var line in preSplit)
                            {
                                var sublines = SplitText(line, StructureColChars);
                                finishLines.AddRange(sublines);
                            }

                            int count = finishLines.Count;
                            int before = (count / 2);
                            if (before < 0) before = 0;
                            int after = count - before - 1;
                            if (after < 0) after = 0;

                            lines.AddRange(finishLines);

                            // значение
                            values.AddRange(Enumerable.Repeat("ㅤ", before));
                            values.Add(pair.Value.ToString("0.##").Replace('.', ','));
                            values.AddRange(Enumerable.Repeat("ㅤ", after));

                            // групповое значение
                            double groupVal = 0;
                            if (groupAreas[groupKey][nameParam].TryGetValue(pair.Key, out double val))
                                groupVal = val;

                            groupValues.AddRange(Enumerable.Repeat("ㅤ", before));
                            groupValues.Add(groupVal.ToString("0.##").Replace('.', ','));
                            groupValues.AddRange(Enumerable.Repeat("ㅤ", after));

                            // отступы — только если не последний элемент
                            if (i < entries.Count - 1)
                            {
                                lines.AddRange(Enumerable.Repeat("", Divide));
                                values.AddRange(Enumerable.Repeat("", Divide));
                                groupValues.AddRange(Enumerable.Repeat("", Divide));
                            }
                        }

                        room.LookupParameter(nameParam)?.Set(string.Join("\n", lines));
                        room.LookupParameter(valueParam)?.Set(string.Join("\n", values));
                        room.LookupParameter(valueParam + ".Гр")?.Set(string.Join("\n", groupValues));
                    }
                }
                trans.Commit();
            }

            //TaskDialog.Show("Результат", "Готово");
            return Result.Succeeded;
        }

        private void ProcessElements(Document doc, BuiltInCategory category, string typeKey, Dictionary<string, Dictionary<string, Dictionary<string, double>>> roomData)
        {
            var elements = new FilteredElementCollector(doc).OfCategory(category).WhereElementIsNotElementType().ToElements();
            foreach (var el in elements)
            {
                var type = doc.GetElement(el.GetTypeId());
                var typeName = type.Name ?? "";

                // Обработка плинтусов из стен
                if (category == BuiltInCategory.OST_Walls)
                {
                    bool isPlinth = typeName.Contains(PlinthString);
                    if (isPlinth && typeKey != "plinth") continue;
                    if (!isPlinth && typeKey == "plinth") continue;
                }

                if (category == BuiltInCategory.OST_Floors && typeName.Contains(FlWallString))
                {
                    typeKey = "wall";
                }

                var roomKey = GetParamValue(el, RoomKeyParam);
                if (string.IsNullOrEmpty(roomKey)) continue;

                var name = GetParamValue(type, StructureComp) ?? GetParamValue(type, "Тип") ?? "Без имени";

                double value = 0;

                if (typeKey == "plinth")
                {
                    // Получаем значение пользовательского параметра
                    var unitTypeParam = type.LookupParameter(DimType);
                    int unitType = (unitTypeParam != null && unitTypeParam.HasValue) ? unitTypeParam.AsInteger() : 0;

                    if (unitType == 1) // по длине (площадь / высота)
                    {
                        var areaParam = el.LookupParameter("Площадь");
                        var heightParam = el.LookupParameter("Неприсоединенная высота") ?? el.LookupParameter("Unconnected Height");
                        if (areaParam != null && areaParam.HasValue && heightParam != null && heightParam.HasValue)
                        {
                            double area = areaParam.AsDouble(); // ft²
                            double height = heightParam.AsDouble(); // ft
                            if (height > 0)
                                value = Math.Round((area / height) * 0.3048, 2); // длина в метрах
                        }
                    }
                    else if (unitType == 2) // по площади
                    {
                        value = GetArea(el);
                    }
                }
                else
                {
                    value = (typeKey == "plinth") ? GetLength(el) : GetArea(el);
                }

                if (!roomData.ContainsKey(roomKey))
                    roomData[roomKey] = InitGroupArea();

                var targetKey = RoomParams[typeKey].Item1;
                if (!roomData[roomKey][targetKey].ContainsKey(name))
                    roomData[roomKey][targetKey][name] = 0;

                roomData[roomKey][targetKey][name] += value;
            }
        }

        private Dictionary<string, Dictionary<string, double>> InitGroupArea()
        {
            return RoomParams.Values.ToDictionary(p => p.Item1, _ => new Dictionary<string, double>());
        }

        private string GetParamValue(Element e, string name)
        {
            var p = e.LookupParameter(name);
            return (p != null && p.HasValue) ? p.AsString() : null;
        }

        private double GetArea(Element e)
        {
            var p = e.LookupParameter("Площадь");
            return (p != null && p.HasValue) ? Math.Round(p.AsDouble() * 0.092903, 2) : 0;
        }

        private double GetLength(Element e)
        {
            var p = e.LookupParameter("Длина");
            return (p != null && p.HasValue) ? Math.Round(p.AsDouble() * 0.3048, 2) : 0;
        }

        private string GetGroupKey(Dictionary<string, Dictionary<string, double>> data)
        {
            var keys = RoomParams.Values
                .Where(p => NeedFloor || p.nameParam != Settings.Default.FloorNameParam)
                .SelectMany(p => data.ContainsKey(p.nameParam) ? data[p.nameParam].Keys.OrderBy(x => x) : Enumerable.Empty<string>())
                .ToList();
            return string.Join("|", keys);
        }

        private List<string> SplitText(string text, int maxChars)
        {
            var words = text.Split(' ');
            var lines = new List<string>();
            string line = "";

            foreach (var word in words)
            {
                if ((line + " " + word).Trim().Length <= maxChars)
                {
                    line = (line + " " + word).Trim();
                }
                else
                {
                    lines.Add(line);
                    line = word;
                }
            }
            if (!string.IsNullOrEmpty(line))
                lines.Add(line);

            return lines;
        }
    }
}
