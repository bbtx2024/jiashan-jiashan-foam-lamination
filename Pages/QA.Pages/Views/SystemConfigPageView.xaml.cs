using System.Windows.Controls;
using System.Windows.Input;

namespace QA.Pages.Views
{
    /// <summary>
    /// SystemConfigPageView.xaml 的交互逻辑
    /// </summary>
    public partial class SystemConfigPageView : UserControl
    {
        public SystemConfigPageView()
        {
            InitializeComponent();
        }

        private void PropertyGridView_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            this.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }
    }
}
