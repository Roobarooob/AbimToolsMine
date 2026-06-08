using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AbimToolsMine
{
    [Transaction(TransactionMode.Manual)]
    public class JoinAdjacentParallelWalls : IExternalCommand
    {
        private const double ParallelTolerance = 1e-6;
        private const double TouchTolerance = 5.0 / 304.8;
        private const double MinLengthOverlap = 50.0 / 304.8;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            var progress = new ProgressWindow();
            progress.Owner = System.Windows.Application.Current?.MainWindow;
            progress.Show();
            progress.UpdateProgress("Сбор стен на активном виде...", 0, 1);

            int checkedPairs = 0;
            int joinedPairs = 0;
            int errorPairs = 0;

            try
            {
                List<Wall> walls = GetWallsOnActiveView(doc);
                int totalPairs = walls.Count * (walls.Count - 1) / 2;
                progress.UpdateProgress("Подготовка проверки пар стен...", 0, totalPairs);

                using (Transaction transaction = new Transaction(doc, "Соединение смежных параллельных стен"))
                {
                    transaction.Start();

                    for (int i = 0; i < walls.Count; i++)
                    {
                        for (int j = i + 1; j < walls.Count; j++)
                        {
                            checkedPairs++;
                            Wall firstWall = walls[i];
                            Wall secondWall = walls[j];

                            if (ShouldUpdateProgress(checkedPairs, totalPairs))
                            {
                                progress.UpdateProgress(
                                    $"Проверка и соединение стен {checkedPairs} из {totalPairs}...",
                                    checkedPairs,
                                    totalPairs);
                            }

                            try
                            {
                                if (!HaveStraightLocationLine(firstWall, out Line firstLine) ||
                                    !HaveStraightLocationLine(secondWall, out Line secondLine))
                                    continue;

                                if (firstWall.LevelId != secondWall.LevelId)
                                    continue;

                                if (!AreWallsParallel(firstLine, secondLine))
                                    continue;

                                if (JoinGeometryUtils.AreElementsJoined(doc, firstWall, secondWall))
                                    continue;

                                if (!AreWallsTouching(firstWall, secondWall, firstLine, secondLine))
                                    continue;

                                if (!HaveLengthOverlap(firstLine, secondLine))
                                    continue;

                                bool joined = TryJoinWalls(doc, firstWall, secondWall, out bool hasError);
                                if (joined)
                                    joinedPairs++;
                                else if (hasError)
                                    errorPairs++;
                            }
                            catch
                            {
                                errorPairs++;
                            }
                        }
                    }

                    progress.UpdateProgress("Завершение транзакции...", totalPairs, totalPairs);
                    transaction.Commit();
                }

                progress.Close();

                TaskDialog.Show(
                    "Соединение стен",
                    $"Найдено стен: {walls.Count}\n" +
                    $"Проверено пар: {checkedPairs}\n" +
                    $"Успешно соединено: {joinedPairs}\n" +
                    $"Пропущено из-за ошибок: {errorPairs}");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                progress.Close();
                message = ex.Message;
                TaskDialog.Show("Соединение стен", ex.Message);
                return Result.Failed;
            }
        }

        private static List<Wall> GetWallsOnActiveView(Document doc)
        {
            return new FilteredElementCollector(doc, doc.ActiveView.Id)
                .OfClass(typeof(Wall))
                .WhereElementIsNotElementType()
                .Cast<Wall>()
                .ToList();
        }

        private static bool AreWallsParallel(Line firstLine, Line secondLine)
        {
            XYZ firstDirection = GetHorizontalDirection(firstLine);
            XYZ secondDirection = GetHorizontalDirection(secondLine);

            if (firstDirection == null || secondDirection == null)
                return false;

            double dot = Math.Abs(firstDirection.DotProduct(secondDirection));
            return Math.Abs(1.0 - dot) <= ParallelTolerance;
        }

        private static bool AreWallsTouching(Wall firstWall, Wall secondWall, Line firstLine, Line secondLine)
        {
            XYZ direction = GetHorizontalDirection(firstLine);
            if (direction == null)
                return false;

            XYZ perpendicular = new XYZ(-direction.Y, direction.X, 0.0);
            XYZ offset = secondLine.GetEndPoint(0) - firstLine.GetEndPoint(0);
            double centerLineDistance = Math.Abs(offset.DotProduct(perpendicular));
            double expectedDistance = (firstWall.Width + secondWall.Width) / 2.0;

            return Math.Abs(centerLineDistance - expectedDistance) <= TouchTolerance;
        }

        private static bool HaveLengthOverlap(Line firstLine, Line secondLine)
        {
            XYZ direction = GetHorizontalDirection(firstLine);
            if (direction == null)
                return false;

            GetProjectionInterval(firstLine, direction, out double firstMin, out double firstMax);
            GetProjectionInterval(secondLine, direction, out double secondMin, out double secondMax);

            double overlap = Math.Min(firstMax, secondMax) - Math.Max(firstMin, secondMin);
            return overlap >= MinLengthOverlap;
        }

        private static bool TryJoinWalls(Document doc, Wall firstWall, Wall secondWall, out bool hasError)
        {
            hasError = false;

            try
            {
                JoinGeometryUtils.JoinGeometry(doc, firstWall, secondWall);
                return true;
            }
            catch
            {
                hasError = true;
                return false;
            }
        }

        private static bool ShouldUpdateProgress(int current, int total)
        {
            if (total <= 100)
                return true;

            int step = Math.Max(1, total / 100);
            return current == total || current % step == 0;
        }

        private static bool HaveStraightLocationLine(Wall wall, out Line line)
        {
            line = null;

            LocationCurve locationCurve = wall.Location as LocationCurve;
            if (locationCurve == null)
                return false;

            line = locationCurve.Curve as Line;
            return line != null;
        }

        private static XYZ GetHorizontalDirection(Line line)
        {
            XYZ direction = line.Direction;
            XYZ horizontalDirection = new XYZ(direction.X, direction.Y, 0.0);

            if (horizontalDirection.GetLength() <= ParallelTolerance)
                return null;

            return horizontalDirection.Normalize();
        }

        private static void GetProjectionInterval(Line line, XYZ direction, out double min, out double max)
        {
            double first = line.GetEndPoint(0).DotProduct(direction);
            double second = line.GetEndPoint(1).DotProduct(direction);

            min = Math.Min(first, second);
            max = Math.Max(first, second);
        }
    }
}
