using System;
using System.ComponentModel;
using System.Linq.Expressions;
using QA.Business.Define;
using QA_Infrastructure;

namespace QA.Business.CacheParam
{
    [SaveParam(FileType.JSON)]
    [Serializable]
    public class WorkPointParam
    {
        //Tray盘物料点位（大料盘）
        public IncomePlatePointParam[] IncomePlatePoints { get; set; } = new IncomePlatePointParam[SysConfig.IncomePlatePointCount];

        //折弯工站-NG料盘点位
        public NGPlatePointParam[] BendStationNGPlatePoints { get; set; } = new NGPlatePointParam[SysConfig.NgPlatePointCount];

        //折弯工站-翻转机构点位
        public UpenderDevicePointParam[] BendStationUpenderPoints { get; set; } = new UpenderDevicePointParam[SysConfig.SuctionNumPerHead];

        //折弯工站下相机点位
        public DownCameraPointParam[] BendStationDownCameraPoints { get; set; } = new DownCameraPointParam[SysConfig.SuctionNumPerHead];

        //折弯工站-折弯机构点位 把料放到折弯机构上
        public BendDevicePointParam[] BendStationBendDevicePoints { get; set; } = new BendDevicePointParam[SysConfig.SuctionNumPerHead];


        //贴合工站-折弯机构点位 从折弯机构上取料
        public BendDevicePointParam[] AttachStationBendDevicePoints { get; set; } = new BendDevicePointParam[SysConfig.SuctionNumPerHead];

        //贴合工站-下相机点位
        public DownCameraPointParam[] AttachStationDownCameraPoints { get; set; } = new DownCameraPointParam[SysConfig.SuctionNumPerHead];

        //贴合工站-NG料盘点位
        public NGPlatePointParam[] AttachStationNGPlatePoints { get; set; } = new NGPlatePointParam[SysConfig.NgPlatePointCount];


        //折弯工站吸嘴偏移
        public SuctionOffset[] BendSuctionOffsets { get; set; } = new SuctionOffset[SysConfig.SuctionNumPerHead];

        //贴合工站吸嘴偏移
        public SuctionOffset[] AttachSuctionOffsets { get; set; } = new SuctionOffset[SysConfig.SuctionNumPerHead];

        public WorkPointParam()
        {
            for (int i = 0; i < SysConfig.IncomePlatePointCount; i++)
            {
                IncomePlatePoints[i] = new IncomePlatePointParam();
            }
            for (int i = 0; i < SysConfig.NgPlatePointCount; i++)
            {
                BendStationNGPlatePoints[i] = new NGPlatePointParam();
                AttachStationNGPlatePoints[i] = new NGPlatePointParam();
            }
            for (int i = 0; i < SysConfig.SuctionNumPerHead; i++)
            {
                BendStationDownCameraPoints[i] = new DownCameraPointParam();
                AttachStationDownCameraPoints[i] = new DownCameraPointParam();
                BendStationUpenderPoints[i] = new UpenderDevicePointParam();
                BendStationBendDevicePoints[i] = new BendDevicePointParam();
                AttachStationBendDevicePoints[i] = new BendDevicePointParam();
                BendSuctionOffsets[i] = new SuctionOffset();
                AttachSuctionOffsets[i] = new SuctionOffset();
            }

        }

    }

    /// <summary>
    /// Tray盘点位
    /// </summary>
    public class IncomePlatePointParam : INotifyPropertyChanged
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

        public float[] vision_xyzr = new float[4];        //上相机视觉点位XYZR
        public float UpVision_X
        {
            get { return vision_xyzr[0]; }
            set { vision_xyzr[0] = value; NotifyOfPropertyChange(() => UpVision_X); }
        }
        public float UpVision_Y
        {
            get { return vision_xyzr[1]; }
            set { vision_xyzr[1] = value; NotifyOfPropertyChange(() => UpVision_Y); }
        }
        public float UpVision_Z
        {
            get { return vision_xyzr[2]; }
            set { vision_xyzr[2] = value; NotifyOfPropertyChange(() => UpVision_Z); }
        }
        public float UpVision_R
        {
            get { return vision_xyzr[3]; }
            set { vision_xyzr[3] = value; NotifyOfPropertyChange(() => UpVision_R); }
        }

        public float[] defsuck_xyzr = new float[4];         //关闭视觉时，默认吸取点位XYZR
        public float DefSuck_X
        {
            get { return defsuck_xyzr[0]; }
            set { defsuck_xyzr[0] = value; NotifyOfPropertyChange(() => DefSuck_X); }
        }
        public float DefSuck_Y
        {
            get { return defsuck_xyzr[1]; }
            set { defsuck_xyzr[1] = value; NotifyOfPropertyChange(() => DefSuck_Y); }
        }
        public float DefSuck_Z
        {
            get { return defsuck_xyzr[2]; }
            set { defsuck_xyzr[2] = value; NotifyOfPropertyChange(() => DefSuck_Z); }
        }
        public float DefSuck_R
        {
            get { return defsuck_xyzr[3]; }
            set { defsuck_xyzr[3] = value; NotifyOfPropertyChange(() => DefSuck_R); }
        }
    }

    /// <summary>
    /// 下相机点位
    /// </summary>
    public class DownCameraPointParam : INotifyPropertyChanged
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

        public float[] xyzr = new float[4];
        public float X
        {
            get { return xyzr[0]; }
            set { xyzr[0] = value; NotifyOfPropertyChange(() => X); }
        }
        public float Y
        {
            get { return xyzr[1]; }
            set { xyzr[1] = value; NotifyOfPropertyChange(() => Y); }
        }
        public float Z
        {
            get { return xyzr[2]; }
            set { xyzr[2] = value; NotifyOfPropertyChange(() => Z); }
        }
        public float R
        {
            get { return xyzr[3]; }
            set { xyzr[3] = value; NotifyOfPropertyChange(() => R); }
        }

    }

    /// <summary>
    /// NG料盘点位
    /// </summary>
    public class NGPlatePointParam : INotifyPropertyChanged
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

        public float[] xyzr = new float[4];
        public float X
        {
            get { return xyzr[0]; }
            set { xyzr[0] = value; NotifyOfPropertyChange(() => X); }
        }
        public float Y
        {
            get { return xyzr[1]; }
            set { xyzr[1] = value; NotifyOfPropertyChange(() => Y); }
        }
        public float Z
        {
            get { return xyzr[2]; }
            set { xyzr[2] = value; NotifyOfPropertyChange(() => Z); }
        }
        public float R
        {
            get { return xyzr[3]; }
            set { xyzr[3] = value; NotifyOfPropertyChange(() => R); }
        }

    }

    /// <summary>
    /// 翻转机构点位
    /// </summary>
    public class UpenderDevicePointParam : INotifyPropertyChanged
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

        public float[] place_xyzr = new float[4];   //翻转机构放料点位
        public float Place_X
        {
            get { return place_xyzr[0]; }
            set { place_xyzr[0] = value; NotifyOfPropertyChange(() => Place_X); }
        }
        public float Place_Y
        {
            get { return place_xyzr[1]; }
            set { place_xyzr[1] = value; NotifyOfPropertyChange(() => Place_Y); }
        }
        public float Place_Z
        {
            get { return place_xyzr[2]; }
            set { place_xyzr[2] = value; NotifyOfPropertyChange(() => Place_Z); }
        }
        public float Place_R
        {
            get { return place_xyzr[3]; }
            set { place_xyzr[3] = value; NotifyOfPropertyChange(() => Place_R); }
        }

        public float[] pick_xyzr = new float[4];    //翻转机构取料点位
        public float Pick_X
        {
            get { return pick_xyzr[0]; }
            set { pick_xyzr[0] = value; NotifyOfPropertyChange(() => Pick_X); }
        }
        public float Pick_Y
        {
            get { return pick_xyzr[1]; }
            set { pick_xyzr[1] = value; NotifyOfPropertyChange(() => Pick_Y); }
        }
        public float Pick_Z
        {
            get { return pick_xyzr[2]; }
            set { pick_xyzr[2] = value; NotifyOfPropertyChange(() => Pick_Z); }
        }
        public float Pick_R
        {
            get { return pick_xyzr[3]; }
            set { pick_xyzr[3] = value; NotifyOfPropertyChange(() => Pick_R); }
        }
    }

    /// <summary>
    /// 折弯机构点位
    /// </summary>
    public class BendDevicePointParam : INotifyPropertyChanged
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

        public float[] vision_xyzr = new float[4];    //上相机视觉点位XYZR
        public float Vision_X
        {
            get { return vision_xyzr[0]; }
            set { vision_xyzr[0] = value; NotifyOfPropertyChange(() => Vision_X); }
        }
        public float Vision_Y
        {
            get { return vision_xyzr[1]; }
            set { vision_xyzr[1] = value; NotifyOfPropertyChange(() => Vision_Y); }
        }
        public float Vision_Z
        {
            get { return vision_xyzr[2]; }
            set { vision_xyzr[2] = value; NotifyOfPropertyChange(() => Vision_Z); }
        }
        public float Vision_R
        {
            get { return vision_xyzr[3]; }
            set { vision_xyzr[3] = value; NotifyOfPropertyChange(() => Vision_R); }
        }

        public float[] pick_xyzr = new float[4];        //不启用视觉时，默认取料点位XYZR
        public float Pick_X
        {
            get { return pick_xyzr[0]; }
            set { pick_xyzr[0] = value; NotifyOfPropertyChange(() => Pick_X); }
        }
        public float Pick_Y
        {
            get { return pick_xyzr[1]; }
            set { pick_xyzr[1] = value; NotifyOfPropertyChange(() => Pick_Y); }
        }
        public float Pick_Z
        {
            get { return pick_xyzr[2]; }
            set { pick_xyzr[2] = value; NotifyOfPropertyChange(() => Pick_Z); }
        }
        public float Pick_R
        {
            get { return pick_xyzr[2]; }
            set { pick_xyzr[2] = value; NotifyOfPropertyChange(() => Pick_R); }
        }

        public float[] place_xyzr = new float[4];       //不启用视觉时，默认放置点位XYZR
        public float Place_X
        {
            get { return place_xyzr[0]; }
            set { place_xyzr[0] = value; NotifyOfPropertyChange(() => Place_X); }
        }
        public float Place_Y
        {
            get { return place_xyzr[1]; }
            set { place_xyzr[1] = value; NotifyOfPropertyChange(() => Place_Y); }
        }
        public float Place_Z
        {
            get { return place_xyzr[2]; }
            set { place_xyzr[2] = value; NotifyOfPropertyChange(() => Place_Z); }
        }
        public float Place_R
        {
            get { return place_xyzr[3]; }
            set { place_xyzr[3] = value; NotifyOfPropertyChange(() => Place_R); }
        }
    }

    /// <summary>
    /// 吸嘴偏移量
    /// </summary>
    public class SuctionOffset : INotifyPropertyChanged
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

        public float[] offset_xy = new float[2];
        public float Offset_X
        {
            get { return offset_xy[0]; }
            set { offset_xy[0] = value; NotifyOfPropertyChange(() => Offset_X); }
        }
        public float Offset_Y
        {
            get { return offset_xy[1]; }
            set { offset_xy[1] = value; NotifyOfPropertyChange(() => Offset_Y); }
        }

    }

}
