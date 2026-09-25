using System.Windows;
using System.Windows.Controls;

namespace QA.UserControls.Controls
{
    public class CavityItem : ContentControl
    {
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register("IsSelected", typeof(bool), typeof(CavityItem));
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }
    }
}
