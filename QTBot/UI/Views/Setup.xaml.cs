using QTBot.Helpers;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace QTBot.UI.Views
{
    /// <summary>
    /// Interaction logic for Setup.xaml
    /// </summary>
    public partial class Setup : UserControl
    {
        private bool isConnected = false;

        public Setup()
        {
            InitializeComponent();

            QTCore.Instance.OnConnected += InstanceOnConnected;
            QTCore.Instance.OnDisconnected += InstanceOnDisonnected;

            CheckConfig();
        }

        ~Setup()
        {
            QTCore.Instance.OnConnected -= InstanceOnConnected;
            QTCore.Instance.OnDisconnected -= InstanceOnDisonnected;
        }

        private void CheckConfig()
        {
            QTCore.Instance.LoadConfigs();
            if (QTCore.Instance.IsMainConfigLoaded)
            {
                ConfigCheck.Visibility = Visibility.Collapsed;
            }
            else
            {
                ConfigCheck.Visibility = Visibility.Visible;
            }

            CurrentStreamerText.Text = "Bot will post to this channel: " + QTCore.Instance.CurrentChannelName;
            CurrentBotText.Text = "Bot will be posting as: " + QTCore.Instance.BotUserName;
        }

        private void InstanceOnConnected(object sender, EventArgs e)
        {
            Utilities.ExecuteOnUIThread(() =>
            {
                isConnected = true;
                TestMessage.IsEnabled = isConnected;
                TestRedeem.IsEnabled = isConnected;
            });
        }

        private void InstanceOnDisonnected(object sender, EventArgs e)
        {
            Utilities.ExecuteOnUIThread(() =>
            {
                isConnected = false;
                TestMessage.IsEnabled = isConnected;
                TestRedeem.IsEnabled = isConnected;
            });
        }

        private void TestMessageClick(object sender, RoutedEventArgs e)
        {
            QTCore.Instance.TestMessage();
        }

        private void TestRedeemClick(object sender, RoutedEventArgs e)
        {
            QTCore.Instance.TestRedemption1();
        }

        private void OnOpenConfigClick(object sender, RoutedEventArgs e)
        {
            Process.Start(ConfigManager.GetConfigDirectory());
        }

        private void OnReloadConfigClick(object sender, RoutedEventArgs e)
        {
            CheckConfig();
        }

        private void ToggleInstructionPanel(object sender, RoutedEventArgs e)
        {
            InstructionPanel.Visibility = InstructionPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
