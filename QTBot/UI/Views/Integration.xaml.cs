using Microsoft.Extensions.Logging;
using QTBot.CustomDLLIntegration;
using QTBot.Helpers;
using System;
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

        private IntegrationInternal activeIntegration;

        public event PropertyChangedEventHandler PropertyChanged;

        public List<IntegrationInternal> IntegrationsList
        {
            get { return this.integrations; }
            set
            {
                this.integrations = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IntegrationsList)));
            }
        }

        public string CurrentIntegrationName { get; set; }

        public Integration()
        {
            InitializeComponent();
            DataContext = this;

            this.IntegrationsList = new List<IntegrationInternal>();

            LoadIntegrationsList();
        }

        /// <summary>
        /// Loads the integrations from the helper
        /// </summary>
        private void LoadIntegrationsList()
        {
            Utilities.Log(LogLevel.Information, "Integration.xaml.cs: Loading integrations list...");

            foreach (var existingIntegration in this.IntegrationsList)
            {
                Utilities.Log(LogLevel.Information, $"Integration.xaml.cs: Removing integration {existingIntegration.Name} from the list");
                existingIntegration.OnActiveChanged -= IntegrationInternalOnActiveChanged;
            }
            this.IntegrationsList.Clear();

            foreach (var integrationModel in IntegrationHelper.GetDLLIntegrations())
            {
                Utilities.Log(LogLevel.Information, $"Integration.xaml.cs: Adding integration {integrationModel.DllIntegration.IntegrationName} to the list");
                var integrationInternal = new IntegrationInternal(integrationModel);
                integrationInternal.OnActiveChanged += IntegrationInternalOnActiveChanged;
                this.IntegrationsList.Add(integrationInternal);
            }

            IntegrationsListView.Items.Refresh();

            if (this.IntegrationsList.Count > 0)
            {
                IntegrationsListView.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Generates the settings view for the <see cref="SettingsUI"/> provided by the selected integration
        /// </summary>
        private void GenerateSettingsUIView(SettingsUI settings)
        {
            Utilities.Log(LogLevel.Information, "Integration.xaml.cs: Generating integration settings UI view");
            IntegrationSettingsView.Children.Clear();

            foreach (var section in settings.Sections)
            {
                Utilities.Log(LogLevel.Information, $"Integration.xaml.cs: - Generating section {section.SectionName}");
                var sectionTitle = new TextBlock() { Text = section.SectionName, FontWeight = System.Windows.FontWeight.FromOpenTypeWeight(700) };
                IntegrationSettingsView.Children.Add(sectionTitle);

                // Generate content of the section
                foreach (var element in section.SectionElements)
                {
                    Utilities.Log(LogLevel.Information, $"Integration.xaml.cs: -- Generating element {element.UIPropertyName} of type {element.GetType().Name}");
                    // Casting type to its name and use that because switching on a type directly is not possible
                    switch (element.GetType().Name)
                    {
                        case nameof(UICheckbox):
                            {
                                // Create UI
                                var checkBox = new CheckBox();
                                checkBox.Content = element.UIText;

                                // Set existing value
                                Utilities.Log(LogLevel.Information, $"Integration.xaml.cs: --- Existing UIValue is {element.UIValue}");
                                if (element.UIValue != null)
                                {
                                    checkBox.IsChecked = (bool)element.UIValue;
                                }
                                else
                                {
                                    checkBox.IsChecked = false;
                                }

                                // Hook events
                                checkBox.Checked += (sender, e) =>
                                {
                                    element.UIValue = true;
                                    Utilities.Log(LogLevel.Information, $"Integration.xaml.cs: {element.UIPropertyName} was updated with {element.UIValue}");
                                };
                                checkBox.Unchecked += (sender, e) =>
                                {
                                    element.UIValue = false;
                                    Utilities.Log(LogLevel.Information, $"Integration.xaml.cs: {element.UIPropertyName} was updated with {element.UIValue}");
                                };

                                IntegrationSettingsView.Children.Add(checkBox);

                                break;
                            }
                        case nameof(UIRadioButton):
                            {
                                AddLabel(element.UIText);
                                var uiRadioButton = element as UIRadioButton;
                                Utilities.Log(LogLevel.Information, $"Integration.xaml.cs: --- Existing UIValue is {element.UIValue}");

                                foreach (var item in uiRadioButton.Options)
                                {
                                    // Create UI
                                    var radioButton = new RadioButton();
                                    radioButton.GroupName = element.UIText;
                                    radioButton.Content = item.Key;

                                    // Set existing value, if UIValue is null, use empty string as comparison, the key is probably not an empty string
                                    radioButton.IsChecked = item.Key == (uiRadioButton.UIValue?.ToString() ?? string.Empty);

                                    // Hook events
                                    radioButton.Checked += (sender, e) =>
                                    {
                                        uiRadioButton.UIValue = item.Key;
                                        Utilities.Log(LogLevel.Information, $"Integration.xaml.cs: {element.UIPropertyName} was updated with {element.UIValue}");
                                    };

                                    IntegrationSettingsView.Children.Add(radioButton);
                                }

                                break;
                            }
                        case nameof(UITextBox):
                            {
                                // Create UI
                                AddLabel(element.UIText);
                                var textBox = new TextBox();

                                // Set existing value
                                Utilities.Log(LogLevel.Information, $"Integration.xaml.cs: --- Existing UIValue is {element.UIValue}");
                                textBox.Text = element.UIValue?.ToString() ?? string.Empty;

                                // Hook events
                                textBox.TextChanged += (sender, e) =>
                                {
                                    element.UIValue = textBox.Text;
                                    Utilities.Log(LogLevel.Information, $"Integration.xaml.cs: {element.UIPropertyName} was updated with {element.UIValue}");
                                };

                                IntegrationSettingsView.Children.Add(textBox);

                                break;
                            }
                        case nameof(UISelectionDropdown):
                            {
                                // Create UI
                                AddLabel(element.UIText);
                                var uiSelection = element as UISelectionDropdown;
                                var selection = new ComboBox();
                                selection.ItemsSource = uiSelection.List.ConvertAll(e => e.Key);

                                // Set existing values, default to 0 if UIValue is null or can't be found
                                Utilities.Log(LogLevel.Information, $"Integration.xaml.cs: --- Existing UIValue is {element.UIValue}");
                                var selectedIndex = uiSelection.List.FindIndex(e => e.Key == (uiSelection.UIValue?.ToString() ?? string.Empty));
                                selection.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;

                                // Hook events
                                selection.SelectionChanged += (sender, e) =>
                                {
                                    uiSelection.UIValue = uiSelection.List[selection.SelectedIndex].Key;
                                    Utilities.Log(LogLevel.Information, $"Integration.xaml.cs: {element.UIPropertyName} was updated with {element.UIValue}");
                                };

                                IntegrationSettingsView.Children.Add(selection);

                                break;
                            }
                        case nameof(UIEditableDropdown):
                            {
                                // Create UI
                                AddLabel(element.UIText);
                                var uiSelection = element as UIEditableDropdown;

                                var selection = new ComboBox();
                                selection.ItemsSource = uiSelection.List.ConvertAll(e => e.Key);

                                IntegrationSettingsView.Children.Add(selection);

                                AddLabel(uiSelection.ValueLabel);
                                var textBox = new TextBox();

                                // Set existing values
                                foreach (var item in uiSelection.List)
                                {
                                    Utilities.Log(LogLevel.Information, $"Integration.xaml.cs: --- Existing value for {item.Key} is {item.Value}");
                                }
                                selection.SelectedIndex = 0; // Always pre-select the first one
                                textBox.Text = uiSelection.List[selection.SelectedIndex].Value?.ToString() ?? string.Empty;

                                // Hook all events after creating all UI
                                textBox.TextChanged += (sender, e) =>
                                {
                                    var selectedItem = uiSelection.List[selection.SelectedIndex];
                                    uiSelection.List[selection.SelectedIndex] = new KeyValuePair<string, object>(selectedItem.Key, textBox.Text);
                                    Utilities.Log(LogLevel.Information, $"Integration.xaml.cs: {element.UIPropertyName}'s item with key ({uiSelection.List[selection.SelectedIndex].Key} was updated with {uiSelection.List[selection.SelectedIndex].Value}");
                                };

                                selection.SelectionChanged += (sender, e) =>
                                {
                                    textBox.Text = uiSelection.List[selection.SelectedIndex].Value.ToString();
                                    Utilities.Log(LogLevel.Information, $"Integration.xaml.cs: {element.UIPropertyName}'s selected item changed to {uiSelection.List[selection.SelectedIndex].Key} with existing value: {uiSelection.List[selection.SelectedIndex].Value}");
                                };

                                IntegrationSettingsView.Children.Add(textBox);

                                break;
                            }
                        case nameof(UISlider):
                            {
                                // Create UI
                                AddLabel(element.UIText);
                                var uiSlider = ((UISlider)element);
                                var slider = new Slider();
                                slider.Maximum = uiSlider.MaxValue;
                                slider.Minimum = uiSlider.MinValue;
                                slider.Value = uiSlider.CurrentValue;
                                slider.TickFrequency = uiSlider.IncrementValue;
                                slider.IsSnapToTickEnabled = true;

                                // Set existing values
                                Utilities.Log(LogLevel.Information, $"Integration.xaml.cs: --- Existing current value is {uiSlider.CurrentValue}");
                                slider.Value = uiSlider.CurrentValue;
                                uiSlider.UIValue = uiSlider.CurrentValue;

                                // Hook events
                                slider.ValueChanged += (sender, e) =>
                                {
                                    uiSlider.CurrentValue = Convert.ToInt32(slider.Value);
                                    uiSlider.UIValue = uiSlider.CurrentValue;
                                    Utilities.Log(LogLevel.Information, $"Integration.xaml.cs: {element.UIPropertyName} was updated with {element.UIValue}");
                                };

                                IntegrationSettingsView.Children.Add(slider);
                                break;
                            }
                        case nameof(UIButton):
                        case nameof(UITable):
                        default:
                            Utilities.Log(LogLevel.Warning, $"Integration.xaml.cs: Skipped trying to build unsupported UI of type: {element.GetType()}");
                            continue;
                    }
                }

                // End section with some spacing
                var separator = new Separator();
                separator.Height = 18;
                separator.Visibility = System.Windows.Visibility.Hidden;
                IntegrationSettingsView.Children.Add(separator);
            }
        }

        private void IntegrationsListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.activeIntegration = this.IntegrationsListView.SelectedItem as IntegrationInternal;
            this.CurrentIntegrationName = this.activeIntegration.Name;
            Utilities.Log(LogLevel.Information, $"Integration.xaml.cs: Selected integration: {this.CurrentIntegrationName}");
            GenerateSettingsUIView(this.activeIntegration.Settings);
        }

        private void OnSaveButtonClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (IntegrationHelper.SaveDLLSettingsToFile(this.activeIntegration.Integration, this.activeIntegration.Settings))
            {
                Utilities.ShowMessage("Integration settings was succesfully saved!");
            }
            else
            {
                Utilities.ShowMessage("Integration settings could not save!");
            }
        }

        private void IntegrationInternalOnActiveChanged(object sender, bool isActive)
        {
            IntegrationInternal integrationInternal = sender as IntegrationInternal;
            Utilities.Log(LogLevel.Information, $"Integration.xaml.cs: {integrationInternal.Integration.IntegrationName} is enabled changed to {isActive}");
            if (isActive)
            {
                IntegrationHelper.EnableDLL(integrationInternal.DllGuidID);
            }
            else
            {
                IntegrationHelper.DisableDLL(integrationInternal.DllGuidID);
            }
        }

        #region UI Helpers

        /// <summary>
        /// Adds a label with the provided text to the <see cref="IntegrationSettingsView"/> stack
        /// </summary>
        private void AddLabel(string text)
        {
            IntegrationSettingsView.Children.Add(new TextBlock() { Text = text });
        }

        #endregion UI Helpers

        /// <summary>
        /// Internal representation of the integration for the view
        /// </summary>
        public class IntegrationInternal
        {
            private bool active = false;

            public bool Active
            { 
                get
                {
                    return this.active;
                }
                set
                {
                    this.active = value;
                    OnActiveChanged?.Invoke(this, this.active);
                }
            }
            public string Name { get; set; }
            public string Definition { get; set; }
            public string Version { get; set; }
            public SettingsUI Settings { get; set; } = null;
            public DLLIntegrationInterface Integration { get; set; }
            public Guid DllGuidID { get; set; }

            public event EventHandler<bool> OnActiveChanged;

            public IntegrationInternal(DLLIntegrationModel integration)
            {
                this.Active = integration.DllProperties.IsEnabled;
                this.Name = integration.DllIntegration.IntegrationName;
                this.Definition = integration.DllIntegration.IntegrationDefinition;
                this.Version = integration.DllIntegration.IntegrationVersion;
                this.Settings = IntegrationHelper.RetrieveDLLSettings(integration.DllIntegration);
                this.Integration = integration.DllIntegration;
                this.DllGuidID = integration.DllProperties.DllGuidID;
            }
        }
    }
}
