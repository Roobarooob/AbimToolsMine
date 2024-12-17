using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows;
using System;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.IO;


namespace AbimToolsMine
{
    public partial class ParameterSelectionWindow : Window
    {
        public bool ready = false;
        public List<ParameterGroup> ParameterGroups { get; set; }
        public List<ParameterInfo> SelectedParameters { get; private set; } = new List<ParameterInfo>();

        public ParameterSelectionWindow()
        {
            InitializeComponent();
            LoadSharedParameters();

            PopulateTreeView();
        }

        // Метод для загрузки параметров из файла общих параметров
        public void LoadSharedParameters()
        {
            // Получаем путь к файлу общих параметров из настроек документа
            UIApplication uiapp = BatchTools.CommandData.Application;
            string sharedParameterFilePath = uiapp.Application.SharedParametersFilename;

            if (File.Exists(sharedParameterFilePath))
            {
                // Список для хранения групп параметров
                ParameterGroups = new List<ParameterGroup>();
                Dictionary<int, ParameterGroup> groupDictionary = new Dictionary<int, ParameterGroup>();

                // Чтение файла построчно
                string[] lines = File.ReadAllLines(sharedParameterFilePath);
                foreach (string line in lines)
                {
                    // Разделение строки на колонки с помощью табуляции
                    var columns = line.Split('\t');

                    // Если строка определяет группу (начинается с "GROUP")
                    if (columns[0] == "GROUP" && columns.Length >= 3)
                    {
                        int groupId = int.Parse(columns[1].Trim()); // ID группы
                        string groupName = columns[2].Trim();       // Имя группы

                        // Создаем новый объект ParameterGroup и добавляем его в словарь
                        var group = new ParameterGroup
                        {
                            GroupName = groupName,
                            Parameters = new List<ParameterInfo>()
                        };
                        groupDictionary[groupId] = group;
                        ParameterGroups.Add(group);
                    }
                    // Если строка определяет параметр (начинается с "PARAM")
                    else if (columns[0] == "PARAM" && columns.Length >= 5)
                    {
                        string parameterName = columns[2].Trim(); // Имя параметра
                        int groupId = int.Parse(columns[5].Trim()); // ID группы, к которой относится параметр

                        // Проверяем, существует ли группа с таким ID в словаре
                        if (groupDictionary.ContainsKey(groupId))
                        {
                            // Добавляем параметр в соответствующую группу
                            ParameterInfo parameter = new ParameterInfo
                            {
                                Name = parameterName,
                                IsSelected = false
                            };
                            groupDictionary[groupId].Parameters.Add(parameter);                          
                        }
                    }
                }
                ready = true;
            }
            else
            {
                MessageBox.Show("Файл общих параметров не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Метод для отображения параметров в TreeView с группировкой
        private void PopulateTreeView(string searchText = "")
        {
            try
            {
                // Сохраняем текущее состояние чекбоксов перед очисткой
                foreach (TreeViewItem groupItem in ParameterTreeView.Items)
                {
                    foreach (TreeViewItem paramItem in groupItem.Items)
                    {
                        CheckBox checkBox = (CheckBox)paramItem.Header;
                        var parameter = (ParameterInfo)paramItem.Tag;
                        parameter.IsSelected = checkBox.IsChecked == true;
                    }
                }

                ParameterTreeView.Items.Clear();
            }
            catch
            {
                // Пропускаем возможные исключения при очистке
            }
            string lowerSearchText = searchText.ToLower(); // Преобразуем текст поиска в нижний регистр
            foreach (var group in ParameterGroups)
            {
                var groupItem = new TreeViewItem
                {
                    Header = group.GroupName,
                    IsExpanded = !string.IsNullOrEmpty(searchText) // Разворачиваем группу, если выполняется поиск
                };

                foreach (var parameter in group.Parameters)
                {
                    if (string.IsNullOrEmpty(searchText) || parameter.Name.ToLower().Contains(lowerSearchText))
                    {
                        var checkBox = new CheckBox
                        {
                            Content = parameter.Name.Replace("_", "__"),
                            IsChecked = parameter.IsSelected
                        };

                        // Обработчики для обновления состояния
                        checkBox.Checked += (s, e) => parameter.IsSelected = true;
                        checkBox.Unchecked += (s, e) => parameter.IsSelected = false;

                        var paramItem = new TreeViewItem
                        {
                            Header = checkBox,
                            Tag = parameter
                        };
                        groupItem.Items.Add(paramItem);
                    }
                }

                // Добавляем группу только если она содержит параметры, соответствующие поисковому запросу
                if (groupItem.Items.Count > 0)
                {
                    ParameterTreeView.Items.Add(groupItem);
                }
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ready)
            {
                PopulateTreeView(SearchBox.Text);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedParameters = ParameterGroups
                .SelectMany(group => group.Parameters)
                .Where(parameter => parameter.IsSelected)
                .ToList();

            DialogResult = true;
            Close();
            ready = false;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

            DialogResult = false;
            Close();
            ready = false;
        }
    }

    // Классы для группировки и хранения параметров
    public class ParameterGroup
    {
        public string GroupName { get; set; }
        public List<ParameterInfo> Parameters { get; set; }
    }

}

