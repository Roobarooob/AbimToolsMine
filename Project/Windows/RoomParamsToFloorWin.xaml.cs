using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Newtonsoft.Json;
using Settings = AbimToolsMine.Properties.Settings;

namespace AbimToolsMine
{
    public partial class RoomParamsToFloorWin : Window
    {
    private readonly UIDocument _uidoc;

        /// <summary>
  /// Категории, которые нужно обработать. Заполняется при нажатии "Запустить".
 /// </summary>
        public List<FinishingCategory> SelectedCategories { get; private set; }

        /// <summary>Список выбранных помещений. Null = все помещения.</summary>
      public List<ElementId> SelectedRoomIds { get; set; }

     /// <summary>
     /// True — пользователь нажал "Выбрать на виде", окно закрылось без результата.
        /// Команда должна сделать PickObjects и снова открыть окно.
        /// </summary>
      public bool NeedRoomPick { get; private set; }
  
        public ObservableCollection<RoomParamMapping> Mappings { get; } =
   new ObservableCollection<RoomParamMapping>();

  public static readonly List<RoomParamMapping> DefaultMappings = new List<RoomParamMapping>
        {
            new RoomParamMapping("Номер",       "ПРО_Номер помещения"),
         new RoomParamMapping("ПРО_Этаж",        "ПРО_Этаж"),
            new RoomParamMapping("ПРО_Секция",          "ПРО_Секция"),
     new RoomParamMapping("ПРО_Группа спецификации", "ПРО_Группа спецификации"),
    };

     public RoomParamsToFloorWin(UIDocument uidoc)
        {
     _uidoc = uidoc;
     InitializeComponent();
       LoadMappings();
    DgMappings.ItemsSource = Mappings;
   DgMappings.RowEditEnding += OnRowEditEnding;
        }

        /// <summary>
        /// Конструктор для повторного открытия с уже выбранными помещениями.
        /// </summary>
       public RoomParamsToFloorWin(UIDocument uidoc, List<ElementId> preselectedRooms)
   : this(uidoc)
        {
   if (preselectedRooms != null && preselectedRooms.Count > 0)
   {
     SelectedRoomIds = preselectedRooms;
            // Ставим режим "выбранные" после загрузки компонентов
        Loaded += (s, e) =>
       {
    RbSelectedRooms.IsChecked = true;  // триггернет OnRoomModeChanged
     UpdateRoomCountLabel();
};
   }
        }

    // ?? Маппинг ??????????????????????????????????????????????????????????????

        private void OnRowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            // Откладываем сохранение на следующий цикл диспетчера —
      // чтобы DataGrid успел зафиксировать значение в модели
     Dispatcher.BeginInvoke(new System.Action(SaveMappings),
     System.Windows.Threading.DispatcherPriority.Background);
        }

        private void LoadMappings()
        {
        Mappings.Clear();
    string json = Settings.Default.RoomToFloorMappings;
            if (!string.IsNullOrWhiteSpace(json))
  {
          try
      {
      var saved = JsonConvert.DeserializeObject<List<RoomParamMapping>>(json);
            if (saved != null && saved.Count > 0)
           {
          foreach (var m in saved)
    Mappings.Add(m);
         return;
       }
    }
        catch { }
    }
      foreach (var m in DefaultMappings)
Mappings.Add(new RoomParamMapping(m.SourceParam, m.TargetParam));
     }

   private void SaveMappings()
        {
    DgMappings.CommitEdit(DataGridEditingUnit.Row, true);
            var list = new List<RoomParamMapping>(Mappings);
      Settings.Default.RoomToFloorMappings = JsonConvert.SerializeObject(list);
    Settings.Default.Save();
        }

        // ?? Режим помещений ???????????????????????????????????????????????????????

        private void OnRoomModeChanged(object sender, RoutedEventArgs e)
        {
    if (PanelRoomPick == null) return;
            PanelRoomPick.Visibility = RbSelectedRooms.IsChecked == true
         ? System.Windows.Visibility.Visible
      : System.Windows.Visibility.Collapsed;
     }

     private void OnPickRoomsClick(object sender, RoutedEventArgs e)
  {
            // Закрываем окно без DialogResult (null) — команда перехватит это
      // как сигнал "нужно сделать выбор помещений"
            NeedRoomPick = true;
            Close(); // DialogResult остаётся null
      }

 private void UpdateRoomCountLabel()
        {
  if (SelectedRoomIds == null || SelectedRoomIds.Count == 0)
      TbRoomCount.Text = "Помещения не выбраны";
            else
           TbRoomCount.Text = $"Выбрано помещений: {SelectedRoomIds.Count}";
     }

     // ?? Запуск ???????????????????????????????????????????????????????????????

        private void OnRunClick(object sender, RoutedEventArgs e)
        {
   // Проверяем что хотя бы одна категория выбрана
            var categories = new List<FinishingCategory>();
          if (CbFloors.IsChecked == true)   categories.Add(FinishingCategory.Floors);
            if (CbCeilings.IsChecked == true) categories.Add(FinishingCategory.Ceilings);
            if (CbWalls.IsChecked == true)    categories.Add(FinishingCategory.Walls);

            if (categories.Count == 0)
   {
        TbStatus.Text = "Выберите хотя бы одну категорию элементов.";
                return;
            }

            // Проверяем выбор помещений
    if (RbSelectedRooms.IsChecked == true &&
              (SelectedRoomIds == null || SelectedRoomIds.Count == 0))
            {
    TbStatus.Text = "Выберите помещения перед запуском.";
                return;
            }

          if (RbAllRooms.IsChecked == true)
    SelectedRoomIds = null;

      SaveMappings();
        SelectedCategories = categories;
            DialogResult = true;
            Close();
        }

        private void OnResetClick(object sender, RoutedEventArgs e)
        {
            Mappings.Clear();
     foreach (var m in DefaultMappings)
         Mappings.Add(new RoomParamMapping(m.SourceParam, m.TargetParam));
    TbStatus.Text = "Сброшено к значениям по умолчанию.";
   }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
     }

        // ?? Фильтр выбора ?????????????????????????????????????????????????????????

        private class RoomSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem) => elem is Room;
     public bool AllowReference(Reference reference, XYZ position) => false;
     }
    }
}
