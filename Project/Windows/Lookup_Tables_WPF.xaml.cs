using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace AbimToolsMine
{

    public partial class LookUpTableWindow : Window
    {
        private Document doc;
        private Family Fam;
        private FamilySizeTableManager FSM;
        private string s_log_file = "";
        private Document familyDoc;

        public LookUpTableWindow(ExternalCommandData commandData)
        {
            InitializeComponent();
            doc = commandData.Application.ActiveUIDocument.Document;
            string title = doc.Title;
            s_log_file = Path.Combine(Path.GetTempPath(), $"log_{title}.txt");
            LoadFamilies();
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            GetLookUpTable.window = null;
        }
        private void LoadFamilies()
        {
            if (!doc.IsFamilyDocument)
            {
                comboBoxFamilies.IsEnabled = true;
                var families = new FilteredElementCollector(doc)
                    .OfClass(typeof(Family))
                    .Cast<Family>()
                    .Where(fam => fam.FamilyCategory.CategoryType == CategoryType.Model && fam.IsEditable)
                    .OrderBy(fam => fam.Name)
                    .ToList();

                comboBoxFamilies.ItemsSource = families.Select(fam => fam.Name).ToList();
                if (comboBoxFamilies.Items.Count > 0)
                {
                    comboBoxFamilies.SelectedIndex = 0;
                }
            }
            else { comboBoxFamilies.IsEnabled = false;}
        }

        private void GetTables(bool familyDocument)
        {
            comboBoxTables.ItemsSource = null;
            labelFamily.Content = "...таблицы выбора не получены...";

            if (Fam == null)
            {
                MessageBox.Show("Семейство не выбрано.");
                return;
            }

            if (!Fam.IsEditable && !familyDocument)
            {
                MessageBox.Show("Семейство является не редактируемым.");
                return;
            }

            familyDoc = familyDocument ? doc : doc.EditFamily(Fam);
            FSM = FamilySizeTableManager.GetFamilySizeTableManager(doc, Fam.Id);

            if (FSM == null)
            {
                MessageBox.Show("В данном семействе нет таблиц выбора.");
                if (!familyDocument) familyDoc.Close(false);
                return;
            }

            var tableNames = FSM.GetAllSizeTableNames();
            if (tableNames == null || tableNames.Count == 0)
            {
                MessageBox.Show("В данном семействе нет таблиц выбора.");
                if (!familyDocument) familyDoc.Close(false);
                return;
            }

            comboBoxTables.ItemsSource = tableNames;
            comboBoxTables.SelectedIndex = 0;
            labelFamily.Content = familyDoc.Title;
        }

        private void ButtonGetTables_Click(object sender, RoutedEventArgs e)
        {
            string selectedFamily = comboBoxFamilies.SelectedItem as string;

            if (doc.IsFamilyDocument)
            {
                Fam = doc.OwnerFamily;
                GetTables(true);
            }
            else
            {
                if (string.IsNullOrEmpty(selectedFamily))
                {
                    MessageBox.Show("Выберите семейство.");
                    return;
                }
                Fam = new FilteredElementCollector(doc)
                    .OfClass(typeof(Family))
                    .Cast<Family>()
                    .FirstOrDefault(fam => fam.Name == selectedFamily);
                GetTables(false);
            }   
        }

        private void ButtonExportTable_Click(object sender, RoutedEventArgs e)
        {
            if (FSM == null || comboBoxTables.SelectedItem == null)
            {
                MessageBox.Show("Сначала получите таблицы.");
                return;
            }

            string tableName = comboBoxTables.SelectedItem.ToString();
            var sizeTable = FSM.GetSizeTable(tableName);

            if (sizeTable == null || sizeTable.NumberOfColumns < 1)
            {
                MessageBox.Show("Выбранная таблица не содержит данных.");
                return;
            }

            // Заголовок таблицы с именами колонок и unitType
            var version = doc.Application.VersionNumber;
            string output = string.Join("\t", Enumerable.Range(0, sizeTable.NumberOfColumns)
                .Select(index =>
                {
                    var columnHeader = sizeTable.GetColumnHeader(index);
                    string str3 = "";
                    string str5 = "";
#if R2020
                    str3 = columnHeader.UnitType.ToString();
                    str5 = columnHeader.DisplayUnitType.ToString();
                     string str4;
                    try
                    {
                        str4 = str3.Remove(0, 3); // Обрезаем первые 3 символа
                    }
                    catch
                    {
                        str4 = str3 + "_ОШИБКА!!!";
                    }
                    string str6;
                    try
                    {
                        str6 = str5.Remove(0, 4); // Обрезаем первые 4 символа
                    }
                    catch
                    {
                        str6 = str5 + "_ОШИБКА!!!";
                    }
                                        // Формируем строку для заголовка, включая имя и unitType

                    return $"{columnHeader.Name}##{str4}##{str6}";
                })) + "\n";
#else

                    str3 = columnHeader.GetSpecTypeId().TypeId;
                    str5 = columnHeader.GetUnitTypeId().TypeId; 
                    string str4;
                    try
                    {
                        string x = str3.Replace("autodesk.spec.aec:", "");
                        str4 = x.Remove(x.Length - 6);//.Remove(0, 3); // Обрезаем первые 3 символа
                    }
                    catch
                    {
                        str4 = "OTHER";
                    }
                    string str6;
                    try
                    {


                        string x = str5.Replace("autodesk.unit.unit:", "").Replace("-1.0.0", "");//.Remove(0, 16); // Обрезаем первые 4 символа
                        str6 = x.Remove(x.Length - 6);


                    }
                    catch
                    {
                        str6 = "OTHER";
                    }

                    // Формируем строку для заголовка, включая имя и unitType
                    if (columnHeader.Name.Length > 0)
                    { 
                    return $"{columnHeader.Name}##{str4}##{str6}";
                }
                    else { return ""; }
                })) + "\n";
#endif



            // Формирование данных таблицы
            for (int i = 0; i < sizeTable.NumberOfRows; i++)
            {
                output += string.Join("\t", Enumerable.Range(0, sizeTable.NumberOfColumns)
                    .Select(j => sizeTable.AsValueString(i, j))) + "\n";
            }

            File.WriteAllText(s_log_file, output);
            Process.Start("notepad.exe", s_log_file);
        }
    }
}
