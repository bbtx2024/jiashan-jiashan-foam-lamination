using System;

namespace QA.Business.Define
{
    public enum En_DeviceStatus
    {
        Connected,
        DisConnected,
        Disabled,
    }

    #region PDCA
    public enum En_PdcaMode
    {
        PROD = 0,
        CPK = 1,
        GRR = 2,
        SCS = 3,
        IT_BG_C = 4,
        GMC = 11,
    }
    #endregion

    #region DashBoard
    public enum EN_DASH_FC_STATE
    {
        AUTOMATION,
        ALARM,
        IDEL,
        MANUAL,
    }
    public enum EN_DASH_UUT_LOCATION
    {
        Unknow,
        LOAD,
        Barcode,
        Vision,
        Altimetry,
        Solder,
        SeparatingModule,
        UNLOAD,
    }
    public enum EN_DASH_EVT_TYPE
    {
        USER,
        ALARM,
        SETALARM,
        CLEARALARM,
        WARNING,
        INFO,
    }
    public enum EN_DASH_UNIT_RESULT
    {
        PASS,
        FAIL,
    }
    #endregion

    #region LaserMeasure
    public enum EN_LASERMEASURE_TYPE
    {
        MeasureCenter = 0,
        MeasureOffset,
        MeasureTwice,
    }
    #endregion

    #region Energy
    public enum En_EnergyMeterType
    {
        Ophir = 0,
        Com,
    }
    public enum En_EnergyCollectType
    {
        Power = 0,
        Energy = 1,
    }
    public enum EN_OPHIR_MEASURE_STATUS
    {
        Ok,
        Overrange,
        Saturated,
        MissingPulse,
        ResetStateInEnergyMeasureMent,
        Waiting,
        Summing,
        TimeOut,
        PeakOver,
        EnergyOver,
    }
    #endregion

    #region PLC
    public enum En_Plc_MasterError
    {
        Ok = 0,
        Warn,
        Error,
    }
    public enum En_BarcodeStatus
    {
        Success = 1,
        Failure,
    }

    public enum EN_Plc_TrigButton
    {
        Start = 1,
        Pause = 2,
        Reset = 3,
    }

    /// <summary>
    /// PC设备状态
    /// </summary>
    public enum PcToPlcMachineStatus
    {
        报警警告清除 = 0,
        运行中 = 1,
        急停中 = 2,
        复位中 = 3,
        暂停中 = 4,
        复位完成 = 5,
        工作完成 = 6,
        警告中 = 7,
        报警中 = 8,
        空闲中 = 9,
    }
    /// <summary>
    /// PLC设备状态
    /// </summary>
    public enum PlcToPcMachineStatus
    {
        设备需复位 = 0,
        运行中 = 1,
        急停中 = 2,
        复位中 = 3,
        暂停中 = 4,
        复位完成 = 5,
        备用 = 6,
        警告中 = 7,
        报警中 = 8,
    }
    #endregion

    //物料左右工位位置
    [Serializable]
    public enum EN_UNIT_LR
    {
        Null,
        Left,
        Right,
    }
    //物料工位位置
    [Serializable]
    public enum EN_UNIT_PLACE
    {
        Null,
        Code,
        Vision,
        LaserSpraySolder,
        Recheck,
        Complete,
    }
}
