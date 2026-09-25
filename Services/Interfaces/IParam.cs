using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using QA.Business.Helper;
using QA.Business.Manager;
using QA_Infrastructure;

namespace QA.Business.Interfaces
{
    public class IParam
    {
        /// <summary>
        /// 注意：如果需要动态修改该标签，必须先在param中override此属性。
        /// </summary>
        [Category("0.启用"), DisplayName("模块启用")]
        public virtual bool BUse { get; set; } = true;

        /// <summary>
        /// 根据权限动态修改可见、只读属性。
        /// 该方法默认实现为使用非三级权限账户查看设置时，将所有带有ReadOnly标签的属性变为只读。
        /// </summary>
        /// <param name="userType"></param>
        public virtual void SetBrowsableAndReadOnly(EN_UserType userType)
        {
            bool readOnly = userType < MyUserManager.userTypeManager;
            PropertyInfo[] properties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (PropertyInfo prop in properties)
            {
                if (!PropAttrUtils.SetReadOnly(this, prop.Name, readOnly))
                {
                    //忽略，可能没有ReadOnly标签
                }
            }
        }

        /// <summary>
        /// 根据当前语言动态修改名称显示。如果语言字典不含翻译，则添加进语言字典。
        /// 该方法无需重写。
        /// </summary>
        /// <param name="languageIndex"></param>
        public void SetLanguage(ResourceDictionary lanDictionary)
        {
            PropertyInfo[] properties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (PropertyInfo prop in properties)
            {
                var categoryAttrs = prop.GetCustomAttributes(typeof(CategoryAttribute), true);
                if (categoryAttrs.Length > 0)
                {
                    var category = ((CategoryAttribute)categoryAttrs[0]).Category;
                    if (new Regex("[0-9]+\\.").IsMatch(category))
                    {
                        int index = category.IndexOf('.');
                        string str1 = category.Substring(0, index + 1);
                        string str2 = category.Substring(index + 1);
                        if (lanDictionary.Contains(str2))
                        {
                            string newCategory = str1 + lanDictionary[str2].ToString();
                            PropAttrUtils.SetCategory(this, prop.Name, newCategory);
                        }
                        else
                        {
                            AddStrToLanguageXAML(str2);
                        }
                    }
                    else
                    {
                        if (lanDictionary.Contains(category))
                        {
                            string newCategory = lanDictionary[category].ToString();
                            PropAttrUtils.SetCategory(this, prop.Name, newCategory);
                        }
                        else
                        {
                            AddStrToLanguageXAML(category);
                        }
                    }
                }
                var displayNameAttrs = prop.GetCustomAttributes(typeof(DisplayNameAttribute), true);
                if (displayNameAttrs.Length > 0)
                {
                    var displayName = ((DisplayNameAttribute)displayNameAttrs[0]).DisplayName;
                    if (lanDictionary.Contains(displayName))
                    {
                        string newDisplayName = lanDictionary[displayName].ToString();
                        PropAttrUtils.SetDisplayName(this, prop.Name, newDisplayName);
                    }
                    else
                    {
                        AddStrToLanguageXAML(displayName);
                    }
                }
            }
        }

        private static List<string> writtenLanguageList = new List<string>();

        private void AddStrToLanguageXAML(string s)
        {
            //string languageXamlDir = AppDomain.CurrentDomain.BaseDirectory + "..\\..\\Language";
            string languageXamlDir = @"D:\QKProject\Language";
            if (!Directory.Exists(languageXamlDir))
            {
                return;
            }
            string lineToBeWrote_cn = "    <Sys:String x:Key=\"" + s + "\">" + s + "</Sys:String>";
            if (writtenLanguageList.Contains(lineToBeWrote_cn))
            {
                return;
            }
            writtenLanguageList.Add(lineToBeWrote_cn);
            string lineToBeWrote_notcn = "    <Sys:String x:Key=\"" + s + "\"></Sys:String>";
            foreach (string file in Directory.GetFiles(languageXamlDir))
            {
                //if (File.Exists(file) && file.EndsWith("xaml"))
                //{
                //    using (StreamWriter sw = new StreamWriter(file, true))
                //    {
                //        if (writtenLanguageList.Count == 1)
                //        {
                //            sw.WriteLine();
                //        }
                //        sw.WriteLine(file.EndsWith("zh-cn.xaml") ? lineToBeWrote_cn : lineToBeWrote_notcn);
                //    }
                //}
                string fileContent = File.ReadAllText(file);
                string resourceDictionaryTag = "</ResourceDictionary>";

                // 定位最后一个 </ResourceDictionary> 标签的位置
                int index = fileContent.LastIndexOf(resourceDictionaryTag);

                string stringToAdd = file.EndsWith("zh-cn.xaml") ? lineToBeWrote_cn : lineToBeWrote_notcn;
                string updatedContent = fileContent.Substring(0, index);

                // 添加新的字符串
                updatedContent += stringToAdd;
                updatedContent += Environment.NewLine + resourceDictionaryTag;

                // 将更新后的内容写回文件
                using (StreamWriter sw = new StreamWriter(file))
                {
                    sw.Write(updatedContent);
                }
            }
        }
    }
}
