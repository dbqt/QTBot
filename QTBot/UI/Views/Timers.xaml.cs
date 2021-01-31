using QTBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QTBot.UI.Views
{
    /// <summary>
    /// Interaction logic for Timers.xaml
    /// </summary>
    public partial class Timers : UserControl
    {
        private List<TimerModel> timersList;
        private List<TimerModel> TimersList { 
            get { return timersList; }
            set { timersList = value; }
        }

        public Timers()
        {
            InitializeComponent();
            DataContext = this;

            this.TimersList = new List<TimerModel>();
            this.TimersList.Add(new TimerModel()
            {
                Active = false,
                Name = "Test name",
                Message = "message test",
                DelayMin = 60,
                OffsetMin = 0
            });

            this.TimersListView.ItemsSource = this.TimersList;
        }
    }
}
