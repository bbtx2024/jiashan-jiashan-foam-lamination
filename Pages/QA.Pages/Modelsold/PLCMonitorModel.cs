using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA.Pages.Models
{
    public class PLCMonitorModel : PropertyChangedBase
    {
        public string _plcVariableName;

        public string PlcVariableName
        {
            get => _plcVariableName;
            set {
                _plcVariableName = value;
                NotifyOfPropertyChange(() => PlcVariableName);
            }           
        }

        //private string _plcAddress;

        //public string PLCAddress
        //{
        //    get => _plcAddress;
        //    set => SetProperty(ref _plcAddress, value);
        //}

        //private int _arrayIndex;
        //public int ArrayIndex
        //{
        //    get => _arrayIndex;
        //    set => SetProperty(ref _arrayIndex, value);
        //}

        //private string _plcValue;

        //public string PLCValue
        //{
        //    get => _plcValue;
        //    set => SetProperty(ref _plcValue, value);
        //}

        //private int _convertFlag;
        //public int ConvertFlag
        //{
        //    get => _convertFlag;
        //    set => SetProperty(ref _convertFlag, value);
        //}
    }
}
