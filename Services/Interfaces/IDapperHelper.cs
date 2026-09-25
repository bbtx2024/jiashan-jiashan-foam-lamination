using System;
using System.Collections.Generic;

namespace QA.Business.Interfaces
{
    public interface IDapperHelper
    {
        List<Type> GenerateManager<T>(string assemblyName = "");
    }
}
