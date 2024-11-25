using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Application = Autodesk.Revit.ApplicationServices.Application;

namespace AbimTools
{
    public partial class ParameterWindow : Window
    {
        public static bool Check { get; set; }
        public static List<ParameterInfo> Parameters { get; set; }
        public static string SetCategories { get; set; }

        public ParameterWindow()
        {
            InitializeComponent();
            Parameters = new List<ParameterInfo>();
            ParameterDataGrid.ItemsSource = Parameters;
        }
        private static bool AddParameterToCategory(Document doc, ParameterInfo param)
        {
            Check = false;
            // Метод для добавления параметра в категорию
            var categories = param.Categories.Split(',')
                                                    .Select(c => c.Trim())
                                                    .ToList();
            ExternalDefinition externalDef = GetExternalDefinition(param.Name);
            CategorySet categorySet = new CategorySet();
            Category category = null;
            foreach (string categoryName in categories)
            {
                foreach (Category cat in doc.Settings.Categories)
                {
                    if (cat.Name == categoryName)
                    {
                        category = cat;
                        categorySet.Insert(category);
                        break;
                    }
                }
            }
            if (externalDef != null)
            {
                Autodesk.Revit.DB.Binding binding;
                if (param.TypeInstance == "Тип")
                    binding = new TypeBinding(categorySet);
                else
                    binding = new InstanceBinding(categorySet);

                doc.ParameterBindings.Insert(externalDef, binding, BuiltInParameterGroup.PG_DATA);
                Check = true;
            }
            if (Check)
            {
                return true;
            }
            else { return false; }
        }

        private static ExternalDefinition GetExternalDefinition(string parameterName)
        {
            // Получение внешнего определения параметра из файла общих параметров
            // Пример получения ExternalDefinition из DefinitionFile
            UIApplication uiapp = BatchTools.CommandData.Application;
            DefinitionFile defFile = uiapp.Application.OpenSharedParameterFile();
            if (defFile == null) return null;

            foreach (DefinitionGroup group in defFile.Groups)
            {
                Definition definition = group.Definitions.get_Item(parameterName);
                if (definition is ExternalDefinition extDef)
                {
                    return extDef;
                }
            }
            return null;
        }
        public static bool UpdateParameterForCategoryByName(Document doc, string categoryName, string parameterName, string value)
        {
            {
                // Проверка, является ли категория "Сведения о проекте"
                if (categoryName.Equals("Сведения о проекте", StringComparison.OrdinalIgnoreCase))
                {
                    Element projectInfo = doc.ProjectInformation;
                    if (projectInfo == null)
                    {
                        TaskDialog.Show("Ошибка", "Элемент 'Сведения о проекте' не найден.");
                        return false;
                    }
                    using (Transaction trans = new Transaction(doc, "Обновление параметров"))
                    {
                        trans.Start();
                        UpdateParameterForElement(projectInfo, parameterName, value);
                        trans.Commit();
                    }
                    return true;
                }

                // Получение категории по имени
                Category category = doc.Settings.Categories.get_Item(categoryName);
                if (category == null)
                {
                    TaskDialog.Show("Ошибка", $"Категория '{categoryName}' не найдена.");
                    return false;
                }

                // Фильтрация элементов категории
                var elements = new FilteredElementCollector(doc)
                    .OfCategoryId(category.Id)
                    .WhereElementIsNotElementType()
                    .ToElements();

                if (!elements.Any())
                {
                    TaskDialog.Show("Результат", "Элементы указанной категории не найдены.");
                    return false;
                }

                // Проверка наличия параметра
                Parameter sampleParam = elements.FirstOrDefault()?.LookupParameter(parameterName);
                if (sampleParam == null)
                {
                    TaskDialog.Show("Ошибка", $"Параметр '{parameterName}' не найден в проекте.");
                    return false;
                }

                // Начало транзакции
                using (Transaction trans = new Transaction(doc, "Обновление параметров"))
                {
                    trans.Start();
                    try
                    {
                        foreach (var element in elements)
                        {
                            if (!UpdateParameterForElement(element, parameterName, value))                          
                            { 
                                return false; 
                            }
                        }

                        trans.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        trans.RollBack();
                        TaskDialog.Show("Ошибка", $"Произошла ошибка: {ex.Message}");
                        return false;
                    }
                }
            }
        }
        private static bool UpdateParameterForElement(Element element, string parameterName, string value)
        {
            Check = false;
            Parameter param = element.LookupParameter(parameterName);
            if (param == null || param.IsReadOnly)
            {
                TaskDialog.Show("Ошибка", $"Параметр '{parameterName}' отсутствует или доступен только для чтения.");
            }

            switch (param.StorageType)
            {
                case StorageType.String:
                    param.Set(value);
                    Check = true;
                    break;

                case StorageType.Double:
                    if (double.TryParse(value, out double doubleValue))
                    {
                        param.Set(doubleValue);
                        Check = true;
                    }
                    else
                    {
                        TaskDialog.Show("Ошибка", $"Значение '{value}' не является числом.");
                        throw new InvalidOperationException("Неверное значение для числового параметра.");
                    }
                    break;

                case StorageType.Integer:
                    if (int.TryParse(value, out int intValue))
                    {
                        param.Set(intValue);
                        Check = true;
                    }
                    else if (value.ToLower() == "да" || value == "1")
                    {
                        param.Set(1); // Да
                        Check = true;
                    }
                    else if (value.ToLower() == "нет" || value == "0")
                    {
                        param.Set(0); // Нет
                        Check = true;
                    }
                    else
                    {
                        TaskDialog.Show("Ошибка", $"Значение '{value}' не подходит для параметра типа Да/Нет.");
                        throw new InvalidOperationException("Неверное значение для параметра Да/Нет.");
                    }
                    break;

                default:
                    TaskDialog.Show("Ошибка", "Тип параметра не поддерживается.");
                    throw new NotSupportedException("Тип параметра не поддерживается.");
            }
            if (Check) { return true; }
            else { return false; }
        }

        public static void SetValueExecute(ExternalCommandData commandData, ObservableCollection<string> filePaths, string parameterName, string value)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            var true_list = new StringBuilder();
            var false_list = new StringBuilder();           
            Opt opt = new Opt();
            Application app = uiapp.Application;
            foreach (string filePath in filePaths)
            {
                ModelPath modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(filePath);
                if (File.Exists(filePath) || modelPath.ServerPath)
                {
                    
                    // Настройте параметры открытия документа с закрытыми рабочими наборами
                    OpenOptions openOptions = new OpenOptions();
                    WorksetConfiguration worksetConfig = new WorksetConfiguration(WorksetConfigurationOption.CloseAllWorksets);
                    openOptions.SetOpenWorksetsConfiguration(worksetConfig);
                    openOptions.AllowOpeningLocalByWrongUser = true;

                    // Откройте документ
                    Document doc = app.OpenDocumentFile(modelPath, openOptions);
                    using (Transaction tx = new Transaction(doc))
                    {
                        try
                        {
                            var categories = SetCategories.Split(',')
                                                    .Select(c => c.Trim())
                                                    .ToList();
                            foreach (var categoryName in categories)
                            {
                                if (UpdateParameterForCategoryByName(doc, categoryName, parameterName, value))
                                {
                                    // Выполните синхронизацию с Revit Server
                                    BatchFunctions.SyncWithRevitServer(doc);
                                    // Сохраните и закройте документ
                                    true_list.AppendLine(doc.Title);
                                    doc.Save();
                                    try
                                    {
                                        doc.Close(false);
                                    }
                                    catch
                                    {
                                        continue;
                                    }
                                }
                                else { false_list.AppendLine(doc.Title); }
                            }
                        }
                        catch
                        {
                            //закройте документ
                            false_list.AppendLine(doc.Title);
                            doc.Close(false);

                        }
                    }
                }
            }
            System.Windows.MessageBox.Show($"Параметры обновлены!\nУспешно обработаны:\n{true_list}\nОшибка:\n{false_list}");
        }
    public static void Execute(ExternalCommandData commandData, ObservableCollection<string> filePaths)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            var true_list = new StringBuilder();
            var false_list = new StringBuilder();

            Opt opt = new Opt();
            Application app = uiapp.Application;
            foreach (string filePath in filePaths)
            {
                ModelPath modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(filePath);
                if (File.Exists(filePath) || modelPath.ServerPath)
                {                  
                    // Настройте параметры открытия документа с закрытыми рабочими наборами
                    OpenOptions openOptions = new OpenOptions();
                    WorksetConfiguration worksetConfig = new WorksetConfiguration(WorksetConfigurationOption.CloseAllWorksets);
                    openOptions.SetOpenWorksetsConfiguration(worksetConfig);
                    openOptions.AllowOpeningLocalByWrongUser = true;

                    // Откройте документ
                    Document doc = app.OpenDocumentFile(modelPath, openOptions);
                    using (Transaction tx = new Transaction(doc))
                    {
                        try
                        {
                            tx.Start("Загрузить параметр");
                            foreach (var param in Parameters)
                            {

                                if (AddParameterToCategory(doc, param))
                                {
                                    tx.Commit();
                                    // Выполните синхронизацию с Revit Server
                                    BatchFunctions.SyncWithRevitServer(doc);
                                    // Сохраните и закройте документ
                                    true_list.AppendLine(doc.Title);
                                    doc.Save();
                                    try
                                    {
                                        doc.Close(false);
                                    }
                                    catch
                                    {
                                        continue;
                                    }
                                }
                                else
                                {
                                    false_list.AppendLine(doc.Title);
                                }
                            }
                            

                        }
                        catch
                        {
                            //закройте документ
                            false_list.AppendLine(doc.Title);
                            doc.Close(false);

                        }
                    }
                }
            }
            System.Windows.MessageBox.Show($"Параметры добавлены!\nУспешно обработаны:\n{true_list}\nНе загружено:\n{false_list}");
        }


        private void AddParametersButton_Click(object sender, RoutedEventArgs e)
        {
            var parameterSelectionWindow = new ParameterSelectionWindow();
            if (parameterSelectionWindow.ShowDialog() == true)
            {
                foreach (var parameter in parameterSelectionWindow.SelectedParameters)
                {
                    Parameters.Add(parameter);
                }
                ParameterDataGrid.Items.Refresh();
            }
        }

        private void AddCategoriesButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedParameters = new List<ParameterInfo>(); 
            try
            {
                selectedParameters = ParameterDataGrid.SelectedItems.Cast<ParameterInfo>().ToList();
            }
            catch
            {
                MessageBox.Show("Пожалуйста, выберите параметры.");
                return;
            }
            if (!selectedParameters.Any())
            {
                MessageBox.Show("Пожалуйста, выберите параметры.");
                return;
            }
            UIApplication uiapp = BatchTools.CommandData.Application;
            //Document doc = uiapp.ActiveUIDocument.Document;

            var categorySelectionWindow = new CategorySelectionWindow();
            if (categorySelectionWindow.ShowDialog() == true)
            {
                foreach (var parameter in selectedParameters)
                {
                    parameter.Categories = string.Join(", ", categorySelectionWindow.SelectedCategories.Select(c => c));
                }
                ParameterDataGrid.Items.Refresh();
            }
        }
        private void SetParCategoriesButton_Click(object sender, RoutedEventArgs e)
        {

            var categorySelectionWindow = new CategorySelectionWindow();
            if (categorySelectionWindow.ShowDialog() == true)
            {

                SetCategories = string.Join(", ", categorySelectionWindow.SelectedCategories.Select(c => c));

                CatLabel.Content = $"Категории: {SetCategories}";
            }
        }
        private void SetTypeInstanceButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedParameters = ParameterDataGrid.SelectedItems.Cast<ParameterInfo>().ToList();
            if (!selectedParameters.Any())
            {
                MessageBox.Show("Пожалуйста, выберите параметры.");
                return;
            }

            var typeInstanceWindow = new TypeInstanceWindow();
            if (typeInstanceWindow.ShowDialog() == true)
            {
                foreach (var parameter in selectedParameters)
                {
                    parameter.TypeInstance = typeInstanceWindow.SelectedTypeInstance;
                }
                ParameterDataGrid.Items.Refresh();
            }
        }

        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {

                Execute(BatchTools.CommandData, CollisionsWin.rvtFilePaths);

        }
        private void SetValue_Click(object sender, RoutedEventArgs e)
        {
            SetValueExecute(BatchTools.CommandData, CollisionsWin.rvtFilePaths, ParameterName.Text.Trim(), Value.Text.Trim());

        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class ParameterInfo
    {
        public string Name { get; set; }
        public string Categories { get; set; }
        public string TypeInstance { get; set; }
        public bool IsSelected { get; set; }
    }
}
