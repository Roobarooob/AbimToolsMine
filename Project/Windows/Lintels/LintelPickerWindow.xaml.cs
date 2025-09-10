using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AbimToolsMine
{
    public partial class LintelPickerWindow : Window
    {
        // ��� ��������� ���������
        private readonly List<Button> _selectionButtons = new List<Button>();
        private readonly List<TextBlock> _selectionTextBlock = new List<TextBlock>();
        public LintelConfig SelectedConfig { get; private set; }

        public LintelPickerWindow(List<string> currentSelection = null)
        {
            InitializeComponent();
            LoadComboBoxes();
            
            // ���� ���� ������� ������������, ����������� ��
            if (currentSelection != null && currentSelection.Count > 0)
            {
                SelectedConfig = new LintelConfig 
                { 
                    LintelType = "���������", 
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
                SelectedConfig = new LintelConfig { LintelType = "���������", BrusElements = new List<string>() };
                RenderBrusButtons(1);
            }
        }

        private void LoadComboBoxes()
        {
            try
            {
                // ������
                if (UgolkTypeComboBox != null)
                    UgolkTypeComboBox.ItemsSource = LintelDataProvider.GetUgolkElements();
                
                // ������
                if (UgolkStripTypeComboBox != null)
                    UgolkStripTypeComboBox.ItemsSource = LintelDataProvider.GetPolosaElements();
                
                // ���� ��� ��������
                if (ArmaturaGostComboBox != null)
                {
                    ArmaturaGostComboBox.ItemsSource = LintelDataProvider.GetArmaturaGosts();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"������ �������� ������: {ex.Message}");
            }
        }

        // ��������� ���������
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
                SelectedConfig = new LintelConfig { LintelType = "���������", BrusElements = new List<string>() };
            
            // �������������� ������ ���������
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
                    Text = "�",
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
                    // ����������, ��� ������ ���������� �������
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
            // ���������� ����� ������� �������
            if (TabControlTypes.SelectedIndex == 0) // ���������
            {
                if (SelectedConfig.BrusElements.Any(x => !string.IsNullOrEmpty(x)))
                {
                    SelectedConfig.LintelType = "���������";
                    DialogResult = true;
                }
                else
                {
                    MessageBox.Show("�������� ���� �� ���� �������.", "������", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        // ��������� ���������
        private void UgolkOK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UgolkStepBox?.Text) ||
                string.IsNullOrWhiteSpace(UgolkOffsetBox?.Text) ||
                UgolkTypeComboBox?.SelectedItem == null ||
                UgolkStripTypeComboBox?.SelectedItem == null)
            {
                MessageBox.Show("��������� ��� ���� ��� ��������� ���������.", "������", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedConfig = new LintelConfig
            {
                LintelType = "���������",
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

        // ���������� ���������
        private void ArmaturaGostComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ArmaturaGostComboBox?.SelectedItem != null)
            {
                var selectedGost = ArmaturaGostComboBox.SelectedItem.ToString();
                
                // ������� ��������� ������ ��� ���������� ����
                var classes = LintelDataProvider.GetArmaturaClassesByGost(selectedGost);
                if (ArmaturaClassComboBox != null)
                {
                    ArmaturaClassComboBox.ItemsSource = classes;
                    ArmaturaClassComboBox.SelectedIndex = -1;
                }
                
                // ������� �������� ��� ����� ����
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

                // ��������� �������� ��� ��������� ���� � ������
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
            // ����� �� ��������� �������� ������������ ������. ������ ������ �������� �� ���� -> ����� -> �������,
            // ������� ���� ���������� �������� ������ (������ �� �������, ���� ����������� ������� �� ����� ��������).
        }

        private void ArmaturaOK_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(ArmaturaCountBox?.Text, out int count) || count <= 0 ||
                ArmaturaGostComboBox?.SelectedItem == null ||
                ArmaturaDiamComboBox?.SelectedItem == null ||
                ArmaturaClassComboBox?.SelectedItem == null)
            {
                MessageBox.Show("��������� ��� ���� ��� ���������� ���������.", "������", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedConfig = new LintelConfig
            {
                LintelType = "����������",
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

        // ��� �������� ������������� � ������������ �����
        public List<string> GetSelectedElements()
        {
            return SelectedConfig?.BrusElements ?? new List<string>();
        }

        // ���������� �������� ��������� ������������ ��� �����������
        public string GetLintelDescription()
        {
            if (SelectedConfig == null) return "�� �������";

            switch (SelectedConfig.LintelType)
            {
                case "���������":
                    var elements = SelectedConfig.BrusElements?.Where(x => !string.IsNullOrEmpty(x)).ToList();
                    return elements?.Any() == true ? 
                        $"���������: {string.Join(", ", elements)}" : 
                        "���������: �� �������";

                case "���������":
                    var ugolk = SelectedConfig.UgolkConfig;
                    return ugolk != null ? 
                        $"���������: {ugolk.UgolkType}, ������ {ugolk.StripType}, ��� {ugolk.Step}, ������ {ugolk.Offset}" :
                        "���������: �� ���������";

                case "����������":
                    var armatura = SelectedConfig.ArmaturaConfig;
                    return armatura != null ?
                        $"����������: {armatura.Count} �����. {armatura.ClassName} ?{armatura.Diameter}" :
                        "����������: �� ���������";

                default:
                    return SelectedConfig.LintelType ?? "�� �������";
            }
        }
    }
}
