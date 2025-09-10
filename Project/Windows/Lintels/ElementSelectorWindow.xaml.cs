using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AbimToolsMine
{
    public partial class ElementSelectorWindow : Window
    {
        // Статическое поле для хранения выбранной вкладки между вызовами
        private static int _lastSelectedTabIndex = 0;

        public string SelectedElement { get; private set; }

        public ElementSelectorWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Заполняем ListBox-ы из LintelDataProvider
            LoadItemsToListBox(ListBrus, LintelDataProvider.GetBruskElements());
            
            // Для арматуры теперь используем фильтрацию
            LoadArmaturaGosts();
            
            LoadItemsToListBox(ListUgolk, LintelDataProvider.GetUgolkElements());
            
            // Временно добавим элементы для швеллеров
            LoadItemsToListBox(ListShveller, new List<string> { "П10", "П20", "П30" });

            // Восстанавливаем последнюю выбранную вкладку
            if (_lastSelectedTabIndex >= 0 && _lastSelectedTabIndex < TabControlCategories.Items.Count)
                TabControlCategories.SelectedIndex = _lastSelectedTabIndex;
        }

        private void LoadArmaturaGosts()
        {
            if (ArmaturaGostComboBox != null)
            {
                ArmaturaGostComboBox.ItemsSource = LintelDataProvider.GetArmaturaGosts();
                ArmaturaGostComboBox.SelectedIndex = -1;
            }
            
            if (ArmaturaClassComboBox != null)
            {
                ArmaturaClassComboBox.ItemsSource = null;
                ArmaturaClassComboBox.SelectedIndex = -1;
            }
            
            if (ArmaturaDiameterComboBox != null)
            {
                ArmaturaDiameterComboBox.ItemsSource = null;
                ArmaturaDiameterComboBox.SelectedIndex = -1;
            }
            
            if (ListArmatura != null)
            {
                ListArmatura.Items.Clear();
            }
        }

        private void ArmaturaGostComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ArmaturaGostComboBox?.SelectedItem != null)
            {
                var selectedGost = ArmaturaGostComboBox.SelectedItem.ToString();
                
                // Заполняем классы для выбранного ГОСТ
                var classes = LintelDataProvider.GetArmaturaClassesByGost(selectedGost);
                if (ArmaturaClassComboBox != null)
                {
                    ArmaturaClassComboBox.ItemsSource = classes;
                    ArmaturaClassComboBox.SelectedIndex = -1;
                }
                
                // Очищаем диаметры и результаты при смене ГОСТ
                if (ArmaturaDiameterComboBox != null)
                {
                    ArmaturaDiameterComboBox.ItemsSource = null;
                    ArmaturaDiameterComboBox.SelectedIndex = -1;
                }
                
                if (ListArmatura != null)
                {
                    ListArmatura.Items.Clear();
                }
            }
        }

        private void ArmaturaClassComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ArmaturaGostComboBox?.SelectedItem != null && 
                ArmaturaClassComboBox?.SelectedItem != null)
            {
                var selectedGost = ArmaturaGostComboBox.SelectedItem.ToString();
                var selectedClass = ArmaturaClassComboBox.SelectedItem.ToString();
                
                // Заполняем диаметры для выбранных ГОСТ и класса
                var diameters = LintelDataProvider.GetArmaturaDiametersByGostAndClass(selectedGost, selectedClass);
                if (ArmaturaDiameterComboBox != null)
                {
                    ArmaturaDiameterComboBox.ItemsSource = diameters;
                    ArmaturaDiameterComboBox.SelectedIndex = -1;
                }
                
                // Очищаем результаты при смене класса
                if (ListArmatura != null)
                {
                    ListArmatura.Items.Clear();
                }
            }
        }

        private void ArmaturaDiameterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ArmaturaGostComboBox?.SelectedItem != null && 
                ArmaturaClassComboBox?.SelectedItem != null &&
                ArmaturaDiameterComboBox?.SelectedItem != null)
            {
                var selectedGost = ArmaturaGostComboBox.SelectedItem.ToString();
                var selectedClass = ArmaturaClassComboBox.SelectedItem.ToString();
                var selectedDiameter = (double)ArmaturaDiameterComboBox.SelectedItem;
                
                // Формируем финальную строку для отображения
                var finalString = $"{selectedClass} ø{selectedDiameter} ({selectedGost})";
                
                if (ListArmatura != null)
                {
                    ListArmatura.Items.Clear();
                    ListArmatura.Items.Add(new ListBoxItem { Content = finalString });
                }
            }
        }

        private void LoadItemsToListBox(ListBox listBox, List<string> items)
        {
            if (listBox == null) return;
            
            listBox.Items.Clear();
            foreach (var item in items)
            {
                listBox.Items.Add(new ListBoxItem { Content = item });
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            SelectedElement = GetSelectedItem();
            if (SelectedElement != null)
            {
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите элемент.", "Выбор элемента", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private string GetSelectedItem()
        {
            // Проверяем каждый ListBox, возвращаем первый выбранный элемент
            if (ListBrus?.SelectedItem is ListBoxItem item1) return item1.Content.ToString();
            if (ListArmatura?.SelectedItem is ListBoxItem item2) return item2.Content.ToString();
            if (ListUgolk?.SelectedItem is ListBoxItem item3) return item3.Content.ToString();
            if (ListShveller?.SelectedItem is ListBoxItem item4) return item4.Content.ToString();
            return null;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Запоминаем индекс выбранной вкладки при закрытии окна
            if (TabControlCategories != null)
                _lastSelectedTabIndex = TabControlCategories.SelectedIndex;
        }
    }
}
