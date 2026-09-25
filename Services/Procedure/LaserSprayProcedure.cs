using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq.Expressions;
using Caliburn.Micro;
using QA.Business.Manager;

namespace QA.Business.Procedure
{
    public class LaserSprayProcedure : IProcedure, INotifyPropertyChanged
    {
        #region NotifyOfPropertyChange
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyOfPropertyChange(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void NotifyOfPropertyChange<TProperty>(Expression<Func<TProperty>> property)
        {
            MemberExpression member = (MemberExpression)property.Body;
            string propName = member.Member.Name;
            NotifyOfPropertyChange(propName);
        }
        #endregion

        /// <summary>
        /// 制程名称
        /// </summary>
        private string _procedureName = "";
        public string ProcedureName
        {
            get { return _procedureName; }
            set
            {
                _procedureName = value;
                NotifyOfPropertyChange(() => ProcedureName);
            }
        }

        /// <summary>
        /// 制程部分参数，例如行数列数等
        /// </summary>
        private LaserSprayGeneralParam _generalParam = new LaserSprayGeneralParam();
        public LaserSprayGeneralParam GeneralParam
        {
            get => _generalParam;
            set
            {
                _generalParam = value;
                NotifyOfPropertyChange(() => GeneralParam);
            }
        }

        /// <summary>
        /// 制程的视觉点列表
        /// </summary>
        private ObservableCollection<LaserSprayVisionPoint> _visionPoints = new ObservableCollection<LaserSprayVisionPoint>();
        public ObservableCollection<LaserSprayVisionPoint> VisionPoints
        {
            get => _visionPoints;
            set
            {
                _visionPoints = value;
                NotifyOfPropertyChange(() => VisionPoints);
            }
        }

        public LaserSprayVisionPoint GetVisionPointByCavityNum(int cavityNum)
        {
            foreach (var point in VisionPoints)
            {
                if (point.CavityNum == cavityNum)
                {
                    return point;
                }
            }
            return null;
        }

        /// <summary>
        /// 如果没有重复的穴位号，则返回true；否则返回false
        /// </summary>
        /// <returns></returns>
        public bool IsCavityReasonable()
        {
            List<int> cavityList = new List<int>();
            foreach (var point in VisionPoints)
            {
                if (!cavityList.Contains(point.CavityNum))
                {
                    cavityList.Add(point.CavityNum);
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is LaserSprayProcedure))
            {
                return false;
            }
            LaserSprayProcedure p = (LaserSprayProcedure)obj;
            if (p.ProcedureName != ProcedureName
                || !p.GeneralParam.Equals(GeneralParam)
                || p.VisionPoints.Count != VisionPoints.Count)
            {
                return false;
            }
            for (int i = 0; i < VisionPoints.Count; i++)
            {
                if (!p.VisionPoints[i].Equals(VisionPoints[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public class LaserSprayGeneralParam
    {
        /// <summary>
        /// 载具行数
        /// </summary>
        public int CarrierRows { get; set; } = 4;

        /// <summary>
        /// 载具列数
        /// </summary>
        public int CarrierColumns { get; set; } = 3;

        public override bool Equals(object obj)
        {
            if (!(obj is LaserSprayGeneralParam))
            {
                return false;
            }
            LaserSprayGeneralParam p = (LaserSprayGeneralParam)obj;
            if (p.CarrierRows != CarrierRows
                || p.CarrierColumns != CarrierColumns)
            {
                return false;
            }
            return true;
        }
    }

    public class LaserSprayVisionPoint : INotifyPropertyChanged
    {
        #region NotifyOfPropertyChange
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyOfPropertyChange(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void NotifyOfPropertyChange<TProperty>(Expression<Func<TProperty>> property)
        {
            MemberExpression member = (MemberExpression)property.Body;
            string propName = member.Member.Name;
            NotifyOfPropertyChange(propName);
        }
        #endregion

        private ParamManager _paramManager = IoC.Get<ParamManager>();

        /// <summary>
        /// 视觉点启用禁用
        /// </summary>
        private bool _isUsed = true;
        public bool IsUsed
        {
            get => _isUsed;
            set { _isUsed = value; NotifyOfPropertyChange(() => IsUsed); }
        }

        /// <summary>
        /// 穴位号
        /// </summary>
        private int _cavityNum = 0;
        public int CavityNum
        {
            get => _cavityNum;
            set { _cavityNum = value; NotifyOfPropertyChange(() => CavityNum); }
        }

        /// <summary>
        /// 拍照位置X
        /// </summary>
        private float _visionPointX = 0;
        public float VisionPointX
        {
            get => _visionPointX;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                if (value > _paramManager.MotionGoogolParam.X1Max)
                {
                    value = _paramManager.MotionGoogolParam.X1Max;
                }
                _visionPointX = value;
                NotifyOfPropertyChange(() => VisionPointX);
            }
        }

        /// <summary>
        /// 拍照位置Y
        /// </summary>
        private float _visionPointY = 0;
        public float VisionPointY
        {
            get => _visionPointY;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                if (value > _paramManager.MotionGoogolParam.Y1Max)
                {
                    value = _paramManager.MotionGoogolParam.Y1Max;
                }
                _visionPointY = value;
                NotifyOfPropertyChange(() => VisionPointY);
            }
        }

        /// <summary>
        /// 返回该视觉点的XY数组
        /// </summary>
        /// <returns></returns>
        public float[] GetPos()
        {
            return new float[]
            {
                VisionPointX,
                VisionPointY,
             };
        }

        /// <summary>
        /// 视觉点贴合X补偿
        /// </summary>
        private float _visionPointXOffset = 0;
        public float VisionPointXOffset
        {
            get => _visionPointXOffset;
            set
            {
                if (value < -1)
                {
                    value = -1;
                }
                if (value > 1)
                {
                    value = 1;
                }
                _visionPointXOffset = value;
                NotifyOfPropertyChange(() => VisionPointXOffset);
            }
        }

        /// <summary>
        /// 视觉点贴合Y补偿
        /// </summary>
        private float _visionPointYOffset = 0;
        public float VisionPointYOffset
        {
            get => _visionPointYOffset;
            set
            {
                if (value < -1)
                {
                    value = -1;
                }
                if (value > 1)
                {
                    value = 1;
                }
                _visionPointYOffset = value;
                NotifyOfPropertyChange(() => VisionPointYOffset);
            }
        }

        /// <summary>
        /// 指示该穴是否已经过上视觉拍照处理
        /// </summary>
        public bool isUpCamFinished = false;

        /// <summary>
        /// 该视觉点对应的贴合点列表
        /// </summary>
        private ObservableCollection<LaserSpraySolderPoint> _solderPoints = new ObservableCollection<LaserSpraySolderPoint>();
        public ObservableCollection<LaserSpraySolderPoint> SolderPoints
        {
            get => _solderPoints;
            set { _solderPoints = value; NotifyOfPropertyChange(() => SolderPoints); }
        }

        public LaserSpraySolderPoint GetSolderPointByNozzleNo(int nozzleNo)
        {
            foreach (var point in SolderPoints)
            {
                if (point.NozzleNo == nozzleNo)
                {
                    return point;
                }
            }
            return null;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is LaserSprayVisionPoint))
            {
                return false;
            }
            LaserSprayVisionPoint p = (LaserSprayVisionPoint)obj;
            if (p.IsUsed != IsUsed
                || p.CavityNum != CavityNum
                || p.VisionPointX != VisionPointX
                || p.VisionPointY != VisionPointY
                || p.VisionPointXOffset != VisionPointXOffset
                || p.VisionPointYOffset != VisionPointYOffset
                || p.SolderPoints.Count != SolderPoints.Count)
            {
                return false;
            }
            for (int i = 0; i < SolderPoints.Count; i++)
            {
                if (!p.SolderPoints[i].Equals(SolderPoints[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public class LaserSpraySolderPoint : INotifyPropertyChanged
    {
        #region NotifyOfPropertyChange
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyOfPropertyChange(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void NotifyOfPropertyChange<TProperty>(Expression<Func<TProperty>> property)
        {
            MemberExpression member = (MemberExpression)property.Body;
            string propName = member.Member.Name;
            NotifyOfPropertyChange(propName);
        }
        #endregion

        private ParamManager _paramManager = IoC.Get<ParamManager>();

        /// <summary>
        /// 贴合点启用禁用
        /// </summary>
        private bool _isUsed = true;
        public bool IsUsed
        {
            get => _isUsed;
            set { _isUsed = value; NotifyOfPropertyChange(() => IsUsed); }
        }

        /// <summary>
        /// 吸嘴号
        /// </summary>
        private int _nozzleNo = 0;
        public int NozzleNo
        {
            get => _nozzleNo;
            set { _nozzleNo = value; NotifyOfPropertyChange(() => NozzleNo); }
        }

        /// <summary>
        /// 配方名称
        /// </summary>
        private string _solderRecipeName = "";
        public string SolderRecipeName
        {
            get => _solderRecipeName;
            set { _solderRecipeName = value; NotifyOfPropertyChange(() => SolderRecipeName); }
        }

        /// <summary>
        /// 贴合位置X
        /// </summary>
        private float _solderPointX = 0;
        public float SolderPointX
        {
            get => _solderPointX;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                if (value > _paramManager.MotionGoogolParam.X2Max)
                {
                    value = _paramManager.MotionGoogolParam.X2Max;
                }
                _solderPointX = value;
                NotifyOfPropertyChange(() => SolderPointX);
            }
        }

        /// <summary>
        /// 贴合位置Y
        /// </summary>
        private float _solderPointY = 0;
        public float SolderPointY
        {
            get => _solderPointY;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                if (value > _paramManager.MotionGoogolParam.Y2Max)
                {
                    value = _paramManager.MotionGoogolParam.Y2Max;
                }
                _solderPointY = value;
                NotifyOfPropertyChange(() => SolderPointY);
            }
        }

        /// <summary>
        /// 贴合位置Z
        /// </summary>
        private float _solderPointZ = 0;
        public float SolderPointZ
        {
            get => _solderPointZ;
            set
            {
                if (value < 0)
                {
                    value = 0;
                }
                if (value > _paramManager.MotionGoogolParam.Z2Max)
                {
                    value = _paramManager.MotionGoogolParam.Z2Max;
                }
                _solderPointZ = value;
                NotifyOfPropertyChange(() => SolderPointZ);
            }
        }

        /// <summary>
        /// 返回该贴合点的XYZ数组
        /// </summary>
        /// <returns></returns>
        public float[] GetPos()
        {
            return new float[]
            {
                SolderPointX,
                SolderPointY,
                SolderPointZ,
            };
        }

        /// <summary>
        /// 贴合点贴合X补偿
        /// </summary>
        private float _solderPointXOffset = 0;
        public float SolderPointXOffset
        {
            get => _solderPointXOffset;
            set
            {
                if (value < -1)
                {
                    value = -1;
                }
                if (value > 1)
                {
                    value = 1;
                }
                _solderPointXOffset = value;
                NotifyOfPropertyChange(() => SolderPointXOffset);
            }
        }

        /// <summary>
        /// 贴合点贴合Y补偿
        /// </summary>
        private float _solderPointYOffset = 0;
        public float SolderPointYOffset
        {
            get => _solderPointYOffset;
            set
            {
                if (value < -1)
                {
                    value = -1;
                }
                if (value > 1)
                {
                    value = 1;
                }
                _solderPointYOffset = value;
                NotifyOfPropertyChange(() => SolderPointYOffset);
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is LaserSpraySolderPoint))
            {
                return false;
            }
            LaserSpraySolderPoint p = (LaserSpraySolderPoint)obj;
            if (p.IsUsed != IsUsed
                || p.NozzleNo != NozzleNo
                || p.SolderRecipeName != SolderRecipeName
                || p.SolderPointX != SolderPointX
                || p.SolderPointY != SolderPointY
                || p.SolderPointZ != SolderPointZ
                || p.SolderPointXOffset != SolderPointXOffset
                || p.SolderPointYOffset != SolderPointYOffset)
            {
                return false;
            }
            return true;
        }
    }
}
