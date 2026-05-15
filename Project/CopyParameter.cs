using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;

namespace AbimToolsMine
{
    [Transaction(TransactionMode.Manual)]
public class CopyParameterCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
     {
            UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
   Document doc = uidoc.Document;

   var win = new CopyParameterWin(commandData);
     if (win.ShowDialog() != true)
      return Result.Cancelled;

  // ?? Собираем элементы ??????????????????????????????????????????????????
 IEnumerable<Element> rawElements;

       if (win.UseSelection)
   {
      var ids = uidoc.Selection.GetElementIds();
    if (ids.Count == 0)
    {
   TaskDialog.Show("Предупреждение", "Нет выбранных элементов.");
    return Result.Cancelled;
       }
     rawElements = ids.Select(id => doc.GetElement(id));
    }
   else
     {
           var catIds = new ElementMulticategoryFilter(
     win.SelectedCategories.Select(c => c.Id).ToList());

     rawElements = new FilteredElementCollector(doc)
       .WherePasses(catIds)
        .WhereElementIsNotElementType()
    .Cast<Element>();
  }

       var allElements = rawElements.ToList();

   if (allElements.Count == 0)
     {
     TaskDialog.Show("Предупреждение", "Не найдено ни одного элемента.");
      return Result.Cancelled;
    }

    // ?? Маршрутизация по режиму ????????????????????????????????????????????
          if (win.NestedMode)
                return ExecuteNestedMode(doc, allElements, win.FromParam, win.OnlyEmpty);
    else
         return ExecuteWithinMode(doc, allElements, win.FromParam, win.ToParam, win.OnlyEmpty);
    }

        // ?? Режим: копирование внутри элемента ????????????????????????????????????
    private Result ExecuteWithinMode(Document doc, List<Element> allElements,
            string fromName, string toName, bool onlyEmpty)
        {
            // Проверки (до транзакции)
          Parameter fromParam = null;
            Parameter toParam   = null;

            foreach (var el in allElements)
    {
            if (fromParam == null) fromParam = FindParamOnInstanceOrType(doc, el, fromName);
     if (toParam   == null) toParam   = FindParamOnInstanceOrType(doc, el, toName);
    if (fromParam != null && toParam != null) break;
          }

            if (fromParam == null)
      {
        TaskDialog.Show("Ошибка", $"Параметр «{fromName}» не найден ни у одного из элементов. Операция отменена.");
     return Result.Cancelled;
}
      if (toParam == null)
  {
    TaskDialog.Show("Ошибка", $"Параметр «{toName}» не найден ни у одного из элементов. Операция отменена.");
        return Result.Cancelled;
       }

    if (fromParam.StorageType != toParam.StorageType)
     {
   TaskDialog.Show("Ошибка",
   $"Тип данных параметров не совпадает.\n" +
 $"«{fromName}»: {fromParam.StorageType}\n" +
       $"«{toName}»: {toParam.StorageType}\n\n" +
          "Операция отменена.");
     return Result.Cancelled;
            }

         bool fromIsInstanceParam    = !(fromParam.Element is ElementType);
       bool toIsTypeParam = toParam.Element is ElementType;
 bool instanceToTypeMismatch = fromIsInstanceParam && toIsTypeParam;

            if (instanceToTypeMismatch)
         {
      var dlg = new TaskDialog("Предупреждение")
    {
          MainContent =
   $"Параметр «{fromName}» является параметром экземпляра, " +
         $"а «{toName}» — параметром типа.\n\n" +
     "Для каждого типоразмера будет использовано значение из первого найденного экземпляра. " +
 "Остальные экземпляры этого типоразмера будут пропущены.\n\n" +
         "Продолжить?",
  CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No
       };
    if (dlg.Show() != TaskDialogResult.Yes)
  return Result.Cancelled;
    }

            int copied  = 0;
    int skipped = 0;

  using (Transaction trans = new Transaction(doc, "Копировать параметр"))
         {
       trans.Start();

          if (instanceToTypeMismatch)
  {
   var byType = allElements
        .GroupBy(el => el.GetTypeId())
        .Where(g => g.Key != ElementId.InvalidElementId);

   foreach (var grp in byType)
             {
      Element firstInst = grp.First();
  Parameter fp = FindParam(firstInst, fromName);
   if (fp == null) continue;

      ElementType elType = doc.GetElement(grp.Key) as ElementType;
     if (elType == null) continue;

    Parameter tp = FindParam(elType, toName);
         if (tp == null || tp.IsReadOnly) continue;

 if (onlyEmpty && !IsEmpty(tp)) { skipped++; continue; }

    if (CopyValue(fp, tp)) copied++; else skipped++;
  }
             }
    else
        {
   foreach (var el in allElements)
 {
         Parameter fp = FindParamOnInstanceOrType(doc, el, fromName);
      Parameter tp = FindParamOnInstanceOrType(doc, el, toName);

      if (fp == null || tp == null || tp.IsReadOnly) { skipped++; continue; }

 if (onlyEmpty && !IsEmpty(tp)) { skipped++; continue; }

         if (CopyValue(fp, tp)) copied++; else skipped++;
  }
      }

      trans.Commit();
  }

 TaskDialog.Show("Готово", $"Скопировано: {copied}\nПропущено: {skipped}");
            return Result.Succeeded;
      }

        // ?? Режим: копирование из родителя во вложенные субэлементы ??????????????
     private Result ExecuteNestedMode(Document doc, List<Element> allElements,
       string paramName, bool onlyEmpty)
 {
            // Проверяем, что параметр вообще существует хотя бы у одного родителя
     Parameter sampleParam = null;
            foreach (var el in allElements)
      {
    sampleParam = FindParamOnInstanceOrType(doc, el, paramName);
      if (sampleParam != null) break;
            }

  if (sampleParam == null)
         {
        TaskDialog.Show("Ошибка",
  $"Параметр «{paramName}» не найден ни у одного из родительских элементов. Операция отменена.");
       return Result.Cancelled;
      }

            int copied  = 0;
       int skipped = 0;
       int noSubs  = 0;

       using (Transaction trans = new Transaction(doc, "Копировать параметр во вложенные"))
            {
         trans.Start();

  foreach (var parent in allElements)
    {
     // Получаем значение параметра из родителя
        Parameter fp = FindParamOnInstanceOrType(doc, parent, paramName);
         if (fp == null) { skipped++; continue; }

         // Получаем субэлементы через GetSubComponentIds (FamilyInstance)
            FamilyInstance fi = parent as FamilyInstance;
                 if (fi == null) { noSubs++; continue; }

        ICollection<ElementId> subIds = fi.GetSubComponentIds();
     if (subIds == null || subIds.Count == 0) { noSubs++; continue; }

      foreach (ElementId subId in subIds)
    {
   Element sub = doc.GetElement(subId);
       if (sub == null) continue;

  Parameter tp = sub.LookupParameter(paramName);

              // Параметр не найден или заблокирован — пропускаем
       if (tp == null || tp.IsReadOnly) { skipped++; continue; }

   if (fp.StorageType != tp.StorageType) { skipped++; continue; }

            if (onlyEmpty && !IsEmpty(tp)) { skipped++; continue; }

      if (CopyValue(fp, tp)) copied++; else skipped++;
 }
             }

      trans.Commit();
   }

            TaskDialog.Show("Готово",
        $"Скопировано: {copied}\nПропущено: {skipped}\nБез субэлементов: {noSubs}");
 return Result.Succeeded;
        }

        // ?? Вспомогательные методы ?????????????????????????????????????????????????

      private static Parameter FindParam(Element el, string name)
        {
       if (el == null) return null;
   return el.LookupParameter(name);
        }

        /// <summary>
        /// Ищет параметр сначала на экземпляре, затем на его типоразмере.
     /// </summary>
private static Parameter FindParamOnInstanceOrType(Document doc, Element el, string name)
      {
            if (el == null) return null;

            Parameter p = el.LookupParameter(name);
       if (p != null) return p;

ElementId typeId = el.GetTypeId();
            if (typeId == ElementId.InvalidElementId) return null;

     Element elType = doc.GetElement(typeId);
            return elType != null ? elType.LookupParameter(name) : null;
        }

        private static bool IsEmpty(Parameter p)
        {
    if (p == null) return true;
         switch (p.StorageType)
       {
                case StorageType.String:
            return string.IsNullOrEmpty(p.AsString());
           case StorageType.ElementId:
                  return p.AsElementId() == ElementId.InvalidElementId;
      case StorageType.Integer:
  case StorageType.Double:
         return false; // числовые никогда не «пустые»
      default:
          return true;
      }
        }

        private static bool CopyValue(Parameter from, Parameter to)
        {
   if (to.IsReadOnly) return false;

            try
            {
      switch (from.StorageType)
             {
       case StorageType.String:
     to.Set(from.AsString() ?? string.Empty);
      break;
     case StorageType.Integer:
          to.Set(from.AsInteger());
      break;
   case StorageType.Double:
         to.Set(from.AsDouble());
       break;
   case StorageType.ElementId:
  to.Set(from.AsElementId());
    break;
      default:
       return false;
          }
      return true;
            }
         catch
        {
        return false;
            }
        }
    }
}
