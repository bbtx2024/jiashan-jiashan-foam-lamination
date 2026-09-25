using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace QA.Business.Procedure
{
    /// <summary>
    /// 贴合制程
    /// </summary>
    public class AttachingProcedure : IProcedure, INotifyPropertyChanged
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

        private string _procedureName = "";                                         //制程名称
        public string ProcedureName
        {
            get { return _procedureName; }
            set { _procedureName = value; ChangeProperty(() => ProcedureName); }
        }

        private ObservableCollection<ProcedureVisionPoint> _visionPoints = new ObservableCollection<ProcedureVisionPoint>();    //视觉点位
        public ObservableCollection<ProcedureVisionPoint> VisionPoints
        {
            get { return _visionPoints; }
            set { _visionPoints = value; ChangeProperty(() => VisionPoints); }
        }

        private PorcedureGeneralParam _generalParam = new PorcedureGeneralParam();      //制程通用参数
        public PorcedureGeneralParam GeneralParam
        {
            get { return _generalParam; }
            set { _generalParam = value; ChangeProperty(() => GeneralParam); }
        }

    }

    /// <summary>
    /// 贴合制程通用参数
    /// </summary>
    public class PorcedureGeneralParam
    {
        [Category("1.载具信息"), DisplayName("载具行数")]
        public int CarrierRows { get; set; } = 1;

        [Category("1.载具信息"), DisplayName("载具列数")]
        public int CarrierColumns { get; set; } = 1;
 

        [Category("1.制程参数 折弯工站"), DisplayName("折弯工站-料盘取料后上抬距离(mm)")]
        public float BendStation_LiftUpAfterPickIncome { get; set; }

        [Category("1.制程参数 折弯工站"), DisplayName("折弯工站-放置到折弯机构后上抬距离(mm)")]
        public float BendStation_LiftUpAfterPlaceBend { get; set; }

        [Category("1.制程参数 贴合工站"), DisplayName("贴合工站-折弯机构取料后上抬距离(mm)")]
        public float AttachStation_LiftUpAfterPickBend { get; set; }

        [Category("1.制程参数 贴合工站"), DisplayName("贴合工站-贴装到产品后上抬距离(mm)")]
        public float AttachStation_LiftUpAfterAttachProduct { get; set; }

        [Category("1.制程参数"), DisplayName("折弯工站-吸取物料高度(mm)")]
        public float SuctionHeightBendStation { get; set; }

        [Category("1.制程参数"), DisplayName("贴合工站-吸取物料高度(mm)")]
        public float SuctionHeightAttachStation { get; set; }

    }

    /// <summary>
    /// 贴合制程视觉点位
    /// </summary>
    public class ProcedureVisionPoint : INotifyPropertyChanged
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

        private bool _isUsed = true;                    //是否启用
        public bool IsUsed
        {
            get { return _isUsed; }
            set { _isUsed = value; ChangeProperty(() => IsUsed); }
        }

        private string _pointName = "";                 //视觉点名称
        public string PointName
        {
            get { return _pointName; }
            set { _pointName = value; ChangeProperty(() => PointName); }
        }

        private int _trayNum;                           //穴位号
        public int TrayNum
        {
            get { return _trayNum; }
            set { _trayNum = value; ChangeProperty(() => TrayNum); }
        }

        private float[] _visionPos = new float[4];      //视觉点位XYZR
        public float[] VisionPos                       
        {
            get { return _visionPos; }
            set { _visionPos = value; ChangeProperty(() => VisionPos); }
        }

        private string _visionTemplateName;             //视觉模板名
        public string VisionTemplateName                
        {
            get { return _visionTemplateName; }
            set { _visionTemplateName = value; ChangeProperty(() => VisionTemplateName); }
        }

        private ObservableCollection<ProcedureAttachPoint> _attachPoints = new ObservableCollection<ProcedureAttachPoint>();  //每个视觉点对应的工作点位
        public ObservableCollection<ProcedureAttachPoint> AttachPoints
        {
            get { return _attachPoints; }
            set { _attachPoints = value; ChangeProperty(() => AttachPoints); }
        }

    }

    /// <summary>
    /// 每个视觉点位下的贴合点位
    /// </summary>
    public class ProcedureAttachPoint : INotifyPropertyChanged
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

        private bool _isUsed = true;                    //是否启用
        public bool IsUsed
        {
            get { return _isUsed; }
            set { _isUsed = value; ChangeProperty(() => IsUsed); }
        }

        private string _pointName = "";                 //贴合点名称
        public string PointName
        {
            get { return _pointName; }
            set { _pointName = value; ChangeProperty(() => PointName); }
        }

        private float[] _attachPos = new float[4];      //贴合点位XYZR
        public float[] AttachPos
        {
            get { return _attachPos; }
            set { _attachPos = value; ChangeProperty(() => AttachPos); }
        }

        private string _attachRecipeName;               //贴合配方名
        public string AttachRecipeName
        {
            get { return _attachRecipeName; }
            set { _attachRecipeName = value; ChangeProperty(() => AttachRecipeName); }
        }

    }

}
