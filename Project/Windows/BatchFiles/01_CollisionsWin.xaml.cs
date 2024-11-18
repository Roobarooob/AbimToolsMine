using Autodesk.Revit.UI;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using static AbimTools.BatchTools;
using Button = System.Windows.Controls.Button;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace AbimTools
{
    public partial class CollisionsWin : Window
    {
        public static ObservableCollection<string> rvtFilePaths = new ObservableCollection<string>();

        public CollisionsWin()
        {
            InitializeComponent();
            RvtFilesListBox.ItemsSource = rvtFilePaths;
        }

        private void SelectXmlFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "XML files (*.xml)|*.xml"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                XmlFilePath.Text = openFileDialog.FileName;
            }
        }

        private void SelectRfaFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Revit Family files (*.rfa)|*.rfa"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                rfaFilePath.Text = openFileDialog.FileName;
            }
        }
        private void AddRvtFiles_Click(object sender, RoutedEventArgs e)
        {
            // Вариант выбора файлов
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Revit files (*.rvt)|*.rvt",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string fileName in openFileDialog.FileNames)
                {
                    rvtFilePaths.Add(fileName);
                }
            }
        }
            private void AddRvtFolder_Click(object sender, RoutedEventArgs e)
        { 
            // Вариант выбора папки
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string selectedPath = folderBrowserDialog.SelectedPath;

                // Получение всех файлов с расширением .rvt во всех подкаталогах
                string[] files = Directory.GetFiles(selectedPath, "*.rvt", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    rvtFilePaths.Add(file);
                }
            }
        }
        private void ClearList(object sender, RoutedEventArgs e)
        {
            // Очистить список
            rvtFilePaths.Clear();

        }


        private void RemoveRvtFile_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                string filePath = (string)button.DataContext;
                rvtFilePaths.Remove(filePath);
            }
        }

        private void StartProcessing_Click(object sender, RoutedEventArgs e)
        {
            // Логика обработки
            string xmlFilePath = XmlFilePath.Text;
            var rvtFiles = rvtFilePaths.ToList();
            BatchFunctions.PlaceCollisions(BatchTools.CommandData,rvtFiles,xmlFilePath);
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            string FilePath = rfaFilePath.Text;
            var rvtFiles = rvtFilePaths.ToList();
            BatchFunctions.DownloadFamily(BatchTools.CommandData, rvtFiles, FilePath);
        }
        private void LinkRemove_Click(object sender, RoutedEventArgs e)
        {
            var rvtFiles = rvtFilePaths.ToList();
            BatchFunctions.BatchLinkRemove(BatchTools.CommandData, rvtFiles);
        }

        private void ParameterTools_Click(object sender, RoutedEventArgs e)
        {
            // Создаем и открываем главное окно "А"
            ParameterWindow par_Window = new ParameterWindow();
            par_Window.ShowDialog();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            BatchTools.window=null;
        }
    }
}