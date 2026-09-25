using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA.Business.Message;
using QA.Business.Procedure;
using QA.Business.Station;
using QA.Business.Steps;
using QA.Pages.Models;

namespace QA.Pages.OtherViews.ViewModels
{
    [Export("CarrierInfoPanelViewModel")]
    public class CarrierInfoPanelViewModel : Screen, IHandle<CarrierInfoPanelMessage>
    {
        #region Field

        private IWindowManager _windowManager;
        private IEventAggregator _eventAggregator;
        private TaskManager _taskManager = null;
        private IBaseBiz _baseBiz;
        private StepStatus _stepStatus;

        #endregion

        #region Property

        private int _carrCols = 3;      //载具列数
        public int CarrCols
        {
            get { return _carrCols; }
            set { _carrCols = value; NotifyOfPropertyChange(() => CarrCols); }
        }

        private int _carrRows = 4;      //载具行数
        public int CarrRows
        {
            get { return _carrRows; }
            set { _carrRows = value; NotifyOfPropertyChange(() => CarrRows); }
        }

        public string ProcedureName { get; set; } = ""; //制程名称

        public ObservableCollection<TrayInfoModel> TrayInfos { get; set; } = new ObservableCollection<TrayInfoModel>(); //穴位信息集合

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

        #endregion

        #region Constructor

        public CarrierInfoPanelViewModel()
        {
            _windowManager = IoC.Get<IWindowManager>();
            _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Subscribe(this);
            _taskManager = IoC.Get<TaskManager>();
            _baseBiz = IoC.Get<IBaseBiz>();
            _stepStatus = IoC.Get<StepStatus>();
        }

        #endregion

        public void Handle(List<IComponent> message)
        {
            EnableButtons = !(_baseBiz as BaseBiz).AutoRun;
        }

        public void Handle(CarrierInfoPanelMessage message)
        {
            SetState(message.Cavity, message.Status);
        }

        /// <summary>
        /// 设置载具行列数
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="cols"></param>
        public void SetCarrierArray(int rows, int cols)
        {
            CarrRows = rows;
            CarrCols = cols;
        }

        /// <summary>
        /// 设置制程名称
        /// </summary>
        /// <param name="name"></param>
        public void SetProcedureName(string name)
        {
            ProcedureName = name;
        }

        /// <summary>
        /// 设置所有Tray信息集合
        /// </summary>
        public void SetTrayInfos(List<TrayInfoModel> taryInfos)
        {
            TrayInfos.Clear();
            foreach (var item in taryInfos)
            {
                TrayInfos.Add(item);
            }
        }

        /// <summary>
        /// 获取载具上某个穴位号对应穴位的信息
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public TrayInfoModel GetTrayInfoByCavity(int cavity)
        {
            foreach (TrayInfoModel model in TrayInfos)
            {
                if (model.CavityNum == cavity)
                {
                    return model;
                }
            }
            return null;
        }

        /// <summary>
        /// 修改主界面穴位显示状态
        /// </summary>
        /// <param name="cavity"></param>
        /// <param name="status"></param>
        public void SetState(int cavity, EN_TrayStatus status)
        {
            TrayInfoModel model = GetTrayInfoByCavity(cavity);
            if (model == null)
            {
                return;
            }
            if (model.IsUsed)
            {
                model.Status = status;
            }
            else
            {
                model.Status = EN_TrayStatus.Unuse;
            }
        }

        /// <summary>
        /// 启用禁用某个点位
        /// </summary>
        /// <param name="isUsed"></param>
        public void ChangeUsed(object sender, RoutedEventArgs e, bool isUsed)
        {
            CheckBox chkbox = sender as CheckBox;
            if (chkbox == null) return;
            TrayInfoModel model = chkbox.DataContext as TrayInfoModel;
            if (model == null) return;
            //修改并保存制程
            model.IsUsed = isUsed;
            LaserSprayProcedure oldtask = _taskManager.GetTaskByName(ProcedureName);
            LaserSprayProcedure newTask = _stepStatus.CurrentProcedure;
            foreach (var x in newTask.VisionPoints)
            {
                if (x.CavityNum == model.CavityNum)
                {
                    x.IsUsed = isUsed;
                    break;
                }
            }
            _taskManager.SaveTask(oldtask, newTask, out string info);
        }
    }
}
