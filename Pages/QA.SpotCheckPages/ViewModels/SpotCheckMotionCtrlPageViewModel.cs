using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using QA.Business.Component.Motion.Googol;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;

namespace QA.SpotCheckPages.ViewModels
{
    [Export("SpotCheckMotionCtrlPageViewModel", typeof(ISpotPageViewModel))]
    public class SpotCheckMotionCtrlPageViewModel : Screen, INotifyPropertyChanged, ISpotPageViewModel
    {
        #region field
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;

        private ParamManager _paramManager;
        private MotionGoogol_Component _mGoogol_Component;
        #endregion

        #region Property
        public override string DisplayName { get; set; } = "主板点检";

        private ushort _OrderID = 0;
        public ushort OrderID
        {
            get { return _OrderID; }
            set { _OrderID = value; }
        }

        private MotionGoogolParam _mGoogolParam { get => _paramManager.MotionGoogolParam; }

        private Visibility _visible = Visibility.Visible;
        public Visibility Visible
        {
            get => _visible;
            set
            {
                _visible = value;
                NotifyOfPropertyChange(() => Visible);
            }
        }

        /// <summary>
        /// 扩展输入，注意这是经过排序的
        /// </summary>
        public ObservableCollection<bool> ExtInput { get; set; } = new ObservableCollection<bool>();

        /// <summary>
        /// 扩展输出，注意这是经过排序的
        /// </summary>
        private ObservableCollection<bool> _extOutput = new ObservableCollection<bool>();
        public ObservableCollection<bool> ExtOutput
        {
            get => _extOutput;
            set
            {
                _extOutput = value;
                NotifyOfPropertyChange(() => ExtOutput);
            }
        }

        /// <summary>
        /// 扩展输入
        /// </summary>
        private ObservableCollection<ExtInputClass> _extInputs = new ObservableCollection<ExtInputClass>();
        public ObservableCollection<ExtInputClass> ExtInputs
        {
            get => _extInputs;
            set
            {
                _extInputs = value;
                NotifyOfPropertyChange(() => ExtInputs);
            }
        }

        /// <summary>
        /// 扩展输出
        /// </summary>
        private ObservableCollection<ExtOutputClass> _extOutputs = new ObservableCollection<ExtOutputClass>();
        public ObservableCollection<ExtOutputClass> ExtOutputs
        {
            get => _extOutputs;
            set
            {
                _extOutputs = value;
                NotifyOfPropertyChange(() => ExtOutputs);
            }
        }

        /// <summary>
        /// 返回已经排序的输出数组，注意顺序必须与xaml中对应
        /// </summary>
        /// <returns></returns>
        private EN_GoogolExtendOutput[] GetOutputs()
        {
            return new[]
            {
                _mGoogolParam.OutUpLightHX,
                _mGoogolParam.OutUpLightHX,//没用的
                _mGoogolParam.OutFeederUpLight,
                _mGoogolParam.OutDownLight,
                _mGoogolParam.OutRightDownLight,
                _mGoogolParam.OutVacuumSuctionNo1,
                _mGoogolParam.OutVacuumRuptureNo1,
                _mGoogolParam.OutCylinderUpNo1,
                _mGoogolParam.OutCylinderDownNo1,
                _mGoogolParam.OutVacuumSuctionNo2,
                _mGoogolParam.OutVacuumRuptureNo2,
                _mGoogolParam.OutCylinderUpNo2,
                _mGoogolParam.OutCylinderDownNo2,
                _mGoogolParam.OutVacuumSuctionNo3,
                _mGoogolParam.OutVacuumRuptureNo3,
                _mGoogolParam.OutCylinderUpNo3,
                _mGoogolParam.OutCylinderDownNo3,
                _mGoogolParam.OutVacuumSuctionNo4,
                _mGoogolParam.OutVacuumRuptureNo4,
                _mGoogolParam.OutCylinderUpNo4,
                _mGoogolParam.OutCylinderDownNo4,
                _mGoogolParam.OutFeederVacuumRuptureLeft,
                _mGoogolParam.OutFeederVacuumRuptureRight,
            };
        }

        /// <summary>
        /// 返回已经排序的输入数组，注意顺序必须与xaml中对应
        /// </summary>
        /// <returns></returns>
        private EN_GoogolExtendInput[] GetInputs()
        {
            return new[]
            {
                _mGoogolParam.InMaterialReadyNo1,
                _mGoogolParam.InCylinderUpReadyNo1,
                _mGoogolParam.InCylinderDownReadyNo1,
                _mGoogolParam.InMaterialReadyNo2,
                _mGoogolParam.InCylinderUpReadyNo2,
                _mGoogolParam.InCylinderDownReadyNo2,
                _mGoogolParam.InMaterialReadyNo3,
                _mGoogolParam.InCylinderUpReadyNo3,
                _mGoogolParam.InCylinderDownReadyNo3,
                _mGoogolParam.InMaterialReadyNo4,
                _mGoogolParam.InCylinderUpReadyNo4,
                _mGoogolParam.InCylinderDownReadyNo4,
            };
        }

        #endregion

        #region Constructor
        public SpotCheckMotionCtrlPageViewModel()
        {
            _paramManager = IoC.Get<ParamManager>();
            _mGoogol_Component = (MotionGoogol_Component)IoC.Get<IMGoogol>();

            //初始化输入
            ExtInputs.Add(new ExtInputClass() { Name = "1#气缸上到位", GoogolExtendInput = _mGoogolParam.InCylinderUpReadyNo1 });
            ExtInputs.Add(new ExtInputClass() { Name = "1#气缸下到位", GoogolExtendInput = _mGoogolParam.InCylinderDownReadyNo1 });
            ExtInputs.Add(new ExtInputClass() { Name = "1#物料OK", GoogolExtendInput = _mGoogolParam.InMaterialReadyNo1 });
            ExtInputs.Add(new ExtInputClass() { Name = "2#气缸上到位", GoogolExtendInput = _mGoogolParam.InCylinderUpReadyNo2 });
            ExtInputs.Add(new ExtInputClass() { Name = "2#气缸下到位", GoogolExtendInput = _mGoogolParam.InCylinderDownReadyNo2 });
            ExtInputs.Add(new ExtInputClass() { Name = "2#物料OK", GoogolExtendInput = _mGoogolParam.InMaterialReadyNo2 });
            //初始化输出
            //ExtOutputs.Add(new ExtOutputClass() { Name = "定位上相机光源", GoogolExtendOutput = _mGoogolParam.OutUpLightHX });
            //ExtOutputs.Add(new ExtOutputClass() { Name = "飞达上相机光源", GoogolExtendOutput = _mGoogolParam.OutFeederUpLight });
            //ExtOutputs.Add(new ExtOutputClass() { Name = "左下相机光源", GoogolExtendOutput = _mGoogolParam.OutDownLight });
            //ExtOutputs.Add(new ExtOutputClass() { Name = "右下相机光源", GoogolExtendOutput = _mGoogolParam.OutRightDownLight });
            ExtOutputs.Add(new ExtOutputClass() { Name = "飞达左吹气", GoogolExtendOutput = _mGoogolParam.OutFeederVacuumRuptureLeft });
            ExtOutputs.Add(new ExtOutputClass() { Name = "飞达右吹气", GoogolExtendOutput = _mGoogolParam.OutFeederVacuumRuptureRight });
            ExtOutputs.Add(new ExtOutputClass() { Name = "1#气缸上", GoogolExtendOutput = _mGoogolParam.OutCylinderUpNo1 });
            ExtOutputs.Add(new ExtOutputClass() { Name = "1#气缸下", GoogolExtendOutput = _mGoogolParam.OutCylinderDownNo1 }); 
            ExtOutputs.Add(new ExtOutputClass() { Name = "1#真空吸", GoogolExtendOutput = _mGoogolParam.OutVacuumSuctionNo1 });
            ExtOutputs.Add(new ExtOutputClass() { Name = "1#真空破", GoogolExtendOutput = _mGoogolParam.OutVacuumRuptureNo1 });
            ExtOutputs.Add(new ExtOutputClass() { Name = "2#气缸上", GoogolExtendOutput = _mGoogolParam.OutCylinderUpNo2 });
            ExtOutputs.Add(new ExtOutputClass() { Name = "2#气缸下", GoogolExtendOutput = _mGoogolParam.OutCylinderDownNo2 });
            ExtOutputs.Add(new ExtOutputClass() { Name = "2#真空吸", GoogolExtendOutput = _mGoogolParam.OutVacuumSuctionNo2 });
            ExtOutputs.Add(new ExtOutputClass() { Name = "2#真空破", GoogolExtendOutput = _mGoogolParam.OutVacuumRuptureNo2 });



            for (int i = 0; i < 32; i++)
            {
                ExtOutput.Add(false);
                ExtInput.Add(false);
            }
            Start();
        }
        #endregion

        #region Method
        //public void ClickExtOutput(object indexObj)
        //{
        //    try
        //    {
        //        int index = Convert.ToInt32(indexObj);
        //        _mGoogol_Component.ChangeOutput(GetOutputs()[index]);
        //    }
        //    catch (Exception)
        //    {
        //    }
        //}
        public void ClickExtOutput(ExtOutputClass extOutputClass)
        {
            try
            {
                _mGoogol_Component.ChangeOutput(extOutputClass.GoogolExtendOutput);
            }
            catch (Exception)
            {
            }
        }



        //public bool Start()
        //{
        //    _cancellationTokenSource = new CancellationTokenSource();
        //    _cancellationToken = _cancellationTokenSource.Token;
        //    Task.Factory.StartNew(async () =>
        //    {
        //        var delayTime = 100;
        //        while (true)
        //        {
        //            try
        //            {
        //                if (_cancellationToken.IsCancellationRequested)
        //                {
        //                    return;
        //                }

        //                Visible = _paramManager.MotionGoogolParam.IsCylinderDoubleSwitch ? Visibility.Visible : Visibility.Hidden;

        //                EN_GoogolExtendOutput[] outputs = GetOutputs();
        //                for (int i = 0; i < outputs.Length; i++)
        //                {
        //                    ExtOutput[i] = _mGoogol_Component.ReadOutput(outputs[i]);
        //                }
        //                NotifyOfPropertyChange(() => ExtOutput);

        //                EN_GoogolExtendInput[] inputs = GetInputs();
        //                for (int i = 0; i < inputs.Length; i++)
        //                {
        //                    ExtInput[i] = _mGoogol_Component.ReadInput(inputs[i]);
        //                }
        //                NotifyOfPropertyChange(() => ExtInput);
        //            }
        //            catch (Exception e)
        //            {
        //                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e.ToString(), En_Logout_Type.Exception);
        //            }
        //            await Task.Delay(delayTime, _cancellationToken);
        //        }
        //    }, _cancellationToken);
        //    return true;
        //}
        public bool Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            Task.Factory.StartNew(async () =>
            {
                var delayTime = 100;
                while (true)
                {
                    try
                    {
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        Visible = _paramManager.MotionGoogolParam.IsCylinderDoubleSwitch ? Visibility.Visible : Visibility.Hidden;

                        foreach (var item in ExtOutputs)
                        {
                            item.ExtBool = _mGoogol_Component.ReadOutput(item.GoogolExtendOutput); 
                        }
                        NotifyOfPropertyChange(() => ExtOutputs);
                        foreach (var item in ExtInputs)
                        {
                            item.ExtBool = _mGoogol_Component.ReadInput(item.GoogolExtendInput);
                        }
                        NotifyOfPropertyChange(() => ExtInputs);
                    }
                    catch (Exception e)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, e.ToString(), En_Logout_Type.Exception);
                    }
                    await Task.Delay(delayTime, _cancellationToken);
                }
            }, _cancellationToken);
            return true;
        }
        public bool Stop()
        {
            try
            {
                //base.Close();
                _cancellationTokenSource?.Cancel();
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, e + e.StackTrace, En_Logout_Type.Exception);
            }
            return true;
        }
        #endregion
    }
    public class ExtInputClass : INotifyPropertyChanged
    {
        #region Property Notify
        public event PropertyChangedEventHandler PropertyChanged;
        public void ChangeProperty(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        public void ChangeProperty<T>(Expression<Func<T>> expression)
        {
            MemberExpression member = (MemberExpression)expression.Body;
            string propName = member.Member.Name;
            ChangeProperty(propName);
        }
        #endregion
        /// <summary>
        /// 名称
        /// </summary>
        private string _name = "";
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                ChangeProperty(() => Name);
            }
        }
        /// <summary>
        /// 输入枚举
        /// </summary>
        private EN_GoogolExtendInput _googolExtendInput;
        public EN_GoogolExtendInput GoogolExtendInput
        {
            get { return _googolExtendInput; }
            set
            {
                _googolExtendInput = value;
                ChangeProperty(() => GoogolExtendInput);
            }
        }
        /// <summary>
        /// 是否开启
        /// </summary>
        private bool _extBool = false;
        public bool ExtBool
        {
            get { return _extBool; }
            set
            {
                _extBool = value;
                ChangeProperty(() => ExtBool);
            }
        }

    }

    public class ExtOutputClass : INotifyPropertyChanged
    {
        #region Property Notify
        public event PropertyChangedEventHandler PropertyChanged;
        public void ChangeProperty(string propName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }
        public void ChangeProperty<T>(Expression<Func<T>> expression)
        {
            MemberExpression member = (MemberExpression)expression.Body;
            string propName = member.Member.Name;
            ChangeProperty(propName);
        }
        #endregion
        /// <summary>
        /// 名称
        /// </summary>
        private string _name = "";
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                ChangeProperty(() => Name);
            }
        }
        /// <summary>
        /// 输出枚举
        /// </summary>
        private EN_GoogolExtendOutput _googolExtendOutput;
        public EN_GoogolExtendOutput GoogolExtendOutput
        {
            get { return _googolExtendOutput; }
            set
            {
                _googolExtendOutput = value;
                ChangeProperty(() => GoogolExtendOutput);
            }
        }
        /// <summary>
        /// 是否开启
        /// </summary>
        private bool _extBool = false;
        public bool ExtBool
        {
            get { return _extBool; }
            set
            {
                _extBool = value;
                ChangeProperty(() => ExtBool);
            }
        }

    }

}
