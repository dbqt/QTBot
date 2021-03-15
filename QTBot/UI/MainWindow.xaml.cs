using System;
using System.Reflection;
using System.Windows;

namespace QTBot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            var v = GetRunningVersion();
            Title = $"QTBot - {v.Major}.{v.Minor}.{v.Build}";
        }

        private Version GetRunningVersion()
        {
            try
            {
                return Assembly.GetEntryAssembly().GetName().Version;
            }
            catch (Exception)
            {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }
    }
}
