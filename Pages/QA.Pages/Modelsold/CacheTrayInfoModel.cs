using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace QA.Pages.Models
{
    public class CacheTrayInfoModel : PropertyChangedBase
    {
        private CacheTrayInfoDetailModel _column1Model;
        public CacheTrayInfoDetailModel Column1Model
        {
            get => _column1Model;
            set {
                _column1Model = value;
                NotifyOfPropertyChange(() => Column1Model);
            } 
        }

        private CacheTrayInfoDetailModel _column2Model;
        public CacheTrayInfoDetailModel Column2Model
        {
            get => _column2Model;
            set {
                _column2Model = value;
                NotifyOfPropertyChange(()=> Column2Model);
            }
            
        }

        private CacheTrayInfoDetailModel _column3Model;
        public CacheTrayInfoDetailModel Column3Model
        {
            get => _column3Model;
            set
            {
                _column3Model = value;
                NotifyOfPropertyChange(() => Column3Model);
            }
            
        }
    }
}
