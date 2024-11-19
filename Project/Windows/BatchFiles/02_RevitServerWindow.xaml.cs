using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Windows;
using System.Xml;

namespace AbimTools
{
    public partial class RevitServerWindow : Window
    {

        public RevitServerWindow()
        {
            InitializeComponent();
            tbxServerName.Text = Properties.Settings.Default.RevitServer_ip;
            GetRvtStrings();
        }

        private XmlDictionaryReader GetResponse(string info)
        {
            UIApplication uiapp = BatchTools.CommandData.Application;
            Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;
            int revitVersion = int.Parse(app.VersionNumber);
            WebRequest request = WebRequest.Create(
                "http://" +
                tbxServerName.Text +
                $"/RevitServerAdminRESTService{revitVersion}/AdminRESTService.svc" +
                info
            );
            request.Method = "GET";

            request.Headers.Add("User-Name", Environment.UserName);
            request.Headers.Add("User-Machine-Name", Environment.MachineName);
            request.Headers.Add("Operation-GUID", Guid.NewGuid().ToString());

            XmlDictionaryReaderQuotas quotas = new XmlDictionaryReaderQuotas();
            return JsonReaderWriterFactory.CreateJsonReader(
                request.GetResponse().GetResponseStream(),
                quotas
            );
        }
        private List<string> GetSelectedFiles(TreeNode node, string currentPath)
        {
            var selectedFiles = new List<string>();

            // Проверяем, является ли узел файлом (листьевым элементом) и если выбран
            if (node.IsChecked == true && node.Children.Count == 0 && node.Header.EndsWith(".rvt"))
            {
                // Добавляем путь к файлу
                selectedFiles.Add(currentPath + "/" + node.Header);
            }

            // Рекурсивно обрабатываем детей узла
            foreach (var child in node.Children)
            {
                selectedFiles.AddRange(GetSelectedFiles(child, currentPath + "/" + node.Header));
            }

            return selectedFiles;
        }

        private void btnProcessFiles_Click(object sender, RoutedEventArgs e)
        {
            if (trvContent.ItemsSource is ObservableCollection<TreeNode> nodes)
            {
                var selectedFiles = nodes.SelectMany(node => GetSelectedFiles(node, "rsn:/")).ToList();
                
                if (selectedFiles.Any())
                {
                    foreach (string file in selectedFiles)
                    {
                        CollisionsWin.rvtFilePaths.Add(file);
                    }                   
                }
                else
                {
                    MessageBox.Show("No files selected.", "Info");
                }
            }
        }
        private void AddContents(TreeNode parentNode, string path)
        {
            XmlDictionaryReader reader = GetResponse(path + "/contents");

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.Name == "Folders")
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Folders")
                            break;

                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "Name")
                        {
                            reader.Read();
                            string content = reader.ReadContentAsString();
                            var node = new TreeNode(content);
                            parentNode.Children.Add(node);
                            AddContents(node, path + "|" + content);
                        }
                    }
                }

                else if (reader.NodeType == XmlNodeType.Element && reader.Name == "Models")
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Models")
                            break;

                        if (reader.NodeType == XmlNodeType.Element && reader.Name == "Name")
                        {
                            reader.Read();
                            parentNode.Children.Add(new TreeNode(reader.Value));
                        }
                    }
                }
            }

            reader.Close();
        }

        void GetRvtStrings()
        {
            try
            {
                var root = new TreeNode(tbxServerName.Text);
                AddContents(root, "/|");

                trvContent.ItemsSource = new ObservableCollection<TreeNode> { root };
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Failed to retrieve data");
            }
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            GetRvtStrings();
        }
    }

    public class TreeNode : INotifyPropertyChanged
    {
        private bool? isChecked = false;

        public string Header { get; set; }
        public ObservableCollection<TreeNode> Children { get; set; } = new ObservableCollection<TreeNode>();

        public bool? IsChecked
        {
            get => isChecked;
            set
            {
                if (isChecked != value)
                {
                    isChecked = value;
                    OnPropertyChanged(nameof(IsChecked));

                    if (isChecked.HasValue)
                    {
                        foreach (var child in Children)
                        {
                            child.IsChecked = isChecked;
                        }
                    }
                }
            }
        }
        public string Path => Header;
        public TreeNode(string header)
        {
            Header = header;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
