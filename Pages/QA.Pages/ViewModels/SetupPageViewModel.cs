/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：胡勇
 * 创建日期：2022-02-23
 * 说明：（点检功能逻辑）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using QA.Business;
using QA.Business.Component.Camera;
using QA.Business.Component.Motion.Googol;
using QA.Business.Component.PLC;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Model.RunTimeInfo;
using QA.Business.Procedure;
using QA.Business.Recipe;
using QA.Business.Steps;
using QA.Pages.Interfaces;
using QA.SpotCheckPages;
using QA.UserControls;
using QA.UserControls.Interfaces;
using QA.UserControls.ViewModels;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using MessageBox = HandyControl.Controls.MessageBox;

namespace QA.Pages.ViewModels
{
    [Export(typeof(IPageViewModel))]
    public class SetupPageViewModel : Screen, INotifyPropertyChanged, IPageViewModel
    {
        #region Field
        private readonly IWindowManager _windowManager;
        private readonly IEventAggregator _eventAggregator;
        private ParamManager _paramManager;
        private CacheParamManager _cacheParamManager;
        private RecipeManager _recipeManager;
        private TaskManager _taskManager;
        private GlobalVariable _globalVariable;
        private MotionGoogol_Component _mGoogol_Component;
        private Camera_Component _camera_Component;
        private PLC_Component _plc_Component;
        private StepStatus _stepStatus;
        private MotionGoogolParam _mGoogolParam;
        #endregion

        #region Property
        public override string DisplayName { get; set; } = "点检页面";
        public ushort OrderID { get; set; } = 1;

        public RuntimeLogs RunTimeLogs { get; } = IoC.Get<RuntimeLogs>();

        public byte _selectQuickPage { get; set; } = 0;
        public byte SelectQuickPage
        {
            get { return _selectQuickPage; }
            set
            {
                _selectQuickPage = value;
                NotifyOfPropertyChange(() => SelectQuickPage);
            }
        }

        /// <summary>
        /// 选择相机类型List
        /// </summary>
        private List<string> _selectCameraType = new List<string>();
        public List<string> SelectCameraType
        {
            get
            {
                _selectCameraType.Clear();
                _selectCameraType.Add("上相机");
                _selectCameraType.Add("下相机");
                return _selectCameraType;
            }
            set { _selectCameraType = value; NotifyOfPropertyChange(() => SelectCameraType); }
        }

        /// <summary>
        /// 相机名称
        /// </summary>
        private string _cameraName = "上相机";
        public string CameraName
        {
            get { return _cameraName; }
            set { _cameraName = value; NotifyOfPropertyChange(() => CameraName); }
        }

        //Hidden,Collapsed,Visible
        private string _visualStep1 = "Visible";
        public string VisualStep1
        {
            get => _visualStep1;
            set
            {
                _visualStep1 = value;
                NotifyOfPropertyChange(() => VisualStep1);
            }
        }

        private string _visualStep2 = "Hidden";
        public string VisualStep2
        {
            get => _visualStep2;
            set
            {
                _visualStep2 = value;
                NotifyOfPropertyChange(() => VisualStep2);
            }
        }

        private string _visualStep3 = "Hidden";
        public string VisualStep3
        {
            get => _visualStep3;
            set
            {
                _visualStep3 = value;
                NotifyOfPropertyChange(() => VisualStep3);
            }
        }

        private string _visualStep4 = "Hidden";
        public string VisualStep4
        {
            get => _visualStep4;
            set
            {
                _visualStep4 = value;
                NotifyOfPropertyChange(() => VisualStep4);
            }
        }

        private int _selNozzleNo = 1;
        public int SelNozzleNo
        {
            get => _selNozzleNo;
            set
            {
                _selNozzleNo = value;
                NotifyOfPropertyChange(() => SelNozzleNo);
            }
        }

        private List<int> _nozzleNoLst = new List<int>();
        public List<int> NozzleNoLst
        {
            get => _nozzleNoLst;
            set
            {
                _nozzleNoLst = value;
                NotifyOfPropertyChange(() => NozzleNoLst);
            }
        }

        private int _selCavityNo = 1;
        public int SelCavityNo
        {
            get => _selCavityNo;
            set
            {
                _selCavityNo = value;
                NotifyOfPropertyChange(() => SelCavityNo);
            }
        }

        private List<int> _cavityNoLst = new List<int>();
        public List<int> CavityNoLst
        {
            get => _cavityNoLst;
            set
            {
                _cavityNoLst = value;
                NotifyOfPropertyChange(() => CavityNoLst);
            }
        }

        /// <summary>
        /// 制程页面显示的制程
        /// </summary>
        private LaserSprayProcedure _currProcedure = null;
        public LaserSprayProcedure CurrProcedure
        {
            get { return _currProcedure; }
            set
            {
                _currProcedure = value;
                NotifyOfPropertyChange(() => CurrProcedure);
            }
        }

        /// <summary>
        /// 制程页面显示的制程对应的索引。如果该制程并非加载的制程，则索引为-1
        /// </summary>
        private int IdxOfOriProcedure = -1;

        /// <summary>
        /// 如果为加载制程，该值表示被加载的制程副本
        /// </summary>
        private LaserSprayProcedure OriProcedure = null;

        /// <summary>
        /// 当前选择的视觉点
        /// </summary>
        public LaserSprayVisionPoint _selectedVisionPoint = null;
        public LaserSprayVisionPoint SelectedVisionPoint
        {
            get => _selectedVisionPoint;
            set
            {
                _selectedVisionPoint = value;
                NotifyOfPropertyChange(() => SelectedVisionPoint);
            }
        }

        /// <summary>
        /// 当前选择的贴合点
        /// </summary>
        public LaserSpraySolderPoint _selectedSolderPoint = null;
        public LaserSpraySolderPoint SelectedSolderPoint
        {
            get => _selectedSolderPoint;
            set
            {
                _selectedSolderPoint = value;
                NotifyOfPropertyChange(() => SelectedSolderPoint);
            }
        }

        /// <summary>
        /// 当前焊接配方
        /// </summary>
        private LaserSprayRecipe _selectedRecipe = null;
        public LaserSprayRecipe SelectedRecipe
        {
            get { return _selectedRecipe; }
            set
            {
                _selectedRecipe = value;
                NotifyOfPropertyChange(() => SelectedRecipe);
            }
        }

        /// <summary>
        /// 配方列表
        /// </summary>
        public ObservableCollection<LaserSprayRecipe> _recipes = new ObservableCollection<LaserSprayRecipe>();
        public ObservableCollection<LaserSprayRecipe> Recipes
        {
            get => _recipes;
            set
            {
                _recipes = value;
                NotifyOfPropertyChange(() => Recipes);
            }
        }

        #endregion

        #region UI
        [Import("SpotCheckMotionCtrlPageViewModel", typeof(ISpotPageViewModel))]
        public ISpotPageViewModel SpotCheckMotionCtrlPageViewModel { get; set; }

        [Import("SpotCheckMesPageViewModel", typeof(ISpotPageViewModel))]
        public ISpotPageViewModel SpotCheckMesPageViewModel { get; set; }

        [Import("SpotCheckScannerPageViewModel", typeof(ISpotPageViewModel))]
        public ISpotPageViewModel SpotCheckScannerPageViewModel { get; set; }

        [Import("SpotCheckCalibPageViewModel", typeof(ISpotPageViewModel))]
        public ISpotPageViewModel SpotCheckCalibPageViewModel { get; set; }

        [Import("SpotCheckPressurePageViewModel", typeof(ISpotPageViewModel))]
        public ISpotPageViewModel SpotCheckPressurePageViewModel { get; set; }

        [Import("SpotCheckPlcPageViewModel", typeof(ISpotPageViewModel))]
        public ISpotPageViewModel SpotCheckPlcPageViewModel { get; set; }

        [Import("SpotCheckCameraPageViewModel", typeof(ISpotPageViewModel))]
        public ISpotPageViewModel SpotCheckCameraPageViewModel { get; set; }

        [Import("SpotCheckFTPUploadImgViewModel", typeof(ISpotPageViewModel))]
        public ISpotPageViewModel SpotCheckFTPUploadImgViewModel { get; set; }

        [Import("RobotTeachNewViewModel", typeof(IUserControl))]
        public RobotTeachNewViewModel RobotTeachNewViewModel { get; set; }
        #endregion

        public SetupPageViewModel()
        {
            _windowManager = IoC.Get<IWindowManager>();
            _eventAggregator = IoC.Get<IEventAggregator>();
            _paramManager = IoC.Get<ParamManager>();
            _cacheParamManager = IoC.Get<CacheParamManager>();
            _recipeManager = IoC.Get<RecipeManager>();
            _taskManager = IoC.Get<TaskManager>();
            _globalVariable = IoC.Get<GlobalVariable>();
            _mGoogol_Component = (MotionGoogol_Component)IoC.Get<IMGoogol>();
            _camera_Component = (Camera_Component)IoC.Get<ICamera>();
            _plc_Component = (PLC_Component)IoC.Get<IPLC>();
            _stepStatus = IoC.Get<StepStatus>();
            _mGoogolParam = IoC.Get<MotionGoogolParam>();


            Recipes.Clear();
            if (_recipeManager.Recipes != null)
            {
                foreach (LaserSprayRecipe recipe in _recipeManager.Recipes)
                {
                    Recipes.Add(recipe);
                }
            }
            NozzleNoLst.Add(1);
            NozzleNoLst.Add(2);
            CavityNoLst.Add(1);
            CavityNoLst.Add(2);
            CavityNoLst.Add(3);
            CavityNoLst.Add(4);
            CavityNoLst.Add(5);
            CavityNoLst.Add(6);
            CavityNoLst.Add(7);
            CavityNoLst.Add(8);
            CavityNoLst.Add(9);
            CavityNoLst.Add(10);
            CavityNoLst.Add(11);
            CavityNoLst.Add(12);
            VisualStep1 = "Visible";
            VisualStep2 = "Hidden";
            VisualStep3 = "Hidden";
            VisualStep4 = "Hidden";
        }

        #region Caliburn.Micro
        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
        }

        protected override void OnActivate()
        {
            base.OnActivate();
        }

        protected override void OnDeactivate(bool close)
        {
            if (RobotTeachNewViewModel != null && RobotTeachNewViewModel.IsActive)
            {
                RobotTeachNewViewModel.TryClose();
            }
            base.OnDeactivate(close);
        }
        #endregion

        #region 制程操作

        /// <summary>
        /// 返回当前显示在制程页面的制程是否属于制程列表，且该制程有变动
        /// </summary>
        /// <returns>如果有变动，返回true；否则返回false</returns>
        private bool IsProcedureModifyAndNotSave()
        {
            if (CurrProcedure == null)
            {
                // 当前为null说明制程页面没有制程
                return false;
            }
            if (IdxOfOriProcedure == -1 || OriProcedure == null)
            {
                // 制程页面有制程，且该制程非读取得到，此时该制程仅在制程页面，应该要存储
                return true;
            }
            // 现在二者都不为空，必定为载入的制程，比较是否有变化
            return !OriProcedure.Equals(CurrProcedure);
        }

        /// <summary>
        /// 加载制程。选择一个制程，让其副本显示在制程页面。保存时会移除原有制程。
        /// </summary>
        public void LoadProcedure()
        {
            if (IsProcedureModifyAndNotSave())
            {
                if (MessageBox.Show("当前制程未保存！\n如果继续，未保存的改动将会丢失！\n确认继续吗？(Quy trình hiện tại không được lưu! Nếu tiếp tục, các thay đổi chưa được lưu sẽ bị mất! Xác nhận tiếp tục?)", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                {
                    return;
                }
            }
            SelectTaskViewModel selectTaskViewModel = new SelectTaskViewModel();
            selectTaskViewModel.DisplayName = "请选择要加载的制程";
            bool? result = _windowManager.ShowDialog(selectTaskViewModel);
            if (result == true)
            {
                OriProcedure = _taskManager.GetTaskByName(selectTaskViewModel.SelectedTaskName);
                IdxOfOriProcedure = _taskManager.Tasks.IndexOf(OriProcedure);
                CurrProcedure = OriProcedure.DeepCopy();
                SelectedVisionPoint = null;
                SelectedSolderPoint = null;
                if (!CurrProcedure.IsCavityReasonable())
                {
                    MessageBox.Show("当前制程穴位号有冲突，修改后才能保存！(Hiện tại có xung đột về số huyệt trong chế độ, sau khi sửa đổi mới có thể lưu lại.)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Question);
                }
            }
        }

        /// <summary>
        /// 新建制程。让一个新制程显示在编辑页面。保存时需检查是否存在同名制程。
        /// </summary>
        public void NewProcedure()
        {
            if (IsProcedureModifyAndNotSave())
            {
                if (MessageBox.Show("当前制程未保存！\n如果继续，未保存的改动将会丢失！\n确认继续吗？(Quy trình hiện tại không được lưu! Nếu tiếp tục, các thay đổi chưa được lưu sẽ bị mất! Xác nhận tiếp tục?)", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                {
                    return;
                }
            }
            OriProcedure = null;
            IdxOfOriProcedure = -1;
            CurrProcedure = new LaserSprayProcedure()
            {
                ProcedureName = $"{DateTime.Now.ToString("MMdd_HHmmss")}"
            };
            SelectedVisionPoint = null;
            SelectedSolderPoint = null;
            MessageBox.Show("已新建并加载制程(Tạo và tải process) " + CurrProcedure.ProcedureName + "！", "操作提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// 复制制程。选择一个制程，让其副本改名后显示在制程页面。保存时需检查是否存在同名制程。
        /// </summary>
        public void CopyProcedure()
        {
            SelectTaskViewModel selectTaskViewModel = new SelectTaskViewModel();
            selectTaskViewModel.DisplayName = "请选择要复制的制程";
            bool? result = _windowManager.ShowDialog(selectTaskViewModel);
            if (result == true)
            {
                OriProcedure = null;
                IdxOfOriProcedure = -1;
                CurrProcedure = _taskManager.GetTaskByName(selectTaskViewModel.SelectedTaskName).DeepCopy();
                CurrProcedure.ProcedureName += " - 副本";
                //判断是否已有同名，有则递增序号
                if (_taskManager.GetTaskByName(CurrProcedure.ProcedureName) != null)
                {
                    int i = 2;
                    while (_taskManager.GetTaskByName(CurrProcedure.ProcedureName + " (" + i + ")") != null)
                    {
                        i++;
                    }
                    CurrProcedure.ProcedureName += " (" + i + ")";
                }
                SelectedVisionPoint = null;
                SelectedSolderPoint = null;
                if (!CurrProcedure.IsCavityReasonable())
                {
                    MessageBox.Show("当前制程穴位号有冲突，修改后才能保存！(Hiện tại có xung đột về số huyệt trong chế độ, sau khi sửa đổi mới có thể lưu lại.)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                MessageBox.Show("已生成制程 " + CurrProcedure.ProcedureName + "，可载入该制程后进行编辑！(Chương trình đã được tạo ra và có thể được tải để chỉnh sửa)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 删除制程。选择一个制程，将该制程移除列表，并删除对应制程文件。
        /// </summary>
        public void DeleteProcedure()
        {
            SelectTaskViewModel selectTaskViewModel = new SelectTaskViewModel();
            selectTaskViewModel.DisplayName = "请选择要删除的制程";
            bool? result = _windowManager.ShowDialog(selectTaskViewModel);
            if (result == true)
            {
                LaserSprayProcedure task = _taskManager.GetTaskByName(selectTaskViewModel.SelectedTaskName).DeepCopy();
                if (MessageBox.Show("确定删除制程 " + selectTaskViewModel.SelectedTaskName + " 吗？(Có chắc về quy trình xóa không?)", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                {
                    if (OriProcedure == task)
                    {
                        // 说明当前制程是要删除的制程
                        OriProcedure = null;
                        IdxOfOriProcedure = -1;
                    }
                    _taskManager.DeleteTask(task);
                }
                MessageBox.Show("已删除制程 " + task.ProcedureName + "！(Quy trình đã xóa)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 保存制程。保存显示在制程页面的制程。如果该制程源于载入，则删除原制程后修改列表中对应制程；否则作为新制程添加到列表并保存文件。
        /// </summary>
        public void SaveProcedure()
        {
            if (CurrProcedure == null)
            {
                MessageBox.Warning("请先新建或加载制程！", "操作提示");
                return;
            }
            if (!CurrProcedure.IsCavityReasonable())
            {
                MessageBox.Warning("当前制程穴位号有冲突，无法保存！", "操作提示");
                return;
            }
            if (MessageBox.Show("确定保存当前制程吗？(Bạn có chắc chắn để lưu quy trình hiện tại?)", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                if (!_taskManager.SaveTask(OriProcedure, CurrProcedure, out string info))
                {
                    MessageBox.Error($"制程保存失败！\n{info}(Quá trình lưu thất bại)", "操作提示");
                }
                else
                {
                    OriProcedure = _taskManager.GetTaskByName(CurrProcedure.ProcedureName);
                    IdxOfOriProcedure = _taskManager.Tasks.IndexOf(OriProcedure);
                    CurrProcedure = OriProcedure.DeepCopy();
                    SelectedVisionPoint = null;
                    SelectedSolderPoint = null;
                    MessageBox.Info(info, "制程变动项");
                    MessageBox.Success($"制程保存成功！\n主界面【重新选择】该制程后，改动才会生效！", "操作提示");
                }
            }
        }

        #endregion

        #region 打开辅助窗口
        /// <summary>
        /// 打开/关闭示教小窗口
        /// </summary>
        public void OpenTeachPanel()
        {
            if (RobotTeachNewViewModel == null)
                return;
            if (!RobotTeachNewViewModel.IsActive)
            {
                var settings = new Dictionary<string, object>
                {
                    { "ResizeMode", ResizeMode.NoResize },
                    { "WindowStartupLocation", WindowStartupLocation.Manual },
                    { "Left", 100 },
                    { "Top", 300 },
                };
                _windowManager.ShowWindow(RobotTeachNewViewModel, null, settings);
            }
            else
            {
                RobotTeachNewViewModel.TryClose();
            }
        }
        #endregion

        #region 视觉点操作

        /// <summary>
        /// 添加视觉点
        /// </summary>
        public void AddVisionPoint()
        {
            if (CurrProcedure == null)
            {
                MessageBox.Show("请先新建或加载制程！(Vui lòng tạo mới hoặc tải chương trình)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                //构建默认焊接点列表
                ObservableCollection<LaserSpraySolderPoint> solderPoints = new ObservableCollection<LaserSpraySolderPoint>();
                for (int nozzleNo = 1; nozzleNo <= 2; nozzleNo++)
                {
                    solderPoints.Add(new LaserSpraySolderPoint()
                    {
                        NozzleNo = nozzleNo,
                        SolderRecipeName = Recipes.Count > 0 ? Recipes[0].RecipeName : "None",
                    });
                }
                //构建新视觉点并添加
                CurrProcedure.VisionPoints.Add(new LaserSprayVisionPoint()
                {
                    CavityNum = CurrProcedure.VisionPoints.Count + 1,
                    VisionPointX = _mGoogol_Component.CurPos[(byte)En_AxisNum.X1],
                    VisionPointY = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1],
                    SolderPoints = solderPoints,
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加视觉点失败！\n{ex}(Lỗi thêm điểm thị giác)", "警告", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除视觉点
        /// </summary>
        public void DeleteVisionPoint()
        {
            if (CurrProcedure == null)
            {
                MessageBox.Show("请先新建或加载制程！(Vui lòng tạo mới hoặc tải chương trình)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SelectedVisionPoint == null)
            {
                MessageBox.Show("请先选中一个拍照点！(Vui lòng chọn một điểm chụp trước.)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show("确定删除该视觉点吗?(Bạn có chắc chắn để loại bỏ điểm thị giác này?)", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                CurrProcedure.VisionPoints.Remove(SelectedVisionPoint);
                SelectedVisionPoint = null;
            }
        }

        /// <summary>
        /// 上移视觉点
        /// </summary>
        public void MoveUpVisionPoint()
        {
            if (CurrProcedure == null)
            {
                MessageBox.Show("请先新建或加载制程！(Vui lòng tạo mới hoặc tải chương trình)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SelectedVisionPoint == null)
            {
                MessageBox.Show("请先选中一个拍照点！(Vui lòng chọn một điểm chụp trước.)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int oldindex = CurrProcedure.VisionPoints.IndexOf(SelectedVisionPoint);
            int newindex = oldindex - 1;
            if (newindex >= 0 && newindex < CurrProcedure.VisionPoints.Count)
            {
                CurrProcedure.VisionPoints.Move(oldindex, newindex);
            }
        }

        /// <summary>
        /// 下移视觉点
        /// </summary>
        public void MoveDownVisionPoint()
        {
            if (CurrProcedure == null)
            {
                MessageBox.Show("请先新建或加载制程！(Vui lòng tạo mới hoặc tải chương trình)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SelectedVisionPoint == null)
            {
                MessageBox.Show("请先选中一个拍照点！(Vui lòng chọn một điểm chụp trước.)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int oldindex = CurrProcedure.VisionPoints.IndexOf(SelectedVisionPoint);
            int newindex = oldindex + 1;
            if (newindex >= 0 && newindex < CurrProcedure.VisionPoints.Count)
            {
                CurrProcedure.VisionPoints.Move(oldindex, newindex);
            }
        }

        /// <summary>
        /// 阵列添加视觉点
        /// </summary>
        public void ArrayVisionPoints()
        {
            if (CurrProcedure == null)
            {
                MessageBox.Show("请先新建或加载制程！(Vui lòng tạo mới hoặc tải chương trình)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var settings = new Dictionary<string, object> { { "ResizeMode", ResizeMode.NoResize } };
            ArrayPointsViewModel arrayPointsViewModel = new ArrayPointsViewModel();
            arrayPointsViewModel.DisplayName = "添加视觉点位阵列";
            arrayPointsViewModel.RowCount = 4;
            arrayPointsViewModel.ColumnCount = 3;
            arrayPointsViewModel.ArrayTypeOfAdd = EnumArrayType.Column;
            arrayPointsViewModel.ArrayTypeOfIdx = EnumArrayType.Row;
            if (_windowManager.ShowDialog(arrayPointsViewModel, null, settings) == true)
            {
                CurrProcedure.VisionPoints.Clear();
                //构建默认贴合点列表
                ObservableCollection<LaserSpraySolderPoint> solderPoints = new ObservableCollection<LaserSpraySolderPoint>();
                for (int nozzleNo = 1; nozzleNo <= 2; nozzleNo++)
                {
                    solderPoints.Add(new LaserSpraySolderPoint()
                    {
                        NozzleNo = nozzleNo,
                        SolderRecipeName = Recipes.Count > 0 ? Recipes[0].RecipeName : "None",
                    });
                }
                //构建新视觉点并添加
                foreach (var pointInfo in arrayPointsViewModel.pointInfoList)
                {
                    CurrProcedure.VisionPoints.Add(new LaserSprayVisionPoint()
                    {
                        CavityNum = pointInfo.idx + 1,
                        VisionPointX = pointInfo.x,
                        VisionPointY = pointInfo.y,
                        SolderPoints = solderPoints.DeepCopy(),
                    });
                }
            }
        }

        /// <summary>
        /// XY设置为当前视觉点坐标
        /// </summary>
        public void GetCurCrdVisionMenuClick()
        {
            if (SelectedVisionPoint == null)
            {
                MessageBox.Show("请先选中一个拍照点！(Vui lòng chọn một điểm chụp trước.)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show("确定使用当前XY作为该拍照点XY吗？(Bạn có chắc chắn sử dụng XY hiện tại làm điểm chụp XY không?)", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                SelectedVisionPoint.VisionPointX = _mGoogol_Component.CurPos[(byte)En_AxisNum.X1];
                SelectedVisionPoint.VisionPointY = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1];
            }
        }

        /// <summary>
        /// 移动到当前视觉点坐标
        /// </summary>
        public async void MoveSelectPointVisionMenuClick()
        {
            if (SelectedVisionPoint == null)
            {
                MessageBox.Show("请先选中一个拍照点！(Vui lòng chọn một điểm chụp trước.)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show("确定移动至该视觉点位吗？(Anh có chắc là mình đang di chuyển tới điểm nhìn đó không?)", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await Task.Run(() =>
                {
                    //false说明有轴在运动
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Show("轴系在运动，等静止再操作！(Trục đang chuyển động. Chờ đến khi dừng lại.)");
                        return;
                    }
                    if (!_mGoogol_Component.GetCurPos())
                    {
                        MessageBox.Show("获取当前坐标失败！(Lỗi lấy tọa độ hiện tại)");
                        return;
                    }
                    if (_mGoogol_Component.CurPos[(byte)En_AxisNum.Y2] > _stepStatus.ParamManager.MotionGoogolParam.SafeY2)
                    {
                        MessageBox.Show("请先将吸嘴轴Y回到安全位置！(Vui lòng đưa trục hút Y trở lại vị trí an toàn trước.)");
                        return;
                    }
                    var visionPos = SelectedVisionPoint.GetPos();
                    if (visionPos != null)
                    {
                        _mGoogol_Component.SetSpeedAll(En_SpeedType.Mid);
                        _mGoogol_Component.MoveAbsoluteX1Y1(visionPos, true, true);
                    }
                });
            }
        }

        /// <summary>
        /// 当前位置视觉定位Mark
        /// </summary>
        public void LocateMarkVisionMenuClick()
        {
            if (MessageBox.Show("确定以当前位置拍照定位上视觉吗？\n请确认光源已开启！(Có chắc là chụp ảnh ở vị trí hiện tại không? Vui lòng xác nhận nguồn sáng đã bật.)", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                string TLTSN = $"TLT_{SelectedVisionPoint.CavityNum}_{DateTime.Now.ToString("yyyyMMdd_HHmmssfff")}";
                if (!_camera_Component.TLT(TLTSN, 0, "Foam", SelectedVisionPoint.CavityNum, "Foam->SIP", SelectedVisionPoint.VisionPointX, SelectedVisionPoint.VisionPointY, 0, out float[] partxya, out string[] data, out float[] xya) || _mGoogol_Component.Exit())
                {
                    MessageBox.Warning("上视觉定位失败");
                }
                else
                {
                    MessageBox.Success("上视觉定位成功");
                }
            }
        }

        #endregion

        #region 贴合点操作

        /// <summary>
        /// 添加贴合点
        /// </summary>
        public void AddSolderPoint()
        {
            if (CurrProcedure == null)
            {
                MessageBox.Show("请先新建或加载制程！(Vui lòng tạo mới hoặc tải chương trình)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SelectedVisionPoint == null)
            {
                MessageBox.Show("请先选中一个拍照点！(Vui lòng chọn một điểm chụp trước.)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SelectedVisionPoint.SolderPoints.Count >= 4)
            {
                MessageBox.Show("一个视觉点最多对应4个贴合吸嘴！(Một điểm thị giác tương ứng với tối đa 4 vòi hút phù hợp)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SelectedVisionPoint.SolderPoints.Add(new LaserSpraySolderPoint()
            {
                NozzleNo = SelectedVisionPoint.SolderPoints.Count + 1,
                SolderRecipeName = Recipes.Count > 0 ? Recipes[0].RecipeName : "None",
            });
        }

        /// <summary>
        /// 删除贴合点
        /// </summary>
        public void DeleteSolderPoint()
        {
            if (CurrProcedure == null)
            {
                MessageBox.Show("请先新建或加载制程！(Vui lòng tạo mới hoặc tải chương trình)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SelectedVisionPoint == null)
            {
                MessageBox.Show("请先选中一个拍照点！(Vui lòng chọn một điểm chụp trước.)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SelectedSolderPoint == null)
            {
                MessageBox.Show("请先选中一个贴合点！(Vui lòng chọn một điểm phù hợp trước)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            SelectedVisionPoint.SolderPoints.Remove(SelectedSolderPoint);
            SelectedSolderPoint = null;
        }

        /// <summary>
        /// 上移贴合点
        /// </summary>
        public void MoveUpSolderPoint()
        {
            if (CurrProcedure == null)
            {
                MessageBox.Show("请先新建或加载制程！(Vui lòng tạo mới hoặc tải chương trình)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SelectedVisionPoint == null)
            {
                MessageBox.Show("请先选中一个拍照点！(Vui lòng chọn một điểm chụp trước.)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SelectedSolderPoint == null)
            {
                MessageBox.Show("请先选中一个贴合点！(Vui lòng chọn một điểm phù hợp trước)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int oldindex = SelectedVisionPoint.SolderPoints.IndexOf(SelectedSolderPoint);
            int newindex = oldindex - 1;
            if (newindex >= 0 && newindex < SelectedVisionPoint.SolderPoints.Count)
            {
                SelectedVisionPoint.SolderPoints.Move(oldindex, newindex);
            }
        }

        /// <summary>
        /// 下移贴合点
        /// </summary>
        public void MoveDownSolderPoint()
        {
            if (CurrProcedure == null)
            {
                MessageBox.Show("请先新建或加载制程！(Vui lòng tạo mới hoặc tải chương trình)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SelectedVisionPoint == null)
            {
                MessageBox.Show("请先选中一个拍照点！(Vui lòng chọn một điểm chụp trước.)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SelectedSolderPoint == null)
            {
                MessageBox.Show("请先选中一个贴合点！(Vui lòng chọn một điểm phù hợp trước)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int oldindex = SelectedVisionPoint.SolderPoints.IndexOf(SelectedSolderPoint);
            int newindex = oldindex + 1;
            if (newindex >= 0 && newindex < SelectedVisionPoint.SolderPoints.Count)
            {
                SelectedVisionPoint.SolderPoints.Move(oldindex, newindex);
            }
        }

        /// <summary>
        /// 阵列添加贴合点
        /// </summary>
        public void ArraySolderPoints()
        {
            if (CurrProcedure != null && SelectedVisionPoint != null)
            {
                var settings = new Dictionary<string, object> { { "ResizeMode", ResizeMode.NoResize } };
                ArrayPointsViewModel arrayPointsViewModel = new ArrayPointsViewModel();
                arrayPointsViewModel.DisplayName = "添加贴合点位阵列";
                arrayPointsViewModel.RowCount = 1;
                arrayPointsViewModel.ColumnCount = 4;
                arrayPointsViewModel.ArrayTypeOfAdd = EnumArrayType.Row;
                arrayPointsViewModel.ArrayTypeOfIdx = EnumArrayType.Row;
                if (_windowManager.ShowDialog(arrayPointsViewModel, null, settings) == true)
                {
                    if (arrayPointsViewModel.RowCount * arrayPointsViewModel.ColumnCount > 4)
                    {
                        MessageBox.Show("一个视觉点最多对应4个贴合吸嘴！(Một điểm thị giác tương ứng với tối đa 4 vòi hút phù hợp)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    SelectedVisionPoint.SolderPoints.Clear();
                    //构建新视觉点并添加
                    foreach (var pointInfo in arrayPointsViewModel.pointInfoList)
                    {
                        SelectedVisionPoint.SolderPoints.Add(new LaserSpraySolderPoint()
                        {
                            NozzleNo = pointInfo.idx + 1,
                            SolderPointX = pointInfo.x,
                            SolderPointY = pointInfo.y,
                            SolderRecipeName = Recipes.Count > 0 ? Recipes[0].RecipeName : "None",
                        });
                    }
                }
            }

        }

        /// <summary>
        /// 选择贴合点对应的配方
        /// </summary>
        public void SelectSolderRecipe()
        {
            List<string> recipeNames = _recipeManager.GetAllRecipeNames();
            SelectSolderRecipeViewModel selectSolderRecipeViewModel = new SelectSolderRecipeViewModel(recipeNames);
            _windowManager.ShowPopup(selectSolderRecipeViewModel);
        }

        /// <summary>
        /// XY设置为当前贴合点坐标
        /// </summary>
        public void SetSolderPointXYAsRealPos()
        {
            if (SelectedSolderPoint == null)
            {
                MessageBox.Show("请先选中一个贴合点！(Vui lòng chọn một điểm phù hợp trước)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show("确定使用当前XY作为该贴合点XY吗？(Bạn có chắc chắn sử dụng XY hiện tại làm XY phù hợp không?)", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                //仅修改这一个穴位的xy
                SelectedSolderPoint.SolderPointX = _mGoogol_Component.CurPos[(byte)En_AxisNum.X2];
                SelectedSolderPoint.SolderPointY = _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2];
            }
        }

        /// <summary>
        /// Z设置为当前贴合点坐标
        /// </summary>
        public void SetSolderPointZAsRealPos()
        {
            if (SelectedVisionPoint == null || SelectedSolderPoint == null)
            {
                MessageBox.Show("请先选中一个贴合点！(Vui lòng chọn một điểm phù hợp trước)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            //仅修改这一个穴位的z
            if (MessageBox.Show("确定使用当前Z作为该贴合点Z吗？(Bạn có chắc chắn sử dụng Z hiện tại làm điểm phù hợp Z không?)", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                SelectedSolderPoint.SolderPointZ = _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2];
            }
        }

        /// <summary>
        /// Z设置为当前贴合点坐标，且根据设置修改其他所有贴合点Z
        /// </summary>
        public void SetAllSolderPointZAsRealPos()
        {
            if (SelectedVisionPoint == null || SelectedSolderPoint == null)
            {
                MessageBox.Show("请先选中一个贴合点！(Vui lòng chọn một điểm phù hợp trước)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            //修改所有吸嘴贴合点的xy
            if (MessageBox.Show("确定使用当前Z作为该贴合点Z，并依据设置的补偿值修改其他贴合点Z吗？(Hãy chắc chắn rằng bạn sử dụng Z hiện tại làm điểm phù hợp Z này và sửa đổi các điểm phù hợp Z khác dựa trên giá trị bồi thường được đặt)", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                //根据当前设置的吸嘴，计算四个吸嘴对应的Z
                float thisZ = _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2];
                int thisNozzleNo = SelectedSolderPoint.NozzleNo;
                float nozzle1Z;
                if (thisNozzleNo == 1)
                {
                    nozzle1Z = thisZ;
                }
                else if (thisNozzleNo == 2)
                {
                    nozzle1Z = thisZ - _paramManager.OtherSettingParam.ZOffset2;
                }
                else if (thisNozzleNo == 3)
                {
                    nozzle1Z = thisZ - _paramManager.OtherSettingParam.ZOffset3;
                }
                else if (thisNozzleNo == 4)
                {
                    nozzle1Z = thisZ - _paramManager.OtherSettingParam.ZOffset4;
                }
                else
                {
                    MessageBox.Show("该贴合点吸嘴编号不为1-4！(Số vòi hút phù hợp này không phải là 1-4)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                float[] nozzleZ =
                {
                    nozzle1Z,
                    nozzle1Z + _paramManager.OtherSettingParam.ZOffset2,
                    nozzle1Z + _paramManager.OtherSettingParam.ZOffset3,
                    nozzle1Z + _paramManager.OtherSettingParam.ZOffset4,
                };
                //遍历所有贴合点，依据点位修改Z
                foreach (var solderPoint in SelectedVisionPoint.SolderPoints)
                {
                    if (solderPoint.NozzleNo >= 1 && solderPoint.NozzleNo <= 4)
                    {
                        solderPoint.SolderPointZ = nozzleZ[solderPoint.NozzleNo - 1];
                    }
                }
            }
        }

        /// <summary>
        /// 移动到当前贴合点坐标
        /// </summary>
        public async void MoveSelectPointPlaceMenuClick()
        {
            MessageBoxResult vr = MessageBox.Show("确定移动选中贴合点位吗?(Hãy chắc chắn để di chuyển chọn vị trí phù hợp)", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (vr == MessageBoxResult.OK)
            {
                await Task.Run(() =>
                {
                    //false说明有轴在运动
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Show("轴系在运动，等静止再操作！(Trục đang chuyển động. Chờ đến khi dừng lại.)");
                        return;
                    }
                    var axis = SelectedSolderPoint?.GetPos();
                    _mGoogol_Component.SetSpeedAll(En_SpeedType.Mid);
                    _mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true);
                    if (!_mGoogol_Component.SafeAvoid( ))
                    {
                        return;
                    }
                    _mGoogol_Component.MoveAbsoluteX2Y2(axis, true, true);
                    _mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, axis[2], true, true);
                });
            }
        }

        #endregion

        #region 配方操作

        /// <summary>
        /// 添加配方
        /// </summary>
        public void AddRecipe()
        {
            LaserSprayRecipe recipe = new LaserSprayRecipe() { RecipeName = DateTime.Now.ToString("MMdd_HHmmss") };
            Recipes.Add(recipe);
            SelectedRecipe = recipe;
        }

        /// <summary>
        /// 删除配方
        /// </summary>
        public void DeleteRecipe()
        {
            if (SelectedRecipe == null)
            {
                MessageBox.Show("请先选中一个配方！(Vui lòng chọn công thức trước)", "操作提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show("确定删除配方吗？(Bạn có chắc chắn về Remove Formula?)", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                Recipes.Remove(SelectedRecipe);
                SelectedRecipe = null;
            }
        }

        /// <summary>
        /// 保存所有配方
        /// </summary>
        public void SaveRecipes()
        {
            if (MessageBox.Show("确定保存配方吗？(Bảo quản công thức)", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                _recipeManager.Recipes.Clear();
                foreach (LaserSprayRecipe recipe in Recipes)
                {
                    _recipeManager.Recipes.Add(recipe);
                }
                _recipeManager.SaveAllRecipes();
            }
        }

        #endregion

        #region 单步执行
        public async void InitStep()
        {
            VisualStep1 = "Visible";
            VisualStep2 = "Hidden";
            VisualStep3 = "Hidden";
            VisualStep4 = "Hidden";
            await Task.Run(() =>
            {
                if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                {
                    MessageBox.Warning("轴系在运动，等静止再操作！");
                    return;
                }
                if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Mid) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"设置轴速失败(Thiết lập tốc độ trục thất bại)", En_Logout_Type.SpotCheck, true);
                    return;
                }
                if (!_mGoogol_Component.SetAllCylindersUpDown(true) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"上抬全部气缸失败(Nâng toàn bộ xi lanh thất bại)", En_Logout_Type.SpotCheck, true);
                    return;
                }
                if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Z2为0失败(Thể thao đến Z2 cho 0 thất bại)", En_Logout_Type.SpotCheck, true);
                    return;
                }
                if (!_mGoogol_Component.MoveAbsoluteX2Y2RAll(new float[4] { 0, 0, 0, 0 }, true, true) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"StationNo2运动到[X2,Y2,R1,R2(0,0,0,0)失败(Chiến dịch StationNo2 thất bại)", En_Logout_Type.SpotCheck, true);
                    return;
                }
                if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { 0, 0 }, true, true) || _mGoogol_Component.Exit())
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"StationNo1运动到[X1,Y1](0,0)失败(Chiến dịch StationNo1 thất bại)", En_Logout_Type.SpotCheck, true);
                    return;
                }
            });
        }
        public async void DoFeederFetchMaterial()
        {
            if (MessageBox.Show("确定步骤1->飞达取料吗(Bước 1 ->Feeder lấy)", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await Task.Run(() =>
                {
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Warning("轴系在运动，等静止再操作！");
                        return;
                    }
                    if (_mGoogol_Component.CurPos[(byte)En_AxisNum.X1] > 1 ||
                        _mGoogol_Component.CurPos[(byte)En_AxisNum.Y1] > 1 ||
                        _mGoogol_Component.CurPos[(byte)En_AxisNum.X2] > 1 ||
                        _mGoogol_Component.CurPos[(byte)En_AxisNum.Y2] > 1 ||
                        _mGoogol_Component.CurPos[(byte)En_AxisNum.Z2] > 1)
                    {
                        MessageBox.Warning("请先将轴移动到安全位置再开始");
                        return;
                    }
                    if (CurrProcedure == null)
                    {
                        MessageBox.Warning("制程为空，请加载制程");
                        return;
                    }
                    if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Mid) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"设置轴速失败(Thiết lập tốc độ trục thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    if (!_mGoogol_Component.SetAllCylindersUpDown(true) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"上抬全部气缸失败(Nâng toàn bộ xi lanh thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Z2为0失败(Thể thao đến Z2 cho 0 thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { 0, 0 }, true, true) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"StationNo1运动到[X1,Y1](0,0)失败(Chiến dịch StationNo1 thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    //等待物料到位
                    if (!_stepStatus.TrigFeederConveyTape(true))
                    {
                        return;
                    }

                    float[] pos = _stepStatus.FeederSingleTapePos(SelNozzleNo, (SelNozzleNo - 1) % 2);
                    /*******新增Feeder拍照*******/
                    FeederId feederId = _cacheParamManager.HomeUiParam.Enable.FeederId;
                    if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2(feederId == FeederId.左飞达 ? _cacheParamManager.manualPositionParam.AxisFeeder1Pos : _cacheParamManager.manualPositionParam.AxisFeeder2Pos, true))
                    {
                        MessageBox.Error($"X2移动到0失败！");
                        return;
                    }
                    if (!_camera_Component.FeederCdd())
                    {
                        MessageBox.Error($"飞达拍照失败！");
                        return;
                    }
                    float[] posxya = _camera_Component.GetPickPos(SelNozzleNo);
                    pos[0] = posxya[0];
                    pos[1] = posxya[1];
                    pos[3] = posxya[2];
                    if (!_mGoogol_Component.MoveAbsoluteX2Y2R((En_AxisNum)(SelNozzleNo + 4), new float[3] { pos[0], pos[1], pos[3] }, true, true) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Feeder Pick XYR坐标失败，Axis[X2,Y2]:{NLogTrace.GetFloatArrayString(pos)}(Chuyển đến Feeder Pick XYR Tọa độ không thành công)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, pos[2], true, false) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Feeder Pick Z坐标失败，Axis[Z2]:{pos[2].ToString("f2")}(Chuyển đến Feeder Pick Z Tọa độ không thành công)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    if (!_mGoogol_Component.SetCylinderUpDown(SelNozzleNo, false) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"气缸{SelNozzleNo}下失败(Mở rộng xi lanh thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    Thread.Sleep(500);
                    if (!_mGoogol_Component.SetVacuum(SelNozzleNo, true, true) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"气缸{SelNozzleNo}吸失败(Xi lanh hút thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    //等待一定时间
                    Thread.Sleep(_paramManager.OtherSettingParam.GetTapeBeforeFeederBreakTime);
                    ////飞达吹气开
                    ////根据x轴当前位置，确定现在取的是左边还是右边。注意点位使用视觉引导，所以误差定在±5
                    //bool left = SelNozzleNo == 1
                    //    ? Math.Abs(_mGoogol_Component.CurPos[2] - _cacheParamManager.manualPositionParam.AxisFeederPickNozzle1LeftPos[0]) < 5
                    //    : Math.Abs(_mGoogol_Component.CurPos[2] - _cacheParamManager.manualPositionParam.AxisFeederPickNozzle3LeftPos[0]) < 5;
                    //if (!_mGoogol_Component.SetFeederVacuum(left, true) || _mGoogol_Component.Exit())
                    //{
                    //    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->设置飞达吹气开失败", En_Logout_Type.Alarm, true);
                    //    return;
                    //}
                    ////等待一定时间
                    //Thread.Sleep(_paramManager.OtherSettingParam.GetTapeFeederBreakTime);
                    ////飞达吹气关
                    //if (!_mGoogol_Component.SetFeederVacuum(left, false) || _mGoogol_Component.Exit())
                    //{
                    //    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"Pick->设置飞达吹气关失败", En_Logout_Type.Alarm, true);
                    //    return;
                    //}
                    if (!_mGoogol_Component.SetCylinderUpDown(SelNozzleNo, true) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"气缸{SelNozzleNo}上失败(Xi lanh tăng thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, false) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Z0坐标失败，Axis[Z2]:0(Chuyển động đến tọa độ Z0 không thành công)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    if (MessageBox.Show("确定进入下一个步骤吗(Chắc chắn để chuyển sang bước tiếp theo)", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        VisualStep1 = "Hidden";
                        VisualStep2 = "Visible";
                        VisualStep3 = "Hidden";
                        VisualStep4 = "Hidden";
                    }
                });
            }
        }
        public async void DoDownCameraLocation()
        {
            if (MessageBox.Show("确定步骤2->下视觉拍照吗(Đảm bảo Bước 2 ->Chụp ảnh trực quan)", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await Task.Run(() =>
                {
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Warning("轴系在运动，等静止再操作！");
                        return;
                    }
                    if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Mid) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"设置轴速失败(Thiết lập tốc độ trục thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    if (!_mGoogol_Component.SetAllCylindersUpDown(true) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"上抬全部气缸失败(Nâng toàn bộ xi lanh thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Z2为0失败(Thể thao đến Z2 cho 0 thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { 0, 0 }, true, true) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"StationNo1运动到[X1,Y1](0,0)失败(Chiến dịch StationNo1 thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    float[] pos;
                    En_AxisNum en_AxisNum;
                    if (SelNozzleNo == 1)
                    {
                        pos = _stepStatus.CacheParamManager.manualPositionParam.AxisDownCamera_No1Pos;
                        en_AxisNum = En_AxisNum.R1;
                    }
                    else
                    {
                        pos = _stepStatus.CacheParamManager.manualPositionParam.AxisDownCamera_No2Pos;
                        en_AxisNum = En_AxisNum.R2;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteX2Y2Z2R(en_AxisNum, pos, true))
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运行到吸嘴下视觉位置失败，Axis[X2,Y2,Z2,R1,R2]:{NLogTrace.GetFloatArrayString(pos)}(Chạy đến vị trí trực quan dưới miệng hút không thành công)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    if (!_mGoogol_Component.SetCylinderUpDown(SelNozzleNo, false) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"气缸{SelNozzleNo}下失败(Thất bại dưới xi lanh)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    Thread.Sleep(500);
                    if (!_camera_Component.DownCdd(SelNozzleNo, pos[0], pos[1], pos[3]))
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"定位{SelNozzleNo.ToString()}号吸嘴上的物料失败，Axis[X2,Y2,Z2,R{SelNozzleNo.ToString()}]:{NLogTrace.GetFloatArrayString(pos)}(Định vị vật liệu trên miệng hút thất bại)", En_Logout_Type.SpotCheck, true);
                        MessageBox.Warning("下视觉定位失败");
                        return;
                    }
                    if (!_mGoogol_Component.SetCylinderUpDown(SelNozzleNo, true) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"气缸{SelNozzleNo}上失败(Xi lanh tăng thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, false) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Z0坐标失败，Axis[Z2]:0(Chuyển động đến tọa độ Z0 không thành công)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    if (MessageBox.Show("确定进入下一个步骤吗(Chắc chắn để chuyển sang bước tiếp theo)", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        VisualStep1 = "Hidden";
                        VisualStep2 = "Hidden";
                        VisualStep3 = "Visible";
                        VisualStep4 = "Hidden";
                    }
                });
            }
        }
        public async void DoUpCameraLocation()
        {
            if (MessageBox.Show("确定步骤3->上视觉拍照吗(Đảm bảo Bước 3 ->Chụp ảnh trực quan)", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await Task.Run(() =>
                {
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Warning("轴系在运动，等静止再操作！");
                        return;
                    }
                    if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Mid) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"设置轴速失败(Thiết lập tốc độ trục thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    if (!_mGoogol_Component.SetAllCylindersUpDown(true) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"上抬全部气缸失败(Nâng toàn bộ xi lanh thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Z2为0失败(Thể thao đến Z2 cho 0 thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    //float[] pos = _stepStatus.CacheParamManager.manualPositionParam.AxisDownCamera_No1Pos;
                    //if (Math.Abs(_mGoogol_Component.CurPos[(byte)En_AxisNum.X2] - pos[0]) > 1
                    //|| Math.Abs(_mGoogol_Component.CurPos[(byte)En_AxisNum.Y2] - pos[1]) > 1)
                    //{
                    //    MessageBox.Warning("吸嘴轴不在下视觉上方");
                    //    return;
                    //}
                    var visionPoint = CurrProcedure.GetVisionPointByCavityNum(SelCavityNo);
                    if (visionPoint == null)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"未在当前制程中找到指定穴位视觉点(Không tìm thấy điểm thị giác huyệt được chỉ định trong quy trình hiện tại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { visionPoint.VisionPointX, visionPoint.VisionPointY }, true, true) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运行到{visionPoint.CavityNum}穴视觉点失败：Axis[X1,Y1]:{NLogTrace.GetFloatArrayString(new float[2] { visionPoint.VisionPointX, visionPoint.VisionPointY })}(Chạy đến điểm nhìn thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    //拍照前延时
                    Thread.Sleep(500);
                    if (!_camera_Component.UpCdd(visionPoint.CavityNum, visionPoint.VisionPointX, visionPoint.VisionPointY, 0))
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"定位贴装的物料的目标位置失败 ：Axis[X1,Y1]:{NLogTrace.GetFloatArrayString(new float[2] { visionPoint.VisionPointX, visionPoint.VisionPointY })}(Xác định vị trí mục tiêu của vật liệu được gắn không thành công)", En_Logout_Type.SpotCheck, true);
                        MessageBox.Warning("上视觉定位失败");
                        return;
                    }
                    //避让
                    if (!_mGoogol_Component.SafeAvoid( ))
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"避让失败(Tránh thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    if (MessageBox.Show("确定进入下一个步骤吗(Chắc chắn để chuyển sang bước tiếp theo)", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        VisualStep1 = "Hidden";
                        VisualStep2 = "Hidden";
                        VisualStep3 = "Hidden";
                        VisualStep4 = "Visible";
                    }
                });
            }
        }
        public async void DoAttach()
        {
            if (MessageBox.Show("确定步骤4->最终贴合吗(Bước 4 ->Final Fit)", "提示信息", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                await Task.Run(() =>
                {
                    if (!_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                    {
                        MessageBox.Warning("轴系在运动，等静止再操作！(Trục đang chuyển động. Chờ đến khi dừng lại.)");
                        return;
                    }
                    if (!_mGoogol_Component.SetSpeedAll(En_SpeedType.Mid) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"设置轴速失败(Thiết lập tốc độ trục thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到Z2为0失败(Thể thao đến Z2 cho 0 thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    //获取视觉点
                    var visionPoint = CurrProcedure.GetVisionPointByCavityNum(SelCavityNo);
                    if (visionPoint == null)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"未在当前制程中找到指定穴位视觉点(Không tìm thấy điểm thị giác huyệt được chỉ định trong quy trình hiện tại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    if (!_camera_Component.GetFitPos(SelNozzleNo, visionPoint.CavityNum, out float[] xya))
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"计算最终贴合坐标失败，穴位号：{visionPoint.CavityNum}(Tính toán tọa độ phù hợp cuối cùng thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    NLogTrace.LogOut(EN_WARN_LEVEL.Sucess, $"计算最终贴合坐标成功XYR:[{xya[0].ToString("f3")},{xya[1].ToString("f3")},{xya[2].ToString("f3")}]，吸嘴编号：{SelNozzleNo.ToString()},穴位号：{visionPoint.CavityNum}", En_Logout_Type.SpotCheck, true);
                    //判断视觉坐标与制程预设坐标差值
                    var solderPoint = visionPoint.GetSolderPointByNozzleNo(SelNozzleNo);
                    if (solderPoint == null)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"未在当前制程中找到指定穴位对应的指定吸嘴基准坐标(Không tìm thấy tọa độ cơ sở miệng hút được chỉ định tương ứng với huyệt được chỉ định trong quy trình hiện tại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    float[] BasePos = solderPoint.GetPos();
                    if (Math.Abs(xya[0] - BasePos[0]) >= 10 || Math.Abs(xya[1] - BasePos[1]) >= 10)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"基准坐标与实际坐标超过10mm，穴位号：{SelCavityNo.ToString()},吸嘴编号：{SelNozzleNo.ToString()}，ActAxis[X2,Y2]:{xya[0].ToString("f2")},{xya[1].ToString("f2")}，BaseAxis[X2,Y2]:{BasePos[0].ToString("f2")},{BasePos[1].ToString("f2")}(Tọa độ cơ sở và tọa độ thực tế hơn 10 mm)", En_Logout_Type.SpotCheck, true);
                        MessageBox.Warning("贴合坐标和预设坐标相差过大(Sự khác biệt quá lớn giữa tọa độ phù hợp và tọa độ đặt trước)");
                        return;
                    }
                    //获取当前R轴坐标
                    if (!_mGoogol_Component.GetCurPos(En_StationNo.StationNo2) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"获取2站当前位置失败，穴位号：{SelCavityNo},吸嘴编号：{SelNozzleNo}(Nhận vị trí hiện tại của 2 trạm không thành công)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    float posR = _mGoogol_Component.CurPos[SelNozzleNo + 4];
                    //如果当前R和目标R相差过大，修改旋转角度
                    while (Math.Abs(posR - xya[2]) > 180)
                    {
                        if (posR > xya[2])
                        {
                            xya[2] += 360;
                        }
                        else
                        {
                            xya[2] -= 360;
                        }
                    }
                    //避让
                    if (!_mGoogol_Component.SafeAvoid())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"避让失败(Tránh thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    //运动到贴合点
                    if (!_mGoogol_Component.MoveAbsoluteX2Y2R((En_AxisNum)(SelNozzleNo + 4), new float[3] { xya[0], xya[1], xya[2] }, true, true) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到贴合点XY位置失败，穴位号：{visionPoint.CavityNum},吸嘴编号：{SelNozzleNo.ToString()}，Axis[X2,Y2]:{xya[0].ToString("f2")},{xya[1].ToString("f2")}(Chuyển động đến vị trí phù hợp XY thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    //获取配方
                    var recipe = _recipeManager.GetRecipe(solderPoint.SolderRecipeName);
                    if (recipe == null)
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"获取贴合配方失败：{visionPoint.CavityNum},吸嘴编号：{SelNozzleNo.ToString()}，Axis[X2,Y2]:{xya[0].ToString("f2")},{xya[1].ToString("f2")}(Nhận công thức phù hợp thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    //运动到贴合一次高度位置
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, solderPoint.SolderPointZ - recipe.Place1stHeight, true, true) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到贴合点一次高度失败，穴位号：{visionPoint.CavityNum},吸嘴编号：{SelNozzleNo.ToString()}，Axis[Z]:{recipe.Place1stHeight.ToString("f2")}(Tập thể dục để phù hợp với một thất bại cao)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    //对应吸嘴气缸下降
                    if (!_mGoogol_Component.SetCylinderUpDown(SelNozzleNo, false) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"打开气缸失败，穴位号：{visionPoint.CavityNum},吸嘴编号：{SelNozzleNo.ToString()}，Axis[X2,Y2]:{xya[0].ToString("f2")},{xya[1].ToString("f2")}(Mở xi lanh thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    //等待气缸下到位一段时间再找压力
                    Thread.Sleep(100);
                    //Z轴降速
                    if (!_mGoogol_Component.SetSpeed(En_AxisNum.Z2, recipe.Place1stHeightSetSpeed) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"设置贴合一次高度速度：{NLogTrace.GetFloatArrayString(recipe.Place1stHeightSetSpeed)} 失败，穴位号：{visionPoint.CavityNum},吸嘴编号：{SelNozzleNo.ToString()}，Axis[R]:{xya[2].ToString("f2")}(Đặt tốc độ độ cao phù hợp)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    //运动到贴合位置，分找压力和不找压力两种情况
                    //realPress表示吸嘴稳定时候的压力值，用于在后面判断压力是否达标
                    float realPress = 0;
                    if (recipe.PosMode)
                    {
                        if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, BasePos[2], false, true, false) || _mGoogol_Component.Exit())
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"开始运动到贴合点失败，穴位号：{visionPoint.CavityNum},吸嘴编号：{SelNozzleNo}，Axis[Z]:{recipe.Place1stHeight.ToString("f2")}(Bắt đầu chuyển động đến điểm phù hợp thất bại)", En_Logout_Type.SpotCheck, true);
                            return;
                        }
                        //找压力模式，需要实时读取压力
                        DateTime startTime = DateTime.Now;
                        //目标压力
                        float targetPress = recipe.Press;
                        //压力提前量，因为发送轴停止指令需要一段时间
                        float beforePress = recipe.BeforePress;
                        //只要到这个压力就立刻发送轴停止指令
                        float stopMovePress = targetPress - beforePress;
                        short pressGet;
                        while (true)
                        {
                            if (_mGoogol_Component.Exit())
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"固高板卡失连(Thẻ cố định bị mất)", En_Logout_Type.SpotCheck, true);
                                return;
                            }
                            //获取当前压力
                            pressGet = _mGoogol_Component.AInput[SelNozzleNo - 1];
                            realPress = _cacheParamManager.PressParam.SglParam[SelNozzleNo - 1].GetCalibedPress(pressGet);
                            //压力达标则停止轴运动，注意发送停止指令后轴还会往下走一段（通信需要时间）
                            if (realPress >= stopMovePress)
                            {
                                _mGoogol_Component.StopAxisMove(En_AxisNum.Z2);
                                break;
                            }
                            //如果规定时间内未找到压力，跳出
                            if ((DateTime.Now - startTime).TotalMilliseconds >= recipe.FindPressTimeout)
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Warn, $"找压力到达超时时间 {recipe.FindPressTimeout} ms(Tìm áp lực đến thời gian chờ)", En_Logout_Type.SpotCheck, true);
                                break;
                            }
                            //如果移动到目标位置还未找到压力，跳出
                            //_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled)为true表示没有轴在运动
                            if (_mGoogol_Component.GetAxisClrSts(En_GetAxisClrSts.Bit10_MoveEnabled))
                            {
                                NLogTrace.LogOut(EN_WARN_LEVEL.Warn, $"未能找到指定压力(Không tìm thấy áp lực quy định)", En_Logout_Type.SpotCheck, true);
                                break;
                            }
                        }
                        //贴合保压
                        Thread.Sleep(recipe.Place1stDelay);
                        //等待一段时间后读到的压力才是稳定的压力，下面再判断压力够不够
                        pressGet = _mGoogol_Component.AInput[SelNozzleNo - 1];
                        realPress = _cacheParamManager.PressParam.SglParam[SelNozzleNo - 1].GetCalibedPress(pressGet);
                        NLogTrace.LogOut(EN_WARN_LEVEL.Info, $"{visionPoint.CavityNum}穴压力 {realPress.ToString("F2")} N", En_Logout_Type.Run, true);
                    }
                    else
                    {
                        //直接下降到固定高度
                        if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, BasePos[2], true, true, false) || _mGoogol_Component.Exit())
                        {
                            NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到贴合点失败，穴位号：{visionPoint.CavityNum},吸嘴编号：{SelNozzleNo}，Axis[Z]:{recipe.Place1stHeight.ToString("f2")}(Chuyển động đến điểm phù hợp thất bại)", En_Logout_Type.SpotCheck, true);
                            return;
                        }
                        //贴合保压
                        Thread.Sleep(recipe.Place1stDelay);
                    }
                    //关闭真空吸 -> 打开真空破 -> 等待 -> 气缸上抬 -> 关闭真空破
                    if (!_stepStatus.SetCloseSucOpenBreakAndCylinderUp(SelNozzleNo) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"关闭真空吸，打开真空破失败，穴位号：{visionPoint.CavityNum},吸嘴编号：{SelNozzleNo.ToString()}，Axis[X2,Y2]:{xya[0].ToString("f2")},{xya[1].ToString("f2")}(Phá vỡ chân không thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    //回归工作速度
                    if (!_mGoogol_Component.SetSpeed(En_AxisNum.Z2, En_SpeedType.Work) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到贴合点Z到位后，设置上抬工作速度失败，穴位号：{visionPoint.CavityNum},吸嘴编号：{SelNozzleNo.ToString()},Axis[Z]:{recipe.Place1stHeight.ToString("f2")}(Thiết lập tốc độ làm việc không thành công)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    //回原点
                    if (!_mGoogol_Component.SetAllCylindersUpDown(true) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"所有气缸上抬失败(Tất cả các xi lanh đều thất bại.)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteSingleAxis(En_AxisNum.Z2, 0, true, true) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"运动到贴合点Z上抬高度：0失败，穴位号：{visionPoint.CavityNum},吸嘴编号：{SelNozzleNo.ToString()},Axis[Z]:{recipe.Place1stHeight.ToString("f2")}(Chuyển động đến điểm phù hợp Z để nâng độ cao thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteX2Y2R((En_AxisNum)(SelNozzleNo + 4), new float[3] { 0, 0, 0 }, true, true) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"StationNo2运动到[X2,Y2,R{SelNozzleNo.ToString()}](0,0)(Chiến dịch StationNo2 thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    if (!_mGoogol_Component.MoveAbsoluteX1Y1(new float[2] { 0, 0 }, true, true) || _mGoogol_Component.Exit())
                    {
                        NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"StationNo1运动到[X1,Y1](0,0)(Chiến dịch StationNo1 thất bại)", En_Logout_Type.SpotCheck, true);
                        return;
                    }
                    MessageBox.Success("贴合完成");
                    VisualStep1 = "Visible";
                    VisualStep2 = "Hidden";
                    VisualStep3 = "Hidden";
                    VisualStep4 = "Hidden";
                });
            }
        }
        #endregion

        public void ClearLogs()
        {
            if (MessageBox.Show("确定清空运行日志吗？", "操作提示", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                RunTimeLogs.ClearLogs();
            }
        }
    }
}
