using System;
using System.Collections.Generic;
using System.Reflection;
using QA.Business.Interfaces;

namespace QA.Business.Helper
{
    public class DapperHelper : IDapperHelper
    {
        /// <summary>
        /// 获取继承某个接口的所有类对象属性
        /// </summary>
        /// <typeparam name="T">接口对象</typeparam>
        /// <param name="assemblyName">Assembly.GetExecutingAssembly().ToString()</param>
        /// <returns></returns>
        public List<Type> GenerateManager<T>(string assemblyName = "")
        {
            var types = Assembly.GetCallingAssembly().GetTypes();
            if (!string.IsNullOrEmpty(assemblyName))
            {
                types = Assembly.Load(assemblyName).GetTypes();
            }
            List<Type> typeList = new List<Type>();
            foreach (var type in types)
            {
                if (type.IsInterface)
                {
                    continue;
                }
                var @interface = type.GetInterface(typeof(T).Name);
                if (null != @interface)
                {
                    typeList.Add(type);
                }
            }
            return typeList;
        }
    }
}
