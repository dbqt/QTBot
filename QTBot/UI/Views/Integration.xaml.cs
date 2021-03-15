using QTBot.CustomDLLIntegration;
using System.Collections.Generic;
using System.Windows.Controls;

namespace QTBot.UI.Views
{
    /// <summary>
    /// Interaction logic for Integration.xaml
    /// </summary>
    public partial class Integration : UserControl
    {
        private List<SettingsUI> settings = new List<SettingsUI>();
        public Integration()
        {
            InitializeComponent();

            foreach (var integrationModel in IntegrationHelper.GetDLLIntegrations())
            {
                settings.Add(IntegrationHelper.RetrieveDLLSettings(integrationModel.DllIntegration));
            }
        }
    }
}
