using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AbimToolsMine
{
    public partial class LintelPickerWindow : Window
    {
        // Для брусковых перемычек
        private readonly List<Button> _selectionButtons = new List<Button>();
        private readonly List<TextBlock> _selectionTextBlock = new List<TextBlock>();
        public LintelConfig SelectedConfig { get; private set; }

        public LintelPickerWindow(List<string> currentSelection = null)
        {
            InitializeComponent();
            LoadComboBoxes();
            
            // Если есть текущая конфигурация, восстановим ее
            if (currentSelection != null && currentSelection.Count > 0)
            {
                SelectedConfig = new LintelConfig 
                { 
                    LintelType = "Брусковая", 
                    BrusElements = new List<string>(currentSelection) 
                };
                int count = currentSelection.Count;
                CountBrusBox.Text = count.ToString();
                RenderBrusButtons(count);
                
                for (int i = 0; i < Math.Min(count, currentSelection.Count); i++)
                {
                    if (i < _selectionTextBlock.Count)
                    {
                        _selectionTextBlock[i].Text = currentSelection[i];
                        TextblockWrap(_selectionTextBlock[i]);
                    }
                }
            }
            else
            {
                SelectedConfig = new LintelConfig { LintelType = "Брусковая", BrusElements = new List<string>() };
                RenderBrusButtons(1);
            }
        }

        private void LoadComboBoxes()
        {
            try
            {
                // Уголки
                if (UgolkTypeComboBox != null)
                    UgolkTypeComboBox.ItemsSource = LintelDataProvider.GetUgolkElements();
                
                // Полосы
                if (UgolkStripTypeComboBox != null)
                    UgolkStripTypeComboBox.ItemsSource = LintelDataProvider.GetPolosaElements();
                
                // ГОСТ для арматуры
                if (ArmaturaGostComboBox != null)
                {
                    ArmaturaGostComboBox.ItemsSource = LintelDataProvider.GetArmaturaGosts();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        // Брусковые перемычки
        private void CountBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender == CountBrusBox && int.TryParse(CountBrusBox.Text, out int count) && count > 0 && count <= 6)
            {
                RenderBrusButtons(count);
            }
        }

        private void RenderBrusButtons(int count)
        {
            if (ButtonContainer == null) return;

            ButtonContainer.Items.Clear();
            _selectionButtons.Clear();
            _selectionTextBlock.Clear();
            
            if (SelectedConfig == null)
                SelectedConfig = new LintelConfig { LintelType = "Брусковая", BrusElements = new List<string>() };
            
            // Инициализируем список элементов
            SelectedConfig.BrusElements = new List<string>();
            for (int i = 0; i < count; i++)
            {
                SelectedConfig.BrusElements.Add("");
            }

            var stack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0) };
            for (int i = 0; i < count; i++)
            {
                int index = i;
                
                var textBlock = new TextBlock
                {
                    Text = "—",
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    MinWidth = 80,
                    MinHeight = 30,
                    Margin = new Thickness(2)
                };

                var button = new Button
                {
                    Content = textBlock,
                    Width = 90,
                    Height = 40,
                    Margin = new Thickness(5, 0, 5, 0)
                };

                button.Click += (s, e) => SelectBrusElement(index);
                
                stack.Children.Add(button);
                _selectionButtons.Add(button);
                _selectionTextBlock.Add(textBlock);
            }
            
            ButtonContainer.Items.Add(stack);
        }

        private void SelectBrusElement(int index)
        {
            var selector = new ElementSelectorWindow();
            if (selector.ShowDialog() == true)
            {
                var selected = selector.SelectedElement;
                if (!string.IsNullOrEmpty(selected))
                {
                    // Убеждаемся, что список достаточно большой
                    while (SelectedConfig.BrusElements.Count <= index)
                        SelectedConfig.BrusElements.Add("");
                    
                    SelectedConfig.BrusElements[index] = selected;
                    _selectionTextBlock[index].Text = selected;
                    TextblockWrap(_selectionTextBlock[index]);
                }
            }
        }

        private void TextblockWrap(TextBlock textBlock)
        {
            textBlock.TextWrapping = TextWrapping.Wrap;
            textBlock.TextAlignment = TextAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            // Определяем какая вкладка активна
            if (TabControlTypes.SelectedIndex == 0) // Брусковые
            {
                if (SelectedConfig.BrusElements.Any(x => !string.IsNullOrEmpty(x)))
                {
                    SelectedConfig.LintelType = "Брусковая";
                    DialogResult = true;
                }
                else
                {
                    MessageBox.Show("Выберите хотя бы один элемент.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        // Уголковые перемычки
        private void UgolkOK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UgolkStepBox?.Text) ||
                string.IsNullOrWhiteSpace(UgolkOffsetBox?.Text) ||
                UgolkTypeComboBox?.SelectedItem == null ||
                UgolkStripTypeComboBox?.SelectedItem == null)
            {
                MessageBox.Show("Заполните все поля для уголковой перемычки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedConfig = new LintelConfig
            {
                LintelType = "Уголковая",
                UgolkConfig = new LintelUgolkConfig
                {
                    Step = UgolkStepBox.Text,
                    Offset = UgolkOffsetBox.Text,
                    UgolkType = UgolkTypeComboBox.SelectedItem.ToString(),
                    StripType = UgolkStripTypeComboBox.SelectedItem.ToString()
                }
            };

            DialogResult = true;
        }

        // Арматурные перемычки
        private void ArmaturaGostComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ArmaturaGostComboBox?.SelectedItem != null)
            {
                var selectedGost = ArmaturaGostComboBox.SelectedItem.ToString();
                
                // Сначала наполняем классы для выбранного ГОСТ
                var classes = LintelDataProvider.GetArmaturaClassesByGost(selectedGost);
                if (ArmaturaClassComboBox != null)
                {
                    ArmaturaClassComboBox.ItemsSource = classes;
                    ArmaturaClassComboBox.SelectedIndex = -1;
                }
                
                // Очищаем диаметры при смене ГОСТ
                if (ArmaturaDiamComboBox != null)
                {
                    ArmaturaDiamComboBox.ItemsSource = null;
                    ArmaturaDiamComboBox.SelectedIndex = -1;
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
                if (ArmaturaDiamComboBox != null)
                {
                    ArmaturaDiamComboBox.ItemsSource = diameters;
                    ArmaturaDiamComboBox.SelectedIndex = -1;
                }
            }
        }

        private void ArmaturaDiamComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Ранее на изменение диаметра подгружались классы. Теперь логика изменена на ГОСТ -> класс -> диаметр,
            // поэтому этот обработчик оставлен пустым (резерв на будущее, если понадобится реакция на выбор диаметра).
        }

        private void ArmaturaOK_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(ArmaturaCountBox?.Text, out int count) || count <= 0 ||
                ArmaturaGostComboBox?.SelectedItem == null ||
                ArmaturaDiamComboBox?.SelectedItem == null ||
                ArmaturaClassComboBox?.SelectedItem == null)
            {
                MessageBox.Show("Заполните все поля для арматурной перемычки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedConfig = new LintelConfig
            {
                LintelType = "Арматурная",
                ArmaturaConfig = new LintelArmaturaConfig
                {
                    Count = count,
                    Gost = ArmaturaGostComboBox.SelectedItem.ToString(),
                    Diameter = (double)ArmaturaDiamComboBox.SelectedItem,
                    ClassName = ArmaturaClassComboBox.SelectedItem.ToString()
                }
            };

            DialogResult = true;
        }

        // Для обратной совместимости с существующим кодом
        public List<string> GetSelectedElements()
        {
            return SelectedConfig?.BrusElements ?? new List<string>();
        }

        // Возвращает описание выбранной конфигурации для отображения
        public string GetLintelDescription()
        {
            if (SelectedConfig == null) return "Не выбрано";

            switch (SelectedConfig.LintelType)
            {
                case "Брусковая":
                    var elements = SelectedConfig.BrusElements?.Where(x => !string.IsNullOrEmpty(x)).ToList();
                    return elements?.Any() == true ? 
                        $"Брусковая: {string.Join(", ", elements)}" : 
                        "Брусковая: не выбрано";

                case "Уголковая":
                    var ugolk = SelectedConfig.UgolkConfig;
                    return ugolk != null ? 
                        $"Уголковая: {ugolk.UgolkType}, полоса {ugolk.StripType}, шаг {ugolk.Step}, отступ {ugolk.Offset}" :
                        "Уголковая: не настроено";

                case "Арматурная":
                    var armatura = SelectedConfig.ArmaturaConfig;
                    return armatura != null ?
                        $"Арматурная: {armatura.Count} стерж. {armatura.ClassName} ?{armatura.Diameter}" :
                        "Арматурная: не настроено";

                default:
                    return SelectedConfig.LintelType ?? "Не выбрано";
            }
        }
    }
}
