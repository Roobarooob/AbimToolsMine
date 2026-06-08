using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private static bool SplitByParam => Settings.Default.SplitByParam;
        private static string SplitParamName => Settings.Default.SplitParamName;

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

        private static string MakeRoomDataKey(string roomNum, string splitValue)
        {
            if (!SplitByParam || string.IsNullOrEmpty(splitValue))
                return roomNum;
            return roomNum + "|__split__|" + splitValue;
        }

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

            var groupingDict = new Dictionary<string, HashSet<string>>();
            var groupAreas = new Dictionary<string, Dictionary<string, Dictionary<string, double>>>();
            var processedRoomsPerGroup = new Dictionary<string, HashSet<string>>(); // roomNum already counted per group

            foreach (var room in rooms)
            {
                var roomNum = GetParamValue(room, RoomNumberParam);
                if (string.IsNullOrEmpty(roomNum))
                    continue;

                string splitValue = SplitByParam ? (GetParamValue(room, SplitParamName) ?? "") : "";
                string roomDataKey = MakeRoomDataKey(roomNum, splitValue);

                if (!roomData.ContainsKey(roomDataKey))
                    continue;

                var data = roomData[roomDataKey];
                string groupKey = GetGroupKey(data, splitValue);

                if (!groupingDict.ContainsKey(groupKey))
                {
                    groupingDict[groupKey] = new HashSet<string>();
                    groupAreas[groupKey] = InitGroupArea();
                    processedRoomsPerGroup[groupKey] = new HashSet<string>();
                }

                groupingDict[groupKey].Add(roomNum);

                // Добавляем площади только если этот roomNum ещё не был обработан для данной группы
                if (!processedRoomsPerGroup[groupKey].Contains(roomDataKey))
                {
                    processedRoomsPerGroup[groupKey].Add(roomDataKey);

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
                    if (string.IsNullOrEmpty(roomNum))
                        continue;

                    string splitValue = SplitByParam ? (GetParamValue(room, SplitParamName) ?? "") : "";
                    string roomDataKey = MakeRoomDataKey(roomNum, splitValue);

                    if (!roomData.ContainsKey(roomDataKey))
                        continue;

                    var data = roomData[roomDataKey];
                    var groupKey = GetGroupKey(data, splitValue);

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

                // Фильтр стен: пропускать все, кроме FlWallString и PlinthString
                if (category == BuiltInCategory.OST_Walls)
                {
                    bool isPlinth = typeName.Contains(PlinthString);
                    if (isPlinth && typeKey != "plinth") continue;
                    if (!isPlinth && typeKey == "plinth") continue;
                }

                string currentTypeKey = typeKey;
                if (category == BuiltInCategory.OST_Floors && typeName.Contains(FlWallString))
                {
                    currentTypeKey = "wall";
                }

                var roomKey = GetParamValue(el, RoomKeyParam);
                if (string.IsNullOrEmpty(roomKey)) continue;

                var name =
                    GetParamValue(type, StructureComp)
                    ?? GetParamValue(type, "Тип");

                if (string.IsNullOrEmpty(name))
                    continue;

                double value = 0;

                if (currentTypeKey == "plinth")
                {
                    var unitTypeParam = type.LookupParameter(DimType);
                    int unitType = (unitTypeParam != null && unitTypeParam.HasValue) ? unitTypeParam.AsInteger() : 0;

                    if (unitType == 1)
                    {
                        var areaParam = el.LookupParameter("Площадь");
                        var heightParam = el.LookupParameter("Неприсоединенная высота") ?? el.LookupParameter("Unconnected Height");
                        if (areaParam != null && areaParam.HasValue && heightParam != null && heightParam.HasValue)
                        {
                            double area = areaParam.AsDouble();
                            double height = heightParam.AsDouble();
                            if (height > 0)
                                value = Math.Round((area / height) * 0.3048, 2);
                        }
                    }
                    else if (unitType == 2)
                    {
                        value = GetArea(el);
                    }
                }
                else if (category == BuiltInCategory.OST_Floors && type.LookupParameter("Комментарии к типоразмеру").AsString() == "Отделка ступеней")
                {
                    value = GetFloorArea(el);
                }
                else
                {
                    value = (currentTypeKey == "plinth") ? GetLength(el) : GetArea(el);
                }

                // Read split parameter from the element to match it to the correct room
                string splitValue = SplitByParam ? (GetParamValue(el, SplitParamName) ?? "") : "";
                string roomDataKey = MakeRoomDataKey(roomKey, splitValue);

                if (!roomData.ContainsKey(roomDataKey))
                    roomData[roomDataKey] = InitGroupArea();

                var targetKey = RoomParams[currentTypeKey].Item1;
                if (!roomData[roomDataKey][targetKey].ContainsKey(name))
                    roomData[roomDataKey][targetKey][name] = 0;

                roomData[roomDataKey][targetKey][name] += value;
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
            return ReadAreaInSquareMeters(e);
        }

        private double GetFloorArea(Element e)
        {
            return ReadAreaInSquareMeters(e);
        }

        private double ReadAreaInSquareMeters(Element e)
        {
            if (TryReadAreaParameter(e.LookupParameter("ПРО_Площадь"), out double area))
                return area;

            if (TryReadAreaParameter(e.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED), out area))
                return area;

            if (TryReadAreaParameter(e.LookupParameter("Площадь"), out area))
                return area;

            return 0;
        }

        private bool TryReadAreaParameter(Parameter parameter, out double area)
        {
            area = 0;

            if (parameter == null || !parameter.HasValue)
                return false;

            if (parameter.StorageType == StorageType.Double)
            {
                area = Math.Round(parameter.AsDouble() * 0.092903, 2);
                return true;
            }

            if (parameter.StorageType == StorageType.String)
            {
                string raw = parameter.AsString();
                if (string.IsNullOrWhiteSpace(raw))
                    return false;

                if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out area) ||
                    double.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out area))
                {
                    area = Math.Round(area, 2);
                    return true;
                }
            }

            return false;
        }

        private double GetLength(Element e)
        {
            var p = e.LookupParameter("Длина");
            return (p != null && p.HasValue) ? Math.Round(p.AsDouble() * 0.3048, 2) : 0;
        }

        private string GetGroupKey(Dictionary<string, Dictionary<string, double>> data, string splitValue = "")
        {
            var keys = RoomParams.Values
                .Where(p => NeedFloor || p.nameParam != Settings.Default.FloorNameParam)
                .SelectMany(p => data.ContainsKey(p.nameParam) ? data[p.nameParam].Keys.OrderBy(x => x) : Enumerable.Empty<string>())
                .ToList();

            // When split mode is active, include the split value so groups are separated
            if (SplitByParam && !string.IsNullOrEmpty(splitValue))
                keys.Add("__split__:" + splitValue);

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
