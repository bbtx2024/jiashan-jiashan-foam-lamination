/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：胡勇
 * 创建日期：2022-02-22
 * 说明：（非硬件设置类的数据存储）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/
using System;
using System.Configuration;
using System.IO;
using QA.Business.CacheParam;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;

namespace QA.Business.Manager
{
    [SaveParam(FileType.JSON)]
    [Serializable]
    public class CacheParamManager
    {
        #region Field
        //public string _cacheDirectory = ConfigurationManager.AppSettings["cachePath"].ToString().Replace("{BaseDirectory}", AppDomain.CurrentDomain.BaseDirectory);
        public string _cacheDirectory = @"D:\QKProject\Setting\Config\Cache";
        private string _dBDir = @"C:\ProgramData\Quick\SpotWelding\";
        #endregion

        #region Property
        private HomeUiParam _homeUiParam = new HomeUiParam();
        public HomeUiParam HomeUiParam
        {
            get { return _homeUiParam; }
            set { _homeUiParam = value; }
        }

        private ManualPositionParam _manualPositionParam = new ManualPositionParam();
        public ManualPositionParam manualPositionParam
        {
            get { return _manualPositionParam; }
            set { _manualPositionParam = value; }
        }

        private TaskParam _taskParam = new TaskParam();
        public TaskParam TaskParam
        {
            get { return _taskParam; }
            set { _taskParam = value; }
        }

        private CalibParam _calibParam = new CalibParam();
        public CalibParam calibParam
        {
            get { return _calibParam; }
            set { _calibParam = value; }
        }

        private RunInfoParam _runInfoParam = new RunInfoParam();
        public RunInfoParam RunInfoParam
        {
            get { return _runInfoParam; }
            set { _runInfoParam = value; }
        }

        private VisionTemplateParam _visionTemplateParam = new VisionTemplateParam();       //视觉模板名称参数
        public VisionTemplateParam VisionTemplateParam
        {
            get { return _visionTemplateParam; }
            set { _visionTemplateParam = value; }
        }

        private WorkPointParam _workPointParam = new WorkPointParam();        //Tray盘信息参数
        public WorkPointParam WorkPointParam
        {
            get { return _workPointParam; }
            set { _workPointParam = value; }
        }

        private PressParam _pressParam = new PressParam();      //压力校准参数
        public PressParam PressParam
        {
            get { return _pressParam; }
            set { _pressParam = value; }
        }

        public string LastAllTimeSpan { get; set; } = string.Empty;
        public DateTime Lastgetdatetime = new DateTime();
        public TimeSpan Lasttotalspan = new TimeSpan();
        public int TotalSolderCount { get; set; } = 0;
        #endregion

        #region Constructor
        public CacheParamManager()
        {
            LoadParams();
        }
        #endregion

        #region Method
        public bool LoadParams()
        {
            try
            {
                if (!Directory.Exists(_cacheDirectory))
                {
                    Directory.CreateDirectory(_cacheDirectory);
                }

                LoadParam<ManualPositionParam>(ref _manualPositionParam, _cacheDirectory);
                LoadParam<CalibParam>(ref _calibParam, _cacheDirectory);
                LoadParam<HomeUiParam>(ref _homeUiParam, _cacheDirectory);
                LoadParam<TaskParam>(ref _taskParam, _cacheDirectory);
                LoadParam<VisionTemplateParam>(ref _visionTemplateParam, _cacheDirectory);
                LoadParam<WorkPointParam>(ref _workPointParam, _cacheDirectory);
                LoadParam<PressParam>(ref _pressParam, _cacheDirectory);
                LoadRunInfoParan();
                return true;
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"加载参数异常:{e},{e.StackTrace}");
                return false;
            }
        }
        public bool SaveAllParam()
        {
            try
            {
                if (!Directory.Exists(_cacheDirectory))
                {
                    Directory.CreateDirectory(_cacheDirectory);
                }
                SaveParam<ManualPositionParam>(manualPositionParam, _cacheDirectory);
                SaveParam<CalibParam>(calibParam, _cacheDirectory);
                SaveParam<HomeUiParam>(HomeUiParam, _cacheDirectory);
                SaveParam<TaskParam>(TaskParam, _cacheDirectory);
                SaveParam<VisionTemplateParam>(VisionTemplateParam, _cacheDirectory);
                SaveParam<WorkPointParam>(WorkPointParam, _cacheDirectory);
                SaveParam<PressParam>(PressParam, _cacheDirectory);
                return true;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"保存参数异常:{ex.Message},{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 保存单个指定的参数
        /// </summary>
        /// <returns></returns>
        public bool SaveSpecifiedParam(Type paramType)
        {
            try
            {
                if (!Directory.Exists(_cacheDirectory))
                    Directory.CreateDirectory(_cacheDirectory);

                if (paramType.Name == typeof(HomeUiParam).Name)
                    SaveParam<HomeUiParam>(HomeUiParam, _cacheDirectory);
                else if (paramType.Name == typeof(ManualPositionParam).Name)
                    SaveParam<ManualPositionParam>(manualPositionParam, _cacheDirectory);
                else if (paramType.Name == typeof(TaskParam).Name)
                    SaveParam<TaskParam>(TaskParam, _cacheDirectory);
                else if (paramType.Name == typeof(CalibParam).Name)
                    SaveParam<CalibParam>(calibParam, _cacheDirectory);
                else if (paramType.Name == typeof(VisionTemplateParam).Name)
                    SaveParam<VisionTemplateParam>(VisionTemplateParam, _cacheDirectory);
                else if (paramType.Name == typeof(WorkPointParam).Name)
                    SaveParam<WorkPointParam>(WorkPointParam, _cacheDirectory);
                else if (paramType.Name == typeof(PressParam).Name)
                    SaveParam<PressParam>(PressParam, _cacheDirectory);

                return true;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"保存参数异常:{ex.Message},{ex.StackTrace}");
            }
            return false;
        }

        public bool SaveManualPositionParam()
        {
            try
            {
                if (!Directory.Exists(_cacheDirectory))
                {
                    Directory.CreateDirectory(_cacheDirectory);
                }
                SaveParam<ManualPositionParam>(manualPositionParam, _cacheDirectory);
                return true;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"保存参数异常:{ex.Message},{ex.StackTrace}");
                return false;
            }
        }
        public bool SaveHomeUiParam()
        {
            try
            {
                if (!Directory.Exists(_cacheDirectory))
                {
                    Directory.CreateDirectory(_cacheDirectory);
                }
                SaveParam<HomeUiParam>(HomeUiParam, _cacheDirectory);
                return true;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"保存参数异常:{ex.Message},{ex.StackTrace}");
                return false;
            }
        }
        public bool SaveCalibParam()
        {
            try
            {
                if (!Directory.Exists(_cacheDirectory))
                {
                    Directory.CreateDirectory(_cacheDirectory);
                }
                SaveParam<CalibParam>(calibParam, _cacheDirectory);
                return true;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"保存参数异常:{ex.Message},{ex.StackTrace}");
                return false;
            }
        }
        public bool SaveTaskParam()
        {
            try
            {
                if (!Directory.Exists(_cacheDirectory))
                {
                    Directory.CreateDirectory(_cacheDirectory);
                }

                SaveParam<TaskParam>(TaskParam, _cacheDirectory);
                return true;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"保存参数异常:{ex.Message},{ex.StackTrace}");
                return false;
            }
        }
        public bool SaveRunInfoParam()
        {
            try
            {
                if (!Directory.Exists(_dBDir))
                {
                    Directory.CreateDirectory(_dBDir);
                }

                Lasttotalspan = new TimeSpan(Lasttotalspan.Ticks + (DateTime.Now - Lastgetdatetime).Ticks);

                RunInfoParam.LastAllTimeSpan = Lasttotalspan.Days + "," + Lasttotalspan.Hours + "," +
                                  Lasttotalspan.Minutes + "," + Lasttotalspan.Seconds + "," +
                                  Lasttotalspan.Milliseconds;
                RunInfoParam.TotalSolderCount = this.TotalSolderCount;
                SaveParam<RunInfoParam>(RunInfoParam, _dBDir);
                return true;
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"保存参数异常:{ex.Message},{ex.StackTrace}");
                return false;
            }
        }
        public bool LoadRunInfoParan()
        {
            string[] strlasttotalspan = new string[5];
            try
            {
                Lastgetdatetime = DateTime.Now;
                if (!Directory.Exists(_dBDir))
                {
                    Directory.CreateDirectory(_dBDir);
                }
                LoadParam<RunInfoParam>(ref _runInfoParam, _dBDir);

                this.TotalSolderCount = _runInfoParam.TotalSolderCount;
                this.LastAllTimeSpan = _runInfoParam.LastAllTimeSpan;
                strlasttotalspan = LastAllTimeSpan.Split(',');
                if (strlasttotalspan.Length <= 1)
                {
                    Lasttotalspan = DateTime.Now - DateTime.Now;
                }
                else
                {
                    if (int.Parse(strlasttotalspan[0]) > (365 * 50))
                    {
                        strlasttotalspan[0] = "0";
                        strlasttotalspan[1] = "0";
                        strlasttotalspan[2] = "0";
                        strlasttotalspan[3] = "0";
                        strlasttotalspan[4] = "0";
                    }
                    Lasttotalspan = new TimeSpan(int.Parse(strlasttotalspan[0]),
                        int.Parse(strlasttotalspan[1]),
                        int.Parse(strlasttotalspan[2]),
                        int.Parse(strlasttotalspan[3]),
                        int.Parse(strlasttotalspan[4]));
                }
                return true;
            }
            catch (Exception e)
            {
                strlasttotalspan[0] = "0";
                strlasttotalspan[1] = "0";
                strlasttotalspan[2] = "0";
                strlasttotalspan[3] = "0";
                strlasttotalspan[4] = "0";

                Lasttotalspan = new TimeSpan(int.Parse(strlasttotalspan[0]),
                    int.Parse(strlasttotalspan[1]),
                    int.Parse(strlasttotalspan[2]),
                    int.Parse(strlasttotalspan[3]),
                    int.Parse(strlasttotalspan[4]));
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"加载参数异常:{e},{e.StackTrace}");
                return false;
            }
        }
        private bool LoadParam<T>(ref T param, string filepath)
        {
            try
            {
                string filePath = Path.Combine(filepath, param?.GetType().Name.TrimEnd('[', ']'));
                var getObj = SaveParamAttribute.Load<T>(filePath);
                if (getObj != null)
                {
                    param = getObj.DeepCopy();
                }
                else
                    return false;
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"加载{param.GetType().Name.TrimEnd('[', ']')}参数异常:{e},{e.StackTrace}");
                return false;
            }
            return true;
        }
        private bool SaveParam<T>(T param, string filepath)
        {
            try
            {
                var fileName = string.Empty;
                if (param.GetType().IsArray)
                {
                    var array = param as Array;
                    if (array.Length > 0)
                    {
                        fileName = array.GetValue(0).GetType().Name.TrimEnd('[', ']');
                    }
                }
                string filePath = Path.Combine(filepath, string.IsNullOrEmpty(fileName) ? param.GetType().Name.TrimEnd('[', ']') : fileName);
                SaveParamAttribute.Save<T>(param, filePath);
            }
            catch (Exception e)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.CriticalError, $"保存{param.GetType().Name.TrimEnd('[', ']')}参数异常:{e},{e.StackTrace}");
                return false;
            }
            return true;
        }
        public string GetAllTotalRunTime()
        {
            TimeSpan span = new TimeSpan(Lasttotalspan.Ticks + (DateTime.Now - Lastgetdatetime).Ticks);
#if LANG_EN
            return (int)span.TotalDays + " Day " + span.Hours + ":" + span.Minutes + ":" + span.Seconds;// +"." + span.Milliseconds;
#else

            return (int)span.TotalDays + " 天 " + string.Format("{0:D2}", span.Hours) + ":" + string.Format("{0:D2}", span.Minutes) + ":" + string.Format("{0:D2}", span.Seconds);
#endif
        }
        #endregion
    }
}
