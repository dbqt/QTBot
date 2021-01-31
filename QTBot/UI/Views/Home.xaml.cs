using QTBot.Helpers;
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
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : UserControl
    {
        private bool isConnected = false;

        public Home()
        {
            InitializeComponent();

            QTCore.Instance.OnConnected += InstanceOnConnected;
            QTCore.Instance.OnDisonnected += InstanceOnDisonnected;

            CheckConfig();
        }

        private void CheckConfig()
        {
            QTCore.Instance.LoadConfigs();
            if (QTCore.Instance.IsMainConfigLoaded)
            {
                this.ConfigCheck.Visibility = Visibility.Collapsed;
                this.Connect.IsEnabled = true;
            }
            else
            {
                this.ConfigCheck.Visibility = Visibility.Visible;
                this.Connect.IsEnabled = false;
            }

            this.CurrentStreamerText.Text = "Bot will post to this channel: " + QTCore.Instance.CurrentChannelName;
            this.CurrentBotText.Text = "Bot will be posting as: " + QTCore.Instance.BotUserName;
        }

        #region Events

        private void InstanceOnConnected(object sender, EventArgs e)
        {
            Utilities.ExecuteOnUIThread(() =>
            {
                this.isConnected = true;
                this.ConnectionStatus.Text = "Connected";
                this.Connect.Content = "Disconnect";
            });
        }

        private void InstanceOnDisonnected(object sender, EventArgs e)
        {
            Utilities.ExecuteOnUIThread(() =>
            {
                this.isConnected = false;
                this.ConnectionStatus.Text = "Disconnected";
                this.Connect.Content = "Connect";
            });
        }

        private void OnConnectClick(object sender, RoutedEventArgs e)
        {
            if (this.isConnected)
            {
                QTCore.Instance.Disconnect();
            }
            else
            {
                _ = QTCore.Instance.Setup();
            }
        }
        #endregion
    }
}
