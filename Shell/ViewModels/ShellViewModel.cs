using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using FontAwesome5;
using HandyControl.Data;
using QA.Business;
using QA.Business.CacheParam;
using QA.Business.Component.Camera;
using QA.Business.Component.HIVE;
using QA.Business.Component.MES;
using QA.Business.Component.Motion.Googol;
using QA.Business.Component.PLC;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Message;
using QA.Business.Model.Alarm;
using QA.Business.Procedure;
using QA.Business.Station;
using QA.Business.Steps;
using QA.IntelligentEquipment.Models;
using QA.Pages.Interfaces;
using QA.Pages.Models;
using QA.Pages.OtherViews.ViewModels;
using QA.Pages.ViewModels;
using QA.UserControls.ViewModels;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using MessageBox = HandyControl.Controls.MessageBox;
using StepStatus = QA.Business.Steps.StepStatus;

namespace QA.IntelligentEquipment.ViewModels
{
    [Export(typeof(ShellViewModel))]
    public class ShellViewModel : Conductor<IPageViewModel>.Collection.OneActive, IHandle<LoginSuccessMessage>, IHandle<List<IComponent>>
    {
        #region Field
        private readonly IWindowManager _windowManager;
        private readonly IEventAggregator _eventAggregator;
        private ParamManager _paramManager;
        private CacheParamManager _cacheParamManager;
        private TaskManager _taskManager;
        private MyUserManager _myUserManager;
        private StepStatus _stepStatus;
        private GlobalVariable _globalVariable;
        private IBaseBiz _baseBiz;
        private Camera_Component _camera_Component;
        private MotionGoogol_Component _mGoogol_Component;
        private PLC_Component _plc_Component;
        private MES_Component _mes_Component;
        private Hive_Component _hive_Component;
        private static readonly SolidColorBrush QuickBackGround = new SolidColorBrush(Color.FromRgb(0x00, 0x8d, 0x86));//快克绿色
        private List<IComponent> _component;
        private List<IPageViewModel> viewModels;
        private string targetVMName = "";
        #region 检查卷料SN
        bool IsFeederLockedbool = true;
        bool CheckTapeSNOkbool = true;
        AlarmInfoModel IsFeederLockedalarm;
        AlarmInfoModel CheckTapeSNOkalarm;
        #endregion
        #endregion

        #region Property
        public IPageViewModel _rbItemMainForm;
        public IPageViewModel rbItemMainForm
        {
            get => _rbItemMainForm;
            set
            {
                _rbItemMainForm = value;
                NotifyOfPropertyChange(() => rbItemMainForm);
            }
        }

        public IPageViewModel _rbItemSetup;
        public IPageViewModel rbItemSetup
        {
            get => _rbItemSetup;
            set
            {
                _rbItemSetup = value;
                NotifyOfPropertyChange(() => rbItemSetup);
            }
        }

        public IPageViewModel _rbItemCCD;
        public IPageViewModel rbItemCCD
        {
            get => _rbItemCCD;
            set
            {
                _rbItemCCD = value;
                NotifyOfPropertyChange(() => rbItemCCD);
            }
        }

        public IPageViewModel _rbItemAlarm;
        public IPageViewModel rbItemAlarm
        {
            get => _rbItemAlarm;
            set
            {
                _rbItemAlarm = value;
                NotifyOfPropertyChange(() => rbItemAlarm);
            }
        }

        public IPageViewModel _rbItemProductivity;
        public IPageViewModel rbItemProductivity
        {
            get => _rbItemProductivity;
            set
            {
                _rbItemProductivity = value;
                NotifyOfPropertyChange(() => rbItemProductivity);
            }
        }

        public IPageViewModel _rbItemLogin;
        public IPageViewModel rbItemLogin
        {
            get => _rbItemLogin;
            set
            {
                _rbItemLogin = value;
                NotifyOfPropertyChange(() => rbItemLogin);
            }
        }

        public GlobalVariable GlobalVariableProperty { get => _globalVariable; }
        private HomeUiParam_Enable Enable { get => _stepStatus.CacheParamManager.HomeUiParam.Enable; }

        public int _selecetedIndex;
        public int SelecetedIndex
        {
            get => _selecetedIndex;
            set
            {
                _selecetedIndex = value;
                NotifyOfPropertyChange("SelecetedIndex");
            }
        }

        private string _machineName = "LA400";
        public string MachineName
        {
            get => _machineName;
            set
            {
                _machineName = value;
                NotifyOfPropertyChange(() => MachineName);
            }
        }

        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                NotifyOfPropertyChange(() => Title);
            }
        }

        private WindowState _windowState = WindowState.Maximized;
        public WindowState WindowState
        {
            get => _windowState;
            set
            {
                _windowState = value;
                NotifyOfPropertyChange(() => WindowState);
            }
        }

        private string _userStatus = "未登录";
        public string UserStatus
        {
            get => _userStatus;
            set
            {
                _userStatus = value;
                NotifyOfPropertyChange(() => UserStatus);
            }
        }

        private string _machineStatus;
        public string MachineStatus
        {
            get => _machineStatus;
            set
            {
                _machineStatus = value;
                NotifyOfPropertyChange(() => MachineStatus);
            }
        }

        private string _localStatusInfo;
        public string LocalStatusInfo
        {
            get => _localStatusInfo;
            set
            {
                _localStatusInfo = value;
                NotifyOfPropertyChange(() => LocalStatusInfo);
            }
        }

        private SolidColorBrush _statusForeColor;
        public SolidColorBrush StatusForeColor
        {
            get => _statusForeColor;
            set
            {
                _statusForeColor = value;
                NotifyOfPropertyChange(() => StatusForeColor);
            }
        }

        private LocalStatusModel _localStatus = new LocalStatusModel();
        public LocalStatusModel LocalStatus
        {
            get => _localStatus;
            set
            {
                _localStatus = value;
                NotifyOfPropertyChange(() => LocalStatus);
            }
        }

        private bool _isOpen;
        public bool IsOpen
        {
            get => _isOpen;
            set
            {
                _isOpen = value;
                NotifyOfPropertyChange(() => IsOpen);
            }
        }

        private bool _isHaveAlarm;
        public bool IsHaveAlarm
        {
            get => _isHaveAlarm;
            set
            {
                _isHaveAlarm = value;
                NotifyOfPropertyChange(() => IsHaveAlarm);
            }
        }

        private bool _isAutoRun = false;
        public bool IsAutoRun
        {
            get => _isAutoRun;
            set
            {
                _isAutoRun = value;
                NotifyOfPropertyChange(() => IsAutoRun);
            }
        }

        private string _softRunDateTime = string.Empty;
        public string SoftRunDateTime
        {
            get => _softRunDateTime;
            set
            {
                _softRunDateTime = value;
                NotifyOfPropertyChange(() => SoftRunDateTime);
            }
        }

        private string _taskName = string.Empty;
        public string TaskName
        {
            get => _taskName;
            set
            {
                _taskName = value;
                NotifyOfPropertyChange(() => TaskName);
            }
        }

        private ObservableCollection<MenuItemModel> _menuItemModels = new ObservableCollection<MenuItemModel>();
        public ObservableCollection<MenuItemModel> MenuItemModels
        {
            get => _menuItemModels;
            set
            {
                _menuItemModels = value;
                NotifyOfPropertyChange(() => MenuItemModels);
            }
        }

        public event EventHandler<ViewAttachedEventArgs> ViewAttached;

        private SolidColorBrush _runModeBrush;
        public SolidColorBrush RunModeBrush
        {
            get => _runModeBrush;
            set
            {
                _runModeBrush = value;
                NotifyOfPropertyChange(() => RunModeBrush);
            }
        }

        private RobotRealStateInfo _robotRealStateInfo = new RobotRealStateInfo();
        public RobotRealStateInfo RobotRealStateInfos
        {
            get => _robotRealStateInfo;
            set
            {
                _robotRealStateInfo = value;
                NotifyOfPropertyChange(() => RobotRealStateInfos);
            }
        }

        private string _robotRealInfo;
        public string RobotRealInfo
        {
            get => _robotRealInfo;
            set
            {
                _robotRealInfo = value;
                NotifyOfPropertyChange(() => RobotRealInfo);
            }
        }

        private bool _enableButtons = false;
        public bool EnableButtons
        {
            get => _enableButtons;
            set
            {
                _enableButtons = value;
                NotifyOfPropertyChange(() => EnableButtons);
            }
        }

        public EN_HiveStatus HiveStatus => _hive_Component.HiveStatus;
        #endregion

        #region UI
        [Import("CarrierInfoPanelViewModel")]
        public CarrierInfoPanelViewModel CarrierInfoPanelViewModel { get; set; }
        #endregion

        #region Constructor
        public ShellViewModel()
        {
            _windowManager = IoC.Get<IWindowManager>();
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Subscribe(this);
            _paramManager = IoC.Get<ParamManager>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
            _taskManager = IoC.Get<TaskManager>();
            _myUserManager = IoC.Get<MyUserManager>();
            _stepStatus = IoC.Get<StepStatus>();
            _globalVariable = IoC.Get<GlobalVariable>();
            _baseBiz = IoC.Get<IBaseBiz>();
            _camera_Component = (Camera_Component)IoC.Get<ICamera>();
            _mGoogol_Component = (MotionGoogol_Component)IoC.Get<IMGoogol>();
            _plc_Component = (PLC_Component)IoC.Get<IPLC>();
            _mes_Component = (MES_Component)IoC.Get<IMES>();
            _hive_Component = (Hive_Component)IoC.Get<IHive>();

            MachineStatus = "  FA06-005  ";
            RunModeBrush = QuickBackGround;

            ScannerHook.Start();
            BC750.Start();

            Init();
        }
        #endregion

        private object loginMsgLockobj = new object();

        /// <summary>
        /// 切换导航页面
        /// </summary>
        /// <param name="tag"></param>
        public void NavigatePage(string vmName)
        {
            lock (loginMsgLockobj)
            {
                if (string.IsNullOrEmpty(vmName))
                {
                    return;
                }
                var targetIndex = viewModels.ToList().FindIndex(t => t.GetType().Name.Equals(vmName));
                var loginIndex = viewModels.ToList().FindIndex(t => t.GetType().Equals(typeof(LoginPageViewModel)));
                if (targetIndex == -1 || loginIndex == -1)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"找不到页面{vmName}，需要修改软件", En_Logout_Type.Exception, true);
                    return;
                }
                if (vmName == typeof(MainFormPageViewModel).Name)
                {
                    //点击主页面时自动切换为OP权限
                    _myUserManager.Login(MyUserManager.userNameOperator);
                    _hive_Component.InSpotCheckPage = false;
                }
                else if (vmName == typeof(SetupPageViewModel).Name || vmName == typeof(CCDPageViewModel).Name)
                {
                    //点击某些页面时需要Engineer权限，修改登录页面的用户选择
                    if (_myUserManager.CurrUserInfo.Type < MyUserManager.userTypeEngineer)
                    {
                        PublishNeedLoginMessage(MyUserManager.userTypeEngineer);
                        SelecetedIndex = loginIndex;
                        targetVMName = vmName;
                        return;
                    }
                    _hive_Component.InSpotCheckPage = true;
                    //防止切换到手动界面不停机，没有上传MES
                    if (_plc_Component.IsCurStart)
                    {
                        _plc_Component.TrigPlcButton(EN_Plc_TrigButton.Pause);
                        NLogTrace.LogOut(EN_WARN_LEVEL.Warn, $"上位机自动运行中切换到手动界面，设备暂停运行", En_Logout_Type.Exception, true);
                    }
                }
                else if (targetIndex == loginIndex)
                {
                    //点击登录页面时，修改登录页面的用户选择
                    PublishNeedLoginMessage(MyUserManager.userTypeOperator);
                }
                try
                {
                    targetVMName = "";
                    SelecetedIndex = targetIndex;
                }
                catch (Exception ex)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, ex.ToString(), En_Logout_Type.Exception, true);
                }
            }
        }

        public void PublishNeedLoginMessage(EN_UserType targetUserType)
        {
            _eventAggregator.Publish(new NeedLoginMessage() { TargetUserType = targetUserType }, action => { Task.Run(action); });
        }

        public void OpenImgDir()
        {
            try
            {
                string dir = ((CameraParam)_camera_Component.Param).ImgPath;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                System.Diagnostics.Process.Start(dir);
            }
            catch (Exception e)
            {
                //ignored
            }
        }

        public void OpenCsvDir()
        {
            try
            {
                //string dir = @"D:\CsvData";
                string dir = @"D:\QKProject\Data";
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                System.Diagnostics.Process.Start(dir);
            }
            catch (Exception e)
            {
                //ignored
            }
        }

        public void OpenLogDir()
        {
            //string dir = AppDomain.CurrentDomain.BaseDirectory + "Logs";
            string dir = @"D:\QKProject\Logs";

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            System.Diagnostics.Process.Start(dir);
        }

        protected override void OnInitialize()
        {
            viewModels = IoC.GetAll<IPageViewModel>().OrderBy(item => item.OrderID).ToList();
            rbItemMainForm = viewModels[0];
            rbItemSetup = viewModels[1];
            rbItemCCD = viewModels[2];
            rbItemAlarm = viewModels[3];
            rbItemProductivity = viewModels[4];
            rbItemLogin = viewModels[5];
            base.OnInitialize();
        }

        protected override void OnActivate()
        {
            InitCarrierPanel(_stepStatus.CurrentProcedure);
            base.OnActivate();
        }

        private void Init()
        {
            MachineName = System.Configuration.ConfigurationManager.AppSettings["MachineName"];
            StatusForeColor = new SolidColorBrush(Color.FromRgb(0xff, 0x00, 0x00));
            LocalStatus.Version = $"Ver {Application.ResourceAssembly.GetName().Version}";
            LocalStatus.CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            LocalStatusInfo = LocalStatus.ToString();
            RefreshLocalStatus();

            _stepStatus.CurrentProcedure = _taskManager.GetTaskByName(_cacheParamManager.RunInfoParam.CurTaskName);
            if (_stepStatus.CurrentProcedure == null)
            {
                if (_taskManager.Tasks.Count > 0)
                {
                    //选择已有的第一个制程
                    _stepStatus.CurrentProcedure = _taskManager.Tasks[0];
                    MessageBox.Warning("制程为空！\n自动选择制程 " + _stepStatus.CurrentProcedure.ProcedureName + "\n请重启软件！", "操作提示");
                }
                else
                {
                    //新生成一个制程
                    LaserSprayProcedure task = new LaserSprayProcedure();
                    task.ProcedureName = "默认制程";
                    _taskManager.SaveTask(null, task, out string info);
                    _stepStatus.CurrentProcedure = task;
                }
                _cacheParamManager.RunInfoParam.CurTaskName = _stepStatus.CurrentProcedure.ProcedureName;
                _cacheParamManager.SaveRunInfoParam();
                BC750.Stop();
                _mGoogol_Component.SetServeOnOff(false);//关闭所有伺服
                _cacheParamManager?.SaveRunInfoParam();
                TryClose();
                Environment.Exit(0);
            }
            if (!_stepStatus.CurrentProcedure.IsCavityReasonable())
            {
                MessageBoxResult vr = MessageBox.Show("该制程穴位号有冲突，修改后才能选择该制程！", "操作提示", MessageBoxButton.OK, MessageBoxImage.Question);
                _stepStatus.CurrentProcedure = null;
                TaskName = "未选择制程";
                return;
            }
            TaskName = _cacheParamManager.RunInfoParam.CurTaskName;
            //这里修改显示没用，界面还没加载出来，要放到OnActivate里面
            //InitCarrierPanel(_stepStatus.CurProcedure);
        }

        /// <summary>
        /// 打开系统设置窗口
        /// </summary>
        public void SystemConfigWindow()
        {
            var loginSettings = new Dictionary<string, object> { { "ResizeMode", ResizeMode.NoResize } };
            var previousType = _myUserManager.CurrUserInfo.Type;
            _myUserManager.IsLoginDialogShowing = true;
            bool? result = _windowManager.ShowDialog(new LoginWindowViewModel(MyUserManager.userTypeEngineer, false), null, loginSettings);
            _myUserManager.IsLoginDialogShowing = false;
            if (result != true)
            {
                return;
            }
            dynamic settings = new ExpandoObject();
            settings.Height = 800;
            settings.Width = 1200;
            settings.SizeToContent = SizeToContent.Manual;
            settings.ResizeMode = ResizeMode.NoResize;
            _hive_Component.InSettingPage = true;
            _windowManager.ShowDialog(new SystemConfigPageViewModel(_windowManager, _eventAggregator), null, settings);
            _myUserManager.Login(previousType);
            _hive_Component.InSettingPage = false;
        }

        public void BtnSelectTask()
        {
            SelectTaskViewModel selectTaskViewModel = new SelectTaskViewModel();
            bool? result = _windowManager.ShowDialog(selectTaskViewModel);
            if (result == true && !string.IsNullOrEmpty(selectTaskViewModel.SelectedTaskName))
            {
                LaserSprayProcedure task = _taskManager.GetTaskByName(selectTaskViewModel.SelectedTaskName);
                if (task == null)
                {
                    MessageBox.Show("制程为空！", "操作提示", MessageBoxButton.OK, MessageBoxImage.Question);
                    return;
                }
                if (!task.IsCavityReasonable())
                {
                    MessageBox.Show("该制程穴位号有冲突，修改后才能选择该制程！", "操作提示", MessageBoxButton.OK, MessageBoxImage.Question);
                    return;
                }
                _stepStatus.CurrentProcedure = task;
                TaskName = selectTaskViewModel.SelectedTaskName;
                _cacheParamManager.RunInfoParam.CurTaskName = TaskName;
                _cacheParamManager.SaveRunInfoParam();
                InitCarrierPanel(task);
            }
        }

        public void InitCarrierPanel(LaserSprayProcedure procedure)
        {
            if (procedure == null)
            {
                return;
            }
            int rows = procedure.GeneralParam.CarrierRows;
            int cols = procedure.GeneralParam.CarrierColumns;
            CarrierInfoPanelViewModel.SetCarrierArray(rows, cols);
            CarrierInfoPanelViewModel.SetProcedureName(TaskName);

            List<TrayInfoModel> trayInfos = new List<TrayInfoModel>();
            if (procedure.VisionPoints.Count == 12)
            {
                //针对12个穴位的制程，专门进行匹配
                int[] cavityOrder = new[] { 10, 11, 12, 7, 8, 9, 4, 5, 6, 1, 2, 3, };
                for (int idx = 0; idx < 12; idx++)
                {
                    int cavity = cavityOrder[idx];
                    foreach (var item in procedure.VisionPoints)
                    {
                        if (item.CavityNum == cavity)
                        {
                            trayInfos.Add(new TrayInfoModel()
                            {
                                CavityNum = item.CavityNum,
                                IsUsed = item.IsUsed,
                                Status = item.IsUsed ? EN_TrayStatus.Common : EN_TrayStatus.Unuse,
                            });
                        }
                    }
                }
            }
            else
            {
                foreach (var item in procedure.VisionPoints)
                {
                    trayInfos.Add(new TrayInfoModel()
                    {
                        CavityNum = item.CavityNum,
                        IsUsed = item.IsUsed,
                        Status = item.IsUsed ? EN_TrayStatus.Common : EN_TrayStatus.Unuse,
                    });
                }
            }
            CarrierInfoPanelViewModel.SetTrayInfos(trayInfos);
        }

        public void MinimizeWindow()
        {
            WindowState = WindowState.Minimized;
        }

        public void MaximizeWindow()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        public void CloseWindow()
        {
            if (MessageBox.Show(new MessageBoxInfo
            {
                Message = "退出软件?",
                Caption = "提示",
                Button = MessageBoxButton.YesNo,
                IconBrushKey = ResourceToken.AccentBrush,
                IconKey = ResourceToken.AskGeometry,
            }) != MessageBoxResult.Yes)
            {
                return;
            }
            ScannerHook.Stop();
            BC750.Stop();
            _mGoogol_Component.SetServeOnOff(false);
            _cacheParamManager?.SaveRunInfoParam();
            TryClose();
            Environment.Exit(0);
        }

        //public void ChangeWorkMode()
        //{
        //    if (!EnableButtons)
        //    {
        //        return;
        //    }
        //    //需要权限才能修改模式
        //    var settings = new Dictionary<string, object> { { "ResizeMode", ResizeMode.NoResize } };
        //    _myUserManager.IsLoginDialogShowing = true;
        //    bool? result = _windowManager.ShowDialog(new LoginWindowViewModel(MyUserManager.userTypeEngineer, true), null, settings);
        //    _myUserManager.IsLoginDialogShowing = false;
        //    if (result != true)
        //    {
        //        return;
        //    }
        //    if (_globalVariable.WorkMode == WorkMode.Product)
        //    {
        //        var dialogResult = MessageBox.Show("要切换至哪种模式？\n是表示切换至调试模式\n否表示切换至计划停机模式", "操作提示", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        //        if (dialogResult == MessageBoxResult.Yes)
        //        {
        //            _globalVariable.WorkMode = WorkMode.Debug;
        //        }
        //        else if (dialogResult == MessageBoxResult.No)
        //        {
        //            _globalVariable.WorkMode = WorkMode.PlannedDT;
        //        }
        //    }
        //    else
        //    {
        //        _globalVariable.WorkMode = WorkMode.Product;
        //    }
        //}

        public void ChangeHiveState()
        {
            if (!EnableButtons)
            {
                return;
            }
            lock (loginMsgLockobj)
            {
                var loginIndex = viewModels.ToList().FindIndex(t => t.GetType().Equals(typeof(LoginPageViewModel)));
                if (loginIndex == -1)
                {
                    return;
                }
                string targetVMName1 = viewModels[SelecetedIndex].GetType().Name;
                if (_myUserManager.CurrUserInfo.Type < MyUserManager.userTypeEngineer)
                {
                    PublishNeedLoginMessage(MyUserManager.userTypeEngineer);
                    SelecetedIndex = loginIndex;
                    if (targetVMName1 != typeof(MainFormPageViewModel).Name)
                    {
                        targetVMName = targetVMName1;
                    }
                    return;
                }
            }
            var settings = new Dictionary<string, object> { { "ResizeMode", ResizeMode.NoResize } };
            bool? result = _windowManager.ShowDialog(new ChangeHiveState1ViewModel(), null, settings);
            if (result != true)
            {
                return;
            }
        }

        public void ChangeStartMode(object _icon)
        {
            //string ttt = (_icon as TextBlock).Tag.ToString();           
            if (null != _icon)
            {
                EFontAwesomeIcon icon = (EFontAwesomeIcon)_icon;

                switch (icon)
                {
                    case EFontAwesomeIcon.Solid_Play:
                        {
                            //_globalVariable.isStartMode = true;
                            break;
                        }
                    case EFontAwesomeIcon.Solid_Pause:
                        {
                            break;
                        }
                    case EFontAwesomeIcon.Solid_UndoAlt:
                        {
                            //_globalVariable.isStartMode = false;
                            break;
                        }
                }
            }
            else
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"ChangeStartMode Method Exception{_icon.ToString()}");
            }
        }

        /// <summary>
        /// 实时刷新当前时间
        /// </summary>
        private async void RefreshLocalStatus()
        {
            await Task.Factory.StartNew(() =>
            {
                _cacheParamManager.HomeUiParam.Statistic.AddSoftwareVersionIfNeed(_hive_Component.MainSoftwareSHA1);
                int count = 0;
                int[] alive = new int[8];
                string[] aliveName = new string[] { "1号保压头", "2号保压头", "3号保压头", "4号保压头", "1号吸嘴", "2号吸嘴", "3号吸嘴", "4号吸嘴" };
                AlarmInfoModel feederNoCodeAlarm = null;
                var _baseBiz = (BaseBiz)IoC.Get<IBaseBiz>();
                bool needShowNoSnTip = true;
                FeederId lastFeeder = FeederId.左飞达;
                bool FeederCleanedFlag = false;
                while (true)
                {
                    FeederId feederId = _cacheParamManager.HomeUiParam.Enable.FeederId;
                    EN_FeederStatus _FeederStatus = _plc_Component.GetFeederInPlaceStatus();
                    MESParam _mesparam = _paramManager.MESParam;
                    var a = _mesparam.GetHashCode();
                    var b = _paramManager.MESParam.GetHashCode();                                                                                                                                                                                                                                                    
                    _mesparam.Feeder1TapeSN = "888";
                    _paramManager.SaveParam(_mesparam);
                    if (a != b) MessageBox.Show("!!!");
                    if (_plc_Component.IsConnected == false) continue;
                    Thread.Sleep(50);
                    if (_component == null)
                    {
                        continue;
                    }
                    count++;
                    //修改主界面顶端报警指示灯的颜色
                    if (count % 5 == 0)
                    {
                        if (_baseBiz.GetAlarmInfo().Count > 0)
                        {
                            IsHaveAlarm = !IsHaveAlarm;
                        }
                        else
                        {
                            IsHaveAlarm = true;
                        }
                    }
                    //大约0.3s
                    if (count % 6 == 0)
                    {
                        ushort value = 0;
                        //判断寿命是否清零
                        for (int index = 0; index < alive.Length; index++)
                        {
                            int aliveCount = _plc_Component.GetAlive(index);
                            if (aliveCount != -1)
                            {
                                if (aliveCount < alive[index])
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, aliveName[index] + "寿命变化：" + alive[index] + "->" + aliveCount, En_Logout_Type.Default, true);
                                }
                                alive[index] = aliveCount;
                            }
                        }
                        //判断plc是否清除次数
                        for (int i = 0; i < 2; i++)
                        {
                            _plc_Component.GetAddrValue(0, (ushort)(1800 + i), ref value);
                            if (value == 1)
                            {
                                _cacheParamManager.HomeUiParam.Statistic.ResetNozzleCount(i + 1);
                                _cacheParamManager.HomeUiParam.Statistic.ResetNozzledebugCount(i + 1);
                                _cacheParamManager.SaveHomeUiParam();
                                _plc_Component.SetAddrValue(0, (ushort)(1800 + i), 0);
                            }
                        }
                        _plc_Component.GetAddrValue(0, 1804, ref value);
                        if (value == 1)
                        {
                            _cacheParamManager.HomeUiParam.Statistic.ResetNozzleCount(0);
                            _cacheParamManager.HomeUiParam.Statistic.ResetNozzledebugCount(0);
                            _cacheParamManager.SaveHomeUiParam();
                            _plc_Component.SetAddrValue(0, 1804, 0);
                        }
                        //清除小时产能数据
                        _plc_Component.GetAddrValue(0, 1805, ref value);
                        if (value == 1)
                        {
                            _cacheParamManager.HomeUiParam.Statistic.ResetUph();
                            _cacheParamManager.SaveHomeUiParam();
                            _plc_Component.SetAddrValue(0, 1805, 0);
                        }
                        //清除吸嘴1数据
                        _plc_Component.GetAddrValue(0, 1806, ref value);
                        if (value == 1)
                        {
                            _cacheParamManager.HomeUiParam.Statistic.ResetNozzleCount(1);
                            _cacheParamManager.SaveHomeUiParam();
                            _plc_Component.SetAddrValue(0, 1806, 0);
                        }
                        //清除吸嘴2数据
                        _plc_Component.GetAddrValue(0, 1807, ref value);
                        if (value == 1)
                        {
                            _cacheParamManager.HomeUiParam.Statistic.ResetNozzleCount(2);
                            _cacheParamManager.SaveHomeUiParam();
                            _plc_Component.SetAddrValue(0, 1807, 0);
                        }
                        //全清
                        _plc_Component.GetAddrValue(0, 1808, ref value);
                        if (value == 1)
                        {
                            _cacheParamManager.HomeUiParam.Statistic.ResetNozzleCount(0);
                            _cacheParamManager.SaveHomeUiParam();
                            _plc_Component.SetAddrValue(0, 1808, 0);
                        }
                        //检测是否需要添加、移除卷膜SN报警
                        MESParam _mesParam = _paramManager.MESParam;
                        bool currFeederOkFlag = _plc_Component.IsFeederLocked();
                        _hive_Component.ChangeMaterial = !currFeederOkFlag;
                        if ((_hive_Component.HiveStatus == EN_HiveStatus.Running || _hive_Component.HiveStatus == EN_HiveStatus.Idle) && string.IsNullOrEmpty(feederId == FeederId.左飞达 ? _mesParam.Feeder1TapeSN: _mesParam.Feeder2TapeSN))
                        {
                            if (feederNoCodeAlarm == null || !_baseBiz.ContainsRunAlarm(feederNoCodeAlarm))
                            {
                                feederNoCodeAlarm = _baseBiz.AddRunAlarm(EN_WarnModules.Robot, "未录入TapeSn", 111);
                                needShowNoSnTip = true;
                            }
                        }
                        if (feederNoCodeAlarm != null && !string.IsNullOrEmpty(feederId == FeederId.左飞达 ? _mesParam.Feeder1TapeSN : _mesParam.Feeder2TapeSN))
                        {
                            if (_hive_Component.HiveStatus != EN_HiveStatus.Running || _hive_Component.HiveStatus != EN_HiveStatus.Idle || _hive_Component.HiveStatus != EN_HiveStatus.Downtime)
                            {
                                _baseBiz.RemoveRunAlarm(feederNoCodeAlarm);
                                feederNoCodeAlarm = null;
                            }
                        }
                        //飞达拉出时必须清除sn
                        if (currFeederOkFlag == false&& FeederCleanedFlag == false)
                        {
                            switch (_FeederStatus)
                            {
                                case EN_FeederStatus.NoUse:
                                    _mesParam.Feeder1TapeSN = "";
                                    _mesParam.Feeder2TapeSN = "";
                                    break;
                                case EN_FeederStatus.Feeder1Using:
                                    _mesParam.Feeder2TapeSN = "";
                                    lastFeeder = FeederId.右飞达;
                                    break;
                                case EN_FeederStatus.Feeder2Using:
                                    _mesParam.Feeder1TapeSN = "";
                                    lastFeeder = FeederId.左飞达;
                                    break;
                                case EN_FeederStatus.BothUse:
                                    break;
                                default:
                                    break;
                            }
                            _paramManager.SaveParam(_mesParam);
                            FeederCleanedFlag = true;
                            ScannerHook.ClearLastBarCode();
                            if (needShowNoSnTip == false)
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Warn, $"请使用扫码枪扫描{lastFeeder}卷料SN", En_Logout_Type.Default, true);
                            }
                            needShowNoSnTip = true;
                            //if (Enable.PickOpportunity == EN_PickOpportunity.AfterUpCam)
                            //{
                            //    _stepStatus.NextStep1 = EN_RunStep.Err;
                            //    _stepStatus.NextStep2 = EN_RunStep.Err;
                            //    _stepStatus.NextStep3 = EN_RunStep.Err;
                            //    _ = Task.Run(() =>
                            //    {
                            //        MessageBox.Show("检测到飞达换料，需要复位机台！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                            //    });
                            //}
                        }
                        if (string.IsNullOrEmpty(_mesParam.Feeder1TapeSN)|| string.IsNullOrEmpty(_mesParam.Feeder2TapeSN))
                        {
                            //飞达扫码
                            string lastBarcode = ScannerHook.GetLastBarCode();
                            if (!string.IsNullOrEmpty(lastBarcode))//扫码枪有输入
                            {
                                lastBarcode = lastBarcode.ToUpper();
                                needShowNoSnTip = false;
                                string info;
                                if (_mes_Component.CheckTapeSNOk(lastBarcode, _mesParam.TapeIPNs, _mesParam.TapeLength, out info))
                                {
                                    switch (_FeederStatus)
                                    {
                                        case EN_FeederStatus.NoUse:
                                            NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "请推回一个要使用的飞达后再扫码", En_Logout_Type.Default, true);
                                            break;
                                        case EN_FeederStatus.Feeder1Using:
                                            _mesParam.Feeder2TapeSN = lastBarcode;
                                            _paramManager.SaveParam(_mesParam);
                                            NLogTrace.LogOut(EN_WARN_LEVEL.Sucess, "Feeder2TapeSN：" + lastBarcode, En_Logout_Type.Default, true);
                                            NLogTrace.LogOut(EN_WARN_LEVEL.Sucess, "已设置飞达2卷料SN为：" + lastBarcode, En_Logout_Type.Default, true);
                                            break;
                                        case EN_FeederStatus.Feeder2Using:
                                            _mesParam.Feeder1TapeSN = lastBarcode;
                                            _paramManager.SaveParam(_mesParam);
                                            NLogTrace.LogOut(EN_WARN_LEVEL.Sucess, "Feeder1TapeSN：" + lastBarcode, En_Logout_Type.Default, true);
                                            NLogTrace.LogOut(EN_WARN_LEVEL.Sucess, "已设置飞达1卷料SN为：" + lastBarcode, En_Logout_Type.Default, true);
                                            break;
                                        case EN_FeederStatus.BothUse:
                                            NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "请拉出一个不使用的飞达后再扫码", En_Logout_Type.Default, true);
                                            break;
                                        default:
                                            break;
                                    }

                                }
                                else
                                {
                                    NLogTrace.LogOut(EN_WARN_LEVEL.Info, "异常扫码输入：" + lastBarcode, En_Logout_Type.Default, true);
                                }
                            }

                        }
                        //飞达推回时允许清除扫码
                        if (_FeederStatus == EN_FeederStatus.BothUse)
                        {
                            FeederCleanedFlag = false;
                        }
                        //飞达推回且没有扫码的异常情况
                        if (currFeederOkFlag == true
                        && string.IsNullOrEmpty(lastFeeder == FeederId.左飞达 ? _mesParam.Feeder1TapeSN : _mesParam.Feeder2TapeSN) && needShowNoSnTip)
                        {
                            _ = Task.Run(() =>
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Warn, "请使用扫码枪扫描卷料SN后再启动", En_Logout_Type.Default, true);
                                MessageBox.Show("请使用扫码枪扫描卷料SN后再启动！", "警告", MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                            needShowNoSnTip = false;
                        }
                        #region 检查卷料SN
                        //if ((_hive_Component.HiveStatus == EN_HiveStatus.Running || _hive_Component.HiveStatus == EN_HiveStatus.Idle) || _stepStatus.ParamManager.MESParam.BUse)
                        //{
                        //    //检查飞达是否锁紧
                        //    if (!_plc_Component.IsFeederLocked())
                        //    {
                        //        if (IsFeederLockedbool)
                        //        {
                        //            IsFeederLockedalarm = _baseBiz.AddRunAlarm(EN_WarnModules.DataMan, $"飞达未锁紧");
                        //            IsFeederLockedbool = false;
                        //        }
                        //    }
                        //    else
                        //    {
                        //        if (!IsFeederLockedbool)
                        //        {
                        //            _baseBiz.RemoveRunAlarm(IsFeederLockedalarm);
                        //            IsFeederLockedbool = true;
                        //        }
                        //    }
                        //    //检查B2BTapeSN是否合规
                        //    if (!_mes_Component.CheckTapeSNOk(out string info))
                        //    {
                        //        if (CheckTapeSNOkbool)
                        //        {
                        //            CheckTapeSNOkalarm = _baseBiz.AddRunAlarm(EN_WarnModules.DataMan, $"飞达未锁紧");
                        //            CheckTapeSNOkbool = false;
                        //        }
                        //    }
                        //    else
                        //    {
                        //        if (!CheckTapeSNOkbool)
                        //        {
                        //            _baseBiz.RemoveRunAlarm(CheckTapeSNOkalarm);
                        //            CheckTapeSNOkbool = true;
                        //        }
                        //    }
                        //    //检查B2BTapeSN是否为调机料
                        //    if (_stepStatus.ParamManager.MESParam.DebugTapeSNs.Contains(_stepStatus.ParamManager.MESParam.TapeSN))
                        //    {
                        //        var alarm = _baseBiz.AddRunAlarm(EN_WarnModules.DataMan, $"当前卷料为调机料");
                        //    }
                        //}
                        #endregion
                    }
                    //大约1s
                    if (count % 20 == 0)
                    {
                        _cacheParamManager.HomeUiParam.Statistic.ResetUphAndSaveDataIfNeed();
                        _cacheParamManager.HomeUiParam.Statistic.ResetUphAndSaveDataIfNeedDebug();
                        //定时写心跳
                        _plc_Component.SetHeartBeat();
                        //给PLC写信息
                        ushort[] values5301 = {
                            (ushort)_cacheParamManager.HomeUiParam.Statistic.UseCount1,
                            (ushort)_cacheParamManager.HomeUiParam.Statistic.UseCount2,
                            (ushort)_cacheParamManager.HomeUiParam.Statistic.UseCount3,
                            (ushort)_cacheParamManager.HomeUiParam.Statistic.UseCount4,
                            (ushort)_cacheParamManager.HomeUiParam.Statistic.UseCountAll,

                            (ushort)_cacheParamManager.HomeUiParam.Statistic.SuckNgCount1,
                            (ushort)_cacheParamManager.HomeUiParam.Statistic.SuckNgCount2,
                            (ushort)_cacheParamManager.HomeUiParam.Statistic.SuckNgCount3,
                            (ushort)_cacheParamManager.HomeUiParam.Statistic.SuckNgCount4,
                            (ushort)_cacheParamManager.HomeUiParam.Statistic.SuckNgCountAll,

                            (ushort)_cacheParamManager.HomeUiParam.Statistic.CamNgCount1,
                            (ushort)_cacheParamManager.HomeUiParam.Statistic.CamNgCount2,
                            (ushort)_cacheParamManager.HomeUiParam.Statistic.CamNgCount3,
                            (ushort)_cacheParamManager.HomeUiParam.Statistic.CamNgCount4,
                            (ushort)_cacheParamManager.HomeUiParam.Statistic.CamNgCountAll,

                            (ushort)(_cacheParamManager.HomeUiParam.Statistic.FlingRate1 * 1000),
                            (ushort)(_cacheParamManager.HomeUiParam.Statistic.FlingRate2 * 1000),
                            (ushort)(_cacheParamManager.HomeUiParam.Statistic.FlingRate3 * 1000),
                            (ushort)(_cacheParamManager.HomeUiParam.Statistic.FlingRate4 * 1000),
                            (ushort)(_cacheParamManager.HomeUiParam.Statistic.FlingRateAll * 1000),

                            (ushort)(_cacheParamManager.HomeUiParam.Statistic.OkRate1 * 1000),
                            (ushort)(_cacheParamManager.HomeUiParam.Statistic.OkRate2 * 1000),
                            (ushort)(_cacheParamManager.HomeUiParam.Statistic.OkRate3 * 1000),
                            (ushort)(_cacheParamManager.HomeUiParam.Statistic.OkRate4 * 1000),
                            (ushort)(_cacheParamManager.HomeUiParam.Statistic.OkRateAll * 1000),
                        };
                        _plc_Component.SetAddrMultiValue(0, 5301, values5301);
                        ushort[] values5333 = {
                            (ushort)_cacheParamManager.HomeUiParam.Statistic.UseCountAll,
                            (ushort)_cacheParamManager.HomeUiParam.Statistic.UseCountAll,//目标产量
                            (ushort)(_cacheParamManager.HomeUiParam.Statistic.UseCountAll - _cacheParamManager.HomeUiParam.Statistic.SuckNgCountAll-_cacheParamManager.HomeUiParam.Statistic.CamNgCountAll),
                            (ushort)(_cacheParamManager.HomeUiParam.Statistic.SuckNgCountAll+_cacheParamManager.HomeUiParam.Statistic.CamNgCountAll),
                    };
                        //当前数目，给plc写
                        _plc_Component.SetAddrMultiValue(0, 5333, values5333);
                        _plc_Component.SetAddrValue(0, 5340, (ushort)_globalVariable.CT.TotalCT);

                        //目标数目，从plc读
                        ushort value = 0;
                        _plc_Component.GetAddrValue(0, 5334, ref value);
                        _cacheParamManager.HomeUiParam.Statistic.TargetNum = value;
                        //ct，给plc写
                        _plc_Component.SetAddrValue(0, 5340, (ushort)(_globalVariable.CT.CycleCT * 100));
                        //uph，给plc写
                        ushort[] values5550 = new ushort[24];
                        for (int i = 0; i < 24; i++)
                        {
                            values5550[i] = (ushort)_cacheParamManager.HomeUiParam.Statistic._uph[i];
                        }
                        _plc_Component.SetAddrMultiValue(0, 5550, values5550);
                        //报警次数，给plc写
                        _plc_Component.SetAddrValue(0, 1590, (ushort)_cacheParamManager.HomeUiParam.Statistic.AlarmCount);
                        //三个时间，从plc读
                        _plc_Component.GetAddrValue(0, 1592, ref value);
                        _cacheParamManager.HomeUiParam.Statistic.RunTimeMinute = value;
                        _plc_Component.GetAddrValue(0, 1594, ref value);
                        _cacheParamManager.HomeUiParam.Statistic.IdleTimeMinute = value;
                        _plc_Component.GetAddrValue(0, 1596, ref value);
                        _cacheParamManager.HomeUiParam.Statistic.ErrTimeMinute = value;
                    }
                    //定时保存生产数据，大约30s
                    if (count % 600 == 0)
                    {
                        count = 0;
                        _cacheParamManager.SaveHomeUiParam();
                    }
                    //清零次数，600是上面所有次数的最小公倍数
                    if (count >= 600)
                    {
                        count -= 600;
                    }

                    LocalStatus.CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    //LocalStatusInfo = LocalStatus.ToString();
                    LocalStatusInfo = $"{_paramManager.OtherSettingParam.SoftwareVersion}   {LocalStatus.CurrentTime}";
                    IsAutoRun = _baseBiz.AutoRun;
                    SoftRunDateTime = _cacheParamManager.GetAllTotalRunTime();
                    var pressGet = _mGoogol_Component.AInput;
                    RobotRealInfo = "X1:" + _mGoogol_Component.CurPos[(byte)En_AxisNum.X1].ToString("f3") + "mm " +
                                    "Y1:" + _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1].ToString("f3") + "mm " +
                                    "X2:" + _mGoogol_Component.CurPos[(byte)En_AxisNum.X2].ToString("f3") + "mm " +
                                    "Y2:" + _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2].ToString("f3") + "mm " +
                                    "Z2:" + _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2].ToString("f3") + "mm " +
                                    "R1:" + _mGoogol_Component.CurPos[(byte)En_AxisNum.R1].ToString("f3") + "° " +
                                    ";" + (_cacheParamManager.PressParam.SglParam[0].GetCalibedPress(_mGoogol_Component.AInput[0]) / 10).ToString("F2") + "kg" + ";" +
                                    _mGoogol_Component.AInput[0] +"  "+
                                    "R2:" + _mGoogol_Component.CurPos[(byte)En_AxisNum.R2].ToString("f3") + "° " +
                                    ";" + (_cacheParamManager.PressParam.SglParam[1].GetCalibedPress(_mGoogol_Component.AInput[1]) / 10).ToString("F2") + "kg" + ";" +
                                    _mGoogol_Component.AInput[1] + "  ";
                }
            });
        }

        /// <summary>
        /// 更新当前登录用户及登录后跳转页面
        /// </summary>
        /// <param name="message"></param>
        public void Handle(LoginSuccessMessage message)
        {
            NLogTrace.LogOut(EN_WARN_LEVEL.Sucess, $"已登录用户{_myUserManager.CurrUserInfo.UserName}", En_Logout_Type.SystemParam, false);
            UserStatus = $"当前用户:{_myUserManager.CurrUserInfo.UserName}";
            NavigatePage(targetVMName);
        }

        public void Start()
        {
            if (MessageBox.Ask("确定启动吗？", "提示") == MessageBoxResult.OK)
            {
                _plc_Component.TrigPlcButton(EN_Plc_TrigButton.Start);
            }
        }

        public void Pause()
        {
            if (MessageBox.Ask("确定暂停吗？", "提示") == MessageBoxResult.OK)
            {
                _plc_Component.TrigPlcButton(EN_Plc_TrigButton.Pause);
            }
        }

        public void Stop()
        {
            if (MessageBox.Ask("确定强制复位吗？\n即使机台正在运行，也会强制复位！", "提示") == MessageBoxResult.OK)
            {
                _plc_Component.TrigPlcButton(EN_Plc_TrigButton.Reset);
            }
        }

        /// <summary>
        /// 9075参数生效
        /// </summary>
        public void Set9075SysConfig()
        {
            //mRobot9075.SetLaserPowerParam(paramManager.Motion9075Param.DebugLaserPower, paramManager.Motion9075Param.MaxLaserPower);
            //mRobot9075.SetLaserSprayParam(paramManager.Motion9075Param.laserPowerWastage);
            //mRobot9075.SetSprayBaseParam((ushort)(paramManager.Motion9075Param.BlowAirPress * 1000 / 1.504f), (ushort)(paramManager.Motion9075Param.EveryRotateAngle * 8.889f), (ushort)(paramManager.Motion9075Param.RotateMaxSpeed * 8.889f), paramManager.Motion9075Param.OutballSteadyTime, paramManager.Motion9075Param.OutballRetryCount, paramManager.Motion9075Param.AutoOpenAir, paramManager.Motion9075Param.CheckAirPress, paramManager.Motion9075Param.CheckAirPressAlarm, paramManager.Motion9075Param.CheckBlowAirPressTime);
            //mRobot9075.SetSpeedAxis();
        }

        public void Handle(List<IComponent> message)
        {
            _component = message;
            EnableButtons = !(_baseBiz as BaseBiz).AutoRun;
            NotifyOfPropertyChange(() => HiveStatus);
        }
    }
}
