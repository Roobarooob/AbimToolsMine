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
            string targetWorksetName = "����� ������ � �����";

            try
            {
                // ������ ���� ����� ������
                var linkTypes = new FilteredElementCollector(doc)
                    .OfClass(typeof(RevitLinkType))
                    .Cast<RevitLinkType>();

                foreach (var linkType in linkTypes)
                {
                    // ��������: �������� �� ��� �����
                    if (!RevitLinkType.IsLoaded(doc, linkType.Id))
                        continue;

                    // �������� ��� ���������� ����� ����
                    var instances = new FilteredElementCollector(doc)
                        .OfClass(typeof(RevitLinkInstance))
                        .Cast<RevitLinkInstance>()
                        .Where(i => i.GetTypeId() == linkType.Id);

                    var firstInstance = instances.FirstOrDefault();
                    if (firstInstance == null)
                        continue;

                    // �������� �������� �����
                    Document linkDoc = firstInstance.GetLinkDocument();
                    if (linkDoc == null)
                        continue;

                    // ����� ������ � �������� ��������
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