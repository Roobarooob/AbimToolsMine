using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;

namespace AbimToolsMine
{
    [Transaction(TransactionMode.Manual)]
    public class FormAssignCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            var win = new FormAssignWin(doc);
            win.ShowDialog();

            if (!win.Confirmed)
                return Result.Cancelled;

            Element formElement = win.SelectedFormElement;
            RevitLinkInstance linkInstance = win.SelectedLinkInstance;
            string formParamName = win.FormParam;

            // --- Read the parameter value from the form element ---
            Parameter formParam = formElement.LookupParameter(formParamName);
            string value = null;

            if (formParam != null)
            {
                value = formParam.StorageType == StorageType.String
               ? formParam.AsString()
                   : formParam.AsValueString();
            }

            if (value == null)
            {
                TaskDialog.Show("Ошибка", $"Не удалось получить значение параметра '{formParamName}' из формообразующего.");
                return Result.Failed;
            }

            // --- Get solids of the form, apply link transform if needed ---
            List<Solid> formSolids = GetSolids(formElement);

            if (linkInstance != null)
            {
                Transform transform = linkInstance.GetTotalTransform();
                var transformed = new List<Solid>();
                foreach (var s in formSolids)
                {
                    try { transformed.Add(SolidUtils.CreateTransformed(s, transform)); }
                    catch { }
                }
                formSolids = transformed;
            }

            if (formSolids.Count == 0)
            {
                TaskDialog.Show("Ошибка", "Не найдена геометрия (solids) формообразующего элемента.");
                return Result.Failed;
            }

            // --- Collect all non-annotation elements that have volume ---
            List<Element> candidates = CollectVolumetricElements(doc);

            // --- Write parameter to intersecting elements ---
            int matched = 0;

            using (Transaction t = new Transaction(doc, "Запись по формообразующему"))
            {
                t.Start();

                foreach (Element el in candidates)
                {
                    List<Solid> elSolids = GetSolids(el);
                    if (elSolids.Count == 0) continue;

                    if (Intersects(formSolids, elSolids))
                    {
                        Parameter p = el.LookupParameter(formParamName);
                        if (p != null && !p.IsReadOnly && value != null)
                        {
                            try
                            {
                                if (p.StorageType == StorageType.String)
                                    p.Set(value);
                                else
                                    p.SetValueString(value);
                                matched++;
                            }
                            catch { }
                        }
                    }
                }

                t.Commit();
            }

            TaskDialog.Show("Готово", $"Обработано элементов: {matched}");
            return Result.Succeeded;
        }

        // ------------------------------------------------------------------
        // Collect all model (non-annotation, volumetric) element instances
        // ------------------------------------------------------------------
        private static List<Element> CollectVolumetricElements(Document doc)
        {
            var result = new List<Element>();

            // Use a broad collector and filter by category type
            var allElements = new FilteredElementCollector(doc)
               .WhereElementIsNotElementType()
                     .ToElements();

            foreach (Element el in allElements)
            {
                Category cat = el.Category;
                if (cat == null) continue;

                // Skip annotation, view-based and internal categories
                if (cat.CategoryType != CategoryType.Model) continue;

                // Skip categories known to be non-volumetric (rooms, spaces, areas, levels, grids, etc.)
#if R2026
                BuiltInCategory bic = (BuiltInCategory)(int)cat.Id.Value;
#else
                BuiltInCategory bic = (BuiltInCategory)cat.Id.IntegerValue;
#endif
                if (IsNonVolumetricCategory(bic)) continue;

                result.Add(el);
            }

            return result;
        }

        private static readonly HashSet<BuiltInCategory> _nonVolumetricCategories = new HashSet<BuiltInCategory>
        {
     BuiltInCategory.OST_Rooms,
            BuiltInCategory.OST_Areas,
      BuiltInCategory.OST_MEPSpaces,
    BuiltInCategory.OST_Levels,
            BuiltInCategory.OST_Grids,
     BuiltInCategory.OST_Views,
    BuiltInCategory.OST_Sheets,
   BuiltInCategory.OST_Cameras,
            BuiltInCategory.OST_SectionBox,
        BuiltInCategory.OST_RvtLinks,
            BuiltInCategory.OST_IOS_GeoLocations,
        };

        private static bool IsNonVolumetricCategory(BuiltInCategory bic)
        {
            return _nonVolumetricCategories.Contains(bic);
        }

        // ------------------------------------------------------------------
        // Extract solids from an element's geometry
        // ------------------------------------------------------------------
        private static List<Solid> GetSolids(Element element)
        {
            var solids = new List<Solid>();

            var opt = new Options { DetailLevel = ViewDetailLevel.Fine };
            GeometryElement geo;
            try { geo = element.get_Geometry(opt); }
            catch { return solids; }

            if (geo == null) return solids;

            foreach (GeometryObject g in geo)
            {
                if (g is Solid s && s.Volume > 1e-9)
                {
                    solids.Add(s);
                }
                else if (g is GeometryInstance gi)
                {
                    foreach (GeometryObject ig in gi.GetInstanceGeometry())
                    {
                        if (ig is Solid ss && ss.Volume > 1e-9)
                            solids.Add(ss);
                    }
                }
            }

            return solids;
        }

        // ------------------------------------------------------------------
        // Boolean intersection check between two solid lists
        // ------------------------------------------------------------------
        private static bool Intersects(List<Solid> solids1, List<Solid> solids2)
        {
            foreach (Solid s1 in solids1)
            {
                foreach (Solid s2 in solids2)
                {
                    try
                    {
                        Solid inter = BooleanOperationsUtils.ExecuteBooleanOperation(
                  s1, s2, BooleanOperationsType.Intersect);
                        if (inter != null && inter.Volume > 1e-9)
                            return true;
                    }
                    catch { }
                }
            }
            return false;
        }
    }
}
