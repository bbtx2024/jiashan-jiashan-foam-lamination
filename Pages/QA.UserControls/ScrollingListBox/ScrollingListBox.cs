using System;
using System.Collections.Specialized;
using System.Windows.Controls;

namespace QA.UserControls
{
    public class ScrollingListBox : ListBox
    {
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (e.NewItems == null) return;
                var newItemCount = e.NewItems.Count;

                if (newItemCount > 0)
                    this.ScrollIntoView(e.NewItems[newItemCount - 1]);

                base.OnItemsChanged(e);
            }
            catch (Exception ex)
            {

            }

        }
    }
}
