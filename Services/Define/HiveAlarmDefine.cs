using System.Collections.Generic;

namespace QA.Business.Define
{
    public class HiveAlarmDefine
    {
        public static Dictionary<int, string> AlarmList = new Dictionary<int, string>()
        {
            {100," F01ESSE-01-03,Safety Error,Emergency stop"} ,
            {101," T01IPPO-01-03,Scanning Error,Code scan failure"},
            {102," S02LOSE-01-03,Sensor Error,Feeder feed failure"},
            {103," S02LOSE-03-03,Sensor Error,Feeder feed failure"},
            {104," S02LOSE-02-03,Sensor Error,Feeder not locked"},
            {105," S02LOSE-04-03,Sensor Error,Feeder not locked"},
            {106," S02SSVV-01-03,Sensor Error,Sucker fetch failure"},
            {107," S02SSVV-02-03,Sensor Error,Sucker fetch failure"},
            {108," V01VSDV-01-03,Vision Error,Visual process failure"},
            {109," V01VSDV-02-03,Vision Error,Visual process failure"},
            {110," V01VSDV-03-03,Vision Error,Visual process failure"},
            {111," V03VSDV-01-03,Vision Error,Camera disconnect"},
            {112," S01SSSL-01-03,Sensor Error,Specified pressure not found"},
            {113," C02PNCY-23-03,Cylinder Error,L1 pressure retaining cylinder 4 shrinkage is not in place"},
            {114," C02PNCY-24-03,Cylinder Error,L1 pressure retaining cylinder 4 extension is not in place"},
            {115," C02PNCY-25-03,Cylinder Error,L1 pressure retaining cylinder 4 shrinkage is not in place"},
            {116," C02PNCY-26-03,Cylinder Error,L1 pressure retaining cylinder 4 extension is not in place"},

            //上方上位机Hive报警，下方PLCHive报警
            {64000,"C01PNCY-01-03,Cylinder Error,anomaly at the origin of the gas tank"},
            {64001,"C02PNCY-01-03,Cylinder Error,anomaly at the origin of the gas tank"},
            {64002,"C01PNCY-02-03,Motion Error  ,anomaly to fit the top-up cylinder"},
            {64003,"C02PNCY-02-03,Cylinder Error,anomaly to fit the top-up cylinder"},
            {64004,"C01PNCY-03-03,Cylinder Error,Adherent blockage causing malfunction at the cylinder's origin."},
            {64005,"C02PNCY-03-03,Cylinder Error,Adherent blockage causing malfunction at the cylinder's origin."},
            {64006,"C01PNCY-04-03,Cylinder Error,Abnormal condition at the origin point of the pressure-holding lifting cylinder."},
            {64007,"C02PNCY-04-03,Cylinder Error,Abnormal condition at the origin point of the pressure-holding lifting cylinder."},
            {64016,"C01PNCY-05-03,Cylinder Error,Abnormal Condition at the Origin of the Pressure-Holding Block Cylinder"},
            {64017,"C02PNCY-05-03,Cylinder Error,Abnormal movement in the pressure-holding blocking cylinder"},
            {64018,"C01PNCY-06-03,Cylinder Error,The one-breath-up-down-a-barrel is abnormal"},
            {64019,"C02PNCY-06-03,Cylinder Error,Abnormal movement detected in Cylinder No. 1 during the up-down pressure-holding operation"},
            {64020,"C01PNCY-07-03,Cylinder Error,Abnormal condition detected at the origin point of Cylinder No. 2 during up-down pressure-holding operation"},
            {64021,"C02PNCY-07-03,Cylinder Error,Abnormal movement detected in Cylinder No. 2 during the up-down pressure-holding operation"},
            {64022,"C01PNCY-08-03,Cylinder Error,Abnormal condition detected at the origin point of Cylinder No. 3 during up-down pressure-holding operation"},
            {64023,"C02PNCY-08-03,Cylinder Error,Abnormal movement detected in Cylinder No. 3 during the up-down pressure-holding operation"},
            {64032,"C01PNCY-09-03,Cylinder Error,Abnormal condition detected at the origin point of Cylinder No. 4 during up-down pressure-holding operation"},
            {64033,"C02PNCY-09-03,Cylinder Error,Abnormal movement detected in Cylinder No. 4 during the up-down pressure-holding operation"},
            {64034,"C01PNCY-10-03,Cylinder Error,Top CCD camera cylinder origin point abnormal"},
            {64035,"C02PNCY-10-03,Cylinder Error,Top CCD camera cylinder actuating point abnormal"},
            {64048,"M02PLMP-01-03,Motion Error,Axle-1 is off alert"},
            {64049,"M02PLMP-02-03,Motion Error,Axle 2 is calling the police"},
            {64050,"M02PLMP-03-03,Motion Error,Backstream Axis"},
            {64051,"M02PLMP-04-03,Motion Error,Flow line wide axle one"},
            {64052,"M02PLMP-05-03,Motion Error,Flow line to wide axle two"},
            {64053,"M02PLMP-06-03,Motion Error,X-AXIS ALARM"},
            {64054,"M02PLMP-07-03,Motion Error,Z-AXIS ALARM"},
            {64080,"F01EEMP-09-03,Motion Error,We're in a state of emergency"},
            {64081,"U01VGVV-07-03,Vacuum Error,Device air pressure alarm"},
            {64082,"F02PLMP-11-03,Motion Error,Security gate 1-2"},
            {64083,"F02PLMP-12-03,Motion Error,Security gate 3-4"},
            {64084,"F02PLMP-13-03,Motion Error,Security gate 5-6"},
            {64085,"F02PLMP-14-03,Motion Error,Security gate 7-8"},
            {64098,"S02SSMP-01-03,Sensor Error,There's an anomaly at the waiting place"},
            {64099,"S02SSMP-02-03,Sensor Error,Unusual dementia test for the toddlers"},
            {64100,"S02SSMP-03-03,Sensor Error,There's something wrong with the adhesive"},
            {64101,"S02SSMP-04-03,Sensor Error,The top-up of the binder is abnormal"},
            {64102,"S02SSMP-05-03,Sensor Error,There's an anomaly in the pressure level test"},
            {64103,"S02SSMP-06-03,Sensor Error,The top-up of the ballast loader is abnormal "},
            {64112,"F99PLDE-01-03,Cylinder Error,Failed to initialize reset device"},
            {64113,"U01VGVV-01-03,Vacuum Error,Negative pressure alarm"},
            {64114,"F02PLMP-15-03,Motion Error,Security raster anomaly alert"},
            {64115,"C02PNCY-02-03,Cylinder Error,All Fester's out of order"},
            {64116,"M08PSMP-01-09,Motion Error,The number of use of No. 1 pressure retaining head has reached"},
            {64117,"M08PSMP-02-09,Motion Error,The number of use of No. 2 pressure retaining head has reached"},
            {64118,"M08PSMP-03-09,Motion Error,The number of use of No. 3 pressure retaining head has reached"},
            {64119,"M08PSMP-04-09,Motion Error,The number of use of No. 4 pressure retaining head has reached"},


        };
    }

    public enum EN_TossingCode
    {
        尺寸超上限 = 1,//尺寸超上限
        尺寸超下限,//尺寸超下限
        角度超限,//角度超限
        材料脏污,//材料脏污
        MaterialDeflect,//材料偏斜
        材料缺陷,//材料缺陷
        螺丝角度,//螺丝角度
        螺丝真空,//螺丝真空
        螺丝GapNG,//螺丝GapNG
        螺丝扭力,//螺丝扭力
        参数超上限,//参数超上限
        参数超下限,//参数超下限
        超时NG,//超时NG
        BreakVacuum,//破真空
        MES未过站,//MES未过站
        ScanCodeNG,//扫码NG
        压力异常,//压力异常
    }
}
