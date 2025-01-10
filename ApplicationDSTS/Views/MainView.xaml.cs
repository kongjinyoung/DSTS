using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SciChart.Charting;

namespace ApplicationDSTS.Views
{
    /// <summary>
    /// Interaction logic for View1.xaml
    /// </summary>
    public partial class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();

            var customTheme = new MyCustomTheme();
            ThemeManager.AddTheme("Kong", customTheme);
            ThemeManager.SetTheme(Chart1, "Kong");
        }
    }
}
