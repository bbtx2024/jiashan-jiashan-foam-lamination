using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace QA.UserControls.Controls
{
    public class CavityItemDecorator : Control
    {
        private Adorner adorner;
        private ContentControl cavityItem;
        public static readonly DependencyProperty ShowDecoratorProperty = DependencyProperty.Register("ShowDecorator", typeof(bool), typeof(CavityItemDecorator), new FrameworkPropertyMetadata(false, new PropertyChangedCallback(ShowDecoratorProperty_Changed)));

        public bool ShowDecorator
        {
            get
            {
                return (bool)GetValue(ShowDecoratorProperty);
            }
            set
            {
                SetValue(ShowDecoratorProperty, value);
            }
        }

        public CavityItemDecorator()
        {
            Unloaded += CavityItemDecorator_Unloaded;
        }

        private void CavityItemDecorator_Unloaded(object sender, RoutedEventArgs e)
        {
            if (null != adorner)
            {
                var adornerLayer = AdornerLayer.GetAdornerLayer(this);
                if (null != adornerLayer)
                {
                    adornerLayer.Remove(adorner);
                }
                adorner = null;
            }
        }

        private void ShowAdorner()
        {
            if (null == adorner)
            {
                var adornerLayer = AdornerLayer.GetAdornerLayer(this);
                if (null == adornerLayer)
                {
                    return;
                }

                cavityItem = this.DataContext as ContentControl;
                adorner = new SizeAdorner(cavityItem);
                adornerLayer.Add(adorner);

                if (!ShowDecorator)
                {
                    adorner.Visibility = Visibility.Hidden;
                    return;
                }
            }

            adorner.Visibility = Visibility.Visible;
        }

        private void HideAdorner()
        {
            if (null != adorner)
            {
                adorner.Visibility = Visibility.Hidden;
            }
        }

        private static void ShowDecoratorProperty_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CavityItemDecorator cavityItemDecorator = d as CavityItemDecorator;
            bool val = (bool)e.NewValue;
            if (val)
            {
                cavityItemDecorator.ShowAdorner();
                return;
            }

            cavityItemDecorator.HideAdorner();
        }
    }
}
