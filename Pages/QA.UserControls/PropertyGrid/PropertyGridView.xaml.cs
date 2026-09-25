using System;
using System.Windows;
using System.Windows.Controls;

namespace QA.UserControls
{
    /// <summary>
    /// PropertyGridView.xaml 的交互逻辑
    /// </summary>
    public partial class PropertyGridView : UserControl
    {
        public static readonly DependencyProperty SelectedObjectProperty =
        DependencyProperty.Register("SelectedObject", typeof(object), typeof(PropertyGridView), new PropertyMetadata(null, SelectedObjectChange));

        private static void SelectedObjectChange(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ppgv = d as PropertyGridView;
            ppgv.ppgrid.SelectedObject = ppgv.SelectedObject;
        }

        public object SelectedObject
        {
            get { return GetValue(SelectedObjectProperty); }
            set { SetValue(SelectedObjectProperty, value); }
        }

        public PropertyGridView()
        {
            InitializeComponent();
            this.ppgrid.PropertySort = System.Windows.Forms.PropertySort.Categorized;
            this.ppgrid.ToolbarVisible = false;
            this.ppgrid.ExpandAllGridItems();
        }

        private void ppgrid_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.Focus();
            if (e is System.Windows.Forms.HandledMouseEventArgs)
            { (e as System.Windows.Forms.HandledMouseEventArgs).Handled = true; }
        }

        private void ppgrid_PropertySortChanged(object sender, EventArgs e)
        {
            this.ppgrid.PropertySort = System.Windows.Forms.PropertySort.Categorized;// PropertySort.
        }
    }
}
