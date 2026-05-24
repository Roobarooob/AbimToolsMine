using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;

namespace AbimToolsMine
{
    [Transaction(TransactionMode.Manual)]
    public class FillFloorParam : IExternalCommand
    {
   public static readonly string EtageParamName    = "ПРО_Этаж";
        private static readonly string WarningParamName = "ПРО_Предупреждение";
        private const string WarnNoLevel  = "Нет уровня";
    private const string WarnNoEtageValue = "Не заполнен \"ПРО_Этаж\" у связанного уровня";

    // ?? IExternalCommand: работает по всем модельным элементам проекта ??????
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
        UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document   doc   = uidoc.Document;

          bool hasEtage   = false;
       bool hasWarning = false;
       foreach (Element el in new FilteredElementCollector(doc).WhereElementIsNotElementType().ToElements())
            {
    if (el.Category == null || el.Category.CategoryType != CategoryType.Model) continue;
    if (!hasEtage   && el.LookupParameter(EtageParamName)!= null) hasEtage   = true;
            if (!hasWarning && el.LookupParameter(WarningParamName)  != null) hasWarning = true;
            if (hasEtage && hasWarning) break;
    }

            if (!hasEtage || !hasWarning)
            {
    string missing = !hasEtage && !hasWarning
         ? $"\"{EtageParamName}\" и \"{WarningParamName}\""
         : !hasEtage ? $"\"{EtageParamName}\""
        : $"\"{WarningParamName}\"";
     TaskDialog.Show("Ошибка",
   $"В проекте не найден параметр {missing}.\n" +
         "Убедитесь, что общий параметр загружен и привязан к категориям.");
      return Result.Cancelled;
      }

            var allElements = new FilteredElementCollector(doc)
        .WhereElementIsNotElementType().ToElements();

         return ExecuteForElements(doc, allElements.Cast<Element>().ToList(), EtageParamName);
        }

        // ?? Публичный метод для вызова из CopyParameterCommand ???????????????????
public static Result ExecuteForElements(
            Document doc,
        List<Element> elements,
            string etageParamName)
        {
            // Кэш уровней: LevelId -> значение параметра этажа
            var levelEtage = new Dictionary<ElementId, string>();
  foreach (Level lvl in new FilteredElementCollector(doc)
 .OfClass(typeof(Level)).Cast<Level>())
          {
          Parameter p = lvl.LookupParameter(etageParamName);
      string val = p != null
          ? (p.StorageType == StorageType.String ? p.AsString() : p.AsValueString())
       : null;
                levelEtage[lvl.Id] = string.IsNullOrWhiteSpace(val) ? null : val;
          }

    int filled = 0, warned = 0, cleared = 0;

            using (Transaction t = new Transaction(doc, "Заполнение параметра этажа"))
  {
      t.Start();

          foreach (Element el in elements)
    {
      Category cat = el.Category;
 if (cat == null || cat.CategoryType != CategoryType.Model) continue;

           Parameter etageParam = el.LookupParameter(etageParamName);
          if (etageParam == null || etageParam.IsReadOnly) continue;

  ElementId levelId = GetLevelId(el, doc);
    bool hasLevel = levelId != null
          && levelId != ElementId.InvalidElementId
      && levelEtage.ContainsKey(levelId);

    if (hasLevel)
   {
         RemoveWarningLine(el, WarnNoLevel, ref cleared);
  string etageValue = levelEtage[levelId];

      if (string.IsNullOrEmpty(etageValue))
      {
         AddWarningLine(el, WarnNoEtageValue, ref warned);
     }
   else
       {
  RemoveWarningLine(el, WarnNoEtageValue, ref cleared);
      try
         {
        if (etageParam.StorageType == StorageType.String)
         etageParam.Set(etageValue);
           else
     etageParam.SetValueString(etageValue);
            filled++;
     }
        catch { }
         }
         }
          else
            {
  AddWarningLine(el, WarnNoLevel, ref warned);
      RemoveWarningLine(el, WarnNoEtageValue, ref cleared);
          }
     }

     t.Commit();
            }

            TaskDialog.Show("Готово",
      $"Заполнено параметров {etageParamName}: {filled}\n" +
      $"Добавлено предупреждений: {warned}\n" +
     $"Удалено предупреждений: {cleared}");

            return Result.Succeeded;
 }

        // ------------------------------------------------------------------
 // Определение уровня элемента
        // ------------------------------------------------------------------
        private static ElementId GetLevelId(Element el, Document doc)
   {
   ElementId invalid = ElementId.InvalidElementId;

   ElementId Valid(ElementId id) => (id != null && id != invalid) ? id : null;
        ElementId ParamId(BuiltInParameter bip)
   {
      try
  {
   Parameter p = el.get_Parameter(bip);
        if (p != null) return Valid(p.AsElementId());
         }
             catch { }
 return null;
            }

            try
{
    if (el is FamilyInstance fi)
     {
      var id = Valid(fi.LevelId);
    if (id != null) return id;
    }

      if (el is Wall wall)       { var id = Valid(wall.LevelId);  if (id != null) return id; }
  if (el is Floor floor)     { var id = Valid(floor.LevelId);   if (id != null) return id; }
 if (el is Ceiling ceiling) { var id = Valid(ceiling.LevelId); if (id != null) return id; }

            // Помещение — уровень через ROOM_LEVEL_ID
            if (el is Room)
    {
        var id = ParamId(BuiltInParameter.ROOM_LEVEL_ID);
            if (id != null) return id;
            }

      // WallSweep (Выступающий профиль) — уровень через WALL_SWEEP_LEVEL_PARAM
            if (el is WallSweep)
            {
                var id = ParamId(BuiltInParameter.WALL_SWEEP_LEVEL_PARAM);
     if (id != null) return id;
  }

             if (el is RoofBase)
          {
      var id = ParamId(BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM);
          if (id != null) return id;
          }

      if (el is Stairs)
 {
      var id = ParamId(BuiltInParameter.STAIRS_BASE_LEVEL_PARAM);
    if (id != null) return id;
      }

      if (el is StairsRun)
          {
      var id = ParamId(BuiltInParameter.STAIRS_BASE_LEVEL_PARAM);
  if (id != null) return id;
            }

  if (el is StairsLanding)
    {
   var id = ParamId(BuiltInParameter.STAIRS_BASE_LEVEL_PARAM);
 if (id != null) return id;
  }

      if (el is MEPCurve)
    {
  var id = ParamId(BuiltInParameter.RBS_START_LEVEL_PARAM);
        if (id != null) return id;
         }

             var candidates = new[]
    {
     BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM,
  BuiltInParameter.SCHEDULE_LEVEL_PARAM,
  BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM,
       BuiltInParameter.FAMILY_LEVEL_PARAM,
           BuiltInParameter.STAIRS_BASE_LEVEL_PARAM,
        BuiltInParameter.ROOF_CONSTRAINT_LEVEL_PARAM,
      BuiltInParameter.RBS_START_LEVEL_PARAM,
                };

         foreach (var bip in candidates)
            {
               var id = ParamId(bip);
                    if (id != null) return id;
          }

  }
            catch { }

         return invalid;
        }

        // ------------------------------------------------------------------
      // Вспомогательные методы для работы с предупреждениями
        // ------------------------------------------------------------------
        private static void AddWarningLine(Element el, string line, ref int counter)
        {
            Parameter wp = el.LookupParameter(WarningParamName);
if (wp == null || wp.IsReadOnly || wp.StorageType != StorageType.String) return;

            string current = wp.AsString() ?? "";
            var lines = current.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None).ToList();
 if (lines.Any(l => l.Trim() == line)) return;

     lines.Add(line);
   try
         {
                wp.Set(string.Join("\n", lines.Select(l => l.Trim()).Where(l => l.Length > 0)));
                counter++;
            }
  catch { }
      }

        private static void RemoveWarningLine(Element el, string line, ref int counter)
     {
         Parameter wp = el.LookupParameter(WarningParamName);
   if (wp == null || wp.IsReadOnly || wp.StorageType != StorageType.String) return;

         string current = wp.AsString() ?? "";
     var lines = current.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None)
     .Select(l => l.Trim()).Where(l => l.Length > 0).ToList();

            if (!lines.Any(l => l == line)) return;

lines.RemoveAll(l => l == line);
          try
 {
       wp.Set(string.Join("\n", lines));
        counter++;
            }
  catch { }
        }
    }
}
