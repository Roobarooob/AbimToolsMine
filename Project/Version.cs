using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbimToolsMine
{
    [Transaction(TransactionMode.Manual)]
    public class Version : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            TaskDialog.Show("Информация", "Версия приложения - 5.0.0\n\n" +

                "Загруженные плагины:\n" +
                "АвтоПиннер\n" +
                "БыстроФильтр\n" +
                "Пакетная обработка v2.0\n"+
                "Загрузка коллизий\n" +
                "Инструменты рабочих наборов\n" +
                "Экспорт таблиц выбора\n\n" +
                "Ведомость отделки\n\n" +
            "создатель Антон Рубинштейн\n");
            return Result.Succeeded;
        }
    }
}