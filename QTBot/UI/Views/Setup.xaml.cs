using QTBot.Helpers;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
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
                this.ConfigCheck.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.ConfigCheck.Visibility = Visibility.Visible;
            }

            this.CurrentStreamerText.Text = "Bot will post to this channel: " + QTCore.Instance.CurrentChannelName;
            this.CurrentBotText.Text = "Bot will be posting as: " + QTCore.Instance.BotUserName;
        }

        private void InstanceOnConnected(object sender, EventArgs e)
        {
            Utilities.ExecuteOnUIThread(() =>
            {
                this.isConnected = true;
                this.TestMessage.IsEnabled = this.isConnected;
                this.TestRedeem.IsEnabled = this.isConnected;
            });
        }

        private void InstanceOnDisonnected(object sender, EventArgs e)
        {
            Utilities.ExecuteOnUIThread(() =>
            {
                this.isConnected = false;
                this.TestMessage.IsEnabled = this.isConnected;
                this.TestRedeem.IsEnabled = this.isConnected;
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
            this.InstructionPanel.Visibility = this.InstructionPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
