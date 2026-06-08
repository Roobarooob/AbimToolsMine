using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AbimToolsMine
{
    [Transaction(TransactionMode.Manual)]
    public class AlignBySheets : IExternalCommand
    {
        private const double Epsilon = 1e-9;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                var sheets = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewSheet))
                    .Cast<ViewSheet>()
                    .Where(s => !s.IsPlaceholder)
                    .OrderBy(s => s.SheetNumber)
                    .ToList();

                if (sheets.Count == 0)
                {
                    TaskDialog.Show("Выравнивание по листам", "В проекте не найдены листы.");
                    return Result.Cancelled;
                }

                var window = new AlignBySheetsWindow(sheets);
                bool? dialogResult = window.ShowDialog();
                if (dialogResult != true)
                    return Result.Cancelled;

                var selectedSheets = window.SelectedSheets;
                ViewSheet referenceSheet = window.ReferenceSheet;
                string firstGridName = window.FirstGridName;
                string secondGridName = window.SecondGridName;
                var viewTypes = window.SelectedViewTypes;

                if (selectedSheets.Count == 0 || referenceSheet == null || viewTypes.Count == 0)
                {
                    TaskDialog.Show("Выравнивание по листам", "Не выбраны листы, эталонный лист или типы видов.");
                    return Result.Cancelled;
                }

                var warnings = new List<string>();
                var referenceTargets = new Dictionary<ViewType, ReferenceTarget>();

                foreach (ViewType viewType in viewTypes)
                {
                    if (!TryCreateReferenceTarget(doc, referenceSheet, viewType, firstGridName, secondGridName, warnings, out ReferenceTarget target))
                    {
                        ShowReport(0, warnings);
                        return Result.Cancelled;
                    }

                    referenceTargets[viewType] = target;
                }

                int moved = 0;
                using (Transaction t = new Transaction(doc, "Выравнивание видов по листам"))
                {
                    t.Start();

                    foreach (ViewSheet sheet in selectedSheets)
                    {
                        foreach (ViewType viewType in viewTypes)
                        {
                            ReferenceTarget reference = referenceTargets[viewType];
                            if (!TryFindSingleViewport(doc, sheet, viewType, warnings, out Viewport viewport, out View view))
                                continue;

                            if (view.Scale != reference.View.Scale)
                            {
                                warnings.Add($"{SheetLabel(sheet)}: вид \"{view.Name}\" пропущен, масштаб 1:{view.Scale} отличается от эталона 1:{reference.View.Scale}.");
                                continue;
                            }

                            if (!TryGetGridIntersection(doc, view, firstGridName, secondGridName, out XYZ intersection, out string gridWarning))
                            {
                                warnings.Add($"{SheetLabel(sheet)}: вид \"{view.Name}\" пропущен. {gridWarning}");
                                continue;
                            }

                            XYZ currentSheetPoint = ModelPointToSheetPoint(viewport, view, intersection);
                            XYZ delta = reference.SheetPoint - currentSheetPoint;

                            if (delta.GetLength() <= Epsilon)
                                continue;

                            viewport.SetBoxCenter(viewport.GetBoxCenter() + delta);
                            moved++;
                        }
                    }

                    t.Commit();
                }

                ShowReport(moved, warnings);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Выравнивание по листам", ex.Message);
                return Result.Failed;
            }
        }

        private static bool TryCreateReferenceTarget(
            Document doc,
            ViewSheet referenceSheet,
            ViewType viewType,
            string firstGridName,
            string secondGridName,
            List<string> warnings,
            out ReferenceTarget target)
        {
            target = null;

            if (!TryFindSingleViewport(doc, referenceSheet, viewType, warnings, out Viewport viewport, out View view))
                return false;

            if (!TryGetGridIntersection(doc, view, firstGridName, secondGridName, out XYZ intersection, out string gridWarning))
            {
                warnings.Add($"{SheetLabel(referenceSheet)}: эталонный вид \"{view.Name}\" не подходит. {gridWarning}");
                return false;
            }

            target = new ReferenceTarget
            {
                Viewport = viewport,
                View = view,
                SheetPoint = ModelPointToSheetPoint(viewport, view, intersection)
            };

            return true;
        }

        private static bool TryFindSingleViewport(
            Document doc,
            ViewSheet sheet,
            ViewType viewType,
            List<string> warnings,
            out Viewport viewport,
            out View view)
        {
            viewport = null;
            view = null;

            var pairs = sheet.GetAllViewports()
                .Select(id => doc.GetElement(id) as Viewport)
                .Where(vp => vp != null)
                .Select(vp => new
                {
                    Viewport = vp,
                    View = doc.GetElement(vp.ViewId) as View
                })
                .Where(x => x.View != null && x.View.ViewType == viewType)
                .ToList();

            if (pairs.Count == 0)
            {
                warnings.Add($"{SheetLabel(sheet)}: не найден {ViewTypeLabel(viewType)}.");
                return false;
            }

            if (pairs.Count > 1)
            {
                warnings.Add($"{SheetLabel(sheet)}: найдено несколько видов \"{ViewTypeLabel(viewType)}\". Лист пропущен, чтобы не сдвинуть лишний viewport.");
                return false;
            }

            viewport = pairs[0].Viewport;
            view = pairs[0].View;
            return true;
        }

        private static bool TryGetGridIntersection(
            Document doc,
            View view,
            string firstGridName,
            string secondGridName,
            out XYZ intersection,
            out string warning)
        {
            intersection = null;
            warning = null;

            var grids = new FilteredElementCollector(doc, view.Id)
                .OfClass(typeof(Grid))
                .Cast<Grid>()
                .ToList();

            Grid firstGrid = grids.FirstOrDefault(g => string.Equals(g.Name, firstGridName, StringComparison.OrdinalIgnoreCase));
            Grid secondGrid = grids.FirstOrDefault(g => string.Equals(g.Name, secondGridName, StringComparison.OrdinalIgnoreCase));

            if (firstGrid == null || secondGrid == null)
            {
                warning = $"Не найдены оси \"{firstGridName}\" и/или \"{secondGridName}\".";
                return false;
            }

            if (!TryIntersectCurves(firstGrid.Curve, secondGrid.Curve, out intersection))
            {
                warning = $"Оси \"{firstGridName}\" и \"{secondGridName}\" не пересекаются в этом виде.";
                return false;
            }

            return true;
        }

        private static bool TryIntersectCurves(Curve first, Curve second, out XYZ intersection)
        {
            intersection = null;

            Curve c1 = ToUnboundLine(first) ?? first;
            Curve c2 = ToUnboundLine(second) ?? second;

            SetComparisonResult result = c1.Intersect(c2, out IntersectionResultArray results);
            if (results != null && results.Size > 0)
            {
                intersection = results.get_Item(0).XYZPoint;
                return intersection != null;
            }

            return result == SetComparisonResult.Overlap && intersection != null;
        }

        private static Curve ToUnboundLine(Curve curve)
        {
            Line line = curve as Line;
            if (line == null)
                return null;

            return Line.CreateUnbound(line.Origin, line.Direction);
        }

        private static XYZ ModelPointToSheetPoint(Viewport viewport, View view, XYZ modelPoint)
        {
            UV paperPoint = ModelPointToViewPaperPoint(view, modelPoint);
            BoundingBoxUV outline = view.Outline;
            UV outlineCenter = new UV(
                (outline.Min.U + outline.Max.U) / 2.0,
                (outline.Min.V + outline.Max.V) / 2.0);

            XYZ offset = new XYZ(paperPoint.U - outlineCenter.U, paperPoint.V - outlineCenter.V, 0);
            return viewport.GetBoxCenter() + offset;
        }

        private static UV ModelPointToViewPaperPoint(View view, XYZ modelPoint)
        {
            double scale = view.Scale;
            XYZ right = view.RightDirection;
            XYZ up = view.UpDirection;

            return new UV(
                modelPoint.DotProduct(right) / scale,
                modelPoint.DotProduct(up) / scale);
        }

        private static void ShowReport(int moved, List<string> warnings)
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine($"Перемещено viewport: {moved}");

            if (warnings.Count > 0)
            {
                report.AppendLine();
                report.AppendLine("Предупреждения:");
                foreach (string warning in warnings.Take(25))
                    report.AppendLine("- " + warning);

                if (warnings.Count > 25)
                    report.AppendLine($"...и ещё {warnings.Count - 25}");
            }

            TaskDialog.Show("Выравнивание по листам", report.ToString());
        }

        private static string SheetLabel(ViewSheet sheet)
        {
            return $"{sheet.SheetNumber} - {sheet.Name}";
        }

        private static string ViewTypeLabel(ViewType viewType)
        {
            if (viewType == ViewType.FloorPlan)
                return "план этажа";

            if (viewType == ViewType.CeilingPlan)
                return "план потолка";

            return viewType.ToString();
        }

        private class ReferenceTarget
        {
            public Viewport Viewport { get; set; }
            public View View { get; set; }
            public XYZ SheetPoint { get; set; }
        }
    }
}
