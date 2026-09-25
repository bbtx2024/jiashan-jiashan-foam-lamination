using Caliburn.Micro;
using QA.Business.Component.HIVE;
using QA.Business.Define;
using QA.Business.Interfaces;
using QA_Infrastructure;
using QA_Infrastructure.NLogOut;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace QA.Business.Model
{
    public class HiveMachineStatusStatistic
    {
        #region Property Notify
        public event PropertyChangedEventHandler PropertyChanged;
        public void ChangeProperty(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region field
        #endregion

        #region property
        #endregion

        public HiveMachineStatusStatistic()
        {
        }

        /// <summary>
        /// 读取指定时间段所有报警，并返回结果。
        /// 报警以结束时间点判定是否在该时间段内。
        /// </summary>
        public Dictionary<string, double> ReadErrorStatistic(DateTime startTime, DateTime endTime)
        {
            Dictionary<string, double> dicErrorCollections = new Dictionary<string, double>();
            //将时间段进行拆分，有跨天和不跨天两种情况
            if (startTime.Date == endTime.Date)
            {
                ReadErrorStatisticOneDay(startTime, endTime, dicErrorCollections);
            }
            else
            {
                //处理两端
                ReadErrorStatisticOneDay(startTime, startTime.Date.AddDays(1), dicErrorCollections);
                startTime = startTime.Date.AddDays(1);
                ReadErrorStatisticOneDay(endTime.Date, endTime, dicErrorCollections);
                endTime = endTime.Date;
                //处理中间
                while (startTime != endTime)
                {
                    ReadErrorStatisticOneDay(startTime, startTime.Date.AddDays(1), dicErrorCollections);
                    startTime = startTime.Date.AddDays(1);
                }
            }
            return dicErrorCollections;
        }

        /// <summary>
        /// 读取某一天内指定时间段所有报警，并将结果添加到dicErrorCollections。
        /// 参数必须位于同一天（到第二天零点是可以的）。
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        private void ReadErrorStatisticOneDay(DateTime startTime, DateTime endTime, Dictionary<string, double> dicErrorCollections)
        {
            //string dir = $@"{AppDomain.CurrentDomain.BaseDirectory}/Datas/Hive";
            string dir = @"D:\QKProject\Data\Hive\ErrorData";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string filePath = $@"{dir}/ErrorData_{startTime.ToString("yyyy-MM-dd")}.csv";
            if (!File.Exists(filePath))
            {
                return;
            }
            try
            {
                using (StreamReader sr = new StreamReader(filePath))
                {
                    sr.ReadLine();//第一行不要
                    string s;
                    while ((s = sr.ReadLine()) != null)
                    {
                        //开始时间,结束时间,错误类型,错误代码,详细信息
                        HiveErrorCodeInfo info = new HiveErrorCodeInfo(s);
                        if (!info.isValid)
                        {
                            continue;
                        }
                        //以结束时间为基准
                        if (startTime <= info.endTime && info.endTime <= endTime)
                        {
                            if (!dicErrorCollections.ContainsKey(info.errorName))
                            {
                                dicErrorCollections.Add(info.errorName, 1);
                            }
                            else
                            {
                                dicErrorCollections[info.errorName]++;
                            }
                        }
                    }
                    sr.Close();
                }
                //对Dic排序
                List<KeyValuePair<string, double>> listkeyValuePairs = new List<KeyValuePair<string, double>>(dicErrorCollections);
                listkeyValuePairs.Sort(delegate (KeyValuePair<string, double> s1, KeyValuePair<string, double> s2)
                {
                    return s1.Value.CompareTo(s2.Value);
                });
                dicErrorCollections.Clear();
                foreach (KeyValuePair<string, double> pair in listkeyValuePairs)
                {
                    dicErrorCollections.Add(pair.Key, pair.Value);
                }
            }
            catch (Exception ex)
            {
                NLogTrace.LogOut(EN_WARN_LEVEL.Error, ex.ToString(), En_Logout_Type.Exception);
            }
        }
    }

    public class HiveMachineStatus
    {
        public double[] machineStatusTime = new double[6];

        public void AddTime(EN_HiveStatus status, double seconds)
        {
            machineStatusTime[(int)status] += seconds;
        }

        public void Clear()
        {
            for (int i = 0; i < machineStatusTime.Length; i++)
            {
                machineStatusTime[i] = 0;
            }
        }
    }

    public class HiveErrorCodeInfo
    {
        public DateTime startTime = DateTime.Now;
        public DateTime endTime = DateTime.Now;
        public string errorName = "";
        public bool isValid = false;

        public HiveErrorCodeInfo(string line)
        {
            try
            {
                string[] data = line.Split(',');
                startTime = DateTime.Parse(data[0]);
                endTime = DateTime.Parse(data[1]);
                errorName = data[2];
                isValid = true;
            }
            catch (Exception) { }
        }
    }

    public class HiveParamDashBoard : PropertyChangedBase
    {
        private string _paramName;
        public string ParamName
        {
            get => _paramName;
            set
            {
                _paramName = value;
                NotifyOfPropertyChange(() => ParamName);
            }
        }

        private string _paramValue;
        public string ParamValue
        {
            get => _paramValue;
            set
            {
                _paramValue = value;
                NotifyOfPropertyChange(() => ParamValue);
            }
        }

        private string _paramLSL;
        public string ParamLSL
        {
            get => _paramLSL;
            set
            {
                _paramLSL = value;
                NotifyOfPropertyChange(() => ParamLSL);
            }
        }

        private string _paramUSL;
        public string ParamUSL
        {
            get => _paramUSL;
            set
            {
                _paramUSL = value;
                NotifyOfPropertyChange(() => ParamUSL);
            }
        }
    }
}
