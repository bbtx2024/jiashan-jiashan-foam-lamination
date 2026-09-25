using System.Windows;
using System.Windows.Controls;

namespace QA.UserControls.Controls
{
    public class SizeChrome : Control
    {
        static SizeChrome()
        {
            FrameworkElement.DefaultStyleKeyProperty.OverrideMetadata(typeof(SizeChrome), new FrameworkPropertyMetadata(typeof(SizeChrome)));
        }
    }
}
