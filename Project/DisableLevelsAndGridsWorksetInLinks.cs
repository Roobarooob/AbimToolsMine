using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AbimToolsMine
{
    [Transaction(TransactionMode.Manual)]
    public class DisableLevelsAndGridsWorksetInLinks : IExternalCommand
    {
        private class SaveCallback : ISaveSharedCoordinatesCallback
        {
            public bool SaveSharedCoordinates() { return false; }
            public SaveModifiedLinksOptions GetSaveModifiedLinksOption(RevitLinkType linkType) { return (SaveModifiedLinksOptions)2; }
        }

        public Result Execute(ExternalCommandData extCmdData, ref string msg, ElementSet elmtSet)
        {
            UIApplication uiapp = extCmdData.Application;
            UIDocument uiDoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uiDoc.Document;
            string targetWorksetName = "Общие уровни и сетки";
            List<string> successfulLinks = new List<string>();
            List<string> failedLinks = new List<string>();
            try
            {
                // Список всех типов связей
                var linkTypes = new FilteredElementCollector(doc)
                    .OfClass(typeof(RevitLinkType))
                    .Cast<RevitLinkType>();

                foreach (var linkType in linkTypes)
                {
                    // Проверка: загружен ли тип связи
                    if (!RevitLinkType.IsLoaded(doc, linkType.Id))
                    {
                        failedLinks.Add(linkType.Name);
                        continue;
                    }

                    // Получаем все экземпляры этого типа
                    var instances = new FilteredElementCollector(doc)
                        .OfClass(typeof(RevitLinkInstance))
                        .Cast<RevitLinkInstance>()
                        .Where(i => i.GetTypeId() == linkType.Id);

                    var firstInstance = instances.FirstOrDefault();
                    if (firstInstance == null)
                    {
                        failedLinks.Add(linkType.Name);
                        continue;
                    }

                    // Получаем документ связи
                    Document linkDoc = firstInstance.GetLinkDocument();
                    if (linkDoc == null)
                    {
                        failedLinks.Add(linkType.Name);
                        continue;
                    }

                    // Здесь работа с рабочими наборами
                    List<WorksetId> lstWkSet_ToOpen = new List<WorksetId>();
                    bool foundTargetWorkset = false;
                    bool targetWorksetIsOpen = false;

                    ModelPath modelPath = linkDoc.GetWorksharingCentralModelPath();
                    WorksetTable worksetTable = linkDoc.GetWorksetTable();
                    IList<WorksetPreview> lstPreview = WorksharingUtils.GetUserWorksetInfo(modelPath);

                    foreach (WorksetPreview item in lstPreview)
                    {
                        Workset wkset = worksetTable.GetWorkset(item.Id);
                        string name = wkset.Name;

                        // Ищем рабочий набор, который содержит targetWorksetName
                        if (name.Contains(targetWorksetName))
                        {
                            foundTargetWorkset = true;
                            targetWorksetIsOpen = wkset.IsOpen;
                            // Этот рабочий набор НЕ добавляем в список открытых
                        }
                        else
                        {
                            // Все остальные рабочие наборы - сохраняем их текущее состояние
                            if (wkset.IsOpen)
                            {
                                lstWkSet_ToOpen.Add(wkset.Id);
                            }
                        }
                    }

                    if (!foundTargetWorkset)
                    {
                        failedLinks.Add(linkType.Name + " (рабочий набор не найден)");
                        continue;
                    }

                    if (!targetWorksetIsOpen)
                    {
                        // Рабочий набор уже закрыт
                        successfulLinks.Add(linkType.Name + " (уже закрыт)");
                        continue;
                    }

                    // Целевой рабочий набор открыт, нужно его закрыть
                    try
                    {
                        // Закрываем все, а затем открываем только те, что были открыты (кроме целевого)
                        WorksetConfiguration wkConfig = new WorksetConfiguration(WorksetConfigurationOption.CloseAllWorksets);
                        wkConfig.Open(lstWkSet_ToOpen);
                        linkType.LoadFrom(modelPath, wkConfig);
                        successfulLinks.Add(linkType.Name);
                    }
                    catch (Exception ex)
                    {
                        failedLinks.Add(linkType.Name + " (" + ex.Message + ")");
                    }
                }
            }
            catch (Exception e)
            {
                msg = e.Message;
                return Result.Failed;
            }

            if (successfulLinks.Any() || failedLinks.Any())
            {
                string message = "";
                if (successfulLinks.Any())
                {
                    message += "Отключено в:\n" + string.Join("\n", successfulLinks) + "\n\n";
                }
                if (failedLinks.Any())
                {
                    message += "Не удалось в:\n" + string.Join("\n", failedLinks);
                }
                TaskDialog.Show("Результат отключения рабочих наборов", message);
            }

            return Result.Succeeded;
        }
    }
}