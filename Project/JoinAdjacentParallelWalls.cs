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
        private const double PerpendicularTolerance = 0.001;
        private const double TouchTolerance = 5.0 / 304.8;
        private const double EndTouchTolerance = 20.0 / 304.8;
        private const double MinLengthOverlap = 50.0 / 304.8;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            var progress = new ProgressWindow();
            RevitWindowOwner.SetOwner(progress, commandData.Application);
            progress.Show();
            progress.UpdateProgress("Сбор стен на активном виде...", 0, 1);

            int checkedPairs = 0;
            int joinedPairs = 0;
            int joinedParallelPairs = 0;
            int joinedPerpendicularPairs = 0;
            int errorPairs = 0;

            try
            {
                List<Wall> walls = GetWallsOnActiveView(doc);
                int totalPairs = walls.Count * (walls.Count - 1) / 2;
                progress.UpdateProgress("Подготовка проверки пар стен...", 0, totalPairs);

                using (Transaction transaction = new Transaction(doc, "Соединение смежных стен"))
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

                                if (JoinGeometryUtils.AreElementsJoined(doc, firstWall, secondWall))
                                    continue;

                                bool isParallelTouching = AreWallsParallelTouching(firstWall, secondWall, firstLine, secondLine);
                                bool isPerpendicularTouching = !isParallelTouching &&
                                    AreWallsPerpendicularTouching(firstWall, secondWall, firstLine, secondLine);

                                bool shouldJoin = isParallelTouching || isPerpendicularTouching;
                                if (!shouldJoin)
                                    continue;

                                bool joined = TryJoinWalls(doc, firstWall, secondWall, out bool hasError);
                                if (joined)
                                {
                                    joinedPairs++;

                                    if (isParallelTouching)
                                        joinedParallelPairs++;
                                    else if (isPerpendicularTouching)
                                        joinedPerpendicularPairs++;
                                }
                                else if (hasError)
                                {
                                    errorPairs++;
                                }
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
                    $"Соединено параллельных пар: {joinedParallelPairs}\n" +
                    $"Соединено перпендикулярных пар: {joinedPerpendicularPairs}\n" +
                    $"Всего соединено: {joinedPairs}\n" +
                    $"Ошибок: {errorPairs}");

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

        private static bool AreWallsParallelTouching(Wall firstWall, Wall secondWall, Line firstLine, Line secondLine)
        {
            return AreWallsParallel(firstLine, secondLine) &&
                AreWallsTouching(firstWall, secondWall, firstLine, secondLine) &&
                HaveLengthOverlap(firstLine, secondLine);
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

        private static bool AreWallsPerpendicularTouching(Wall firstWall, Wall secondWall, Line firstLine, Line secondLine)
        {
            XYZ firstDirection = GetHorizontalDirection(firstLine);
            XYZ secondDirection = GetHorizontalDirection(secondLine);

            if (firstDirection == null || secondDirection == null)
                return false;

            if (Math.Abs(firstDirection.DotProduct(secondDirection)) >= PerpendicularTolerance)
                return false;

            return HasAnyEndCapTouchingWallBody(firstLine, firstWall.Width, secondLine, secondWall.Width) ||
                HasAnyEndCapTouchingWallBody(secondLine, secondWall.Width, firstLine, firstWall.Width);
        }

        private static bool HasAnyEndCapTouchingWallBody(
            Line sourceLine,
            double sourceWallWidth,
            Line targetLine,
            double targetWallWidth)
        {
            XYZ sourceDirection = GetHorizontalDirection(sourceLine);
            XYZ targetDirection = GetHorizontalDirection(targetLine);
            if (sourceDirection == null || targetDirection == null)
                return false;

            XYZ sourcePerpendicular = new XYZ(-sourceDirection.Y, sourceDirection.X, 0.0);
            XYZ targetPerpendicular = new XYZ(-targetDirection.Y, targetDirection.X, 0.0);
            double halfSourceWallWidth = sourceWallWidth / 2.0;
            double halfTargetWallWidth = targetWallWidth / 2.0;

            GetProjectionInterval(targetLine, targetDirection, out double targetMin, out double targetMax);

            foreach (XYZ endPoint in GetLineEndPoints(sourceLine))
            {
                if (IsEndCapTouchingWallBody(
                    endPoint,
                    sourcePerpendicular,
                    halfSourceWallWidth,
                    targetLine,
                    targetDirection,
                    targetPerpendicular,
                    targetMin,
                    targetMax,
                    halfTargetWallWidth))
                    return true;
            }

            return false;
        }

        private static bool IsEndCapTouchingWallBody(
            XYZ endPoint,
            XYZ sourcePerpendicular,
            double halfSourceWallWidth,
            Line targetLine,
            XYZ targetDirection,
            XYZ targetPerpendicular,
            double targetMin,
            double targetMax,
            double halfTargetWallWidth)
        {
            XYZ firstCapPoint = endPoint + sourcePerpendicular * halfSourceWallWidth;
            XYZ secondCapPoint = endPoint - sourcePerpendicular * halfSourceWallWidth;
            XYZ targetOffset = endPoint - targetLine.GetEndPoint(0);
            double signedDistanceToCenterLine = targetOffset.DotProduct(targetPerpendicular);
            double distanceToCenterLine = Math.Abs(signedDistanceToCenterLine);

            if (distanceToCenterLine > halfTargetWallWidth + EndTouchTolerance)
                return false;

            GetPointProjectionInterval(firstCapPoint, secondCapPoint, targetDirection, out double capMin, out double capMax);
            if (!IntervalsTouchOrOverlap(capMin, capMax, targetMin, targetMax, EndTouchTolerance))
                return false;

            GetPointProjectionInterval(firstCapPoint, secondCapPoint, targetPerpendicular, out double capSideMin, out double capSideMax);
            double targetSide = targetLine.GetEndPoint(0).DotProduct(targetPerpendicular);
            double targetSideMin = targetSide - halfTargetWallWidth;
            double targetSideMax = targetSide + halfTargetWallWidth;

            return IntervalsTouchOrOverlap(capSideMin, capSideMax, targetSideMin, targetSideMax, EndTouchTolerance);
        }

        private static IEnumerable<XYZ> GetLineEndPoints(Line line)
        {
            yield return line.GetEndPoint(0);
            yield return line.GetEndPoint(1);
        }

        private static void GetPointProjectionInterval(XYZ firstPoint, XYZ secondPoint, XYZ direction, out double min, out double max)
        {
            double first = firstPoint.DotProduct(direction);
            double second = secondPoint.DotProduct(direction);

            min = Math.Min(first, second);
            max = Math.Max(first, second);
        }

        private static bool IntervalsTouchOrOverlap(double firstMin, double firstMax, double secondMin, double secondMax, double tolerance)
        {
            return Math.Min(firstMax, secondMax) >= Math.Max(firstMin, secondMin) - tolerance;
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
