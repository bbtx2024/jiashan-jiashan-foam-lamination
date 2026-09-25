using System;
using System.ComponentModel;
using System.Reflection;

namespace QA.JiaShan.Business
{
    public class CommonFunc
    {
        public static bool SetAttributes<T>(object obj, string propertyName, object value)
        {
            var props = TypeDescriptor.GetProperties(obj);
            var prop = obj.GetType().GetProperty(propertyName);
            if (props.Find(propertyName, false) == null || prop == null)
            {
                return false;
            }
            Type attributeType = typeof(T);
            if (!prop.IsDefined(attributeType))
            {
                return false;
            }

            string attributeFieldName = string.Empty;
            switch (attributeType.Name)
            {
                case "CategoryAttribute": attributeFieldName = "categoryValue"; break;
                case "DescriptionAttribute": attributeFieldName = "description"; break;
                case "DisplayNameAttribute": attributeFieldName = "_displayName"; break;
                case "ReadOnlyAttribute": attributeFieldName = "isReadOnly"; break;
                case "BrowsableAttribute": attributeFieldName = "browsable"; break;
            }

            FieldInfo fieldInfo = typeof(T).GetField(attributeFieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance);
            if (fieldInfo == null)
            {
                return false;
            }
            fieldInfo.SetValue(props[propertyName].Attributes[attributeType], value);

            //AttributeCollection attrs = props[propertyName].Attributes;
            //Type type = typeof(ReadOnlyAttribute);
            //FieldInfo fld = type.GetField("isReadOnly", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance);
            //foreach (Attribute attr in attrs)
            //{
            //    if (attr.GetType() == type)
            //    {
            //        fld.SetValue(attr, value);
            //        return true;
            //    }
            //}

            return true;
        }
    }
}
