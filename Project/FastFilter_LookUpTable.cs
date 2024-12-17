using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;

namespace AbimToolsMine
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class FastFilter : IExternalCommand
    {
        public static FastFilterWin win = null;
        public Autodesk.Revit.ApplicationServices.Application RevitApp;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            FastFilterHandler FastFilterHander = new FastFilterHandler();
            ExternalEvent FastFilterEvent = ExternalEvent.Create(FastFilterHander);
            if (win == null)
            {
                win = new FastFilterWin(FastFilterHander, FastFilterEvent, commandData);
                win.Show();
            }
            else
            {
                win.Activate();
            }
            return Result.Succeeded;
        }
    }
    
    [Transaction(TransactionMode.Manual)]
    public class GetLookUpTable : IExternalCommand
    {
        public static ExternalCommandData CommandData { get; set; }

        public static LookUpTableWindow window = null;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (window == null)
            {
                window = new LookUpTableWindow(commandData);
                window.ShowDialog();

            }
            else
            {
                window.Activate();
            }
            return Result.Succeeded;
        }

    }

    public class FastFilterHandler : IExternalEventHandler
    {
        public void Execute(UIApplication uiapp)
        {

            Category selectedCategory = FastFilter.win.SelectedCategory;
            if (selectedCategory != null)
            {
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;

                var selectedIds = uidoc.Selection.GetElementIds();
                var selection = new List<ElementId>();
                foreach (var id in selectedIds)
                {
                    Element element = doc.GetElement(id);
                    if (element.Category.Id == selectedCategory.Id)
                    {
                        selection.Add(id);
                    }
                }
                uidoc.Selection.SetElementIds(selection);
            }

        }
        public string GetName()
        {
            return "FastFilterHandler";
        }
    }
}
