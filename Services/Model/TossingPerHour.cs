using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using QA.Business.Define;
using QA.Business.Steps;

namespace QA.Business.Model
{
    public class TossingPerHour
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
        /// 一天总产量路径
        /// </summary>
        /// D:\Project\RunInfo\
        private string _outputPath = $"D:\\QKProject\\RunInfo\\DayOutputPerHour";

        public string OutputPath
        {
            get
            {
                if (!Directory.Exists(_outputPath))
                {
                    Directory.CreateDirectory(_outputPath);
                }
                return _outputPath;
            }
        }

        /// <summary>
        /// 一天NG产量路径
        /// </summary>
        private string _tossingPath = $"D:\\QKProject\\RunInfo\\DayTossingPerHour";
        public string TossingPath
        {
            get
            {
                if (!Directory.Exists(_tossingPath))
                {
                    Directory.CreateDirectory(_tossingPath);
                }
                return _tossingPath;
            }
        }



        private int _okCount;
        public int OkCount
        {
            get { return _okCount; }
            set { _okCount = value; NotifyOfPropertyChange(() => OkCount); }
        }

        private int _ngCount;
        public int NgCount
        {
            get { return _ngCount; }
            set { _ngCount = value; NotifyOfPropertyChange(() => NgCount); }
        }

        public int TotalCount
        {
            get { return OkCount + NgCount; }
        }
        public float OkRate
        {
            get { return TotalCount == 0 ? 0.0f : OkCount * 1.0f / TotalCount; }
        }
        public void Clear()
        {
            OkCount = 0;
            NgCount = 0;
            for (int i = 0; i < 24; i++)
            {
                DayTossingQuantitys[i].Clear();
            }
            NotifyOfPropertyChange(() => TotalCount);
            NotifyOfPropertyChange(() => OkRate);
        }

        private int _nozzelNgCount;
        public int NozzelNgCount
        {
            get { return _nozzelNgCount; }
            set { _nozzelNgCount = value; }
        }

        private int _reasonNgCount;
        public int ReasonNgCount
        {
            get { return _reasonNgCount; }
            set { _reasonNgCount = value; }
        }


        public TossingPerHour(DateTime dateTime)
        {
            for (int i = 0; i < 24; i++)
            {
                _dayTossingQuantitys[i] = new TossingQuantity();
            }
            for (int i = 0; i < 7; i++)
            {
                _weekTossingQuantitys[i] = new TossingQuantity();
            }
            for (int i = 0; i < 4; i++)
            {
                _nozzelTossingQuantitys[i] = new TossingQuantity();
            }
            for (int i = 0; i < 2; i++)
            {
                _reasonTossingQuantitys[i] = new TossingQuantity();
            }

            ReadOutputPerHour(dateTime);
            ReadTossingPerHour(dateTime);
        }

        //一天抛料数据
        private TossingQuantity[] _dayTossingQuantitys = new TossingQuantity[24];
        public TossingQuantity[] DayTossingQuantitys
        {
            get { return _dayTossingQuantitys; }
            set { _dayTossingQuantitys = value; NotifyOfPropertyChange(() => DayTossingQuantitys); }
        }

        //一周抛料数据
        private TossingQuantity[] _weekTossingQuantitys = new TossingQuantity[7];
        public TossingQuantity[] WeekTossingQuantitys
        {
            get { return _weekTossingQuantitys; }
            set { _weekTossingQuantitys = value; NotifyOfPropertyChange(() => WeekTossingQuantitys); }
        }

        //吸嘴抛料数据
        private TossingQuantity[] _nozzelTossingQuantitys = new TossingQuantity[4];
        public TossingQuantity[] NozzelTossingQuantitys
        {
            get { return _nozzelTossingQuantitys; }
            set { _nozzelTossingQuantitys = value; NotifyOfPropertyChange(() => NozzelTossingQuantitys); }
        }

        //抛料原因数据
        private TossingQuantity[] _reasonTossingQuantitys = new TossingQuantity[2];
        public TossingQuantity[] ReasonTossingQuantitys
        {
            get { return _reasonTossingQuantitys; }
            set { _reasonTossingQuantitys = value; NotifyOfPropertyChange(() => ReasonTossingQuantitys); }
        }

        private bool SaveOutputPerHour()
        {
            if (!Directory.Exists(_outputPath + "\\" + DateTime.Now.ToString("yyyyMM")))
            {
                Directory.CreateDirectory(_outputPath + "\\" + DateTime.Now.ToString("yyyyMM"));
            }
            string resultPath = _outputPath + "\\" + DateTime.Now.ToString("yyyyMM") + "\\" + DateTime.Now.ToString("yyyy-MM-dd") + ".csv";
            try
            {
                using (StreamWriter file = new StreamWriter(resultPath, false))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("时间");
                    sb.Append(",");
                    sb.Append("OK产量");
                    sb.Append(",");
                    sb.Append("NG产量");
                    sb.Append(",");
                    sb.Append("总产量");
                    sb.Append(",");
                    sb.Append("良率");
                    sb.Append("\r\n");
                    for (int i = 0; i < DayTossingQuantitys.Length; i++)
                    {
                        sb.Append($"{i}.00~{i + 1}.00");
                        sb.Append(",");
                        sb.Append(DayTossingQuantitys[i].OkCount.ToString());
                        sb.Append(",");
                        sb.Append(DayTossingQuantitys[i].NgCount.ToString());
                        sb.Append(",");
                        sb.Append(DayTossingQuantitys[i].TotalCount.ToString());
                        sb.Append(",");
                        sb.Append(DayTossingQuantitys[i].OkRate * 100 + "%");
                        sb.Append("\r\n");
                    }
                    sb.Append("0.00~24.00");
                    sb.Append(",");
                    sb.Append(OkCount);
                    sb.Append(",");
                    sb.Append(NgCount);
                    sb.Append(",");
                    sb.Append(TotalCount);
                    sb.Append(",");
                    sb.Append(OkRate * 100 + "%");
                    sb.Append("\r\n");
                    file.Write(sb.ToString());
                    file.Close();
                    file.Dispose();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool SaveTossingPerHour(CarrierStatus carrierStatus, int cavity, int nozzel, EN_TossingCode eN_TossingCode)
        {
            if (!Directory.Exists(_tossingPath + "\\" + DateTime.Now.ToString("yyyyMM")))
            {
                Directory.CreateDirectory(_tossingPath + "\\" + DateTime.Now.ToString("yyyyMM"));
            }
            string resultPath = _tossingPath + "\\" + DateTime.Now.ToString("yyyyMM") + "\\" + DateTime.Now.ToString("yyyy-MM-dd") + ".csv";
            try
            {

                if (!File.Exists(resultPath))
                {
                    using (StreamWriter file = new StreamWriter(resultPath, false))
                    {
                        file.WriteLine("时间,卷料号,CarrierSN,SIPSN,穴位号,吸嘴号,是否抛料,抛料类型");
                    }
                }
                using (StreamWriter file = new StreamWriter(resultPath, true))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(DateTime.Now.ToString());
                    sb.Append(",");
                    sb.Append(carrierStatus.Tape_SN[cavity]);
                    sb.Append(",");
                    sb.Append(carrierStatus.carrierSN.ToString());
                    sb.Append(",");
                    sb.Append(carrierStatus.sipSN[cavity].ToString());
                    sb.Append(",");
                    sb.Append(cavity.ToString());
                    sb.Append(",");
                    sb.Append(nozzel.ToString());
                    sb.Append(",");
                    sb.Append("1");
                    sb.Append(",");
                    sb.Append(eN_TossingCode.ToString());
                    file.WriteLine(sb.ToString());
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool ReadOutputPerHour(DateTime dateTime)
        {
            string resultPath = _outputPath + "\\" + dateTime.ToString("yyyyMM") + "\\" + dateTime.ToString("yyyy-MM-dd") + ".csv";
            if (!File.Exists(resultPath))
            {
                return false;
            }
            try
            {
                using (StreamReader file = new StreamReader(resultPath))
                {
                    string str = file.ReadLine();
                    OkCount = 0;
                    NgCount = 0;
                    for (int i = 0; i < 24; i++)
                    {
                        str = file.ReadLine();
                        string[] strs = str.Split(',');
                        DayTossingQuantitys[i].OkCount = Convert.ToInt32(strs[1]);
                        DayTossingQuantitys[i].NgCount = Convert.ToInt32(strs[2]);
                        if (file.Peek() == -1)
                        {
                            break;
                        }
                        OkCount += DayTossingQuantitys[i].OkCount;
                        NgCount += DayTossingQuantitys[i].NgCount;
                    }
                    file.Close();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool ReadTossingPerHour(DateTime dateTime)
        {
            string resultPath = _tossingPath + "\\" + dateTime.ToString("yyyyMM") + "\\" + dateTime.ToString("yyyy-MM-dd") + ".csv";
            if (!File.Exists(resultPath))
            {
                return false;
            }
            try
            {
                using (StreamReader file = new StreamReader(resultPath))
                {
                    string str = file.ReadLine();
                    for (int i = 0; i < 4; i++)
                    {
                        NozzelTossingQuantitys[i].NozzelNgCount = 0;
                    }
                    for (int i = 0; i < 2; i++)
                    {
                        ReasonTossingQuantitys[i].ReasonNgCount = 0;
                    }

                    while (true)
                    {
                        str = file.ReadLine();
                        string[] strs = str.Split(',');
                        //筛选不同吸嘴的NG数据
                        if (Convert.ToInt32(strs[5]) == 1)
                        {
                            NozzelTossingQuantitys[0].NozzelNgCount++;
                        }
                        else if (Convert.ToInt32(strs[5]) == 2)
                        {
                            NozzelTossingQuantitys[1].NozzelNgCount++;
                        }
                        else if (Convert.ToInt32(strs[5]) == 3)
                        {
                            NozzelTossingQuantitys[2].NozzelNgCount++;
                        }
                        else
                        {
                            NozzelTossingQuantitys[3].NozzelNgCount++;
                        }
                        //筛选不同抛料原因的数据
                        if (strs[7] == "DefeatVacuum")
                        {
                            ReasonTossingQuantitys[0].ReasonNgCount++;
                        }
                        else
                        {
                            ReasonTossingQuantitys[1].ReasonNgCount++;
                        }
                        if (file.Peek() == -1)
                        {
                            break;
                        }
                    }
                    file.Close();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void AddOne(bool isOk, CarrierStatus carrierStatus = null, int cavity = 0, int nozzel = 0, EN_TossingCode eN_TossingCode = EN_TossingCode.MaterialDeflect)
        {
            if (isOk)
            { OkCount++; }
            else
            { NgCount++; }
            NotifyOfPropertyChange(() => TotalCount);
            NotifyOfPropertyChange(() => OkRate);
            int i = DateTime.Now.Hour;
            DayTossingQuantitys[i].AddOne(isOk);

            SaveOutputPerHour();//只存贮OK NG数量
            if (!isOk)
            {
                SaveTossingPerHour(carrierStatus, cavity, nozzel, eN_TossingCode);//存储NG抛料信息
            }
        }

    }

    public class TossingQuantity
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

        private int _okCount;
        public int OkCount
        {
            get { return _okCount; }
            set { _okCount = value; NotifyOfPropertyChange(() => OkCount); }
        }

        private int _ngCount;
        public int NgCount
        {
            get { return _ngCount; }
            set { _ngCount = value; NotifyOfPropertyChange(() => NgCount); }
        }

        public int TotalCount
        {
            get { return OkCount + NgCount; }
        }

        public float OkRate
        {
            get { return TotalCount == 0 ? 0.0f : OkCount * 1.0f / TotalCount; }
        }

        private int _nozzelNgCount;
        public int NozzelNgCount
        {
            get { return _nozzelNgCount; }
            set { _nozzelNgCount = value; }
        }

        private int _reasonNgCount;
        public int ReasonNgCount
        {
            get { return _reasonNgCount; }
            set { _reasonNgCount = value; }
        }

        public void AddOne(bool isOk)
        {
            if (isOk)
            { OkCount++; }
            else
            { NgCount++; }
            NotifyOfPropertyChange(() => TotalCount);
            NotifyOfPropertyChange(() => OkRate);
        }

        public void Clear()
        {
            OkCount = 0;
            NgCount = 0;
            NotifyOfPropertyChange(() => TotalCount);
            NotifyOfPropertyChange(() => OkRate);
        }




    }
}
