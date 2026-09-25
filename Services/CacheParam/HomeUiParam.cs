using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Caliburn.Micro;
using QA.Business.Define;
using QA.Business.Message;
using QA_Infrastructure;

namespace QA.Business.CacheParam
{
    [SaveParam(FileType.JSON)]
    [Serializable]
    public class HomeUiParam
    {
        /// <summary>
        /// 产量统计
        /// </summary>
        public HomeUiParam_Statistic Statistic { get; set; } = new HomeUiParam_Statistic();

        /// <summary>
        /// 功能启用
        /// </summary>
        public HomeUiParam_Enable Enable { get; set; } = new HomeUiParam_Enable();

        /// <summary>
        /// 喷咀管理参数
        /// </summary>
        public NozzleManageParam NozeleManageParam { get; set; } = new NozzleManageParam();

        /// <summary>
        /// 锡球管理参数
        /// </summary>
        public TinBallManageParam TinBallManageParam { get; set; } = new TinBallManageParam();

        /// <summary>
        /// 飞达tape使用次数参数
        /// </summary>
        public TapeUseInfoParam TapeUseInfoParam { get; set; } = new TapeUseInfoParam();
    }

    public class HomeUiParam_Statistic : INotifyPropertyChanged
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

        private int _currentNum;
        public int CurrentNum
        {
            get { return _currentNum; }
            set { _currentNum = value; NotifyOfPropertyChange(() => CurrentNum); }
        }

        private int _currentdebugNum;
        public int CurrentdebugNum
        {
            get { return _currentdebugNum; }
            set { _currentdebugNum = value; NotifyOfPropertyChange(() => CurrentdebugNum); }
        }

        private int _targetNum;
        public int TargetNum
        {
            get { return _targetNum; }
            set { _targetNum = value; NotifyOfPropertyChange(() => TargetNum); }
        }

        private int _setTargetNum;
        public int SetTargetNum
        {
            get { return _setTargetNum; }
            set { _setTargetNum = value; NotifyOfPropertyChange(() => SetTargetNum); }
        }

        private void ChangeAll()
        {
            string[] s1 = { "UseCount", "SuckNgCount", "CamNgCount", "FlingRate", "OkRate",/* "AliveCount",*/ };
            string[] s2 = { "1", "2", "3", "4", "All", };
            foreach (var ss1 in s1)
            {
                foreach (var ss2 in s2)
                {
                    NotifyOfPropertyChange(ss1 + ss2);
                }
            }
        }
        private void ChangeDebugAll()
        {
            string[] s3 = { "UsedebugCount", "SuckNgdebugCount", "CamNgdebugCount", "FlingDebugRate", "OkDebugRate",/* "AliveCount",*/ };
            string[] s4 = { "1", "2", "3", "4", "All", };
            foreach (var ss3 in s3)
            {
                foreach (var ss4 in s4)
                {
                    NotifyOfPropertyChange(ss3 + ss4);
                }
            }
        }
        private int[] _useCount = new int[4];
        public int UseCount1
        {
            get { return _useCount[0]; }
            set { _useCount[0] = value; ChangeAll(); }
        }
        public int UseCount2
        {
            get { return _useCount[1]; }
            set { _useCount[1] = value; ChangeAll(); }
        }
        public int UseCount3
        {
            get { return _useCount[2]; }
            set { _useCount[2] = value; ChangeAll(); }
        }
        public int UseCount4
        {
            get { return _useCount[3]; }
            set { _useCount[3] = value; ChangeAll(); }
        }
        public int UseCountAll
        {
            get
            {
                int ret = 0;
                foreach (var x in _useCount)
                {
                    ret += x;
                }
                return ret;
            }
        }
        public void AddUseCount(int nozzleNo)
        {
            _useCount[nozzleNo - 1]++;
            ChangeAll();
        }
        //20250512增加调试数据表信息
        private int[] _usedebugCount = new int[4];
        public int UsedebugCount1
        {
            get { return _usedebugCount[0]; }
            set { _usedebugCount[0] = value; ChangeDebugAll(); }
        }
        public int UsedebugCount2
        {
            get { return _usedebugCount[1]; }
            set { _usedebugCount[1] = value; ChangeDebugAll(); }
        }
        public int UsedebugCount3
        {
            get { return _usedebugCount[2]; }
            set { _usedebugCount[2] = value; ChangeDebugAll(); }
        }
        public int UsedebugCount4
        {
            get { return _usedebugCount[3]; }
            set { _usedebugCount[3] = value; ChangeDebugAll(); }
        }
        public int UsedebugCountAll
        {
            get
            {
                int ret = 0;
                foreach (var x in _usedebugCount)
                {
                    ret += x;
                }
                return ret;
            }
        }
        public void AddUseDebugCount(int nozzleNo)
        {
            _usedebugCount[nozzleNo - 1]++;
            ChangeDebugAll();
        }



        private int[] _suckNgCount = new int[4];
        public int SuckNgCount1
        {
            get { return _suckNgCount[0]; }
            set { _suckNgCount[0] = value; ChangeAll(); }
        }
        public int SuckNgCount2
        {
            get { return _suckNgCount[1]; }
            set { _suckNgCount[1] = value; ChangeAll(); }
        }
        public int SuckNgCount3
        {
            get { return _suckNgCount[2]; }
            set { _suckNgCount[2] = value; ChangeAll(); }
        }
        public int SuckNgCount4
        {
            get { return _suckNgCount[3]; }
            set { _suckNgCount[3] = value; ChangeAll(); }
        }
        public int SuckNgCountAll
        {
            get
            {
                int ret = 0;
                foreach (var x in _suckNgCount)
                {
                    ret += x;
                }
                return ret;
            }
        }
        public void AddSuckNgCount(int nozzleNo)
        {
            _suckNgCount[nozzleNo - 1]++;
            ChangeAll();
        }
        //20250512增加调试数据表信息
        private int[] _suckNgdebugCount = new int[4];
        public int SuckNgdebugCount1
        {
            get { return _suckNgdebugCount[0]; }
            set { _suckNgdebugCount[0] = value; ChangeDebugAll(); }
        }
        public int SuckNgdebugCount2
        {
            get { return _suckNgdebugCount[1]; }
            set { _suckNgdebugCount[1] = value; ChangeDebugAll(); }
        }
        public int SuckNgdebugCount3
        {
            get { return _suckNgdebugCount[2]; }
            set { _suckNgdebugCount[2] = value; ChangeDebugAll(); }
        }
        public int SuckNgdebugCount4
        {
            get { return _suckNgdebugCount[3]; }
            set { _suckNgdebugCount[3] = value; ChangeDebugAll(); }
        }
        public int SuckNgdebugCountAll
        {
            get
            {
                int ret = 0;
                foreach (var x in _suckNgdebugCount)
                {
                    ret += x;
                }
                return ret;
            }
        }
        public void AddSuckNgDebugCount(int nozzleNo)
        {
            _suckNgdebugCount[nozzleNo - 1]++;
            ChangeDebugAll();
        }

        //20250512增加调试数据表信息
        private int[] _camNgCount = new int[4];
        public int CamNgCount1
        {
            get { return _camNgCount[0]; }
            set { _camNgCount[0] = value; ChangeAll(); }
        }
        public int CamNgCount2
        {
            get { return _camNgCount[1]; }
            set { _camNgCount[1] = value; ChangeAll(); }
        }
        public int CamNgCount3
        {
            get { return _camNgCount[2]; }
            set { _camNgCount[2] = value; ChangeAll(); }
        }
        public int CamNgCount4
        {
            get { return _camNgCount[3]; }
            set { _camNgCount[3] = value; ChangeAll(); }
        }
        public int CamNgCountAll
        {
            get
            {
                int ret = 0;
                foreach (var x in _camNgCount)
                {
                    ret += x;
                }
                return ret;
            }
        }
        /// <summary>
        /// <param name="camType">camType:1 吸嘴1下视觉NG，2 吸嘴2下视觉NG，3 上视觉NG</param>
        /// </summary>
        public void AddCamNgCount(int camType)
        {
            switch (camType)
            {
                case 1:
                    CamNgCount1++;
                    break;
                case 2:
                    CamNgCount2++;
                    break;
                case 3:
                    CamNgCount3++;
                    break;
                default:
                    break;
            }
            ChangeAll();
        }
        //20250512增加调试数据表信息
        private int[] _camNgDebugCount = new int[4];
        public int CamNgdebugCount1
        {
            get { return _camNgDebugCount[0]; }
            set { _camNgDebugCount[0] = value; ChangeDebugAll(); }
        }
        public int CamNgdebugCount2
        {
            get { return _camNgDebugCount[1]; }
            set { _camNgDebugCount[1] = value; ChangeDebugAll(); }
        }
        public int CamNgdebugCount3
        {
            get { return _camNgDebugCount[2]; }
            set { _camNgDebugCount[2] = value; ChangeDebugAll(); }
        }
        public int CamNgdebugCount4
        {
            get { return _camNgDebugCount[3]; }
            set { _camNgDebugCount[3] = value; ChangeDebugAll(); }
        }
        public int CamNgdebugCountAll
        {
            get
            {
                int ret = 0;
                foreach (var x in _camNgDebugCount)
                {
                    ret += x;
                }
                return ret;
            }
        }
        public void AddCamNgDebugCount(int nozzleNo)
        {
            _camNgDebugCount[nozzleNo - 1]++;
            ChangeDebugAll();
        }
        //20250512增加调试数据表信息
        private float getFlingRate(int i)
        {
            if (i == -1)
            {
                return UseCountAll == 0 ? 0 : (float)(SuckNgCountAll + CamNgCountAll) / UseCountAll;
            }
            else
            {
                return _useCount[i] == 0 ? 0 : (float)(_suckNgCount[i] + _camNgCount[i]) / _useCount[i];
            }
        }
        public float FlingRate1 { get => getFlingRate(0); }
        public float FlingRate2 { get => getFlingRate(1); }
        public float FlingRate3 { get => getFlingRate(2); }
        public float FlingRate4 { get => getFlingRate(3); }
        public float FlingRateAll { get => getFlingRate(-1); }
        //20250512增加调试数据表信息
        private float getFlingDebugRate(int i)
        {
            if (i == -1)
            {
                return UsedebugCountAll == 0 ? 0 : (float)(SuckNgdebugCountAll + CamNgdebugCountAll) / UsedebugCountAll;
            }
            else
            {
                return _usedebugCount[i] == 0 ? 0 : (float)(_suckNgdebugCount[i] + _camNgDebugCount[i]) / _usedebugCount[i];
            }
        }
        public float FlingDebugRate1 { get => getFlingDebugRate(0); }
        public float FlingDebugRate2 { get => getFlingDebugRate(1); }
        public float FlingDebugRate3 { get => getFlingDebugRate(2); }
        public float FlingDebugRate4 { get => getFlingDebugRate(3); }
        public float FlingDebugRateAll { get => getFlingDebugRate(-1); }


        private float getOkDebugRate(int i)
        {
            if (i == -1)
            {
                return UsedebugCountAll == 0 ? 1 : (float)(UsedebugCountAll - SuckNgdebugCountAll - CamNgdebugCountAll) / UsedebugCountAll;
            }
            else
            {
                return _usedebugCount[i] == 0 ? 1 : (float)(_usedebugCount[i] - _suckNgdebugCount[i] - _camNgDebugCount[i]) / _usedebugCount[i];
            }
        }
        public float OkDebugRate1 { get => getOkDebugRate(0); }
        public float OkDebugRate2 { get => getOkDebugRate(1); }
        public float OkDebugRate3 { get => getOkDebugRate(2); }
        public float OkDebugRate4 { get => getOkDebugRate(3); }
        public float OkDebugRateAll { get => getOkDebugRate(-1); }

        public void ResetNozzledebugCount(int nozzleNo)
        {
            if (nozzleNo == 0)
            {
                for (int i = 1; i <= 4; i++)
                {
                    ResetNozzledebugCount(i);
                }
                ChangeDebugAll();
                return;
            }
            _usedebugCount[nozzleNo - 1] = 0;
            _suckNgdebugCount[nozzleNo - 1] = 0;
            _camNgDebugCount[nozzleNo - 1] = 0;
            ChangeDebugAll();
        }

        //20250512增加调试数据表信息
        private float getOkRate(int i)
        {
            if (i == -1)
            {
                return UseCountAll == 0 ? 1 : (float)(UseCountAll - SuckNgCountAll - CamNgCountAll) / UseCountAll;
            }
            else
            {
                return _useCount[i] == 0 ? 1 : (float)(_useCount[i] - _suckNgCount[i] - _camNgCount[i]) / _useCount[i];
            }
        }
        public float OkRate1 { get => getOkRate(0); }
        public float OkRate2 { get => getOkRate(1); }
        public float OkRate3 { get => getOkRate(2); }
        public float OkRate4 { get => getOkRate(3); }
        public float OkRateAll { get => getOkRate(-1); }

        public void ResetNozzleCount(int nozzleNo)
        {
            if (nozzleNo == 0)
            {
                for (int i = 1; i <= 4; i++)
                {
                    ResetNozzleCount(i);
                }
                ChangeAll();
                return;
            }
            _useCount[nozzleNo - 1] = 0;
            _suckNgCount[nozzleNo - 1] = 0;
            _camNgCount[nozzleNo - 1] = 0;
            ChangeAll();
        }

        public float OkCount => UseCountAll - SuckNgCountAll - CamNgCountAll;
        public float NgCount => SuckNgCountAll + CamNgCountAll;
        public float OkRate => getOkRate(-1);

        private void ChangeAllUph()
        {
            for (int i = 0; i < 24; i++)
            {
                NotifyOfPropertyChange("Uph" + i);
            }
        }
        //修改增加
        private void ChangeAlldebugUph()
        {
            for (int i = 0; i < 24; i++)
            {
                NotifyOfPropertyChange("Uphdebug" + i);
            }
        }

        //修改增加
        public int[] _uph = new int[24];
        public int[] _uphdebug = new int[24];
        /// <summary>
        /// 8H-9H对应的产能
        /// </summary>
        public int Uph0
        {
            get { return _uph[0]; }
            set { _uph[0] = value; ChangeAllUph(); }
        }
        public int Uph1
        {
            get { return _uph[1]; }
            set { _uph[1] = value; ChangeAllUph(); }
        }
        public int Uph2
        {
            get { return _uph[2]; }
            set { _uph[2] = value; ChangeAllUph(); }
        }
        public int Uph3
        {
            get { return _uph[3]; }
            set { _uph[3] = value; ChangeAllUph(); }
        }
        public int Uph4
        {
            get { return _uph[4]; }
            set { _uph[4] = value; ChangeAllUph(); }
        }
        public int Uph5
        {
            get { return _uph[5]; }
            set { _uph[5] = value; ChangeAllUph(); }
        }
        public int Uph6
        {
            get { return _uph[6]; }
            set { _uph[6] = value; ChangeAllUph(); }
        }
        public int Uph7
        {
            get { return _uph[7]; }
            set { _uph[7] = value; ChangeAllUph(); }
        }
        public int Uph8
        {
            get { return _uph[8]; }
            set { _uph[8] = value; ChangeAllUph(); }
        }
        public int Uph9
        {
            get { return _uph[9]; }
            set { _uph[9] = value; ChangeAllUph(); }
        }
        public int Uph10
        {
            get { return _uph[10]; }
            set { _uph[10] = value; ChangeAllUph(); }
        }
        public int Uph11
        {
            get { return _uph[11]; }
            set { _uph[11] = value; ChangeAllUph(); }
        }
        public int Uph12
        {
            get { return _uph[12]; }
            set { _uph[12] = value; ChangeAllUph(); }
        }
        public int Uph13
        {
            get { return _uph[13]; }
            set { _uph[13] = value; ChangeAllUph(); }
        }
        public int Uph14
        {
            get { return _uph[14]; }
            set { _uph[14] = value; ChangeAllUph(); }
        }
        public int Uph15
        {
            get { return _uph[15]; }
            set { _uph[15] = value; ChangeAllUph(); }
        }
        public int Uph16
        {
            get { return _uph[16]; }
            set { _uph[16] = value; ChangeAllUph(); }
        }
        public int Uph17
        {
            get { return _uph[17]; }
            set { _uph[17] = value; ChangeAllUph(); }
        }
        public int Uph18
        {
            get { return _uph[18]; }
            set { _uph[18] = value; ChangeAllUph(); }
        }
        public int Uph19
        {
            get { return _uph[19]; }
            set { _uph[19] = value; ChangeAllUph(); }
        }
        public int Uph20
        {
            get { return _uph[20]; }
            set { _uph[20] = value; ChangeAllUph(); }
        }
        public int Uph21
        {
            get { return _uph[21]; }
            set { _uph[21] = value; ChangeAllUph(); }
        }
        public int Uph22
        {
            get { return _uph[22]; }
            set { _uph[22] = value; ChangeAllUph(); }
        }
        public int Uph23
        {
            get { return _uph[23]; }
            set { _uph[23] = value; ChangeAllUph(); }
        }
        //
        public int Uphdebug0
        {
            get { return _uphdebug[0]; }
            set { _uphdebug[0] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug1
        {
            get { return _uphdebug[1]; }
            set { _uphdebug[1] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug2
        {
            get { return _uphdebug[2]; }
            set { _uphdebug[2] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug3
        {
            get { return _uphdebug[3]; }
            set { _uphdebug[3] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug4
        {
            get { return _uphdebug[4]; }
            set { _uphdebug[4] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug5
        {
            get { return _uphdebug[5]; }
            set { _uphdebug[5] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug6
        {
            get { return _uphdebug[6]; }
            set { _uphdebug[6] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug7
        {
            get { return _uphdebug[7]; }
            set { _uphdebug[7] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug8
        {
            get { return _uphdebug[8]; }
            set { _uphdebug[8] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug9
        {
            get { return _uphdebug[9]; }
            set { _uphdebug[9] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug10
        {
            get { return _uphdebug[10]; }
            set { _uphdebug[10] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug11
        {
            get { return _uphdebug[11]; }
            set { _uphdebug[11] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug12
        {
            get { return _uphdebug[12]; }
            set { _uphdebug[12] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug13
        {
            get { return _uphdebug[13]; }
            set { _uphdebug[13] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug14
        {
            get { return _uphdebug[14]; }
            set { _uphdebug[14] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug15
        {
            get { return _uphdebug[15]; }
            set { _uphdebug[15] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug16
        {
            get { return _uphdebug[16]; }
            set { _uphdebug[16] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug17
        {
            get { return _uphdebug[17]; }
            set { _uphdebug[17] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug18
        {
            get { return _uphdebug[18]; }
            set { _uphdebug[18] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug19
        {
            get { return _uphdebug[19]; }
            set { _uphdebug[19] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug20
        {
            get { return _uphdebug[20]; }
            set { _uphdebug[20] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug21
        {
            get { return _uphdebug[21]; }
            set { _uphdebug[21] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug22
        {
            get { return _uphdebug[22]; }
            set { _uphdebug[22] = value; ChangeAlldebugUph(); }
        }
        public int Uphdebug23
        {
            get { return _uphdebug[23]; }
            set { _uphdebug[23] = value; ChangeAlldebugUph(); }
        }
        //

        /// <summary>
        /// _uph[0]对应的时间起点（某一天的8H）
        /// </summary>
        public DateTime CurrUphStartTime { get; set; } = DateTime.MinValue;
        public DateTime CurrdebugUphStartTime { get; set; } = DateTime.MinValue;
        public void AddUph(bool isUPH)
        {
            if (isUPH)
            {
                ResetUphAndSaveDataIfNeed();
                int hour = DateTime.Now.Hour;
                _uph[(hour + 16) % 24]++;
                ChangeAllUph();
                CurrentNum++;
            }
            else
            {
                ResetUphAndSaveDataIfNeedDebug();
                int hour = DateTime.Now.Hour;
                _uphdebug[(hour + 16) % 24]++;
                ChangeAlldebugUph();
                CurrentdebugNum++;
            }
        }

        private object uphLock = new object();

        /// <summary>
        /// 判断是否需要刷新uph数据。如果是，保存当前uph数据，并清零uph数据，修改主界面显示。
        /// </summary>
        public void ResetUphAndSaveDataIfNeed()
        {
            lock (uphLock)
            {
                // 找到上一个8H
                DateTime currTime = DateTime.Now;
                DateTime previous8H = currTime.Date.AddHours(8);
                if (currTime < previous8H)
                {
                    previous8H = previous8H.AddDays(-1);
                }
                // 如果与CurrUphStartTime不匹配，说明需要更新
                if (previous8H == CurrUphStartTime)
                {
                    return;
                }
                try
                {
                    string dir0 = $@"D:\CsvData\产能数据";
                    if (!Directory.Exists(dir0))
                    {
                        Directory.CreateDirectory(dir0);
                    }
                    string file = $@"{dir0}\产能生产数据_{CurrUphStartTime.ToString("yyyyMMddHH")}.csv";
                    using (StreamWriter sw = new StreamWriter(file, false))
                    {
                        DateTime previous0H = CurrUphStartTime.Date;
                        sw.WriteLine("开始时间,结束时间,产能数据");
                        for (int i = 0; i < _uph.Length; i++)
                        {
                            DateTime timeStart = CurrUphStartTime.AddHours(i);
                            DateTime timeEnd = CurrUphStartTime.AddHours(i + 1);
                            sw.WriteLine(timeStart.ToString("yyyy-MM-dd HH:mm:ss") + ","
                                + timeEnd.ToString("yyyy-MM-dd HH:mm:ss") + ","
                                + _uph[i]);
                        }
                    }
                }
                catch (Exception e)
                {
                    //ignore
                }
                _uph = new int[24];
                CurrUphStartTime = previous8H;
                ChangeAllUph();
            }
        }

        private object uphdebugLock = new object();
        public void ResetUphAndSaveDataIfNeedDebug()
        {
            lock (uphdebugLock)
            {
                // 找到上一个8H
                DateTime currTime = DateTime.Now;
                DateTime previous8H = currTime.Date.AddHours(8.05);
                if (currTime < previous8H)
                {
                    previous8H = previous8H.AddDays(-1);
                }
                // 如果与CurrdebugUphStartTime不匹配，说明需要更新
                if (previous8H == CurrdebugUphStartTime)
                {
                    return;
                }
                try
                {
                    string dir0 = $@"D:\CsvData\产能数据";
                    if (!Directory.Exists(dir0))
                    {
                        Directory.CreateDirectory(dir0);
                    }
                    string file = $@"{dir0}\产能调试数据_{CurrdebugUphStartTime.ToString("yyyyMMddHH")}.csv";
                    using (StreamWriter sw = new StreamWriter(file, false))
                    {
                        DateTime previous0H = CurrdebugUphStartTime.Date;
                        sw.WriteLine("开始时间,结束时间,产能数据");
                        for (int i = 0; i < _uphdebug.Length; i++)
                        {
                            DateTime timeStart = CurrdebugUphStartTime.AddHours(i);
                            DateTime timeEnd = CurrdebugUphStartTime.AddHours(i + 1);
                            sw.WriteLine(timeStart.ToString("yyyy-MM-dd HH:mm:ss") + ","
                                + timeEnd.ToString("yyyy-MM-dd HH:mm:ss") + ","
                                + _uphdebug[i]);
                        }
                    }
                }
                catch (Exception e)
                {
                    //ignore
                }
                _uphdebug = new int[24];
                CurrdebugUphStartTime = previous8H;
                ChangeAlldebugUph();
            }

        }
        public void ResetUph()
        {
            _uph = new int[24];
        }

        private int _runTimeMinute = 0;
        public int RunTimeMinute
        {
            get => _runTimeMinute;
            set
            {
                _runTimeMinute = value;
                NotifyOfPropertyChange(() => RunTimeMinute);
                NotifyOfPropertyChange(() => RunTimeStr);
            }
        }
        public string RunTimeStr { get => _runTimeMinute / 60 + " 时 " + _runTimeMinute % 60 + " 分"; }

        private int _idleTimeMinute = 0;
        public int IdleTimeMinute
        {
            get => _idleTimeMinute;
            set
            {
                _idleTimeMinute = value;
                NotifyOfPropertyChange(() => IdleTimeMinute);
                NotifyOfPropertyChange(() => IdleTimeStr);
            }
        }
        public string IdleTimeStr { get => _idleTimeMinute / 60 + " 时 " + _idleTimeMinute % 60 + " 分"; }

        private int _errTimeMinute = 0;
        public int ErrTimeMinute
        {
            get => _errTimeMinute;
            set
            {
                _errTimeMinute = value;
                NotifyOfPropertyChange(() => ErrTimeMinute);
                NotifyOfPropertyChange(() => ErrTimeStr);
            }
        }
        public string ErrTimeStr { get => _errTimeMinute / 60 + " 时 " + _errTimeMinute % 60 + " 分"; }

        private int _alarmCount = 0;
        public int AlarmCount
        {
            get => _alarmCount;
            set
            {
                _alarmCount = value;
                NotifyOfPropertyChange(() => AlarmCount);
                NotifyOfPropertyChange(() => AlarmCountStr);
            }
        }
        public string AlarmCountStr { get => _alarmCount + " 次"; }


        public List<string> SoftwareHashList { get; set; } = new List<string>();

        private int _currSoftwareVersion = 0;
        public int CurrSoftwareVersion
        {
            get => _currSoftwareVersion;
            set { _currSoftwareVersion = value; NotifyOfPropertyChange(() => CurrSoftwareVersion); }
        }

        public void AddSoftwareVersionIfNeed(string currHash)
        {
            if (!SoftwareHashList.Contains(currHash))
            {
                SoftwareHashList.Add(currHash);
            }
            CurrSoftwareVersion = SoftwareHashList.IndexOf(currHash) + 1;
        }
    }

    public class HomeUiParam_Enable : INotifyPropertyChanged
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

        public void NotifyAll()
        {
            NotifyOfPropertyChange(() => PickOpportunity);
            NotifyOfPropertyChange(() => OncePickNum);
            NotifyOfPropertyChange(() => KeepNoTapeOnFeeder);
            NotifyOfPropertyChange(() => AutoThrowTapesAfterReset);
            NotifyOfPropertyChange(() => UseNozzle1);
            NotifyOfPropertyChange(() => UseNozzle2);
            NotifyOfPropertyChange(() => UseNozzle3);
            NotifyOfPropertyChange(() => UseNozzle4);
            NotifyOfPropertyChange(() => UseVacSucCheck);
            NotifyOfPropertyChange(() => IsFeederCheck);
            NotifyOfPropertyChange(() => IsCDDCheck);
        }
        private FeederId _feederId = FeederId.左飞达;
        public FeederId FeederId
        {
            get { return _feederId; }
            set { _feederId = value; NotifyOfPropertyChange(() => FeederId); }
        }

        private EN_PickOpportunity _PickOpportunity = EN_PickOpportunity.AfterStart;
        public EN_PickOpportunity PickOpportunity
        {
            get { return _PickOpportunity; }
            set { _PickOpportunity = value; NotifyOfPropertyChange(() => PickOpportunity); }
        }

        private EN_OncePickNum _OncePickNum = EN_OncePickNum.One;
        public EN_OncePickNum OncePickNum
        {
            get { return _OncePickNum; }
            set { _OncePickNum = value; NotifyOfPropertyChange(() => OncePickNum); }
        }

        private bool _keepNoTapeOnFeeder = true;
        public bool KeepNoTapeOnFeeder
        {
            get { return _keepNoTapeOnFeeder; }
            set { _keepNoTapeOnFeeder = value; NotifyOfPropertyChange(() => KeepNoTapeOnFeeder); }
        }

        private bool _autoThrowTapesAfterReset = true;
        public bool AutoThrowTapesAfterReset
        {
            get { return _autoThrowTapesAfterReset; }
            set { _autoThrowTapesAfterReset = value; NotifyOfPropertyChange(() => AutoThrowTapesAfterReset); }
        }

        private bool _isUseNozzleNo1 = true;
        public bool UseNozzle1
        {
            get { return _isUseNozzleNo1; }
            set { _isUseNozzleNo1 = value; NotifyOfPropertyChange(() => UseNozzle1); }
        }

        private bool _isUseNozzleNo2 = true;
        public bool UseNozzle2
        {
            get { return _isUseNozzleNo2; }
            set { _isUseNozzleNo2 = value; NotifyOfPropertyChange(() => UseNozzle2); }
        }

        private bool _isUseNozzleNo3 = true;
        public bool UseNozzle3
        {
            get { return _isUseNozzleNo3; }
            set { _isUseNozzleNo3 = value; NotifyOfPropertyChange(() => UseNozzle3); }
        }

        private bool _isUseNozzleNo4 = true;
        public bool UseNozzle4
        {
            get { return _isUseNozzleNo4; }
            set { _isUseNozzleNo4 = value; NotifyOfPropertyChange(() => UseNozzle4); }
        }

        private bool _isUseVacSucCheck = true;
        public bool UseVacSucCheck
        {
            get { return _isUseVacSucCheck; }
            set { _isUseVacSucCheck = value; NotifyOfPropertyChange(() => UseVacSucCheck); }
        }
        private bool _isFeederCheck = true;
        public bool IsFeederCheck
        {
            get { return _isFeederCheck; }
            set { _isFeederCheck = value; NotifyOfPropertyChange(() => IsFeederCheck); }
        }
        private bool _isCDDCheck = false;
        public bool IsCDDCheck
        {
            get { return _isCDDCheck; }
            set { _isCDDCheck = value; NotifyOfPropertyChange(() => IsCDDCheck); }
        }

        public FeederId GetCurFeederID()
        {
            return _feederId;
        }
    }

    public class NozzleManageParam : INotifyPropertyChanged
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

        private string _nozzleSN = "";  //喷咀SN
        public string NozzleSN
        {
            get { return _nozzleSN; }
            set { _nozzleSN = value; NotifyOfPropertyChange(() => NozzleSN); }
        }

        private string _nozzleChangeTime = "";  //喷咀更换时间
        public string NozzleChangeTime
        {
            get { return _nozzleChangeTime; }
            set { _nozzleChangeTime = value; NotifyOfPropertyChange(() => NozzleChangeTime); }
        }

        private string _tinBallSN = "";  //锡球SN
        public string TinBallSN
        {
            get { return _tinBallSN; }
            set { _tinBallSN = value; NotifyOfPropertyChange(() => TinBallSN); }
        }

        private string _tinBallOpenTime = "";  //锡球打开时间
        public string TinBallOpenTime
        {
            get { return _tinBallOpenTime; }
            set { _tinBallOpenTime = value; NotifyOfPropertyChange(() => TinBallOpenTime); }
        }

        private string _tinBallChangeTime = "";  //锡球更换时间
        public string TinBallChangeTime
        {
            get { return _tinBallChangeTime; }
            set { _tinBallChangeTime = value; NotifyOfPropertyChange(() => TinBallChangeTime); }
        }

        private int _nozzleLifetime = 5000;  //允许使用寿命
        public int NozzleLifetime
        {
            get { return _nozzleLifetime; }
            set { _nozzleLifetime = value; NotifyOfPropertyChange(() => NozzleLifetime); }
        }

        private int _nozzleCleanInterval = 0;  //清洗间隔
        public int NozzleCleanInterval
        {
            get { return _nozzleCleanInterval; }
            set { _nozzleCleanInterval = value; NotifyOfPropertyChange(() => NozzleCleanInterval); }
        }

        private int _nozzleLifetimeAlarmPercent = 90;  //使用寿命预警阈值
        public int NozzleLifetimeAlarmPercent
        {
            get { return _nozzleLifetimeAlarmPercent; }
            set { _nozzleLifetimeAlarmPercent = value; NotifyOfPropertyChange(() => NozzleLifetimeAlarmPercent); }
        }

        private int _nozzleCleanAlarmPercent = 90;  //清洗间隔预警阈值
        public int NozzleCleanAlarmPercent
        {
            get { return _nozzleCleanAlarmPercent; }
            set { _nozzleCleanAlarmPercent = value; NotifyOfPropertyChange(() => NozzleCleanAlarmPercent); }
        }

        private int _nozzleCurrUseTotalCount = 0;  //当前使用总计数
        public int NozzleCurrUseTotalCount
        {
            get { return _nozzleCurrUseTotalCount; }
            set { _nozzleCurrUseTotalCount = value; NotifyOfPropertyChange(() => NozzleCurrUseTotalCount); }
        }

        private int _nozzleCurrUseCount = 0;  //当前使用次数
        public int NozzleCurrUseCount
        {
            get { return _nozzleCurrUseCount; }
            set { _nozzleCurrUseCount = value; NotifyOfPropertyChange(() => NozzleCurrUseCount); }
        }

        private int _nozzleCleanCount = 0;  //已清洗次数
        public int NozzleCleanCount
        {
            get { return _nozzleCleanCount; }
            set { _nozzleCleanCount = value; NotifyOfPropertyChange(() => NozzleCleanCount); }
        }

    }

    public class TinBallManageParam : INotifyPropertyChanged
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

        private string _tinBallSN = "";  //锡球编号
        public string TinBallSN
        {
            get { return _tinBallSN; }
            set { _tinBallSN = value; NotifyOfPropertyChange(() => TinBallSN); }
        }

        private string _addStartTime = "";  //加料起始时间
        public string AddStartTime
        {
            get { return _addStartTime; }
            set { _addStartTime = value; NotifyOfPropertyChange(() => AddStartTime); }
        }

        private string _addCount = "0";  //加料次数
        public string AddCount
        {
            get { return _addCount; }
            set { _addCount = value; NotifyOfPropertyChange(() => AddCount); }
        }
    }

    public class TapeUseInfoParam : INotifyPropertyChanged
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

        private Dictionary<string, int> _countDic = new Dictionary<string, int>();
        public Dictionary<string, int> CountDic
        {
            get { return _countDic; }
            set { _countDic = value; NotifyOfPropertyChange(() => CountDic); }
        }

        public int GetCountBySn(string tapeSn)
        {
            if (CountDic.ContainsKey(tapeSn))
            {
                return CountDic[tapeSn];
            }
            else
            {
                return 0;
            }
        }

        public void AddCountBySn(string tapeSn)
        {
            if (CountDic.ContainsKey(tapeSn))
            {
                CountDic[tapeSn]++;
            }
            else
            {
                CountDic.Add(tapeSn, 1);
            }
            IEventAggregator _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Publish(new TapeUseCountChangeMessage(), action => { Task.Run(action); });
        }

        public void ResetCountBySn(string tapeSn)
        {
            if (CountDic.ContainsKey(tapeSn))
            {
                CountDic[tapeSn] = 0;
            }
            IEventAggregator _eventAggregator = IoC.Get<IEventAggregator>();
            _eventAggregator.Publish(new TapeUseCountChangeMessage(), action => { Task.Run(action); });
        }
    }
}
