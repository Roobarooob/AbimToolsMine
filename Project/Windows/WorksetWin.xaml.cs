using Autodesk.Revit.UI;
using System.Windows;


namespace AbimToolsMine
{
    public partial class WorksetWin : Window
    {
        public string DefaultPrefix { get; set; }
        public string DWGPrefix { get; set; }

        public WorksetWin()
        {
            InitializeComponent();
            // Загружаем значение из Default.Settings и устанавливаем его в TextBox
            DefaultPrefix = Properties.Settings.Default.LinkPrefix;
            DWGPrefix = Properties.Settings.Default.DWGWorksetName;
            PrefixTextBox.Text = $"Связи RVT - {DefaultPrefix}";
            DWGTextBox.Text = $"Связи DWG - {DWGPrefix}";
            DataContext = this;
        }

        // Обработчик кнопки "Назначить связи по рабочим наборам"
        private void AssignWorksetsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LinksWokset.LinksToWorksets(LinksWokset.CommandData, PrefixTextBox.Text);
                MessageBox.Show("Процесс завершен!");
            }
            catch { MessageBox.Show("Что-то не получилось..."); }
        }

        // Обработчик кнопки "Фильтр пустых рабочих наборов"
        private void FilterEmptyWorksetsButton_Click(object sender, RoutedEventArgs e)
        {
            // Вызов метода для фильтрации пустых рабочих наборов
            try
            {
                UIApplication uiapp = LinksWokset.CommandData.Application;
                Autodesk.Revit.ApplicationServices.Application app = uiapp.Application;
                int revitVersion = int.Parse(app.VersionNumber);
                if (revitVersion >= 2023)
                {
                    LinksWokset.DeleteUnusedWorksets(LinksWokset.CommandData, DefaultPrefix);
                }
                else
                {
                    LinksWokset.RenameUnusedWorksets(LinksWokset.CommandData, DefaultPrefix);
                }
                MessageBox.Show("Процесс завершен!");
            }
            catch
            {
                MessageBox.Show("Что-то не получилось");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            LinksWokset.window = null;
        }

        private void WorsetPrefClick(object sender, RoutedEventArgs e)
        {
            WorksetPrefWin worksetPrefWin = new WorksetPrefWin();
            worksetPrefWin.ShowDialog();
        }
    }
}