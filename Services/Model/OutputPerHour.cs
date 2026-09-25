using System;
using System.ComponentModel;
using System.IO;
using System.Linq.Expressions;
using System.Text;

namespace QA.Business.Model
{
    public class OutputPerHour
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

        //private string _basePath = AppDomain.CurrentDomain.BaseDirectory + "OutputPerHour";
        private string _basePath = @"D:\QKProject\RunInfo\OutputPerHour";
        public string BasePath
        {
            get
            {
                if (!Directory.Exists(_basePath))
                {
                    Directory.CreateDirectory(_basePath);
                }
                return _basePath;
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
                ProductionQuantitys[i].Clear();
            }
            NotifyOfPropertyChange(() => TotalCount);
            NotifyOfPropertyChange(() => OkRate);
        }
        public OutputPerHour(DateTime dateTime)
        {
            for (int i = 0; i < 24; i++)
            {
                _productionQuantitys[i] = new ProductionQuantity();
            }
            ReadOutputPerHour(dateTime);
        }
        private ProductionQuantity[] _productionQuantitys = new ProductionQuantity[24];
        public ProductionQuantity[] ProductionQuantitys
        {
            get { return _productionQuantitys; }
            set { _productionQuantitys = value; NotifyOfPropertyChange(() => ProductionQuantitys); }
        }
        private bool SaveOutputPerHour()
        {
            if (!Directory.Exists(_basePath + "\\" + DateTime.Now.ToString("yyyyMM")))
            {
                Directory.CreateDirectory(_basePath + "\\" + DateTime.Now.ToString("yyyyMM"));
            }
            string resultPath = _basePath + "\\" + DateTime.Now.ToString("yyyyMM") + "\\" + DateTime.Now.ToString("yyyy-MM-dd") + ".csv";
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
                    for (int i = 0; i < ProductionQuantitys.Length; i++)
                    {
                        sb.Append($"{i}.00~{i + 1}.00");
                        sb.Append(",");
                        sb.Append(ProductionQuantitys[i].OkCount.ToString());
                        sb.Append(",");
                        sb.Append(ProductionQuantitys[i].NgCount.ToString());
                        sb.Append(",");
                        sb.Append(ProductionQuantitys[i].TotalCount.ToString());
                        sb.Append(",");
                        sb.Append(ProductionQuantitys[i].OkRate * 100 + "%");
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
        public bool ReadOutputPerHour(DateTime dateTime)
        {
            string resultPath = _basePath + "\\" + dateTime.ToString("yyyyMM") + "\\" + dateTime.ToString("yyyy-MM-dd") + ".csv";
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
                        ProductionQuantitys[i].OkCount = Convert.ToInt32(strs[1]);
                        ProductionQuantitys[i].NgCount = Convert.ToInt32(strs[2]);
                        if (file.Peek() == -1)
                        {
                            break;
                        }
                        OkCount += ProductionQuantitys[i].OkCount;
                        NgCount += ProductionQuantitys[i].NgCount;
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
        public void AddOne(bool isOk)
        {
            if (isOk)
            { OkCount++; }
            else
            { NgCount++; }
            NotifyOfPropertyChange(() => TotalCount);
            NotifyOfPropertyChange(() => OkRate);
            int i = DateTime.Now.Hour;
            ProductionQuantitys[i].AddOne(isOk);
            SaveOutputPerHour();
        }
    }
    public class ProductionQuantity
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
