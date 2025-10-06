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
        public Result Execute(ExternalCommandData extCmdData, ref string msg, ElementSet elmtSet)
        {
            UIApplication uiapp = extCmdData.Application;
            UIDocument uiDoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uiDoc.Document;
            string targetWorksetName = "Общие уровни и сетки";

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
                        continue;

                    // Получаем все экземпляры этого типа
                    var instances = new FilteredElementCollector(doc)
                        .OfClass(typeof(RevitLinkInstance))
                        .Cast<RevitLinkInstance>()
                        .Where(i => i.GetTypeId() == linkType.Id);

                    var firstInstance = instances.FirstOrDefault();
                    if (firstInstance == null)
                        continue;

                    // Получаем документ связи
                    Document linkDoc = firstInstance.GetLinkDocument();
                    if (linkDoc == null)
                        continue;

                    // Здесь работа с рабочими наборами
                    List<WorksetId> lstWkSet_Close = new List<WorksetId>();

                    ModelPath modelPath = linkDoc.GetWorksharingCentralModelPath();
                    WorksetTable worksetTable = linkDoc.GetWorksetTable();
                    IList<WorksetPreview> lstPreview = WorksharingUtils.GetUserWorksetInfo(modelPath);

                    foreach (WorksetPreview item in lstPreview)
                    {
                        Workset wkset = worksetTable.GetWorkset(item.Id);
                        string name = wkset.Name;

                        if (!name.Contains(targetWorksetName) || !wkset.IsOpen)
                        {
                            lstWkSet_Close.Add(wkset.Id);
                        }
                    }

                    if (lstWkSet_Close.Count > 0)
                    {
                        WorksetConfiguration wkConfig = new WorksetConfiguration(WorksetConfigurationOption.CloseAllWorksets);
                        wkConfig.Open(lstWkSet_Close);
                        linkType.LoadFrom(modelPath, wkConfig);
                    }
                }
            }
            catch (Exception e)
            {
                msg = e.Message;
                return Result.Failed;
            }
            return Result.Succeeded;
        }
    }
}