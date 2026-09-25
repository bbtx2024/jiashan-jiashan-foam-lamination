/*******************************************************
 * 快克智能装备股份有限公司
 * 作者：胡勇
 * 创建日期：2022-03-16
 * 说明：（其他功能参数）
 * 版本号：1.0.0 
 * 修改记录：日期 + 修改内容
 * 
*******************************************************/
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Caliburn.Micro;
using QA.Business.Interfaces;
using QA.Business.Manager;
using QA_Infrastructure;

namespace QA.Business.Component.OtherSetting
{
    [SaveParam(FileType.Binary)]
    [Serializable]
    [Description("其他设置")]
    public class OtherSettingParam : IParam
    {
        [Category("0.启用"), DisplayName("模块启用")]
        [Browsable(false)]
        public override bool BUse { get; set; }

        [Category("语言"), DisplayName("语言")]
        [Browsable(false)]
        public int LanguageIndex { get; set; } = 0;

        [Category("1.其他设置"), DisplayName("允许使用用户名密码登录")]
        [ReadOnly(true)]
        public bool EnableCommonLogin { get; set; } = true;

        [Category("1.其他设置"), DisplayName("手动设定软件版本号")]
        public string ManualSoftwareVersion { get; set; } = "";

        [Category("1.其他设置"), DisplayName("软件版本号")]
        public string SoftwareVersion
        {
            get
            {
                if (!string.IsNullOrEmpty(ManualSoftwareVersion))
                {
                    return ManualSoftwareVersion;
                }
                return $"QK_{IoC.Get<CacheParamManager>().HomeUiParam.Statistic.CurrSoftwareVersion}.1.0.1_{new FileInfo(Process.GetCurrentProcess().MainModule.FileName).LastWriteTime.ToString("yyMMdd")}_POR";
            }
        }

        [Category("1.其他设置"), DisplayName("是否为大线")]
        public bool IsLargeLine { get; set; } = false;

        [Category("1.其他设置"), DisplayName("是否为量产")]
        public bool IsMP { get; set; } = true;

        [Category("2.吸嘴Z高度相对补偿"), DisplayName("2#相对于1#补偿"), Description("mm")]
        public float ZOffset2 { get; set; } = 0;

        [Category("2.吸嘴Z高度相对补偿"), DisplayName("3#相对于1#补偿"), Description("mm")]
        public float ZOffset3 { get; set; } = 0;

        [Category("2.吸嘴Z高度相对补偿"), DisplayName("4#相对于1#补偿"), Description("mm")]
        public float ZOffset4 { get; set; } = 0;

        [Category("3.吸嘴取料清料设定"), DisplayName("吸嘴开吸后等待多久开飞达吹气"), Description("ms")]
        public int GetTapeBeforeFeederBreakTime { get; set; } = 50;

        [Category("3.吸嘴取料清料设定"), DisplayName("飞达吹气持续时间"), Description("ms")]
        public int GetTapeFeederBreakTime { get; set; } = 100;

        //[Category("3.吸嘴取料清料设定"), DisplayName("吸嘴取料后检测真空吸等待时间"), Description("ms")]
        //public int CheckVacWaitTime { get; set; } = 50;

        [Category("3.吸嘴取料清料设定"), DisplayName("吸嘴取料停留时间"), Description("ms")]
        public int GetTapeWaitTime { get; set; } = 200;

        [Category("3.吸嘴取料清料设定"), DisplayName("吸嘴清料吹气时间"), Description("ms")]
        public int OpenBreakTime { get; set; } = 20;

        [Category("3.吸嘴取料清料设定"), DisplayName("单吸嘴连续取料失败报警次数")]
        public int OneNozzleGetTapeFailCount { get; set; } = 3;

        [Category("3.吸嘴取料清料设定"), DisplayName("所有吸嘴取料失败报警次数")]
        public int NozzlesGetTapeFailCount { get; set; } = 5;




        //[Category("4.吸嘴取料偏移"), DisplayName("是否启用偏移")]
        //public bool IsOpenExcursion { get; set; } = true;
        //[Category("4.吸嘴取料偏移"), DisplayName("吸嘴1-X")]
        //public float Nozzle1ExcursionX { get; set; }
        //[Category("4.吸嘴取料偏移"), DisplayName("吸嘴1-Y")]
        //public float Nozzle1ExcursionY { get; set; }
        //[Category("4.吸嘴取料偏移"), DisplayName("吸嘴1-R")]
        //public float Nozzle1ExcursionR { get; set; }
        //[Category("4.吸嘴取料偏移"), DisplayName("吸嘴2-X")]
        //public float Nozzle2ExcursionX { get; set; }
        //[Category("4.吸嘴取料偏移"), DisplayName("吸嘴2-Y")]
        //public float Nozzle2ExcursionY { get; set; }
        //[Category("4.吸嘴取料偏移"), DisplayName("吸嘴2-R")]
        //public float Nozzle2ExcursionR { get; set; }
        //[Category("4.吸嘴取料偏移"), DisplayName("吸嘴3-X")]
        //public float Nozzle3ExcursionX { get; set; }
        //[Category("4.吸嘴取料偏移"), DisplayName("吸嘴3-Y")]
        //public float Nozzle3ExcursionY { get; set; }
        //[Category("4.吸嘴取料偏移"), DisplayName("吸嘴3-R")]
        //public float Nozzle3ExcursionR { get; set; }
        //[Category("4.吸嘴取料偏移"), DisplayName("吸嘴4-X")]
        //public float Nozzle4ExcursionX { get; set; }
        //[Category("4.吸嘴取料偏移"), DisplayName("吸嘴4-Y")]
        //public float Nozzle4ExcursionY { get; set; }
        //[Category("4.吸嘴取料偏移"), DisplayName("吸嘴4-R")]
        //public float Nozzle4ExcursionR { get; set; }
    }
}

