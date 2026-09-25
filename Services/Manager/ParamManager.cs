using System;
using System.Collections.Generic;
using System.IO;
using Caliburn.Micro;
using QA.Business.Component.Camera;
using QA.Business.Component.HIVE;
using QA.Business.Component.LaserHeightSensor;
using QA.Business.Component.MES;
using QA.Business.Component.Motion.Googol;
using QA.Business.Component.OtherSetting;
using QA.Business.Component.PDCA;
using QA.Business.Component.PLC;
using QA.Business.Component.Scanner;
using QA.Business.Interfaces;
using QA.Business.Station;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;

namespace QA.Business.Manager
{
    [SaveParam(FileType.JSON)]
    [Serializable]
    public class ParamManager
    {
        #region Field
        //private string _paramDirectory = AppDomain.CurrentDomain.BaseDirectory + @"Param";
        private string _paramDirectory = @"D:\QKProject\Setting\Config\" + @"Param";

        #endregion

        #region Property
        private PLCParam _PLCParam;
        public PLCParam PLCParam
        {
            get => _PLCParam;
            set => _PLCParam = value;
        }
        private MotionGoogolParam _MotionGoogolParam;
        public MotionGoogolParam MotionGoogolParam
        {
            get => _MotionGoogolParam;
            set => _MotionGoogolParam = value;
        }
        private ScannerParam _ScannerParam;
        public ScannerParam ScannerParam
        {
            get => _ScannerParam;
            set => _ScannerParam = value;
        }
        private CameraParam _CameraParam;
        public CameraParam CameraParam
        {
            get => _CameraParam;
            set => _CameraParam = value;
        }
        //private LaserHeightSensorParam _LaserHeightSensorParam;
        //public LaserHeightSensorParam LaserHeightSensorParam
        //{
        //    get => _LaserHeightSensorParam;
        //    set => _LaserHeightSensorParam = value;
        //}
        private MESParam _MESParam;
        public MESParam MESParam
        {
            get => _MESParam;
            set => _MESParam = value;
        }
        //private PDCAParam _PDCAParam;
        //public PDCAParam PDCAParam
        //{
        //    get => _PDCAParam;
        //    set => _PDCAParam = value;
        //}
        private HiveParam _HiveParam;
        public HiveParam HiveParam
        {
            get => _HiveParam;
            set => _HiveParam = value;
        }
        private OtherSettingParam _OtherSettingParam;
        public OtherSettingParam OtherSettingParam
        {
            get => _OtherSettingParam;
            set => _OtherSettingParam = value;
        }
        #endregion

        #region Constructor
        public ParamManager()
        {
            LoadParams();
        }
        #endregion

        #region Method
        public void LoadParams()
        {
            try
            {
                if (!Directory.Exists(_paramDirectory))
                {
                    Directory.CreateDirectory(_paramDirectory);
                }
                LoadParam(ref _PLCParam);
                LoadParam(ref _MotionGoogolParam);
                LoadParam(ref _ScannerParam);
                LoadParam(ref _CameraParam);
                //LoadParam(ref _LaserHeightSensorParam);
                LoadParam(ref _MESParam);
                //LoadParam(ref _PDCAParam);
                LoadParam(ref _HiveParam);
                LoadParam(ref _OtherSettingParam);
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"加载参数异常:{ex}", En_Logout_Type.Exception, true);
            }
        }

        public void SaveParams()
        {
            try
            {
                if (!Directory.Exists(_paramDirectory))
                {
                    Directory.CreateDirectory(_paramDirectory);
                }
                SaveParam(PLCParam, false);
                SaveParam(MotionGoogolParam, false);
                SaveParam(ScannerParam, false);
                SaveParam(CameraParam, false);
                //SaveParam(LaserHeightSensorParam, false);
                SaveParam(MESParam, false);
                //SaveParam(PDCAParam, false);
                SaveParam(HiveParam, false);
                SaveParam(OtherSettingParam, false);
                var _baseBiz = (BaseBiz)IoC.Get<IBaseBiz>();
                _baseBiz.InitialReset();
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"保存参数异常:{ex}", En_Logout_Type.Exception, true);
            }
        }

        private string GetPath<P>(P p)
        {
            string fileName = typeof(P).Name.TrimEnd('[', ']');
            return Path.Combine(_paramDirectory, fileName);
        }

        public List<string> GetPaths()
        {
            return new List<string>
            {
                GetPath(_PLCParam),
                GetPath(_MotionGoogolParam),
                GetPath(_ScannerParam),
                GetPath(_CameraParam),
                //GetPath(_LaserHeightSensorParam),
                GetPath(_MESParam),
                //GetPath(_PDCAParam),
                GetPath(_HiveParam),
                GetPath(_OtherSettingParam),
            };
        }

        public void LoadParam<P>(ref P param)
        {
            string filePath = GetPath(param);
            if (File.Exists(filePath))
            {
                try
                {
                    param = SaveParamAttribute.Load<ParamManager, P>(filePath);
                }
                catch (Exception e)
                {
                    NLogTrace.LogOut(EN_WARN_LEVEL.Error, $"加载{filePath.Substring(filePath.LastIndexOf('/'))}参数异常:{e}", En_Logout_Type.Exception, true);
                }
            }
            else
            {
                param = IoC.Get<P>();
            }
        }

        public void SaveParam<P>(P param, bool freshComponentParams = true)
        {
            string filePath = GetPath(param);
            try
            {
                SaveParamAttribute.Save(this, param, filePath);
                if (freshComponentParams)
                {
                    var _baseBiz = (BaseBiz)IoC.Get<IBaseBiz>();
                    _baseBiz.InitialReset();
                }
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"保存{filePath.Substring(filePath.LastIndexOf('/'))}参数异常:{e}", En_Logout_Type.Exception, true);
            }
        }
        #endregion
    }
}
