using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using Settings = AbimToolsMine.Properties.Settings;

namespace AbimToolsMine
{
    [Transaction(TransactionMode.Manual)]

    public class LinksWokset : IExternalCommand
    {
        public static ExternalCommandData CommandData { get; set; }
        public static WorksetWin window = null;
        private static string LinkWoksetSymbol => Settings.Default.LinkPrefix;
        private static string DWGWorksetName => Settings.Default.DWGWorksetName;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            CommandData = commandData;
            if (window == null)
            {
                window = new WorksetWin();
                window.ShowDialog();
            }
            else
            {
                window.Activate();
            }
            window = null;
            return Result.Succeeded;
        }

        public static void LinksToWorksets(ExternalCommandData commandData, string prefix)
        {
            List<string> razdelOptions = new List<string>
            {
                "_АР", "_КР", "_КЖ", "_КМ", "_ОВ", "_ВК", "_ЭОМ", "_СС", "_ТМ", "_РФ", "_АК", "_НТС", "_ТХ",
                "_AR", "_KR", "_KJ", "_KM", "_OV", "_VK", "_EOM", "_SS", "_TM", "_BF", "_AK", "_HTS", "_TH",
            };
            // Словарь для соответствия разделов и префиксов
            Dictionary<string, string> specialPrefixMap = new Dictionary<string, string>
            {
                { "_КР", "КР" }, { "_КЖ", "КР" }, { "_КМ", "КР" },
                { "_KR", "KR" }, { "_KJ", "KR" }, { "_KM", "KR" }
            };

            {
                UIApplication uiapp = commandData.Application;
                Document doc = uiapp.ActiveUIDocument.Document;

                // Получаем все RVT связи
                var rvtLinks = new FilteredElementCollector(doc)
                    .OfClass(typeof(RevitLinkType))
                    .Cast<RevitLinkType>();
                var cadLinks = new FilteredElementCollector(doc)
                      .OfClass(typeof(ImportInstance))
                      .Cast<ImportInstance>()
                      .Where(x => x.IsLinked); // Только связанные DWG

                using (Transaction tx = new Transaction(doc, "Назначить рабочие наборы связям"))
                {
                    tx.Start();

                    foreach (var linkType in rvtLinks)
                    {
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(linkType.Name);
                        string razdel = GetRazdelFromFileName(fileName);
                        string workSetName = $"{LinkWoksetSymbol}{razdel}_{fileName}";

                        Workset workset = GetOrCreateWorkset(doc, workSetName);

                        // Назначаем рабочий набор экземплярам и типу связи
                        AssignWorksetToLinkInstances(doc, linkType, workset);
                    }
                    // CAD связи
                    string cadWorksetName = DWGWorksetName;
                    Workset cadWorkset = GetOrCreateWorkset(doc, cadWorksetName);

                    foreach (var cad in cadLinks)
                    {
                        Parameter wsParam = cad.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM);
                        Parameter typeParam = doc.GetElement(cad.GetTypeId()).get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM);
                        if (wsParam != null && !wsParam.IsReadOnly)
                        {
                            wsParam.Set(cadWorkset.Id.IntegerValue);
                            typeParam.Set(cadWorkset.Id.IntegerValue);
                        }
                    }

                    tx.Commit();
                }
            }
            // Метод для определения раздела и префикса
            string GetRazdelFromFileName(string fileName)
            {
                // Ищем раздел в имени файла
                foreach (string option in razdelOptions)
                {
                    if (fileName.Contains(option))
                    {
                        // Если раздел есть в specialPrefixMap, используем его префикс
                        if (specialPrefixMap.ContainsKey(option))
                        {
                            return specialPrefixMap[option];
                        }
                        // Иначе возвращаем сам раздел без подчеркивания (например, "_АР" -> "АР")
                        return option.Replace("_", string.Empty);
                    }
                }
                return "ХЗ"; // Если раздел не найден, возвращаем "ХЗ"
            }

            Workset GetOrCreateWorkset(Document doc, string workSetName)
            {

                Workset workset = new FilteredWorksetCollector(doc)
                    .OfKind(WorksetKind.UserWorkset)
                    .FirstOrDefault(w => w.Name.Equals(workSetName, StringComparison.OrdinalIgnoreCase));

                if (workset == null)
                {
                    Workset.Create(doc, workSetName);
                    workset = new FilteredWorksetCollector(doc)
                        .OfKind(WorksetKind.UserWorkset)
                        .FirstOrDefault(w => w.Name.Equals(workSetName, StringComparison.OrdinalIgnoreCase));
                }
                return workset;
            }

            void AssignWorksetToLinkInstances(Document doc, RevitLinkType linkType, Workset workset)
            {
                linkType.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM).Set(workset.Id.IntegerValue);

                var linkInstances = new FilteredElementCollector(doc)
                    .OfClass(typeof(RevitLinkInstance))
                    .WhereElementIsNotElementType()
                    .Cast<RevitLinkInstance>()
                    .Where(i => i.GetTypeId() == linkType.Id);

                foreach (var instance in linkInstances)
                {
                    try
                    {
                        instance.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM).Set(workset.Id.IntegerValue);
                    }
                    catch
                    {
                        //Вложенности и занятости
                    }
                }
            }
        }
        public static void RenameUnusedWorksets(ExternalCommandData commandData, string prefix)
        {
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;
            using (Transaction tx = new Transaction(doc, "Фильтр пустых наборов"))
            {
                tx.Start();

                // Получаем все рабочие наборы с символом "#" в названии
                var worksetsToCheck = new FilteredWorksetCollector(doc)
                    .OfKind(WorksetKind.UserWorkset)
                    .Where(w => w.Name.Contains(prefix) && !w.Name.Contains("Арматура"))
                    .ToList();

                foreach (var workset in worksetsToCheck)
                {
                    bool isEmpty = !IsWorksetUsed(workset.Id);

                    // Если рабочий набор пуст
                    if (isEmpty)
                    {
                        // Переименование для версий 2022 и ниже
                        string newName = $"!Удалить!_{workset.Name}";
                        WorksetTable.RenameWorkset(doc, workset.Id, newName);
                    }
                }

                tx.Commit();

            }

            bool IsWorksetUsed(WorksetId worksetId)
            {
                // Проверка наличия элементов в рабочем наборе
                var collector = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .Where(e => e.WorksetId == worksetId);

                return collector.Any();
            }
        }
        public static void DeleteUnusedWorksets(ExternalCommandData commandData, string prefix)
        {
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;


            using (Transaction tx = new Transaction(doc, "Фильтр пустых наборов"))
            {
                tx.Start();

                // Получаем все рабочие наборы с символом "#" в названии
                var worksetsToCheck = new FilteredWorksetCollector(doc)
                    .OfKind(WorksetKind.UserWorkset)
                    .Where(w => w.Name.Contains(prefix))
                    .ToList();

                foreach (var workset in worksetsToCheck)
                {
                    bool isEmpty = !IsWorksetUsed(workset.Id);

                    // Если рабочий набор пуст
                    if (isEmpty)
                    {
#if R2023 || R2024 || R2025
                        try
                        {
                            DeleteWorksetSettings deleteSettings = new DeleteWorksetSettings();
                            // Удаление для версий 2023 и выше
                            WorksetTable.DeleteWorkset(doc, workset.Id, deleteSettings);
                        }
                        catch
                        {
                            //что то не так с ворксетом
                        }
#endif
                    }
                }

                tx.Commit();
            }

            bool IsWorksetUsed(WorksetId worksetId)
            {
                // Проверка наличия элементов в рабочем наборе
                var collector = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .Where(e => e.WorksetId == worksetId);

                return collector.Any();
            }
        }


    }
    public static class Extensions
    {
        public static long GetValue(this ElementId elementId)
        {
#if R2017 || R2018 || R2019 || R2020 || R2021 || R2022 || R2023
            return elementId.IntegerValue;
#else
            return elementId.Value;
#endif
        }
    }
}