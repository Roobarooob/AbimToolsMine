using System;
using System.Windows;
using System.Windows.Threading;

namespace AbimToolsMine
{
    public partial class ProgressWindow : Window
    {
        public ProgressWindow()
      {
   InitializeComponent();
        }

        /// <summary>
        /// message Ч текст над баром, current/total Ч позици€.
        /// ћожно вызывать из любого потока.
        /// </summary>
        public void UpdateProgress(string message, int current, int total)
        {
          Dispatcher.Invoke(new Action(() =>
      {
          TbLabel.Text   = message;
         Pb.Maximum     = total > 0 ? total : 1;
                Pb.Value   = current;
   int pct     = total > 0 ? (int)Math.Round(current * 100.0 / total) : 0;
    TbPercent.Text = pct + "%";
    }),
            DispatcherPriority.Background);
      }
    }
}
