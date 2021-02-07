using QTBot.Core;
using QTBot.Helpers;
using QTBot.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static QTBot.Core.QTEventsManager;

namespace QTBot.UI.Views
{
    /// <summary>
    /// Interaction logic for Events.xaml
    /// </summary>
    public partial class Events : UserControl, INotifyPropertyChanged
    {
        private List<EventInternal> eventsList;

        private object itemLock = new object();

        public event PropertyChangedEventHandler PropertyChanged;

        public List<EventInternal> EventsList
        {
            get { return eventsList; }
            set
            {
                eventsList = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.EventsList)));
            }
        }

        public Events()
        {
            InitializeComponent();
            this.DataContext = this;
            this.EventsList = new List<EventInternal>();

            LoadEvents();
        }

        /// <summary>
        /// Reads events from config file, adds them to the list and refreshes the view.
        /// </summary>
        private void LoadEvents()
        {
            // Legacy events
            var options = QTCore.Instance.TwitchOptions;
            this.IsRedemptionInChatBox.IsChecked = options.IsRedemptionInChat;
            this.IsTagUserBox.IsChecked = options.IsRedemptionTagUser;
            this.UserNameTextBox.Text = options.RedemptionTagUser;
            this.IsAutoShoutOutBox.IsChecked = options.IsAutoShoutOutHost;
            this.GreetingMessage.Text = options.GreetingMessage;

            // Custom events
            this.EventsList.Clear();
            var rawEvents = ConfigManager.ReadEvents();
            foreach (var item in rawEvents.Events)
            {
                AddNewEvent(new EventInternal(item));
            }
        }

        /// <summary>
        /// Adds a new event in memory.
        /// </summary>
        private void AddNewEvent(EventInternal newEvent)
        {
            this.EventsList.Add(newEvent);
            UpdateEventsIndex();
            this.EventsListView.Items.Refresh();
        }

        /// <summary>
        /// Iterates the entire list of events in memory and updates the validity.
        /// </summary>
        private void UpdateValidity()
        {
            foreach (var eventItem in this.EventsList)
            {
                eventItem.UpdateValidity();
            }
        }

        /// <summary>
        /// Iterates the entire list of events in memory and updates the indexes.
        /// </summary>
        private void UpdateEventsIndex()
        {
            foreach (var eventItem in this.EventsList)
            {
                eventItem.Index = this.EventsList.IndexOf(eventItem);
            }
        }

        #region Callbacks
        private void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            lock (this.itemLock)
            {
                UpdateValidity();

                if (this.EventsList.Any(item => item.IsInvalid))
                {
                    MainContent.Instance.ShowSimpleDialog("Errors in events", "Please make there are no invalid values in events.");
                }
                else
                {
                    // Legacy events
                    var twitchOptions = new TwitchOptions()
                    {
                        IsRedemptionInChat = this.IsRedemptionInChatBox.IsChecked ?? false,
                        IsRedemptionTagUser = this.IsTagUserBox.IsChecked ?? false,
                        RedemptionTagUser = this.UserNameTextBox.Text,
                        IsAutoShoutOutHost = this.IsAutoShoutOutBox.IsChecked ?? false,
                        GreetingMessage = this.GreetingMessage.Text ?? "Hai hai, I'm connected and ready to go!"
                    };
                    ConfigManager.SaveTwitchOptionsConfigs(twitchOptions);

                    // Custom events
                    ConfigManager.SaveEvents(new EventsModel() { Events = this.EventsList.ConvertAll(item => item.ToModel()) });

                    MainContent.Instance.ShowSimpleDialog("Events saved!", "");
                }
            }
        }

        private void ReloadButtonClick(object sender, RoutedEventArgs e)
        {
            lock(this.itemLock)
            {
                LoadEvents();
                MainContent.Instance.ShowSimpleDialog("Events reloaded!", "");
            }
        }

        private void LegacyEventToggleClick(object sender, RoutedEventArgs e)
        {
            if (this.LegacyEventContainer.Visibility != Visibility.Visible)
            {
                this.LegacyEventContainer.Visibility = Visibility.Visible;
                this.LegacyEventToggleIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.ExpandLess;
            }
            else
            {
                this.LegacyEventContainer.Visibility = Visibility.Collapsed;
                this.LegacyEventToggleIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.ExpandMore;
            }
        }

        private void CustomEventToggleClick(object sender, RoutedEventArgs e)
        {
            if (this.CustomEventsContainer.Visibility != Visibility.Visible)
            {
                this.CustomEventsContainer.Visibility = Visibility.Visible;
                this.CustomEventToggleIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.ExpandLess;
            }
            else
            {
                this.CustomEventsContainer.Visibility = Visibility.Collapsed;
                this.CustomEventToggleIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.ExpandMore;
            }
        }

        private void AddNewEventButtonClick(object sender, RoutedEventArgs e)
        {
            AddNewEvent(new EventInternal(new EventModel()));
        }

        private void DeleteEventClick(object sender, RoutedEventArgs e)
        {
            lock (this.itemLock)
            {
                var data = (EventInternal)((Button)sender).DataContext;
                this.EventsList.RemoveAt(data.Index);
                UpdateEventsIndex();
                this.EventsListView.Items.Refresh();
            }
        }

        private void TextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateValidity();
        }

        private void EventTypeComboBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateValidity();
        }

        private void HelpButtonClick(object sender, RoutedEventArgs e)
        {
            Utilities.ShowMessage(
@"For each event, you can customize the message and use optional parameter with {{}} that will be replaced with the correct info.

None events
These events are not used.

Follow events
{{user}} : Display name of the user that followed

Subscription events
{{user}} : Display name of the user that subscribed or was gifted a Subscription
{{month}} : Cumulative months count
{{tier}} : Subscription tier (Prime, Tier 1, Tier 2, Tier 3)

Raid events
{{user}} : Display name of the user that raided
{{count}} : Number of users participating in the raid

Bits events
{{user}} : Display name of the user that gave bits
{{bits}} : Amount of bits given
{{total_bits}} : Total amount of bits given by the user on the  channel
{{message}} : Message sent by the user giving bits

Reward redeems events
*Name: title of the reward must match, but casing is not important
{{user}} : Display name of the user that redeemed the Reward
{{reward}} : Title name of the reward
{{message}} : Message sent by the user with the reward (if any)

Greeting events
*Name: username of the user to greet must match, but casing is not important
{{user}} : Display name of the user that just joined chat for the first time this session");
        }
        #endregion Callbacks

        /// <summary>
        /// Representation of an event for the UI
        /// </summary>
        public class EventInternal : INotifyPropertyChanged
        {
            private EventType type = EventType.None;

            public Dictionary<EventType, string> EventTypeEnumToString { get; } = new Dictionary<EventType, string>()
            {
                {EventType.None, "None"},
                {EventType.Greeting, "Greeting" },
                {EventType.Redeem, "Redeem" },
                {EventType.Follow, "Follow" },
                {EventType.Subscription, "Subscription" },
                {EventType.Bits, "Bits" },
                {EventType.Raid, "Raid" },
            };

            public EventType Type
            {
                get
                {
                    return this.type;
                }
                set
                {
                    this.type = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Type)));
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsOptionNeeded)));
                }
            }

            public int Index { get; set; } = -1;
            public string Message { get; set; } = "";
            public string Option { get; set; } = "";
            public bool Active { get; set; } = true;
            public bool IsInvalid { get; set; } = false;
            public bool IsOptionNeeded => Type == EventType.Greeting || Type == EventType.Redeem;

            public event PropertyChangedEventHandler PropertyChanged;

            public EventInternal(EventModel model)
            {
                this.Type = model.Type;
                this.Message = model.Message;
                this.Option = model.Option;
                this.Active = model.Active;
            }

            /// <summary>
            /// Converts the <see cref="EventInternal"/> to a <see cref="EventModel"/>
            /// </summary>
            public EventModel ToModel()
            {
                return new EventModel()
                {
                    Type = this.Type,
                    Message = this.Message,
                    Option = this.Option,
                    Active = this.Active
                };
            }

            /// <summary>
            /// Updates the <see cref="IsInvalid"/> property of the event
            /// </summary>
            public void UpdateValidity()
            {
                // Is invalid if message is empty OR option is needed but empty
                this.IsInvalid = string.IsNullOrWhiteSpace(this.Message) || (this.IsOptionNeeded && string.IsNullOrWhiteSpace(this.Option));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsInvalid)));
            }
        }

        private void OnListViewScroll(object sender, MouseWheelEventArgs e)
        {
            if (!e.Handled)
            {
                e.Handled = true;
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                var parent = ((Control)sender).Parent as UIElement;
                parent.RaiseEvent(eventArg);
            }
        }
    }
}
