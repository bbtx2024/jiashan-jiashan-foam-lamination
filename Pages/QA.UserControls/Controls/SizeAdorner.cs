using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace QA.UserControls.Controls
{
    public class SizeAdorner : Adorner
    {
        private VisualCollection visuals;
        public SizeAdorner(ContentControl cavityItem) : base(cavityItem)
        {

        }
    }
}
