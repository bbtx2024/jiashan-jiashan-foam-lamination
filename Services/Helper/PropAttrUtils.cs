using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace QA.Business.Helper
{
    class PropAttrUtils
    {
        /// <summary>
        /// 遍历修改属性时使用，xxx表示处于遍历的属性变量。
        /// PropAttrUtils.SetBrowsable(this, xxx.Name, false);
        /// </summary>
        public static bool SetBrowsable(object obj, string propName, bool visible)
        {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(obj);
            AttributeCollection attrs = props[propName].Attributes;
            Type type = typeof(BrowsableAttribute);
            FieldInfo fld = type.GetField("browsable", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (Attribute attr in attrs)
            {
                if (attr.GetType() == type)
                {
                    fld.SetValue(attr, visible);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 修改单个属性时使用，XXX表示属性。
        /// PropAttrUtils.SetBrowsable(this, () => XXX, false);
        /// </summary>
        public static bool SetBrowsable<TProperty>(object obj, Expression<Func<TProperty>> property, bool visible)
        {
            string propName = ((MemberExpression)property.Body).Member.Name;
            return SetBrowsable(obj, propName, visible);
        }

        /// <summary>
        /// 遍历修改属性时使用，xxx表示处于遍历的属性变量。
        /// PropAttrUtils.SetReadOnly(this, xxx.Name, true);
        /// </summary>
        public static bool SetReadOnly(object obj, string propName, bool readOnly)
        {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(obj);
            AttributeCollection attrs = props[propName].Attributes;
            Type type = typeof(ReadOnlyAttribute);
            FieldInfo fld = type.GetField("isReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (Attribute attr in attrs)
            {
                if (attr.GetType() == type)
                {
                    fld.SetValue(attr, readOnly);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 修改单个属性时使用，XXX表示属性。
        /// PropAttrUtils.SetReadOnly(this, () => XXX, true);
        /// </summary>
        public static bool SetReadOnly<TProperty>(object obj, Expression<Func<TProperty>> property, bool visible)
        {
            string propName = ((MemberExpression)property.Body).Member.Name;
            return SetReadOnly(obj, propName, visible);
        }

        /// <summary>
        /// 遍历修改属性时使用，xxx表示处于遍历的属性变量。
        /// PropAttrUtils.SetCategory(this, xxx.Name, "example");
        /// </summary>
        public static bool SetCategory(object obj, string propName, string category)
        {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(obj);
            AttributeCollection attrs = props[propName].Attributes;
            Type type = typeof(CategoryAttribute);
            FieldInfo fld = type.GetField("categoryValue", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (Attribute attr in attrs)
            {
                if (attr.GetType() == type)
                {
                    fld.SetValue(attr, category);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 修改单个属性时使用，XXX表示属性。
        /// PropAttrUtils.SetCategory(this, () => XXX, "example");
        /// </summary>
        public static bool SetCategory<TProperty>(object obj, Expression<Func<TProperty>> property, string category)
        {
            string propName = ((MemberExpression)property.Body).Member.Name;
            return SetCategory(obj, propName, category);
        }

        /// <summary>
        /// 遍历修改属性时使用，xxx表示处于遍历的属性变量。
        /// PropAttrUtils.SetDisplayName(this, xxx.Name, "example");
        /// </summary>
        public static bool SetDisplayName(object obj, string propName, string displayName)
        {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(obj);
            AttributeCollection attrs = props[propName].Attributes;
            Type type = typeof(DisplayNameAttribute);
            FieldInfo fld = type.GetField("_displayName", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (Attribute attr in attrs)
            {
                if (attr.GetType() == type)
                {
                    fld.SetValue(attr, displayName);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 修改单个属性时使用，XXX表示属性。
        /// PropAttrUtils.SetDisplayName(this, () => XXX, "example");
        /// </summary>
        public static bool SetDisplayName<TProperty>(object obj, Expression<Func<TProperty>> property, string displayName)
        {
            string propName = ((MemberExpression)property.Body).Member.Name;
            return SetDisplayName(obj, propName, displayName);
        }

        /// <summary>
        /// 遍历修改属性时使用，xxx表示处于遍历的属性变量。
        /// PropAttrUtils.SetDescription(this, xxx.Name, "example");
        /// </summary>
        public static bool SetDescription(object obj, string propName, string description)
        {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(obj);
            AttributeCollection attrs = props[propName].Attributes;
            Type type = typeof(DescriptionAttribute);
            FieldInfo fld = type.GetField("description", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (Attribute attr in attrs)
            {
                if (attr.GetType() == type)
                {
                    fld.SetValue(attr, description);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 修改单个属性时使用，XXX表示属性。
        /// PropAttrUtils.SetDescription(this, () => XXX, "example");
        /// </summary>
        public static bool SetDescription<TProperty>(object obj, Expression<Func<TProperty>> property, string description)
        {
            string propName = ((MemberExpression)property.Body).Member.Name;
            return SetDescription(obj, propName, description);
        }
    }
}
