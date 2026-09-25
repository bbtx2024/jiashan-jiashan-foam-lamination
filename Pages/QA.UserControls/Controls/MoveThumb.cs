using System;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace QA.UserControls.Controls
{
    public class MoveThumb : Thumb
    {
        CavityItem cavityItem;
        CavityCanvas cavityCanvas;

        public MoveThumb()
        {
            DragStarted += MoveThumb_DragStarted;
            DragDelta += MoveThumb_DragDelta;
        }

        private void MoveThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            cavityItem = this.DataContext as CavityItem;
            if (null != cavityItem)
            {
                cavityCanvas = VisualTreeHelper.GetParent(cavityItem) as CavityCanvas;
            }
        }

        private void MoveThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (null != cavityItem && null != cavityCanvas && cavityItem.IsSelected)
            {
                double minLeft = double.MaxValue;
                double minTop = double.MaxValue;

                foreach (var selectedItem in cavityCanvas.SelectedItems)
                {
                    minLeft = Math.Min(Canvas.GetLeft(selectedItem), minLeft);
                    minTop = Math.Min(Canvas.GetTop(selectedItem), minTop);
                }

                double deltaHorizontal = Math.Max(-minLeft, e.HorizontalChange);
                double deltaVertical = Math.Max(-minTop, e.VerticalChange);

                foreach (var item in this.cavityCanvas.SelectedItems)
                {
                    Canvas.SetLeft(item, Canvas.GetLeft(item) + deltaHorizontal);
                    Canvas.SetTop(item, Canvas.GetTop(item) + deltaVertical);
                }

                cavityCanvas.InvalidateMeasure();
                e.Handled = true;
            }
        }
    }
}
