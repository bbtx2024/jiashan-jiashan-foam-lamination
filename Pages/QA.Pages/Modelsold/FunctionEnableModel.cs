using Caliburn.Micro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA.Pages.Models
{
    public class FunctionEnableModel : PropertyChangedBase
    {
        private string _functionName;

        public string FunctionName
        {
            get => _functionName;
            set {
                _functionName = value;
                NotifyOfPropertyChange(()=> FunctionName);
            } 
        }
        private bool _functionEnable;

        public bool FunctionEnable
        {
            get => _functionEnable;
            set {
                _functionEnable = value;
                NotifyOfPropertyChange(()=> FunctionEnable);
            } 
        }

    }
}
