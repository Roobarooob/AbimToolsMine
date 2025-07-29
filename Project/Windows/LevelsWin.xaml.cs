using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using Autodesk.Revit.Attributes;
using System.Text.RegularExpressions;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using System.Data.Common;


namespace AbimToolsMine
{
    /// <summary>
    /// Логика взаимодействия для UserControl1.xaml
    /// </summary>
    public partial class LevelsWin : Window
    {
        public LevelsWin()
        {
            InitializeComponent();
        }
        private void WinClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            LevelTools.win = null;
        }

        private void GetLevelLinks(object sender, RoutedEventArgs e)
        {
            LevelTools.CheckLevels(LevelTools.cData);
        }

        private void SetToParameter(object sender, RoutedEventArgs e)
        {
            LevelTools.SetParameter(LevelTools.cData);
        }
    }



    [Transaction(TransactionMode.Manual)]
    public class LevelTools : IExternalCommand
    {
        public static LevelsWin win = null;
        public static ExternalCommandData cData = null;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            cData = commandData;
            win = new LevelsWin();
            win.ShowDialog();
            return Result.Succeeded;
        }

        public static void CheckLevels(ExternalCommandData commandData)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Получаем все элементы, которые нужно проверить
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            IList<Element> elementsToCheck = collector
                .WherePasses(new LogicalOrFilter(new List<ElementFilter>
                {
            new ElementClassFilter(typeof(FamilyInstance)),
            new ElementClassFilter(typeof(Wall)),
            new ElementClassFilter(typeof(Floor)),
            new ElementClassFilter(typeof(Ceiling)),
            new ElementClassFilter(typeof(Pipe)),
            new ElementClassFilter(typeof(Duct)),
            new ElementClassFilter(typeof(CableTray))
                }))
                .ToElements();

            // Получаем все уровни в проекте
            IList<Level> levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(l => l.Elevation).ToList();

            using (Transaction trans = new Transaction(doc, "Проверка привязок"))
            {
                trans.Start();

                foreach (Element element in elementsToCheck)
                {
                    XYZ insertionPoint = null;

                    // Определяем точку вставки элемента в зависимости от его типа
                    if (element.Location is LocationPoint locationPoint)
                    {
                        insertionPoint = locationPoint.Point;
                    }
                    else if (element.Location is LocationCurve locationCurve)
                    {
                        insertionPoint = locationCurve.Curve.GetEndPoint(0);
                    }
                    else if (element is Wall wall)
                    {
                        insertionPoint = (wall.Location as LocationCurve)?.Curve.GetEndPoint(0);
                    }
                    else if (element is Floor floor)
                    {
                        // Для перекрытия используем центр масс
                        Options geomOptions = new Options();
                        GeometryElement geomElem = floor.get_Geometry(geomOptions);
                        if (geomElem != null)
                        {
                            foreach (GeometryObject geomObj in geomElem)
                            {
                                if (geomObj is Solid solid)
                                {
                                    insertionPoint = solid.ComputeCentroid();
                                    break;
                                }
                            }
                        }
                    }
                    else if (element is Ceiling ceiling)
                    {
                        // Для перекрытия используем центр масс
                        Options geomOptions = new Options();
                        GeometryElement geomElem = ceiling.get_Geometry(geomOptions);
                        if (geomElem != null)
                        {
                            foreach (GeometryObject geomObj in geomElem)
                            {
                                if (geomObj is Solid solid)
                                {
                                    insertionPoint = solid.ComputeCentroid();
                                    break;
                                }
                            }
                        }
                    }
                    else if (element is MEPCurve mepCurve)
                    {
                        insertionPoint = (mepCurve.Location as LocationCurve)?.Curve.GetEndPoint(0);
                    }

                    if (insertionPoint == null)
                        continue;

                    // Находим уровни между которыми находится точка вставки
                    Level lowerLevel = null;
                    Level upperLevel = null;

                    for (int i = 0; i < levels.Count; i++)
                    {
                        if (levels[i].Elevation > insertionPoint.Z)
                        {
                            upperLevel = levels[i];
                            lowerLevel = i > 0 ? levels[i - 1] : levels[i];
                            break;
                        }
                    }

                    if (lowerLevel == null)
                    {
                        lowerLevel = levels.Last();
                    }

                    // Получаем уровень, к которому привязан элемент
                    ElementId genLevelId = null;
                    if (element is FamilyInstance familyInstance)
                    {
                        genLevelId = familyInstance.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM).AsElementId();
                    }
                    else if (element is Wall wall)
                    {
                        genLevelId = wall.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT).AsElementId();
                    }
                    /*else if (element is Floor floor)
                    {
                        genLevelId = floor.get_Parameter(BuiltInParameter.LEVEL_PARAM).AsElementId();
                    }*/
                    else if (element is Ceiling ceiling)
                    {
                        genLevelId = ceiling.get_Parameter(BuiltInParameter.LEVEL_PARAM).AsElementId();
                    }
                    else if (element is MEPCurve mepCurve)
                    {
                        genLevelId = mepCurve.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM).AsElementId();
                    }

                    Level genLevel = doc.GetElement(genLevelId) as Level;
                    Parameter par = null;
                    try
                    {
                        par = element.LookupParameter("ПРО_Этаж");
                    }
                    catch
                    {
                        continue;
                    }

                    // Проверяем соответствие уровней
                    if (par != null && genLevel != null && genLevel.Id == lowerLevel.Id)
                    {
                        // Записываем имя уровня в параметр "ПРО_Этаж"
                        element.LookupParameter("ПРО_Этаж").Set(ExtractLastPart(genLevel.Name));
                    }
                    else
                    {
                        try
                        {
                            if (par != null)
                            {
                                // Записываем сообщение об ошибке
                                par.Set("Ошибка, проверьте привязку уровня");
                            }
                        }
                        catch
                        {
                            // Обработка исключения
                        }
                    }
                }

                trans.Commit();
            }
        }

        private static string ExtractLastPart(string input)
        {
            // Используем регулярное выражение для поиска последней части строки после последнего '_'
            Match match = Regex.Match(input, @"_([^_]+)$");

            // Если совпадение найдено, возвращаем его значение
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            // Если совпадение не найдено, возвращаем исходную строку
            return input;
        }
        public static void SetParameter(ExternalCommandData commandData)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Получаем все элементы FamilyInstance
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            IList<Element> familyInstances = collector.OfClass(typeof(FamilyInstance)).ToElements();

            // Получаем все уровни в проекте
            IList<Level> levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().OrderBy(l => l.Elevation).ToList();

            using (Transaction trans = new Transaction(doc, "Запись параметра"))
            {
                trans.Start();

                foreach (Element element in familyInstances)
                {
                  
                    // Проверяем соответствие уровней
                    try
                    {
                        // Записываем имя уровня в параметр "ПРО_Этаж"
                        string floor = element.LookupParameter("ПРО_Этаж").AsString();
                        element.LookupParameter(win.ParName.Text).Set(floor);
                    }
                    catch
                    {
                    }
                }

                trans.Commit();
            }
        }

    }

}
