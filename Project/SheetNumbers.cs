using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Linq;

namespace AbimToolsMine
{
    [Transaction(TransactionMode.Manual)]
    public class SheetNumbers : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Имя категории
                
                // Имена параметров
                string sourceParamName = "Номер листа";
                string targetParamName = "ПРО_Номер листа";

                // Фильтрация всех листов
                var sheets = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Sheets)
                    .WhereElementIsNotElementType()
                    .ToList();

                if (sheets.Count == 0)
                {
                    TaskDialog.Show("Результат", $"Не найдено Листов");
                    return Result.Cancelled;
                }

                using (Transaction t = new Transaction(doc, "Коррекция номеров листов"))
                {
                    t.Start();

                    foreach (var sheet in sheets)
                    {
                        // Получаем исходное значение
                        string value = sheet.LookupParameter(sourceParamName)?.AsString();

                        if (!string.IsNullOrEmpty(value))
                        {
                            // Заменяем "^" на пустую строку
                            string newValue = value.Replace("�", "");

                            // Записываем в целевой параметр
                            var param = sheet.LookupParameter(targetParamName);
                            if (param != null && !param.IsReadOnly)
                            {
                                param.Set(newValue);
                            }
                        }
                    }

                    t.Commit();
                }

                TaskDialog.Show("Готово", $"Обработано листов: {sheets.Count}");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}