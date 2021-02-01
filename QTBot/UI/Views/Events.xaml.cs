using QTBot.Core;
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
    /// Interaction logic for Events.xaml
    /// </summary>
    public partial class Events : UserControl
    {
        public Events()
        {
            InitializeComponent();

            var options = QTCore.Instance.TwitchOptions;
            this.IsRedemptionInChatBox.IsChecked = options.IsRedemptionInChat;
            this.IsTagUserBox.IsChecked = options.IsRedemptionTagUser;
            this.UserNameTextBox.Text = options.RedemptionTagUser;
            this.IsAutoShoutOutBox.IsChecked = options.IsAutoShoutOutHost;
            this.GreetingMessage.Text = options.GreetingMessage;
        }

        private void OnSaveButtonClick(object sender, RoutedEventArgs e)
        {
            var twitchOptions = new TwitchOptions()
            {
                IsRedemptionInChat = this.IsRedemptionInChatBox.IsChecked ?? false,
                IsRedemptionTagUser = this.IsTagUserBox.IsChecked ?? false,
                RedemptionTagUser = this.UserNameTextBox.Text,
                IsAutoShoutOutHost = this.IsAutoShoutOutBox.IsChecked ?? false,
                GreetingMessage = this.GreetingMessage.Text ?? "Hai hai, I'm connected and ready to go!"
            };

            QTCore.Instance.SetupTwitchOptions(twitchOptions);
        }
    }
}
