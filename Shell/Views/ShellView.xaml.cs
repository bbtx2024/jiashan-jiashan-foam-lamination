using System.Windows;
using System.Windows.Input;

namespace QA.IntelligentEquipment.Views
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ShellView : Window
    {
        public ShellView()
        {
            InitializeComponent();
            this.MaxWidth = SystemParameters.WorkArea.Width;
            this.MaxHeight = SystemParameters.WorkArea.Height;
        }

        private void Shell_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && this.WindowState != WindowState.Maximized)
            {
                this.DragMove();
            }

            if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Left)
            {
                this.WindowState = this.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
        }

        private void ShotcutKey_TestFunction1(object sender, CanExecuteRoutedEventArgs e)
        {
            //Console.WriteLine("Shortcut Key Test1");
        }

        private void ShotcutKey_TestFunction2(object sender, CanExecuteRoutedEventArgs e)
        {
            //Console.WriteLine("Shortcut Key Test2");
        }

        private void ShotcutKey_UpdateFireware(object sender, CanExecuteRoutedEventArgs e)
        {
            //Console.WriteLine("Shortcut Key Update");
        }
    }
}
