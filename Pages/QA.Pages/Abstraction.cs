using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA.Pages
{
    //public interface IShellViewModel
    //{

    //}
    //[InheritedExport(typeof(IPageViewModel))]

    public interface IPageViewModel
    {
        ushort OrderID { get; set; }
    }  


    public interface IMetaData
    { 
        string PageType { get; }
    }

}
