using QTBot.Helpers;
using QTBot.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace QTBot.UI.Views
{
    /// <summary>
    /// Interaction logic for Timers.xaml
    /// </summary>
    public partial class Timers : UserControl, INotifyPropertyChanged
    {
        private List<TimerInternal> timersList;

        public event PropertyChangedEventHandler PropertyChanged;

        private object itemLock = new object();

        public List<TimerInternal> TimersList
        {
            get { return timersList; }
            set
            { 
                timersList = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.TimersList)));
            }
        }

        public Timers()
        {
            InitializeComponent();
            DataContext = this;

            this.TimersList = new List<TimerInternal>();

            // Load for the first time
            LoadTimers();
        }

        /// <summary>
        /// Save the timers from memory into the timer config files.
        /// </summary>
        private void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            lock (this.itemLock)
            {
                UpdateTimersValidity();

                if (this.TimersList.Any(item => item.IsInvalid))
                {
                    MainContent.Instance.ShowSimpleDialog("Errors in timers", "Please make there are no invalid values in timers.");
                }
                else
                {
                    ConfigManager.SaveTimers(new TimersModel() { Timers = this.TimersList.ConvertAll((item) => item.ToModel()) });
                    MainContent.Instance.ShowSimpleDialog("Timers saved!", "");
                }
            }
        }

        /// <summary>
        /// Read the timers config files and replace the in memory version.
        /// </summary>
        private void ReloadButtonClick(object sender, RoutedEventArgs e)
        {
            lock (this.itemLock)
            {
                LoadTimers();
                MainContent.Instance.ShowSimpleDialog("Timers reloaded!", "");
            }
        }

        /// <summary>
        /// Creates a new timer object in the UI.
        /// </summary>
        private void AddNewTimerClick(object sender, RoutedEventArgs e)
        {
            lock (this.itemLock)
            {
                AddTimer(new TimerModel());
            }
        }

        /// <summary>
        /// Deletes timer in memory.
        /// </summary>
        private void DeleteTimerClick(object sender, RoutedEventArgs e)
        {
            lock (this.itemLock)
            {
                var data = (TimerInternal)((Button)sender).DataContext;
                this.TimersList.RemoveAt(data.Index);
                UpdateTimersIndex();
                this.TimersListView.Items.Refresh();
            }
        }

        private void TextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTimersValidity();
        }

        private void IntegerUpDownValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            UpdateTimersValidity();
        }

        private void TextBoxSelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateTimersValidity();
        }

        /// <summary>
        /// Adds timer to the list and update the indexes correctly, then refreshes the view.
        /// </summary>
        private void AddTimer(TimerModel model)
        {
            this.TimersList.Add(new TimerInternal(model));
            UpdateTimersIndex();
            this.TimersListView.Items.Refresh();
        }

        /// <summary>
        /// Reads timers from config file, adds them to the list and refreshes the view.
        /// </summary>
        private void LoadTimers()
        {
            this.TimersList.Clear();

            var rawTimers = ConfigManager.ReadTimers();
            foreach (var timer in rawTimers.Timers)
            {
                AddTimer(timer);
            }
        }

        /// <summary>
        /// Iterates the entire list of timers in memory and updates the indexes.
        /// </summary>
        private void UpdateTimersIndex()
        {
            foreach (var timer in this.TimersList)
            {
                timer.Index = this.TimersList.IndexOf(timer);
            }
        }

        private void UpdateTimersValidity()
        {
            foreach (var timer in this.TimersList)
            {
                timer.UpdateValidity();
            }
        }

        public class TimerInternal : INotifyPropertyChanged
        {
            public int Index { get; set; } = -1;
            public string Name { get; set; } = "";
            public string Message { get; set; } = "";
            public int DelayMin { get; set; } = -1;
            public int OffsetMin { get; set; } = -1;
            public bool Active { get; set; } = false;
            public bool IsInvalid { get; set; } = false;

            public TimerInternal(TimerModel model)
            {
                this.Name = model.Name;
                this.Message = model.Message;
                this.DelayMin = model.DelayMin;
                this.OffsetMin = model.DelayMin;
                this.Active = model.Active;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public TimerModel ToModel()
            {
                return new TimerModel()
                {
                    Name = this.Name,
                    Message = this.Message,
                    DelayMin = this.DelayMin,
                    OffsetMin = this.OffsetMin,
                    Active = this.Active
                };
            }

            public void UpdateValidity()
            {
                this.IsInvalid = string.IsNullOrWhiteSpace(this.Name) || string.IsNullOrWhiteSpace(this.Message) || this.DelayMin <= 0 || this.OffsetMin < 0;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsInvalid)));
            }
        }
    }
}
