using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA.Pages.Models
{
    public class CacheTrayInfoDetailModel : PropertyChangedBase
    {
        private string _name;
        public string Name
        {
            get => _name;
            set {
                _name = value;
                NotifyOfPropertyChange(() => Name);
            } 
        }

        private byte _status;
        public byte Status
        {
            get => _status;
            set {
                _status = value;
                NotifyOfPropertyChange(()=> Status);
            } 
        }
    }
}
