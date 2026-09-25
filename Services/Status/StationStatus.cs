/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：胡勇
 * 创建日期：2022-03-15
 * 说明：（各工站状态和产品信息）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/
using System;
using System.Collections.Generic;
using QA.Business.Define;
using QA_Infrastructure.GeneralTool;
using QA_Infrastructure.NLogOut;

namespace QA.Business.Status
{
    public class StationStatus
    {
        private object lockobj = new object();

        public List<UnitInfo> lstPorcessUnitInfos = new List<UnitInfo>();  //机台处理队列中的产品信息

        public StationStatus()
        {

        }

        public bool IsProcessContain(string sn)
        {
            foreach (UnitInfo unit in lstPorcessUnitInfos)
            {
                if (unit.UnitSN == sn)
                {
                    return true;
                }
            }
            return false;
        }

        public string GetProcessListSnInfo()
        {
            //lock(lockobj)
            {
                string info = "";
                foreach (UnitInfo unit in lstPorcessUnitInfos)
                {
                    info += $"{unit.UnitSN},";
                }
                return info;
            }
        }

        public void ClearProcessList()
        {
            lock (lockobj)
            {
                lstPorcessUnitInfos.Clear();
            }
        }

        public void AddOneUnit(UnitInfo unit)
        {
            lock (lockobj)
            {
                lstPorcessUnitInfos.Add(unit);
                NLogTrace.LogOut(QA_Infrastructure.EN_WARN_LEVEL.Info, $"添加工站信息：" +
                    $"UnitSN：{unit.UnitSN}，ProductSns:{GeneralTools.GetStringArrayString(unit.ProductSns.ToArray())}," +
                    $"Needwork:{GeneralTools.GetStringArrayString(unit.Needworks.ToArray())}," +
                    $"WorkPlace:{unit.WorkPlace.ToString()}");

            }
        }

        public UnitInfo GetFirstUnit(EN_UNIT_PLACE place)
        {
            lock (lockobj)
            {
                foreach (UnitInfo unit in lstPorcessUnitInfos)
                {
                    if (unit.WorkPlace == place)
                        return unit;

                }
                return null;
            }
        }

        public void RemoveUnit(UnitInfo rmunit)
        {
            lock (lockobj)
            {
                if (lstPorcessUnitInfos.Contains(rmunit))
                    lstPorcessUnitInfos.Remove(rmunit);
            }
        }
    }
    //物料信息
    [Serializable]
    public class UnitInfo
    {
        public string UnitSN;                   //载具SN
        public List<string> ProductSns = new List<string>();//多个产品Sn
        public List<string> FlexSns = new List<string>();
        public List<string> Needworks = new List<string>();

        public EN_UNIT_PLACE WorkPlace = EN_UNIT_PLACE.Null;   //当前工位
        public EN_UNIT_LR WorkLeftOrRight = EN_UNIT_LR.Null;   //左工位工作或右工位工作
        public DateTime StartTime;             //开始处理时间
        public DateTime EndTime;               //结束处理时间
        public List<PointInfo> Point_XYZ = new List<PointInfo>();//焊接坐标
        public double LaserHeightCT = 0;         //测高CT
        public double VisionCT = 0;              //视觉CT
        public double SolderCT = 0;             //激光焊接CT
        public List<float> RecheckDates = new List<float>(); //复检数据，覆盖率
        public UnitInfo()
        {

        }
    }

    /// <summary>
    /// 点位坐标xyz
    /// </summary>
    [Serializable]
    public class PointInfo
    {
        public float X = 0;
        public float Y = 0;
        public float Z = 0;
    }
}
