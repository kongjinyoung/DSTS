using DevExpress.Xpf.Core;
using DevExpress.Xpf.WindowsUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ApplicationDSTS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ThemedWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            App app = Application.Current as App;
            app.MainWindow = this;
            NaviFrame.Content = app.MainView;
        }
        // 페이지 이동
        private void btn_monitoring_Click(object sender, RoutedEventArgs e)
        {
            App app = Application.Current as App;
            app.MainWindow = this;
            NaviFrame.Content = app.MainView;
        }

        private void btn_history_Click(object sender, RoutedEventArgs e)
        {

        }

        // 편의 기능
        private void InfoBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ExitBtn_Click(object sender, RoutedEventArgs e)
        {
            App a = Application.Current as App;

            MessageBoxResult msgResult = WinUIMessageBox.Show(Window.GetWindow(a.MainWindow), "모니터링 중입니다.\r\n그래도 종료 하시겠습니까?", null, MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None, FloatingMode.Window);

            if (msgResult == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }

    }
}
