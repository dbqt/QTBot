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

            QTCore.Instance.OnConnectingStatusChanged += InstanceOnConnectingStatusChanged;
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
                ConnectionStatus.Text = "Connected";
                Connect.Content = "Disconnect";
            });
        }

        private void InstanceOnDisconnected(object sender, EventArgs e)
        {
            Utilities.ExecuteOnUIThread(() =>
            {
                isConnected = false;
                ConnectionStatus.Text = "Disconnected";
                Connect.Content = "Connect";
            });
        }

        private void InstanceOnConnectingStatusChanged(object sender, bool isConnecting)
        {
            // Disable button is connecting
            Connect.IsEnabled = !isConnecting;
        }

        private void OnConnectClick(object sender, RoutedEventArgs e)
        {
            if (isConnected)
            {
                QTCore.Instance.Disconnect();
            }
            else
            {
                QTCore.Instance.Setup();
            }
        }
        #endregion
    }
}
