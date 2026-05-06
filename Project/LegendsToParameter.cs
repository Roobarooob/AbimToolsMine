using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Path = System.IO.Path;
using Settings = AbimToolsMine.Properties.Settings;
using View = Autodesk.Revit.DB.View;

namespace AbimToolsMine
{
    [Transaction(TransactionMode.Manual)]
    public class LegendsToParameter : IExternalCommand
    {
        private static readonly string RoomKey = Settings.Default.FloorRoomKeyParam;
        private static readonly string GroupKey = Settings.Default.ElementRoomGroupParam;
        private static readonly string EtageKey = Settings.Default.EtageParam;
        private static readonly string GroupEtageKey = Settings.Default.RoomGroupEtageParam;

        private const int MaxFileNameLength = 60;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            string exportFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
              "RvtFlrImg");
            Directory.CreateDirectory(exportFolder);

            var legendViews = new FilteredElementCollector(doc)
       .OfClass(typeof(View))
       .Cast<View>()
    .Where(v => v.ViewType == ViewType.DraftingView && v.Name.Contains("Пирог -"))
                .ToList();

            // Строим словарь RoomKey → Этаж один раз для всех категорий
            var roomKeyToEtage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(EtageKey) && !string.IsNullOrEmpty(GroupEtageKey))
            {
                var rooms = new FilteredElementCollector(doc)
              .OfCategory(BuiltInCategory.OST_Rooms)
          .WhereElementIsNotElementType()
          .ToList();

                foreach (var room in rooms)
                {
                    var rkParam = room.get_Parameter(BuiltInParameter.ROOM_NUMBER);
                    var etParam = room.LookupParameter(EtageKey);
                    if (rkParam == null || etParam == null) continue;
                    string rkVal = rkParam.AsString();
                    string etVal = etParam.AsString();
                    if (string.IsNullOrWhiteSpace(rkVal) || string.IsNullOrWhiteSpace(etVal)) continue;
                    if (!roomKeyToEtage.ContainsKey(rkVal))
                        roomKeyToEtage[rkVal] = etVal;
                }
            }

            using (Transaction t = new Transaction(doc, "Присвоение изображений и параметров групп"))
            {
                t.Start();

                // ── Полы: изображения + площадь (только если есть легенды) ──────────
                if (legendViews.Any())
                {
                    var fileNameToTypeName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var exportIds = new List<ElementId>();

                    for (int i = 0; i < legendViews.Count; i++)
                    {
                        View v = legendViews[i];
                        string shortName = $"floor_{i:D4}";
                        string legendPrefix = "Пирог - ";
                        string typeName = v.Name.Contains(legendPrefix)
                          ? v.Name.Substring(v.Name.IndexOf(legendPrefix, StringComparison.Ordinal) + legendPrefix.Length)
           : v.Name;
                        fileNameToTypeName[shortName] = typeName;
                        exportIds.Add(v.Id);
                    }

                    string tempPrefix = "fl";
                    ImageExportOptions options = new ImageExportOptions
                    {
                        ExportRange = ExportRange.SetOfViews,
                        FilePath = Path.Combine(exportFolder, tempPrefix),
                        FitDirection = FitDirectionType.Horizontal,
                        HLRandWFViewsFileType = ImageFileType.PNG,
                        ImageResolution = ImageResolution.DPI_300,
                        ZoomType = ZoomFitType.FitToPage,
                        PixelSize = 2048,
                        ShadowViewsFileType = ImageFileType.PNG
                    };
                    options.SetViewsAndSheets(exportIds);
                    doc.ExportImage(options);

                    string revitLegendInfix = " - Чертежный вид - Пирог - ";
                    foreach (var kv in fileNameToTypeName)
                    {
                        string shortName = kv.Key;
                        string typeName = kv.Value;
                        string normalizedTypeName = NormalizeTypeNameForFilename(typeName);
                        string expectedFilePath = Path.Combine(exportFolder, tempPrefix + revitLegendInfix + normalizedTypeName + ".png");
                        string shortFilePath = Path.Combine(exportFolder, shortName + ".png");

                        if (File.Exists(expectedFilePath))
                        {
                            if (File.Exists(shortFilePath)) File.Delete(shortFilePath);
                            File.Move(expectedFilePath, shortFilePath);
                        }
                    }

                    foreach (var kv in fileNameToTypeName)
                    {
                        string shortName = kv.Key;
                        string typeName = kv.Value;
                        string filePath = Path.Combine(exportFolder, shortName + ".png");
                        if (!File.Exists(filePath)) continue;

                        try
                        {
                            FloorType floorType = new FilteredElementCollector(doc)
                                   .OfClass(typeof(FloorType)).Cast<FloorType>()
                          .FirstOrDefault(f => f.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));

                            if (floorType == null) { File.Delete(filePath); continue; }

                            string imageNameInDoc = shortName + ".png";
                            ImageType existingImage = new FilteredElementCollector(doc)
                               .OfClass(typeof(ImageType)).Cast<ImageType>()
                 .FirstOrDefault(img => img.Name.Equals(imageNameInDoc, StringComparison.OrdinalIgnoreCase));

                            ImageType imageType;
#if R2020
     ImageTypeOptions imageOptions = new ImageTypeOptions(filePath, false);
#else
                        ImageTypeOptions imageOptions = new ImageTypeOptions(filePath, false, ImageTypeSource.Import);
#endif
                            if (existingImage != null) { existingImage.ReloadFrom(imageOptions); imageType = existingImage; }
                            else { imageType = ImageType.Create(doc, imageOptions); }

                            Parameter imgParam = floorType.LookupParameter("ПРО_Изображение типоразмера");
                            if (imgParam != null && imgParam.StorageType == StorageType.ElementId)
                                imgParam.Set(imageType.Id);

                            // Площадь
                            var instances = new FilteredElementCollector(doc)
                    .OfClass(typeof(Floor)).Cast<Floor>()
                .Where(f => f.GetTypeId() == floorType.Id).ToList();

                            foreach (var inst in instances)
                            {
                                Parameter sysArea = inst.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED);
                                if (sysArea == null) continue;
                                double areaInternal = sysArea.AsDouble();
                                Parameter targetArea = inst.LookupParameter("ПРО_Площадь");
                                if (targetArea == null) continue;
                                if (targetArea.StorageType == StorageType.Double)
                                    targetArea.Set(areaInternal);
                                else if (targetArea.StorageType == StorageType.String)
                                {
                                    double areaM2 = UnitUtils.ConvertFromInternalUnits(areaInternal, UnitTypeId.SquareMeters);
                                    targetArea.Set(Math.Round(areaM2, 2).ToString(CultureInfo.InvariantCulture));
                                }
                            }
                        }
                        finally { File.Delete(filePath); }
                    }
                }

                // ── Групповые параметры для Полов, Стен, Потолков (всегда) ──────────
                FillGroupParams(doc, BuiltInCategory.OST_Floors, roomKeyToEtage);
                FillGroupParams(doc, BuiltInCategory.OST_Walls, roomKeyToEtage);
                FillGroupParams(doc, BuiltInCategory.OST_Ceilings, roomKeyToEtage);

                t.Commit();
            }

            try
            {
                if (Directory.Exists(exportFolder) && !Directory.EnumerateFileSystemEntries(exportFolder).Any())
                    Directory.Delete(exportFolder);
            }
            catch { }

            TaskDialog.Show("Готово", "Параметры заполнены");
            return Result.Succeeded;
        }

        /// <summary>
        /// Заполняет GroupKey (все типы, весь проект) и GroupEtageKey (тип + этаж)
        /// для элементов указанной категории.
        /// </summary>
        private void FillGroupParams(Document doc,
  BuiltInCategory category,
       Dictionary<string, string> roomKeyToEtage)
        {
            // Собираем элементы у которых есть RoomKey и GroupKey
            var elements = new FilteredElementCollector(doc)
              .OfCategory(category)
                  .WhereElementIsNotElementType()
       .Cast<Element>()
              .Where(e =>
           {
               var rp = e.LookupParameter(RoomKey);
               var gp = e.LookupParameter(GroupKey);
               return rp != null && gp != null &&
           rp.StorageType == StorageType.String &&
             gp.StorageType == StorageType.String &&
               !gp.IsReadOnly;
           })
                      .ToList();

            if (!elements.Any()) return;

            // ── GroupKey: группируем по типоразмеру, пишем все RoomKey проекта ──────
            var byType = elements.GroupBy(e => doc.GetElement(e.GetTypeId()).Name).ToList();
            foreach (var group in byType)
            {
                var roomKeys = group
           .Select(e => e.LookupParameter(RoomKey)?.AsString())
                   .Where(v => !string.IsNullOrWhiteSpace(v))
                        .Distinct().OrderBy(v => v).ToList();

                string joinedKeys = string.Join(", ", roomKeys);
                foreach (var el in group)
                {
                    var p = el.LookupParameter(GroupKey);
                    if (p != null && !p.IsReadOnly && p.StorageType == StorageType.String)
                        p.Set(joinedKeys);
                }
            }

            // ── GroupEtageKey: группируем по (тип + этаж) ────────────────────────────
            if (string.IsNullOrEmpty(EtageKey) || string.IsNullOrEmpty(GroupEtageKey) || !roomKeyToEtage.Any())
                return;

            var infos = elements
               .Select(e =>
                    {
                        string rkVal = e.LookupParameter(RoomKey)?.AsString() ?? "";
                        string etage = (!string.IsNullOrWhiteSpace(rkVal) &&
                             roomKeyToEtage.TryGetValue(rkVal, out string et)) ? et : "";
                        string typeName = doc.GetElement(e.GetTypeId()).Name;
                        return new { El = e, RoomKeyVal = rkVal, Etage = etage, TypeName = typeName };
                    })
            .Where(x => !string.IsNullOrWhiteSpace(x.RoomKeyVal))
                .ToList();

            var etageGroupValues = infos
            .GroupBy(x => new { x.TypeName, x.Etage })
                      .ToDictionary(
                    g => g.Key,
          g => string.Join(", ", g
               .Select(x => x.RoomKeyVal)
                     .Where(v => !string.IsNullOrWhiteSpace(v))
              .Distinct().OrderBy(v => v)));

            foreach (var info in infos)
            {
                var key = new { info.TypeName, info.Etage };
                if (!etageGroupValues.TryGetValue(key, out string joinedEtageKeys)) continue;
                var p = info.El.LookupParameter(GroupEtageKey);
                if (p != null && !p.IsReadOnly && p.StorageType == StorageType.String)
                    p.Set(joinedEtageKeys);
            }
        }

        private string NormalizeTypeNameForFilename(string typeName)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                typeName = typeName.Replace(c, '-');
            typeName = typeName.Replace('.', '-');
            return typeName.Trim();
        }
    }
}
