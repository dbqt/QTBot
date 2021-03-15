using QTBot.CustomDLLIntegration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;

namespace QTBot.UI.Views
{
    /// <summary>
    /// Interaction logic for Integration.xaml
    /// </summary>
    public partial class Integration : UserControl, INotifyPropertyChanged
    {
        private List<IntegrationInternal> integrations;

        public event PropertyChangedEventHandler PropertyChanged;

        public List<IntegrationInternal> Integrations
        {
            get { return this.integrations; }
            set
            {
                integrations = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Integrations)));
            }
        }

        public Integration()
        {
            InitializeComponent();
            DataContext = this;

            LoadSettingsUI();
        }

        /// <summary>
        /// Loads the integrations from the helper
        /// </summary>
        private void LoadSettingsUI()
        {
            foreach (var integrationModel in IntegrationHelper.GetDLLIntegrations())
            {
                this.Integrations.Add(new IntegrationInternal(integrationModel));
            }
        }

        /// <summary>
        /// Internal representation of the integration for the view
        /// </summary>
        public class IntegrationInternal
        {
            public bool Active = false;
            public string Name = "";
            public string Definition = "";
            public string Version = "";
            public SettingsUI Settings = null;

            public IntegrationInternal(DLLIntegrationModel integration)
            {
                this.Active = integration.DllProperties.IsEnabled;
                this.Name = integration.DllIntegration.IntegrationName;
                this.Definition = integration.DllIntegration.IntegrationDefinition;
                this.Version = integration.DllIntegration.IntegrationVersion;
                this.Settings = IntegrationHelper.RetrieveDLLSettings(integration.DllIntegration);
            }
        }
    }
}
