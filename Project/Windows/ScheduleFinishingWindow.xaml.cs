using System;
using System.Windows;
using AbimToolsMine.Properties;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Settings = AbimToolsMine.Properties.Settings;

namespace AbimToolsMine
{
    [Transaction(TransactionMode.Manual)]

    public class Pref_ScheduleFinishingWindow : IExternalCommand
    {
        public static ExternalCommandData CommandData { get; set; }
        public static ScheduleFinishingWindow window = null;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            if (window == null)
            {
                window = new ScheduleFinishingWindow();
                window.ShowDialog();
            }
            else
            {
                window.Activate();
            }
            window = null;
            return Result.Succeeded;
        }
    }

        public partial class ScheduleFinishingWindow : Window
    {
        public ScheduleFinishingWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            //Отделка
            RoomNumberParamBox.Text = Settings.Default.RoomNumberParam;
            RoomGroupParamBox.Text = Settings.Default.RoomGroupParam;
            RoomKeyParamBox.Text = Settings.Default.RoomKeyParam;
            StructureWidthBox.Text = Settings.Default.StructureColWidthMM.ToString();
            DivideBox.Text = Settings.Default.Divide.ToString();

            WallNameBox.Text = Settings.Default.WallNameParam;
            WallValueBox.Text = Settings.Default.WallValueParam;
            WallGroupBox.Text = Settings.Default.WallGroupParam;

            FloorNameBox.Text = Settings.Default.FloorNameParam;
            FloorValueBox.Text = Settings.Default.FloorValueParam;
            FloorGroupBox.Text = Settings.Default.FloorGroupParam;

            CeilingNameBox.Text = Settings.Default.CeilingNameParam;
            CeilingValueBox.Text = Settings.Default.CeilingValueParam;
            CeilingGroupBox.Text = Settings.Default.CeilingGroupParam;

            PlinthNameBox.Text = Settings.Default.PlinthNameParam;
            PlinthValueBox.Text = Settings.Default.PlinthValueParam;
            PlinthGroupBox.Text = Settings.Default.PlinthGroupParam;
            PlinthStringBox.Text = Settings.Default.PlinthString;

            StructureCompBox.Text = Settings.Default.StructureComp;
            DimTypeBox.Text = Settings.Default.DimType;
            
            //Полы
            FloorStructureCompBox.Text = Settings.Default.FloorStructureComp;
            FloorRoomGroupParamBox.Text = Settings.Default.FloorRoomGroupParam;
            FloorKeyParamBox.Text = Settings.Default.FloorRoomKeyParam;
            FoorLayerNameBox.Text = Settings.Default.FloorLayerName;
            ViewTemplateNameBox.Text = Settings.Default.viewTemplateName;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.RoomNumberParam = RoomNumberParamBox.Text;
            Settings.Default.RoomGroupParam = RoomGroupParamBox.Text;
            Settings.Default.RoomKeyParam = RoomKeyParamBox.Text;

            if (int.TryParse(StructureWidthBox.Text, out int structureWidth))
                Settings.Default.StructureColWidthMM = structureWidth;

            if (int.TryParse(DivideBox.Text, out int divide))
                Settings.Default.Divide = divide;

            Settings.Default.WallNameParam = WallNameBox.Text;
            Settings.Default.WallValueParam = WallValueBox.Text;
            Settings.Default.WallGroupParam = WallGroupBox.Text;

            Settings.Default.FloorNameParam = FloorNameBox.Text;
            Settings.Default.FloorValueParam = FloorValueBox.Text;
            Settings.Default.FloorGroupParam = FloorGroupBox.Text;

            Settings.Default.CeilingNameParam = CeilingNameBox.Text;
            Settings.Default.CeilingValueParam = CeilingValueBox.Text;
            Settings.Default.CeilingGroupParam = CeilingGroupBox.Text;

            Settings.Default.PlinthNameParam = PlinthNameBox.Text;
            Settings.Default.PlinthValueParam = PlinthValueBox.Text;
            Settings.Default.PlinthGroupParam = PlinthGroupBox.Text;
            Settings.Default.PlinthString = PlinthStringBox.Text;
            
            Settings.Default.StructureComp = StructureCompBox.Text;
            Settings.Default.DimType = DimTypeBox.Text;

            Settings.Default.Save();
            this.DialogResult = true;
            this.Close();
        }
        private void Floor_OK_Click(object sender, RoutedEventArgs e)
        {
            //Полы
            Settings.Default.FloorStructureComp = FloorStructureCompBox.Text;
            Settings.Default.FloorRoomGroupParam = FloorRoomGroupParamBox.Text;
            Settings.Default.FloorRoomKeyParam = FloorKeyParamBox.Text;
            Settings.Default.FloorLayerName= FoorLayerNameBox.Text;
            Settings.Default.viewTemplateName = ViewTemplateNameBox.Text;

        Settings.Default.Save();
            this.DialogResult = true;
            this.Close();
        }
    }
}