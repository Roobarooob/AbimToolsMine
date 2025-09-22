using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
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
        private static readonly string GroupKey = Settings.Default.FloorRoomGroupParam;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;


            List<ElementId> legends = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => v.ViewType == ViewType.DraftingView && v.Name.Contains("Пирог -"))
                .Select(v => v.Id)
                .ToList();

            string p = Path.GetTempPath();
            string exportFolder = p;
            Directory.CreateDirectory(exportFolder);

            ImageExportOptions options = new ImageExportOptions
            {
                ExportRange = ExportRange.SetOfViews,
                FilePath = Path.Combine(exportFolder, "Пол"),
                FitDirection = FitDirectionType.Horizontal,
                HLRandWFViewsFileType = ImageFileType.PNG,
                ImageResolution = ImageResolution.DPI_300,
                ZoomType = ZoomFitType.FitToPage,
                PixelSize = 2048,
                ShadowViewsFileType = ImageFileType.PNG
            };

            options.SetViewsAndSheets(legends);

            doc.ExportImage(options);


            string[] imageFiles = Directory.GetFiles(exportFolder, "*.png");
            using (Transaction t = new Transaction(doc, "Присвоение изображений типам полов"))
            {
                t.Start();

                foreach (string filePath in imageFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);

                    string prefix = "Пол - Чертежный вид - Пирог - ";
                    if (!fileName.StartsWith(prefix)) continue;

                    string typeName = fileName.Substring(prefix.Length);
                    FilteredElementCollector collector = new FilteredElementCollector(doc)
                        .OfClass(typeof(FloorType));

                    FloorType floorType = collector
                        .Cast<FloorType>()
                        .FirstOrDefault(f =>
                            NormalizeTypeNameForFilename(f.Name)
                            .Equals(typeName, StringComparison.OrdinalIgnoreCase));

                    if (floorType == null)
                    {
                        File.Delete(filePath);
                        //TaskDialog.Show("Внимание", $"Не найден тип перекрытия с именем: {typeName}");
                        continue;
                    }

                    // Проверяем, существует ли изображение с таким именем
                    FilteredElementCollector imageCollector = new FilteredElementCollector(doc)
                        .OfClass(typeof(ImageType));

                    ImageType existingImage = imageCollector
                        .Cast<ImageType>()
                        .FirstOrDefault(img => img.Name.Equals(fileName + ".png", StringComparison.OrdinalIgnoreCase));

                    ImageType imageType;
#if R2020
                    ImageTypeOptions imageOptions = new ImageTypeOptions(filePath, false);
#else
                    ImageTypeOptions imageOptions = new ImageTypeOptions(filePath, false, ImageTypeSource.Import);
#endif
                    if (existingImage != null)
                    {
                        // Обновляем изображение, если оно уже существует
                        existingImage.ReloadFrom(imageOptions);
                        imageType = existingImage;
                    }
                    else
                    {
                        // Создаём новое

                        imageType = ImageType.Create(doc, imageOptions);
                    }

                    // Присваиваем изображение параметру типа пола
                    Parameter param = floorType.LookupParameter("ПРО_Изображение типоразмера");
                    if (param != null && param.StorageType == StorageType.ElementId)
                    {
                        param.Set(imageType.Id);
                    }
                    else
                    {
                        TaskDialog.Show("Ошибка", $"Параметр не найден или неправильного типа у {typeName}");
                    }
                    File.Delete(filePath);
                }
                // Собираем все полы
                var floors = new FilteredElementCollector(doc)
                    .OfClass(typeof(Floor))
                    .Cast<Floor>()
                    .Where(f =>
                    {
                        var roomParam = f.LookupParameter(RoomKey);
                        var groupParam = f.LookupParameter(GroupKey);
                        return roomParam != null && groupParam != null &&
                               roomParam.StorageType == StorageType.String &&
                               groupParam.StorageType == StorageType.String &&
                               !groupParam.IsReadOnly;
                    })
                    .ToList();

                // Группировка по типоразмеру
                var grouped = floors
                    .GroupBy(f => doc.GetElement(f.GetTypeId()).Name)
                    .ToList();

                foreach (var group in grouped)
                {
                    var floorsOfType = group.ToList();

                    // Получаем уникальные RoomKey
                    var roomKeys = floorsOfType
                        .Select(f => f.LookupParameter(RoomKey)?.AsString())
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                        .Distinct()
                        .OrderBy(v => v)
                        .ToList();

                    string joinedKeys = string.Join(", ", roomKeys);

                    // Записываем в каждый пол этой группы
                    foreach (var floor in floorsOfType)
                    {
                        var param = floor.LookupParameter(GroupKey);
                        if (param != null && !param.IsReadOnly && param.StorageType == StorageType.String)
                        {
                            param.Set(joinedKeys);
                        }
                    }
                }
                t.Commit();
            }
            return Result.Succeeded;
        }
        private string NormalizeTypeNameForFilename(string typeName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();

            foreach (char c in invalidChars)
            {
                typeName = typeName.Replace(c, '-');
            }

            return typeName.Trim();
        }
    }
}
