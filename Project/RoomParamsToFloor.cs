using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AbimToolsMine
{
    [Transaction(TransactionMode.Manual)]
    public class RoomParamsToFloor : IExternalCommand
    {
        private const double SampleStepFt = 1.0;
        // ~1 м в футах
        private const double ZOffsetFt = 3.28084;

        // Смещения для поиска помещения со стороны стены: 20, 50, 100, 200, 400, 500 мм
        private static readonly double[] WallOffsetsFt = new double[]
        {
  20  / 304.8,
         50  / 304.8,
    100 / 304.8,
    200 / 304.8,
      400 / 304.8,
  500 / 304.8,
        };

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Трёхфазный цикл: окно ? выбор помещений ? окно снова
            List<ElementId> preselectedRooms = null;
            RoomParamsToFloorWin win;

            while (true)
            {
                win = preselectedRooms != null
             ? new RoomParamsToFloorWin(uidoc, preselectedRooms)
             : new RoomParamsToFloorWin(uidoc);
                RevitWindowOwner.SetOwner(win, commandData.Application);
                win.ShowDialog();

                if (win.NeedRoomPick)
                {
                    // Пользователь нажал "Выбрать на виде" — делаем выбор без модального окна
                    try
                    {
                        IList<Reference> refs = uidoc.Selection.PickObjects(
                        Autodesk.Revit.UI.Selection.ObjectType.Element,
                                   new RoomSelectionFilter(),
                          "Выберите помещения и нажмите Готово");
                        preselectedRooms = refs.Select(r => r.ElementId).ToList();
                    }
                    catch
                    {
                        // Отмена — возвращаемся в окно без изменений
                        preselectedRooms = preselectedRooms ?? new List<ElementId>();
                    }
                    continue; // открываем окно снова
                }

                // Пользователь нажал Запустить (true) или Закрыть (false/null)
                if (win.DialogResult != true)
                    return Result.Cancelled;

                break;
            }

            var mappings = win.Mappings
        .Where(m => !string.IsNullOrWhiteSpace(m.SourceParam)
                      && !string.IsNullOrWhiteSpace(m.TargetParam))
      .ToList();

            if (!mappings.Any())
            {
                TaskDialog.Show("Предупреждение", "Не задано ни одного соответствия параметров.");
                return Result.Cancelled;
            }

            // Все помещения модели — для поиска
      var allRooms = new FilteredElementCollector(doc)
      .OfCategory(BuiltInCategory.OST_Rooms)
      .WhereElementIsNotElementType()
.Cast<Room>()
      .Where(r => r.Area > 0)
 .ToList();

            if (!allRooms.Any())
      {
      TaskDialog.Show("Предупреждение", "В проекте не найдено помещений.");
    return Result.Cancelled;
     }

 // allowedIds: null = все помещения, иначе — только выбранные
     HashSet<ElementId> allowedIds = null;
    if (win.SelectedRoomIds != null && win.SelectedRoomIds.Count > 0)
 {
        allowedIds = new HashSet<ElementId>(win.SelectedRoomIds);
       if (!allRooms.Any(r => allowedIds.Contains(r.Id)))
        {
           TaskDialog.Show("Предупреждение", "Ни одно из выбранных помещений не найдено.");
    return Result.Cancelled;
  }
    }

            var geomOptions = new Options
            {
       ComputeReferences = true,
IncludeNonVisibleObjects = true
            };

 // ?? Сбор целевых элементов ????????????????????????????????????????????
       // Если помещения выбраны — используем пространственную предфильтрацию:
  // для каждого помещения расширяем BBox на 500 мм и берём только ближайшие элементы.
    // Если "все помещения" — обычный полный сбор.
            var allTargets = new List<(FinishingCategory cat, Element el)>();

        if (allowedIds == null)
        {
   // Режим "все помещения" — собираем все целевые элементы
     foreach (FinishingCategory category in win.SelectedCategories)
   foreach (Element el in GetTargets(doc, category))
  allTargets.Add((category, el));
            }
 else
         {
     // Режим "выбранные помещения" — пространственная предфильтрация
                const double toleranceFt = 500.0 / 304.8; // 500 мм в футах
            var processedIds = new HashSet<ElementId>();
         var selectedRooms = allRooms.Where(r => allowedIds.Contains(r.Id)).ToList();

       foreach (Room selectedRoom in selectedRooms)
    {
       BoundingBoxXYZ bb = selectedRoom.get_BoundingBox(null);
         if (bb == null) continue;

     // Расширяем BBox на toleranceFt по XY (Z не трогаем — этажи разные)
var outline = new Outline(
            new XYZ(bb.Min.X - toleranceFt, bb.Min.Y - toleranceFt, bb.Min.Z),
         new XYZ(bb.Max.X + toleranceFt, bb.Max.Y + toleranceFt, bb.Max.Z));

         var bbFilter = new BoundingBoxIntersectsFilter(outline);

        foreach (FinishingCategory category in win.SelectedCategories)
      {
                foreach (Element el in GetTargetsWithFilter(doc, category, bbFilter))
   {
  // processedIds.Add возвращает true если элемент добавлен впервые
           if (processedIds.Add(el.Id))
        allTargets.Add((category, el));
}
      }
    }
  }

       int total   = allTargets.Count;
            int current = 0;

     var progress = new ProgressWindow();
     RevitWindowOwner.SetOwner(progress, commandData.Application);
     progress.Show();

            using (Transaction t = new Transaction(doc, "Передача параметров из помещений в элементы отделки"))
            {
      t.Start();

          foreach (var pair in allTargets)
                {
      FinishingCategory category = pair.cat;
          Element el = pair.el;

            current++;
            progress.UpdateProgress($"Обработка {current} из {total}...", current, total);

 Room room = FindRoomForElement(el, category, allRooms, geomOptions);
    if (room == null) continue;

  // Если задан фильтр — пропускаем элементы "чужих" помещений
     if (allowedIds != null && !allowedIds.Contains(room.Id)) continue;

                  foreach (var mapping in mappings)
               {
 string value = GetParamStringValue(room, mapping.SourceParam);
    if (value == null) continue;
     Parameter tp = el.LookupParameter(mapping.TargetParam);
     if (tp == null || tp.IsReadOnly) continue;
            try
           {
    if (tp.StorageType == StorageType.String) tp.Set(value);
          else tp.SetValueString(value);
      }
   catch { }
         }

       // WallSweep для стен
        if (category == FinishingCategory.Walls)
       {
         foreach (ElementId depId in el.GetDependentElements(null))
             {
       WallSweep sweep = doc.GetElement(depId) as WallSweep;
          if (sweep == null) continue;
        foreach (var mapping in mappings)
  {
          string value = GetParamStringValue(room, mapping.SourceParam);
         if (value == null) continue;
      Parameter tp = sweep.LookupParameter(mapping.TargetParam);
            if (tp == null || tp.IsReadOnly) continue;
  try { if (tp.StorageType == StorageType.String) tp.Set(value); else tp.SetValueString(value); }
                catch { }
    }
    }
              }
         } // foreach allTargets

    t.Commit();
      }

     progress.Close();
         TaskDialog.Show("Готово", "Данные успешно записаны.");
   return Result.Succeeded;
  }

        private static List<Element> GetTargets(Document doc, FinishingCategory category)
        {
            switch (category)
            {
                case FinishingCategory.Ceilings:
                    return new FilteredElementCollector(doc)
                     .OfCategory(BuiltInCategory.OST_Ceilings)
                        .WhereElementIsNotElementType()
                       .Cast<Element>().ToList();

                case FinishingCategory.Walls:
                    return new FilteredElementCollector(doc)
                         .OfCategory(BuiltInCategory.OST_Walls)
                  .WhereElementIsNotElementType()
              .Cast<Element>()
                .Where(e =>
              {
                  ElementType et = doc.GetElement(e.GetTypeId()) as ElementType;
                  return et != null && et.Name.StartsWith("ВО Стена", StringComparison.OrdinalIgnoreCase);
              })
                    .ToList();

                default: // Floors
                    return new FilteredElementCollector(doc)
                   .OfCategory(BuiltInCategory.OST_Floors)
                       .WhereElementIsNotElementType()
                          .Cast<Element>()
                         .Where(e =>
                     {
                         ElementType et = doc.GetElement(e.GetTypeId()) as ElementType;
                         return et != null && (
                        et.Name.StartsWith("ВО Пол", StringComparison.OrdinalIgnoreCase) ||
            et.Name.StartsWith("ВО Стена", StringComparison.OrdinalIgnoreCase));
                     })
                   .ToList();
            }
        }

/// <summary>
    /// То же что GetTargets, но с дополнительным пространственным фильтром BBox.
  /// </summary>
        private static List<Element> GetTargetsWithFilter(
        Document doc, FinishingCategory category, BoundingBoxIntersectsFilter bbFilter)
        {
 switch (category)
     {
          case FinishingCategory.Ceilings:
    return new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Ceilings)
            .WherePasses(bbFilter)
 .WhereElementIsNotElementType()
      .Cast<Element>().ToList();

       case FinishingCategory.Walls:
  return new FilteredElementCollector(doc)
 .OfCategory(BuiltInCategory.OST_Walls)
         .WherePasses(bbFilter)
   .WhereElementIsNotElementType()
     .Cast<Element>()
    .Where(e =>
  {
      ElementType et = doc.GetElement(e.GetTypeId()) as ElementType;
      return et != null && et.Name.StartsWith("ВО Стена", StringComparison.OrdinalIgnoreCase);
     })
         .ToList();

     default: // Floors
  return new FilteredElementCollector(doc)
       .OfCategory(BuiltInCategory.OST_Floors)
  .WherePasses(bbFilter)
.WhereElementIsNotElementType()
  .Cast<Element>()
       .Where(e =>
  {
    ElementType et = doc.GetElement(e.GetTypeId()) as ElementType;
      return et != null && (
         et.Name.StartsWith("ВО Пол",   StringComparison.OrdinalIgnoreCase) ||
    et.Name.StartsWith("ВО Стена", StringComparison.OrdinalIgnoreCase));
})
  .ToList();
     }
        }

        // ?? Диспетчер поиска помещения по категории ??????????????????????????????

        private static Room FindRoomForElement(Element el, FinishingCategory category,
       List<Room> rooms, Options opt)
        {
            switch (category)
            {
                case FinishingCategory.Floors: return FindRoomForFloor(el, rooms, opt);
                case FinishingCategory.Ceilings: return FindRoomForCeiling(el, rooms, opt);
                case FinishingCategory.Walls: return FindRoomForWall(el, rooms, opt);
                default: return null;
            }
        }

        /// <summary>
        /// Пол: сэмплируем горизонтальные грани, смещаемся ВВЕРХ (+Z) в помещение.
        /// </summary>
        private static Room FindRoomForFloor(Element el, List<Room> rooms, Options opt)
        {
            GeometryElement geom = el.get_Geometry(opt);
            if (geom == null) return null;

            foreach (GeometryObject gObj in geom)
            {
                Solid solid = gObj as Solid;
                if (solid == null || solid.Volume <= 0) continue;

                foreach (Face face in solid.Faces)
                {
                    PlanarFace pf = face as PlanarFace;
                    if (pf == null || Math.Abs(pf.FaceNormal.Z) < 0.9) continue;

                    // Верхняя грань (нормаль вверх) ? смещаемся чуть вверх
                    double offset = pf.FaceNormal.Z > 0 ? ZOffsetFt : -ZOffsetFt;
                    Room found = SampleFaceForRoom(pf, rooms, offset);
                    if (found != null) return found;
                }
            }
            return null;
        }

        /// <summary>
        /// Потолок: сэмплируем горизонтальные грани, смещаемся ВНИЗ (-Z) в помещение.
        /// </summary>
        private static Room FindRoomForCeiling(Element el, List<Room> rooms, Options opt)
        {
            GeometryElement geom = el.get_Geometry(opt);
            if (geom == null) return null;

            foreach (GeometryObject gObj in geom)
            {
                Solid solid = gObj as Solid;
                if (solid == null || solid.Volume <= 0) continue;

                foreach (Face face in solid.Faces)
                {
                    PlanarFace pf = face as PlanarFace;
                    if (pf == null || Math.Abs(pf.FaceNormal.Z) < 0.9) continue;

                    // Нижняя грань (нормаль вниз) ? смещаемся ещё ниже в помещение
                    double offset = pf.FaceNormal.Z < 0 ? -ZOffsetFt : ZOffsetFt;
                    Room found = SampleFaceForRoom(pf, rooms, offset);
                    if (found != null) return found;
                }
            }
            return null;
        }

        /// <summary>
        /// Стена: вычисляем нормаль из LocationCurve, пробуем обе стороны на 6 смещениях.
        /// </summary>
        private static Room FindRoomForWall(Element el, List<Room> rooms, Options opt)
        {
            Wall wall = el as Wall;
            if (wall == null) return null;

            LocationCurve lc = wall.Location as LocationCurve;
            if (lc == null) return null;

            Curve curve = lc.Curve;
            XYZ p0 = curve.GetEndPoint(0);
            XYZ p1 = curve.GetEndPoint(1);

            // Середина стены
            XYZ mid = p0.Add(p1).Multiply(0.5);

            // Горизонтальная нормаль (перпендикуляр к оси стены в плане)
            XYZ dir = p1.Subtract(p0).Normalize();
            XYZ normal = new XYZ(-dir.Y, dir.X, 0).Normalize();

            foreach (double offset in WallOffsetsFt)
            {
                XYZ ptPlus = mid.Add(normal.Multiply(offset));
                XYZ ptMinus = mid.Subtract(normal.Multiply(offset));

                foreach (Room room in rooms)
                {
                    try
                    {
                        if (room.IsPointInRoom(ptPlus)) return room;
                        if (room.IsPointInRoom(ptMinus)) return room;
                    }
                    catch { }
                }
            }

            return null;
        }

        // ?? Сэмплирование граней ?????????????????????????????????????????????????

        private static Room SampleFaceForRoom(PlanarFace face, List<Room> rooms, double zOffset)
        {
            BoundingBoxUV bbox = face.GetBoundingBox();
            double uMin = bbox.Min.U, uMax = bbox.Max.U;
            double vMin = bbox.Min.V, vMax = bbox.Max.V;

            for (double u = uMin; u <= uMax + 1e-6; u += SampleStepFt)
            {
                for (double v = vMin; v <= vMax + 1e-6; v += SampleStepFt)
                {
                    XYZ pt;
                    try { pt = face.Evaluate(new UV(u, v)); }
                    catch { continue; }

                    if (face.Project(pt) == null) continue;

                    XYZ testPt = new XYZ(pt.X, pt.Y, pt.Z + zOffset);

                    foreach (Room room in rooms)
                    {
                        try { if (room.IsPointInRoom(testPt)) return room; }
                        catch { }
                    }
                }
            }
            return null;
        }

        // ?? Утилиты ??????????????????????????????????????????????????????????????

        private static string GetParamStringValue(Element element, string paramName)
        {
            Parameter p = element.LookupParameter(paramName);

            if (p == null && paramName == "Номер")
                p = element.get_Parameter(BuiltInParameter.ROOM_NUMBER);
            if (p == null && paramName == "Имя")
                p = element.get_Parameter(BuiltInParameter.ROOM_NAME);

            if (p == null) return null;

            switch (p.StorageType)
            {
                case StorageType.String: return p.AsString();
                case StorageType.Integer: return p.AsInteger().ToString();
                case StorageType.Double: return p.AsValueString();
                case StorageType.ElementId: return p.AsValueString();
                default: return null;
            }
        }
        private class RoomSelectionFilter : Autodesk.Revit.UI.Selection.ISelectionFilter
    {
        public bool AllowElement(Element elem) => elem is Room;
        public bool AllowReference(Reference reference, XYZ position) => false;
    }
} // class RoomParamsToFloor
} // namespace
