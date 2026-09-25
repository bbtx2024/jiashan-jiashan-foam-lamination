using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using Caliburn.Micro;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.Pages.ViewModels
{
    public class SystemConfigPageViewModel : Screen
    {
        #region Field
        private readonly IWindowManager _windowManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly ParamManager _paramManager;
        private ParamManager _tmpParamManager;
        private MyUserManager _myUserManager;
        private ResourceDictionary _languageDic;
        #endregion

        #region Property
        public override string DisplayName { get; set; } = "系统设置";
        public ushort OrderID { get; set; } = 999;
        public ObservableCollection<SysConfigNode> SysConfigNodes { get; set; }

        public SysConfigNode _selectedSysConfigNode;
        public SysConfigNode SelectedSysConfigNode
        {
            get { return _selectedSysConfigNode; }
            set { _selectedSysConfigNode = value; NotifyOfPropertyChange(() => SelectedSysConfigNode); }
        }
        #endregion

        public SystemConfigPageViewModel(IWindowManager windowManager, IEventAggregator eventAggregator)
        {
            _windowManager = windowManager;
            _eventAggregator = eventAggregator;
            _paramManager = IoC.Get<ParamManager>();
            _tmpParamManager = _paramManager.DeepCopy();
            _myUserManager = IoC.Get<MyUserManager>();
            _languageDic = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(t => t.Source != null && t.Source.ToString().Contains("Language"));

            SysConfigNodes = new ObservableCollection<SysConfigNode>();
            GetTreeViewItems(_tmpParamManager);  //生成系统设置参数节点
        }

        private string TransLate(string s)
        {
            if (!_languageDic.Contains(s))
            {
                return s;
            }
            string ret = (string)_languageDic[s];
            if (string.IsNullOrEmpty(ret))
            {
                return s;
            }
            return ret;
        }

        /// <summary>
        /// 切换系统设置参数节点
        /// </summary>
        /// <param name="SelectedItem"></param>
        public void SelectedNodeChanged(object SelectedItem)
        {
            SysConfigNode node = SelectedItem as SysConfigNode;
            SelectedSysConfigNode = node;
        }

        public void BackupSysConfig()
        {
        }

        /// <summary>
        /// 保存系统设置参数
        /// </summary>
        public void ConfirmModifySysConfig()
        {
            List<string> diffmsgs = new List<string>();
            SyncAndCheckModify(_paramManager, _tmpParamManager, ref diffmsgs);
            _paramManager.SaveParams();

            string modifyInfo = "";
            foreach (string diffmsg in diffmsgs)
            {
                modifyInfo += $"{diffmsg}\r\n";
            }
            NLogTrace.LogOut(EN_WARN_LEVEL.SettingChange, modifyInfo, En_Logout_Type.SystemParam, false);
            MessageBox.Info($"[Setting]修改参数:\r\n{modifyInfo}", "Info");

            this.TryClose();
        }

        /// <summary>
        /// 取消系统设置参数修改
        /// </summary>
        public void CancelModifySysConfig()
        {
            this.TryClose();
        }

        /// <summary>
        /// 生成系统设置参数节点
        /// </summary>
        /// <param name="paramManager"></param>
        private void GetTreeViewItems(ParamManager paramManager)
        {
            var propertyInfos = paramManager.GetType().GetProperties();
            foreach (var propertyInfo in propertyInfos)
            {
                var propertyType = propertyInfo.PropertyType;
                if (propertyType.IsArray)
                {
                    var replaceName = propertyType.FullName?.Replace("[]", "");
                    if (!string.IsNullOrEmpty(replaceName))
                    {
                        var childType = propertyType.Assembly.GetType(replaceName);
                        var childAttribute = childType.GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute;
                        IParam[] paramVals = propertyInfo.GetValue(paramManager) as IParam[];

                        SysConfigNodes.Add(new SysConfigNode()
                        {
                            NodeName = childAttribute?.Description ?? $"{propertyInfo.Name}",
                            NodeObject = null,
                            ChildNodes = new ObservableCollection<SysConfigNode>()
                        });

                        for (int i = 0; i < paramVals.Length; i++)
                        {
                            SysConfigNodes[SysConfigNodes.Count - 1].ChildNodes.Add(new SysConfigNode()
                            {
                                NodeName = $"{childAttribute?.Description ?? propertyInfo.Name}{i + 1}",
                                NodeObject = paramVals[i],
                                ChildNodes = null
                            });
                        }
                    }
                    continue;
                }
                //找到与propertyInfo对应的某一类参数
                if (!propertyType.IsClass)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"{propertyInfo.Name}不是class，无法读取参数", En_Logout_Type.SystemParam, true);
                    return;
                }
                IParam param = propertyInfo.GetValue(paramManager) as IParam;
                if (param == null)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"{propertyInfo.Name}不是IParam，无法读取参数", En_Logout_Type.SystemParam, true);
                    return;
                }
                //动态修改可见、只读显示
                param.SetBrowsableAndReadOnly(_myUserManager.CurrUserInfo.Type);
                //动态修改名称显示
                param.SetLanguage(_languageDic);
                //添加一级设置（如PLC设置）至SysConfigNodes
                string nodeName;
                var descriptionAttr = param.GetType().GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (descriptionAttr != null)
                {
                    //descriptionAttr.Description: 指定Param里，最上方的DescriptionAttribute标签内的字符串，如"PLC设置"
                    nodeName = TransLate(descriptionAttr.Description);
                }
                else
                {
                    //propertyInfo.Name: ParamManager里面这个参数的属性的名称，如"PLCParam"
                    nodeName = TransLate(propertyInfo.Name);
                }
                SysConfigNode parentnode = new SysConfigNode()
                {
                    NodeName = nodeName,
                    NodeObject = param,
                    ChildNodes = null,
                };
                SysConfigNodes.Add(parentnode);
                //添加二级设置至parentnode
                PropertyInfo ppinfo = param.GetType().GetProperty("SglParams");
                if (ppinfo != null)
                {
                    object[] sglparams = ppinfo.GetValue(param) as object[];
                    if (sglparams != null && sglparams.Length > 0)
                    {
                        parentnode.ChildNodes = new ObservableCollection<SysConfigNode>();
                        for (int i = 0; i < sglparams.Length; i++)
                        {
                            object sglparam = sglparams[i];
                            var childDescriptionAttr = sglparam.GetType().GetCustomAttribute(typeof(DescriptionAttribute)) as DescriptionAttribute;
                            SysConfigNode childnode = new SysConfigNode()
                            {
                                NodeName = $"{childDescriptionAttr?.Description ?? $"{sglparam.GetType().Name}"}{i + 1}",
                                NodeObject = sglparam,
                                ChildNodes = null,
                            };
                            parentnode.ChildNodes.Add(childnode);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 同步修改内容到系统参数，并检查修改条目
        /// </summary>
        /// <param name="paramManager"></param>
        /// <param name="tmpParamManager"></param>
        private void SyncAndCheckModify(ParamManager paramManager, ParamManager tmpParamManager, ref List<string> diffmsgs)
        {
            var propertyInfos = typeof(ParamManager).GetProperties();
            foreach (PropertyInfo ppInfo in propertyInfos)
            {
                object oldParam = ppInfo.GetValue(paramManager);
                object newParam = ppInfo.GetValue(tmpParamManager);

                //检查修改条目
                CompareAllProperties(oldParam, newParam, ref diffmsgs);
                //同步参数修改
                ppInfo.SetValue(paramManager, newParam);
            }
            if (diffmsgs.Count == 0)
            {
                diffmsgs.Add("未发现有变动项！");
            }
        }

        private void CompareAllProperties(object oldParamObj, object newParamObj, ref List<string> diffmsgs)
        {
            if (oldParamObj.GetType() != newParamObj.GetType())
            {
                return;
            }
            var properties = oldParamObj.GetType().GetProperties();
            foreach (PropertyInfo info in properties)
            {
                object oldValueObj = info.GetValue(oldParamObj);
                object newValueObj = info.GetValue(newParamObj);
                //暂存字符串显示（如果类型为数组的话）
                string oldStr = "";
                string newStr = "";
                //新为空则忽略
                if (newValueObj == null)
                {
                    continue;
                }
                //新非空且旧为空，直接显示，只需要判断新旧都非空的情况
                if (oldValueObj != null)
                {
                    //此时新旧都不为null，需要根据具体类型转换后再判断
                    if (oldValueObj.GetType() == newValueObj.GetType())
                    {
                        if (oldValueObj == newValueObj || oldValueObj.Equals(newValueObj))
                        {
                            //只要不是数组，都可以通过这个判断
                            continue;
                        }
                        if (oldValueObj is bool[])
                        {
                            bool[] oldArr = (bool[])oldValueObj;
                            bool[] newArr = (bool[])newValueObj;
                            if (oldArr.Length == newArr.Length)
                            {
                                bool equal = true;
                                for (int i = 0; i < oldArr.Length; i++)
                                {
                                    if (oldArr[i] != newArr[i])
                                    {
                                        equal = false;
                                    }
                                    oldStr += oldArr[i] + ",";
                                    newStr += newArr[i] + ",";
                                }
                                if (equal)
                                {
                                    continue;
                                }
                            }
                        }
                        else if (oldValueObj is byte[])
                        {
                            byte[] oldArr = (byte[])oldValueObj;
                            byte[] newArr = (byte[])newValueObj;
                            if (oldArr.Length == newArr.Length)
                            {
                                bool equal = true;
                                for (int i = 0; i < oldArr.Length; i++)
                                {
                                    if (oldArr[i] != newArr[i])
                                    {
                                        equal = false;
                                    }
                                    oldStr += oldArr[i] + ",";
                                    newStr += newArr[i] + ",";
                                }
                                if (equal)
                                {
                                    continue;
                                }
                            }
                        }
                        else if (oldValueObj is ushort[])
                        {
                            ushort[] oldArr = (ushort[])oldValueObj;
                            ushort[] newArr = (ushort[])newValueObj;
                            if (oldArr.Length == newArr.Length)
                            {
                                bool equal = true;
                                for (int i = 0; i < oldArr.Length; i++)
                                {
                                    if (oldArr[i] != newArr[i])
                                    {
                                        equal = false;
                                    }
                                    oldStr += oldArr[i] + ",";
                                    newStr += newArr[i] + ",";
                                }
                                if (equal)
                                {
                                    continue;
                                }
                            }
                        }
                        else if (oldValueObj is int[])
                        {
                            int[] oldArr = (int[])oldValueObj;
                            int[] newArr = (int[])newValueObj;
                            if (oldArr.Length == newArr.Length)
                            {
                                bool equal = true;
                                for (int i = 0; i < oldArr.Length; i++)
                                {
                                    if (oldArr[i] != newArr[i])
                                    {
                                        equal = false;
                                    }
                                    oldStr += oldArr[i] + ",";
                                    newStr += newArr[i] + ",";
                                }
                                if (equal)
                                {
                                    continue;
                                }
                            }
                        }
                        else if (oldValueObj is float[])
                        {
                            float[] oldArr = (float[])oldValueObj;
                            float[] newArr = (float[])newValueObj;
                            if (oldArr.Length == newArr.Length)
                            {
                                bool equal = true;
                                for (int i = 0; i < oldArr.Length; i++)
                                {
                                    if (oldArr[i] != newArr[i])
                                    {
                                        equal = false;
                                    }
                                    oldStr += oldArr[i] + ",";
                                    newStr += newArr[i] + ",";
                                }
                                if (equal)
                                {
                                    continue;
                                }
                            }
                        }
                        else if (oldValueObj is double[])
                        {
                            double[] oldArr = (double[])oldValueObj;
                            double[] newArr = (double[])newValueObj;
                            if (oldArr.Length == newArr.Length)
                            {
                                bool equal = true;
                                for (int i = 0; i < oldArr.Length; i++)
                                {
                                    if (oldArr[i] != newArr[i])
                                    {
                                        equal = false;
                                    }
                                    oldStr += oldArr[i] + ",";
                                    newStr += newArr[i] + ",";
                                }
                                if (equal)
                                {
                                    continue;
                                }
                            }
                        }
                        else if (oldValueObj is string[])
                        {
                            string[] oldArr = (string[])oldValueObj;
                            string[] newArr = (string[])newValueObj;
                            if (oldArr.Length == newArr.Length)
                            {
                                bool equal = true;
                                for (int i = 0; i < oldArr.Length; i++)
                                {
                                    if (oldArr[i] != newArr[i])
                                    {
                                        equal = false;
                                    }
                                    oldStr += oldArr[i] + ",";
                                    newStr += newArr[i] + ",";
                                }
                                if (equal)
                                {
                                    continue;
                                }
                            }
                        }
                    }
                    oldStr = oldStr == "" ? oldValueObj.ToString() : "{" + oldStr.Substring(0, oldStr.Length - 1) + "}";
                }
                else
                {
                    oldStr = "Null";
                }
                newStr = newStr == "" ? newValueObj.ToString() : "{" + newStr.Substring(0, newStr.Length - 1) + "}";
                //paramName, eg.TransLate("MotionGoogolParam")
                string paramName = TransLate(oldParamObj.GetType().Name);
                //Property Name, eg.WorkSpeedX1
                string propertyName = info.Name;
                //Property Attribute DisplayName, eg.X1工作速度
                string displayName = "";
                object[] dispattr = info.GetCustomAttributes(typeof(DisplayNameAttribute), false);
                if (dispattr.Length > 0)
                {
                    displayName = ((DisplayNameAttribute)dispattr[0]).DisplayName;
                }
                //Add
                diffmsgs.Add($"{paramName}.{displayName} {oldStr}->{newStr}");
            }
        }
    }

    public class SysConfigNode : PropertyChangedBase
    {
        private string _nodeName = "";
        public string NodeName
        {
            get { return _nodeName; }
            set { _nodeName = value; NotifyOfPropertyChange(() => NodeName); }
        }

        private object _nodeObject = null;
        public object NodeObject
        {
            get { return _nodeObject; }
            set { _nodeObject = value; NotifyOfPropertyChange(() => NodeObject); }
        }

        private ObservableCollection<SysConfigNode> _childNodes = null;
        public ObservableCollection<SysConfigNode> ChildNodes
        {
            get { return _childNodes; }
            set { _childNodes = value; NotifyOfPropertyChange(() => ChildNodes); }
        }
    }
}
