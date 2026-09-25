using System.ComponentModel.Composition;
using Caliburn.Micro;
using QA.Pages.Interfaces;

namespace QA.Pages.ViewModels
{
    [Export(typeof(IPageViewModel))]
    public class CCDPageViewModel : Screen, IPageViewModel
    {
        #region Property
        public override string DisplayName { get; set; } = "相机页面";
        public ushort OrderID { get; set; } = 2;
        #endregion

        #region Constructor
        public CCDPageViewModel()
        {
        }
        #endregion
    }
}
