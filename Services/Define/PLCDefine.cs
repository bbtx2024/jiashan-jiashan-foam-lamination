namespace QA.Business.Define
{
    public enum EN_PLC_PCRunStatus
    {
        Run = 1,
        EStop = 2,
        Reseting = 3,
        Pause = 4,
        ResetComplete = 5,
        WorkComplete = 6,
        Idle = 9,
    }

    public enum EN_PLC_ScanResult
    {
        ScanOK = 1,
        ScanNG = 2,
    }

    public enum EN_PLC_SetAlarm
    {
        Normal = 0,     //清除报警
        Warning = 1,    //闪灯
        Error = 2,      //闪灯加蜂鸣器
    }

    public enum EN_PLC_UpenderRead
    {
        AllowPlace = 1,
        AllowPick = 2,
    }

    public enum EN_PLC_UpenderWrite
    {
        PlaceOk = 1,
        PickOk = 2,
    }

    public enum EN_PLC_BendRead
    {
        AllowPlace = 1,
        AllowPick = 2,
        AllowUp = 3,
    }

    public enum EN_PLC_BendWrite
    {
        PlaceOk = 1,
        DownOk = 2,
        PickOk = 3,
    }

}
