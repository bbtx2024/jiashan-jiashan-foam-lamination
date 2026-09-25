using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;

namespace QA.Pages
{
   /// <summary>
   /// AllowMultiple = false,代表一个类不允许多次使用此属性
   /// </summary>
   [MetadataAttribute]
   [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
   public class ExportPageAttribute : ExportAttribute
   {
      public ExportPageAttribute()
         :base(typeof(IPageViewModel))
      {
      }

      public string PageType { get; set; }
   }
}
