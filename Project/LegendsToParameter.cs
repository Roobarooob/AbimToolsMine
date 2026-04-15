using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using Path = System.IO.Path;
using Settings = AbimToolsMine.Properties.Settings;
using View = Autodesk.Revit.DB.View;

namespace AbimToolsMine
{
    [Transaction(TransactionMode.Manual)]
    public class LegendsToParameter : IExternalCommand
    {
        private static readonly string RoomKey = Settings.Default.FloorRoomKeyParam;
        private static readonly string GroupKey = Settings.Default.FloorRoomGroupParam;
        private static readonly string EtageKey = Settings.Default.EtageParam;
        private static readonly string GroupEtageKey = Settings.Default.RoomGroupEtageParam;

        // Максимальная длина имени файла (без расширения) с учётом базового пути
        private const int MaxFileNameLength = 60;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Используем короткий путь в корне диска C: чтобы избежать превышения MAX_PATH (260 символов)
            // AppData\Local гарантированно доступен на запись для любого пользователя
            string exportFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RvtFlrImg");
            Directory.CreateDirectory(exportFolder);

            // Собираем легенды-пироги и строим маппинг: короткое имя файла → реальное имя типа пола
            // Это решает обе проблемы:
            //   - точки в имени (GetFileNameWithoutExtension обрезает по первой точке)
            //   - длинные пути (используем короткий числовой ID вместо полного имени)
            var legendViews = new FilteredElementCollector(doc)
     .OfClass(typeof(View))
  .Cast<View>()
           .Where(v => v.ViewType == ViewType.DraftingView && v.Name.Contains("Пирог -"))
        .ToList();

            if (!legendViews.Any())
   return Result.Succeeded;

        // Маппинг: короткое имя файла (без расширения) → реальное имя типа пола
            // Формат: "floor_0001", "floor_0002", ...
            // Извлекаем имя типа пола из имени легенды вида "Пирог - <ИмяТипа>"
    var fileNameToTypeName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
  var exportIds = new List<ElementId>();

            for (int i = 0; i < legendViews.Count; i++)
            {
    View v = legendViews[i];
                string shortName = $"floor_{i:D4}";

    // Имя легенды ожидается вида "Пирог - <ИмяТипа>"
          string legendPrefix = "Пирог - ";
        string typeName = v.Name.Contains(legendPrefix)
               ? v.Name.Substring(v.Name.IndexOf(legendPrefix, StringComparison.Ordinal) + legendPrefix.Length)
                 : v.Name;

           fileNameToTypeName[shortName] = typeName;
    exportIds.Add(v.Id);
        }

            // Экспортируем все легенды сразу с коротким базовым именем файла "floor"
  // Revit добавит суффикс с именем вида, но мы переименуем файлы через маппинг
            // Используем временную папку с коротким именем
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

          // После экспорта Revit создаёт файлы вида:
          //   "fl - Чертежный вид - Пирог - <ИмяТипа>.png"
            // Переименовываем их в короткие имена из маппинга, используя реальное имя типа
   string revitLegendInfix = " - Чертежный вид - Пирог - ";

  foreach (var kv in fileNameToTypeName)
            {
     string shortName  = kv.Key;   // "floor_0001"
       string typeName   = kv.Value; // реальное имя типа пола

                // Нормализуем имя типа для имени файла (как это делает Revit)
      string normalizedTypeName = NormalizeTypeNameForFilename(typeName);

       // Revit формирует имя файла как "<prefix><infix><normalizedTypeName>.png"
           string expectedFileName = tempPrefix + revitLegendInfix + normalizedTypeName + ".png";
                string expectedFilePath = Path.Combine(exportFolder, expectedFileName);

     string shortFilePath = Path.Combine(exportFolder, shortName + ".png");

      if (File.Exists(expectedFilePath))
    {
             // Переименовываем в короткое имя
    if (File.Exists(shortFilePath)) File.Delete(shortFilePath);
    File.Move(expectedFilePath, shortFilePath);
      }
   }

            // Теперь обрабатываем переименованные файлы
       using (Transaction t = new Transaction(doc, "Присвоение изображений типам полов"))
{
          t.Start();

    foreach (var kv in fileNameToTypeName)
    {
          string shortName = kv.Key;
      string typeName  = kv.Value;
       string filePath  = Path.Combine(exportFolder, shortName + ".png");

   if (!File.Exists(filePath))
                 continue;

          try
              {
           // Ищем тип пола по реальному имени (без нормализации — сравниваем напрямую)
  FloorType floorType = new FilteredElementCollector(doc)
      .OfClass(typeof(FloorType))
         .Cast<FloorType>()
  .FirstOrDefault(f => f.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));

       if (floorType == null)
        {
         File.Delete(filePath);
             continue;
       }

   // Проверяем, есть ли уже изображение с таким именем в документе
            // Ищем по короткому имени файла (floor_XXXX.png)
             string imageNameInDoc = shortName + ".png";
         ImageType existingImage = new FilteredElementCollector(doc)
        .OfClass(typeof(ImageType))
    .Cast<ImageType>()
 .FirstOrDefault(img => img.Name.Equals(imageNameInDoc, StringComparison.OrdinalIgnoreCase));

                  ImageType imageType;
#if R2020
             ImageTypeOptions imageOptions = new ImageTypeOptions(filePath, false);
#else
      ImageTypeOptions imageOptions = new ImageTypeOptions(filePath, false, ImageTypeSource.Import);
#endif
      if (existingImage != null)
            {
              existingImage.ReloadFrom(imageOptions);
          imageType = existingImage;
         }
   else
    {
                imageType = ImageType.Create(doc, imageOptions);
            }

            // Присваиваем изображение параметру типа пола
    Parameter param = floorType.LookupParameter("ПРО_Изображение типоразмера");
            if (param != null && param.StorageType == StorageType.ElementId)
      {
    param.Set(imageType.Id);
           }

 // Также записываем системную площадь перекрытия в параметр "ПРО_Площадь" у каждого экземпляра пола этого типа
 // Берём все экземпляры данного типа
 var instances = new FilteredElementCollector(doc)
 .OfClass(typeof(Floor))
 .Cast<Floor>()
 .Where(f => f.GetTypeId() == floorType.Id)
 .ToList();

 foreach (var inst in instances)
 {
 // Получаем системную площадь (BuiltInParameter.HOST_AREA_COMPUTED)
 Parameter sysAreaParam = inst.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED);
 if (sysAreaParam == null) continue;

 double areaInternal = sysAreaParam.AsDouble(); // квадратные футы (внутренние единицы)

 // Целевой параметр
 Parameter targetAreaParam = inst.LookupParameter("ПРО_Площадь");
 if (targetAreaParam == null) continue;

 if (targetAreaParam.StorageType == StorageType.Double)
 {
 // Устанавливаем значение в внутренних единицах (Revit ожидает внутренние единицы)
 targetAreaParam.Set(areaInternal);
 }
 else if (targetAreaParam.StorageType == StorageType.String)
 {
 // Конвертируем в квадратные метры и сохраняем строкой с2 знаками
 double areaM2 = UnitUtils.ConvertFromInternalUnits(areaInternal, UnitTypeId.SquareMeters);
 targetAreaParam.Set(Math.Round(areaM2,2).ToString(CultureInfo.InvariantCulture));
 }
 }

 }
   finally
          {
               File.Delete(filePath);
              }
      }

    // Собираем все полы и группируем по типоразмеру
             var floors = new FilteredElementCollector(doc)
   .OfClass(typeof(Floor))
               .Cast<Floor>()
  .Where(f =>
            {
   var roomParam  = f.LookupParameter(RoomKey);
           var groupParam = f.LookupParameter(GroupKey);
 return roomParam  != null && groupParam != null &&
  roomParam.StorageType  == StorageType.String &&
    groupParam.StorageType == StorageType.String &&
 !groupParam.IsReadOnly;
       })
          .ToList();

         var grouped = floors.GroupBy(f => doc.GetElement(f.GetTypeId()).Name).ToList();

     foreach (var group in grouped)
      {
  var floorsOfType = group.ToList();

         var roomKeys = floorsOfType
            .Select(f => f.LookupParameter(RoomKey)?.AsString())
        .Where(v => !string.IsNullOrWhiteSpace(v))
       .Distinct()
            .OrderBy(v => v)
            .ToList();

    string joinedKeys = string.Join(", ", roomKeys);

     foreach (var floor in floorsOfType)
      {
  var param = floor.LookupParameter(GroupKey);
                 if (param != null && !param.IsReadOnly && param.StorageType == StorageType.String)
     param.Set(joinedKeys);
       }
       }

   // Поэтажная группировка:
   // Для каждого пола берём его RoomKey, ищем помещение с таким значением,
   // читаем у него EtageKey, затем группируем полы по (тип + этаж)
// и записываем RoomKey через запятую в параметр GroupEtageKey
   if (!string.IsNullOrEmpty(EtageKey) && !string.IsNullOrEmpty(GroupEtageKey))
   {
       // Строим словарь: значение RoomKey помещения → значение EtageParam
       var roomKeyToEtage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
       var rooms = new FilteredElementCollector(doc)
        .OfCategory(BuiltInCategory.OST_Rooms)
           .WhereElementIsNotElementType()
           .ToList();

       foreach (var room in rooms)
       {
   // У помещения номер хранится в системном параметре ROOM_NUMBER, а не в ПРО_Номер помещения
      var rkParam = room.get_Parameter(BuiltInParameter.ROOM_NUMBER);
           var etParam = room.LookupParameter(EtageKey);
           if (rkParam == null || etParam == null) continue;
    string rkVal = rkParam.AsString();
           string etVal = etParam.AsString();
           if (string.IsNullOrWhiteSpace(rkVal) || string.IsNullOrWhiteSpace(etVal)) continue;
  if (!roomKeyToEtage.ContainsKey(rkVal))
             roomKeyToEtage[rkVal] = etVal;
       }

  // Для каждого экземпляра пола определяем его этаж через его собственный RoomKey
       // floorInfo: экземпляр пола + его RoomKey + его этаж
       var floorInfos = floors
           .Select(f =>
           {
         string rkVal = f.LookupParameter(RoomKey)?.AsString() ?? "";
          string etage = (!string.IsNullOrWhiteSpace(rkVal) &&
                  roomKeyToEtage.TryGetValue(rkVal, out string e)) ? e : "";
               string typeName = doc.GetElement(f.GetTypeId()).Name;
    return new { Floor = f, RoomKeyVal = rkVal, Etage = etage, TypeName = typeName };
           })
      .Where(x => !string.IsNullOrWhiteSpace(x.RoomKeyVal))
           .ToList();

       // Строим словарь: (тип + этаж) → список уникальных RoomKey (отсортированных)
       // Это будет значение для GroupEtageKey всех полов этой группы
       var etageGroupValues = floorInfos
   .GroupBy(x => new { x.TypeName, x.Etage })
           .ToDictionary(
               g => g.Key,
               g => string.Join(", ", g
     .Select(x => x.RoomKeyVal)
        .Where(v => !string.IsNullOrWhiteSpace(v))
           .Distinct()
      .OrderBy(v => v)));

       // Записываем в каждый экземпляр пола его поэтажное значение
       foreach (var info in floorInfos)
       {
      var key = new { info.TypeName, info.Etage };
       if (!etageGroupValues.TryGetValue(key, out string joinedEtageKeys)) continue;

   var param = info.Floor.LookupParameter(GroupEtageKey);
  if (param != null && !param.IsReadOnly && param.StorageType == StorageType.String)
     param.Set(joinedEtageKeys);
       }
   }
   t.Commit();
   }

   TaskDialog.Show("Готово", "Параметры заполнены");

      // Удаляем временную папку если она пуста
    try
        {
   if (Directory.Exists(exportFolder) && !Directory.EnumerateFileSystemEntries(exportFolder).Any())
          Directory.Delete(exportFolder);
    }
   catch { }

  return Result.Succeeded;
        }

  private string NormalizeTypeNameForFilename(string typeName)
        {
    char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
   typeName = typeName.Replace(c, '-');

            // Revit дополнительно заменяет точку на дефис при формировании имени файла
            typeName = typeName.Replace('.', '-');

      return typeName.Trim();
        }
    }
}
