using QTBot.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;

namespace QTBot.UI.Views
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : UserControl
    {
        private bool isConnected = false;

        public Home()
        {
            InitializeComponent();

            QTCore.Instance.OnConnected += InstanceOnConnected;
            QTCore.Instance.OnDisconnected += InstanceOnDisconnected;

            CheckConfig();
        }

        private void CheckConfig()
        {
            QTCore.Instance.LoadConfigs();
            if (QTCore.Instance.IsMainConfigLoaded)
            {
                ConfigCheck.Visibility = Visibility.Collapsed;
                Connect.IsEnabled = true;
            }
            else
            {
                ConfigCheck.Visibility = Visibility.Visible;
                Connect.IsEnabled = false;
            }

            CurrentStreamerText.Text = "Bot will post to this channel: " + QTCore.Instance.CurrentChannelName;
            CurrentBotText.Text = "Bot will be posting as: " + QTCore.Instance.BotUserName;
        }

        #region Events

        private void InstanceOnConnected(object sender, EventArgs e)
        {
            Utilities.ExecuteOnUIThread(() =>
            {
                isConnected = true;
                Connect.IsEnabled = true;
                ConnectionStatus.Text = "Connected";
                Connect.Content = "Disconnect";
            });
        }

        private void InstanceOnDisconnected(object sender, EventArgs e)
        {
            Utilities.ExecuteOnUIThread(() =>
            {
                isConnected = false;
                Connect.IsEnabled = true;
                ConnectionStatus.Text = "Disconnected";
                Connect.Content = "Connect";
            });
        }

        private void OnConnectClick(object sender, RoutedEventArgs e)
        {
            if (isConnected)
            {
                Connect.IsEnabled = false;
                QTCore.Instance.Disconnect();
            }
            else
            {
                Connect.IsEnabled = false;
                QTCore.Instance.Setup();
            }
        }
        #endregion
    }
}
