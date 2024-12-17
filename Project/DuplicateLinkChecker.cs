using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;

namespace AbimToolsMine
{
    [Transaction(TransactionMode.Manual)]
    public class DuplicateLinkChecker : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Получаем доступ к текущему документу
            Document doc = commandData.Application.ActiveUIDocument.Document;

            // Словарь для подсчёта количества экземпляров связей
            Dictionary<string, int> linkCount = new Dictionary<string, int>();

            // Получаем все экземпляры связей (RevitLinkInstance)
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance));

            foreach (RevitLinkInstance linkInstance in collector.Cast<RevitLinkInstance>())
            {
                // Получаем имя связи
                string linkName = linkInstance.Name.Split(':')[0];

                if (linkCount.ContainsKey(linkName))
                {
                    linkCount[linkName]++;
                }
                else
                {
                    linkCount[linkName] = 1;
                }
            }

            // Формируем сообщение с результатами
            string resultMessage = "Проверка связей:\n";
            bool duplicatesFound = false;

            foreach (var link in linkCount)
            {
                if (link.Value > 1)
                {
                    duplicatesFound = true;
                    resultMessage += $"{link.Key}, Количество: {link.Value}\n";
                }
            }

            if (!duplicatesFound)
            {
                resultMessage += "Дубликаты связей не найдены.";
            }

            // Выводим сообщение пользователю
            TaskDialog.Show("Результат проверки", resultMessage);

            return Result.Succeeded;
        }
    }

}